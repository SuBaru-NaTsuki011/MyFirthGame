using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.World;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.RoadNetwork
{
    public enum RoadBuildStatus
    {
        Ok = 0,
        OutOfBounds = 1,
        AlreadyRoad = 2,
        Occupied = 3,
        LockedLand = 4,
        NeedsBridge = 5,
        BridgeOnLand = 6,
        NotEnoughCoins = 7
    }

    /// <summary>
    /// Shahar yo'l tarmog'i. Yo'llar oddiy dekoratsiya emas:
    ///   - har bir katak RoadSegment (tur, sig'im, narx, holat);
    ///   - graf (RoadGraph) yo'l o'zgarganda qayta quriladi;
    ///   - avtomatik burchak/T/chorraha ulanishlari (ConnectionMask);
    ///   - qulflangan yerga ruxsatsiz yo'l qurilmaydi.
    /// </summary>
    public sealed class RoadNetworkManager
    {
        private readonly MapManager _map;
        private readonly GameStateManager _state;
        private readonly RoadGraphBuilder _builder = new RoadGraphBuilder();
        private readonly Dictionary<long, RoadSegment> _segments = new Dictionary<long, RoadSegment>();
        private readonly Dictionary<int, RoadSegment> _byId = new Dictionary<int, RoadSegment>();
        private int _nextId = 1;

        public RoadGraph Graph { get; } = new RoadGraph();
        public RouteCache Cache { get; } = new RouteCache();
        public int MapWidth => _map.Width;
        public int MapHeight => _map.Height;
        public int Version => Graph.Version;
        public int SegmentCount => _segments.Count;

        /// <summary>Yo'l qurildi (yangi segment).</summary>
        public event Action<RoadSegment> SegmentBuilt;
        public event Action<RoadSegment> SegmentRemoved;
        public event Action RoadNetworkChanged;

        private static readonly Point[] Dirs =
        {
            new Point(0, -1), new Point(1, 0), new Point(0, 1), new Point(-1, 0)
        };

        public RoadNetworkManager(MapManager map, GameStateManager state)
        {
            _map = map;
            _state = state;
            AdoptGeneratedRoads();
            RebuildGraph();
        }

        public IEnumerable<RoadSegment> Segments => _segments.Values;

        private long Key(int x, int y) => (long)y * _map.Width + x;

        public RoadSegment SegmentAt(int x, int y)
            => _segments.TryGetValue(Key(x, y), out RoadSegment s) ? s : null;

        public bool IsRoad(int x, int y) => _segments.ContainsKey(Key(x, y));

        public bool IsBridgeAt(int x, int y)
        {
            RoadSegment s = SegmentAt(x, y);
            return s != null && s.IsBridge;
        }

        /// <summary>Shahar generatori qurgan ko'chalarni tarmoqqa qabul qiladi (bepul, o'yinchiniki emas).</summary>
        private void AdoptGeneratedRoads()
        {
            for (int y = 0; y < _map.Height; y++)
                for (int x = 0; x < _map.Width; x++)
                {
                    if (_map.GetTile(x, y) != TileType.Road) continue;
                    CreateSegment(x, y, RoadType.Cobblestone, builtByPlayer: false, day: 0);
                }
        }

        private RoadSegment CreateSegment(int x, int y, RoadType type, bool builtByPlayer, int day)
        {
            var seg = new RoadSegment(_nextId++, type, new Point(x, y))
            {
                BuiltByPlayer = builtByPlayer,
                BuiltDay = day,
                OwnerCityId = 0
            };
            _segments[Key(x, y)] = seg;
            _byId[seg.Id] = seg;
            return seg;
        }

        // ==================================================================
        //  QURILISH
        // ==================================================================

        public RoadBuildStatus Validate(int x, int y, RoadType type, out string reason)
        {
            reason = "";
            if (!_map.InBounds(x, y)) { reason = "Xarita chetidan tashqarida"; return RoadBuildStatus.OutOfBounds; }
            if (IsRoad(x, y)) { reason = "Bu katakda yo'l bor"; return RoadBuildStatus.AlreadyRoad; }

            object occupant = _map.GetOccupant(x, y);
            if (occupant is Building) { reason = "Bu yerda bino bor"; return RoadBuildStatus.Occupied; }
            if (occupant is Decoration d && !d.Removable) { reason = "Bu yerda muhofaza qilinadigan obyekt bor"; return RoadBuildStatus.Occupied; }

            if (_state != null && !_state.Land.IsBuildable(x, y))
            {
                reason = "Yer qulflangan — avval hududni oching";
                return RoadBuildStatus.LockedLand;
            }

            TileType tile = _map.GetTile(x, y);
            bool wantsBridge = RoadTypeInfo.IsBridge(type);
            if (tile == TileType.Water)
            {
                if (!wantsBridge) { reason = "Suv ustidan faqat ko'prik quriladi"; return RoadBuildStatus.NeedsBridge; }
                return RoadBuildStatus.Ok;
            }
            if (wantsBridge) { reason = "Ko'prik faqat suv ustiga quriladi"; return RoadBuildStatus.BridgeOnLand; }
            if (tile == TileType.Road) { reason = "Bu katak allaqachon ko'cha"; return RoadBuildStatus.AlreadyRoad; }
            return RoadBuildStatus.Ok;
        }

        /// <summary>Yo'l qurish: narx xazinadan BIR marta ayiriladi, graf qayta quriladi.</summary>
        public bool TryBuild(int x, int y, RoadType type, out string message)
        {
            RoadBuildStatus status = Validate(x, y, type, out message);
            if (status != RoadBuildStatus.Ok) return false;

            int cost = RoadTypeInfo.BuildCost(type);
            if (_state != null)
            {
                if (!_state.CanAfford(cost))
                {
                    message = "Tanga yetarli emas (" + cost + ")";
                    return false;
                }
                _state.Coins -= cost;
            }

            // Yo'l ochish uchun to'siq bo'lgan bezak (daraxt) olib tashlanadi.
            object occupant = _map.GetOccupant(x, y);
            if (occupant is Decoration decor && decor.Removable)
                _map.RemoveDecoration(decor);

            RoadSegment seg = CreateSegment(x, y, type, builtByPlayer: true, day: _state?.Day ?? 0);
            if (!seg.IsBridge && _map.GetTile(x, y) != TileType.Road)
                _map.SetTile(x, y, TileType.Road);

            RefreshMaskAround(x, y);
            RebuildGraph();
            SegmentBuilt?.Invoke(seg);
            RoadNetworkChanged?.Invoke();
            message = RoadTypeInfo.NameUz(type) + " qurildi (-" + cost + " tanga)";
            return true;
        }

        /// <summary>Yo'lni keyingi darajaga ko'taradi (tuproq → shag'al → tosh → katta ko'cha).</summary>
        public bool TryUpgrade(int x, int y, out string message, out int cost)
        {
            cost = 0;
            RoadSegment seg = SegmentAt(x, y);
            if (seg == null) { message = "Bu yerda yo'l yo'q"; return false; }
            if (seg.IsBridge) { message = "Ko'prikni yaxshilash alohida qurilishni talab qiladi"; return false; }

            RoadType next = RoadTypeInfo.Next(seg.Type);
            if (next == RoadType.None) { message = "Yo'l allaqachon eng yuqori darajada"; return false; }

            cost = RoadTypeInfo.UpgradeCost(seg.Type, next);
            if (_state != null && !_state.CanAfford(cost)) { message = "Tanga yetarli emas (" + cost + ")"; return false; }
            if (_state != null) _state.Coins -= cost;

            seg.SetType(next);
            RefreshMaskAround(x, y);
            RebuildGraph();
            RoadNetworkChanged?.Invoke();
            message = "Yo'l yangilandi: " + RoadTypeInfo.NameUz(next) + " (-" + cost + " tanga)";
            return true;
        }

        /// <summary>Yo'lni buzadi. Ustidagi transportlar uchun marshrut qayta hisoblanadi.</summary>
        public bool TryRemove(int x, int y, out string message)
        {
            RoadSegment seg = SegmentAt(x, y);
            if (seg == null) { message = "Bu yerda yo'l yo'q"; return false; }
            if (!seg.BuiltByPlayer) { message = "Asosiy shahar ko'chasini buzish mumkin emas"; return false; }

            bool wasBridge = seg.IsBridge;
            _segments.Remove(Key(x, y));
            _byId.Remove(seg.Id);
            if (!wasBridge) _map.SetTile(x, y, TileType.Ground);

            RefreshMaskAround(x, y);
            RebuildGraph();
            SegmentRemoved?.Invoke(seg);
            RoadNetworkChanged?.Invoke();
            message = "Yo'l buzildi";
            return true;
        }

        /// <summary>Yo'lni vaqtincha yopadi (masalan ta'mirlash) — marshrutlar chetlab o'tadi.</summary>
        public void SetBlocked(int x, int y, bool blocked)
        {
            RoadSegment seg = SegmentAt(x, y);
            if (seg == null || seg.IsBlocked == blocked) return;
            seg.IsBlocked = blocked;
            RebuildGraph();
            RoadNetworkChanged?.Invoke();
        }

        // ==================================================================
        //  AUTO-TILING
        // ==================================================================

        /// <summary>Qo'shni kataklar bilan ulanish bitmaskini qayta hisoblaydi (burchak, T, chorraha).</summary>
        public void RefreshMaskAround(int x, int y)
        {
            UpdateMask(x, y);
            foreach (Point d in Dirs) UpdateMask(x + d.X, y + d.Y);
        }

        private void UpdateMask(int x, int y)
        {
            RoadSegment seg = SegmentAt(x, y);
            if (seg == null) return;
            int mask = 0;
            if (IsRoad(x, y - 1)) mask |= 1;
            if (IsRoad(x + 1, y)) mask |= 2;
            if (IsRoad(x, y + 1)) mask |= 4;
            if (IsRoad(x - 1, y)) mask |= 8;
            seg.ConnectionMask = mask;
        }

        /// <summary>Ulanish turi nomi (UI va debug uchun).</summary>
        public static string MaskName(int mask)
        {
            int n = 0;
            for (int i = 0; i < 4; i++) if ((mask & (1 << i)) != 0) n++;
            if (n == 0) return "Tugash";
            if (n == 1) return "Tugash";
            if (n == 2) return (mask == 5 || mask == 10) ? "To'g'ri" : "Burchak";
            if (n == 3) return "T-kechish";
            return "Chorraha";
        }

        // ==================================================================
        //  GRAF
        // ==================================================================

        public void RebuildGraph()
        {
            _builder.Rebuild(Graph, _segments.Values, _map);
            Cache.Invalidate(Graph.Version);
        }

        /// <summary>
        /// Qirra narxlarini joriy tirbandlik va yo'l holatiga moslaydi. Har soat/kun bir marta
        /// chaqiriladi (har kadrda emas) — marshrutlar tirband yo'lni chetlab o'tadi.
        /// </summary>
        public void RefreshEdgeCosts()
        {
            foreach (RoadEdge edge in Graph.Edges.Values)
            {
                Point? a = Graph.LocateNode(edge.Source);
                Point? b = Graph.LocateNode(edge.Target);
                float ca = CostAt(a), cb = CostAt(b);
                edge.MovementCost = Math.Max(0.05f, (ca + cb) * 0.5f);
            }
            Cache.Invalidate(Graph.Version);
        }

        private float CostAt(Point? p)
        {
            if (!p.HasValue) return 1f;
            RoadSegment s = SegmentAt(p.Value.X, p.Value.Y);
            return s != null ? s.EffectiveMovementCost : RoadGraphBuilder.PlazaCost;
        }

        /// <summary>Tarqatilgan yuklamani tozalaydi (har simulyatsiya qadamida qayta yig'iladi).</summary>
        public void ClearLoads()
        {
            foreach (RoadSegment s in _segments.Values)
            {
                s.CurrentLoad = 0f;
                s.PedestrianLoad = 0f;
            }
        }

        public RoadSegment NearestSegment(int x, int y, int maxRadius)
        {
            RoadSegment best = null;
            int bestDist = int.MaxValue;
            for (int r = 0; r <= maxRadius; r++)
            {
                for (int dy = -r; dy <= r; dy++)
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Math.Abs(dx) != r && Math.Abs(dy) != r) continue;
                        RoadSegment s = SegmentAt(x + dx, y + dy);
                        if (s == null || s.IsBlocked) continue;
                        int d = Math.Abs(dx) + Math.Abs(dy);
                        if (d < bestDist) { bestDist = d; best = s; }
                    }
                if (best != null) return best;
            }
            return best;
        }

        /// <summary>Bino atrofida (radius ichida) yo'l bormi — xizmat va suv hisobi uchun.</summary>
        public bool HasRoadAccess(Building b, int radius = 3)
        {
            if (b == null) return false;
            int size = b.Size.X;
            int cx = b.Position.X + size / 2;
            int cy = b.Position.Y + size / 2;
            return NearestSegment(cx, cy, radius + size / 2) != null;
        }

        /// <summary>Katakka eng yaqin yo'l tuguni Id'si (-1 = yo'l yo'q).</summary>
        public int NearestNodeId(int x, int y, int maxRadius, out Point roadTile)
        {
            RoadSegment s = NearestSegment(x, y, maxRadius);
            roadTile = s != null ? s.GridPosition : new Point(-1, -1);
            return s == null ? -1 : Graph.FindNodeAt(s.GridPosition, _map.Width);
        }

        public int TotalLoad
        {
            get
            {
                float sum = 0f;
                foreach (RoadSegment s in _segments.Values) sum += s.CurrentLoad;
                return (int)sum;
            }
        }

        public int CongestedCount
        {
            get
            {
                int n = 0;
                foreach (RoadSegment s in _segments.Values)
                    if (s.Band == CongestionBand.High || s.Band == CongestionBand.Congested) n++;
                return n;
            }
        }
    }
}
