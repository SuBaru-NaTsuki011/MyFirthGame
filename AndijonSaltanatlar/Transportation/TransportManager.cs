using System;
using System.Collections.Generic;
using System.Linq;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.RoadNetwork;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Transportation
{
    /// <summary>
    /// Transportlarni boshqaradi: marshrut beradi (yo'l grafi orqali), harakatlantiradi,
    /// tirbandlikni hisoblaydi va yetib kelganini e'lon qiladi.
    ///
    /// MUHIM: marshrut har kadrda qayta hisoblanmaydi. Marshrut transportga bir marta beriladi;
    /// faqat yo'l yopilganda yoki graf o'zgarganda qayta hisoblanadi.
    /// </summary>
    public sealed class TransportManager
    {
        private readonly Dictionary<int, TransportEntity> _entities = new Dictionary<int, TransportEntity>();
        private readonly Dictionary<int, List<TransportEntity>> _byOwner = new Dictionary<int, List<TransportEntity>>();
        private readonly RoadNetworkManager _network;
        private readonly GameStateManager _state;
        private int _nextId = 1;
        private float _statsTimer;

        public TransportAnimationController Animator { get; } = new TransportAnimationController();
        public TrafficStatistics Traffic { get; } = new TrafficStatistics();

        public event Action<TransportEntity> EntityCreated;
        public event Action<TransportEntity> EntityDestroyed;
        /// <summary>Transport manzilga yetib keldi (logistika shu hodisaga tayanadi).</summary>
        public event Action<TransportEntity> Arrived;
        /// <summary>Marshrutni topib bo'lmadi yoki yo'l yopildi.</summary>
        public event Action<TransportEntity, string> RouteFailed;

        public int EntityCount => _entities.Count;
        public IReadOnlyDictionary<int, TransportEntity> Entities => _entities;
        public RoadNetworkManager Network => _network;

        /// <summary>Ko'rinadigan va faol transportlar soni.</summary>
        public int MovingCount
        {
            get
            {
                int n = 0;
                foreach (TransportEntity t in _entities.Values)
                    if (t.AnimationState == TransportAnimationState.Moving) n++;
                return n;
            }
        }

        public TransportManager(RoadNetworkManager network, GameStateManager state = null)
        {
            _network = network;
            _state = state;
        }

        // ==================================================================
        //  Yaratish / o'chirish
        // ==================================================================

        public int Spawn(TransportType type, Point tile, int ownerId = 0)
        {
            Vector2 pos = TransportRoute.TileCenter(tile);
            int id = CreateEntity(ownerId, type, pos);
            TransportEntity e = GetEntity(id);
            if (e != null)
            {
                e.CurrentTile = tile;
                e.RouteFrom = tile;
                e.RouteTo = tile;
            }
            return id;
        }

        public int CreateEntity(int ownerId, TransportType type, Vector2 position, float maxSpeed = 0f)
        {
            int id = _nextId++;
            var t = new TransportEntity(id, type, position, maxSpeed > 0 ? maxSpeed : DefaultSpeed(type));
            t.OwnerId = ownerId;
            _entities[id] = t;

            if (!_byOwner.TryGetValue(ownerId, out List<TransportEntity> list))
            {
                list = new List<TransportEntity>();
                _byOwner[ownerId] = list;
            }
            list.Add(t);
            EntityCreated?.Invoke(t);
            return id;
        }

        public static float DefaultSpeed(TransportType type)
        {
            switch (type)
            {
                case TransportType.Mule: return 2.2f;
                case TransportType.Wagon: return 1.4f;
                case TransportType.Caravan: return 1.1f;
                case TransportType.CitizenCarrier: return 1.0f;
                default: return 1.2f;
            }
        }

        public TransportEntity GetEntity(int id)
            => _entities.TryGetValue(id, out TransportEntity t) ? t : null;

        public bool RemoveEntity(int id)
        {
            if (!_entities.TryGetValue(id, out TransportEntity t)) return false;
            _entities.Remove(id);
            Animator.Forget(id);
            if (_byOwner.TryGetValue(t.OwnerId, out List<TransportEntity> list)) list.Remove(t);
            EntityDestroyed?.Invoke(t);
            return true;
        }

        public void Clear()
        {
            foreach (int id in _entities.Keys.ToList()) RemoveEntity(id);
            Animator.Reset();
            Traffic.Clear();
            _nextId = 1;
        }

        public IReadOnlyList<TransportEntity> GetByOwner(int ownerId)
            => _byOwner.TryGetValue(ownerId, out List<TransportEntity> list) ? list : (IReadOnlyList<TransportEntity>)Array.Empty<TransportEntity>();

        /// <summary>Bo'sh (marshrutsiz) transportlar — yetkazib berish uchun.</summary>
        public List<TransportEntity> IdleTransports()
        {
            var list = new List<TransportEntity>();
            foreach (TransportEntity t in _entities.Values)
                if (!t.HasRoute && t.WaitTimer <= 0f) list.Add(t);
            return list;
        }

        // ==================================================================
        //  Marshrut
        // ==================================================================

        /// <summary>
        /// Yo'l grafi bo'yicha marshrut beradi. Yo'l yo'q bo'lsa false — transport bekorga
        /// yo'lga chiqmaydi va hech qanday yuk "yo'qolmaydi".
        /// </summary>
        public bool AssignRoute(int transportId, Point from, Point to, out string message)
        {
            message = "";
            TransportEntity e = GetEntity(transportId);
            if (e == null) { message = "Transport topilmadi"; return false; }

            List<Point> path = RouteFinder.FindPath(_network, from, to);
            if (path == null || path.Count < 2)
            {
                message = "Yo'l topilmadi — manzil yo'l tarmog'iga ulanmagan";
                return false;
            }

            e.RouteTiles = path;
            e.RouteIndex = path.Count > 1 ? 1 : 0;
            e.RouteFrom = path[0];
            e.RouteTo = path[path.Count - 1];
            e.CurrentTile = path[0];
            e.Arrived = false;
            e.WaitTimer = 0f;
            e.AnimationState = TransportAnimationState.Moving;
            e.AssignedRouteId = transportId;
            e.Destination = TransportRoute.TileCenter(path[path.Count - 1]);
            message = "Marshrut: " + (path.Count - 1) + " katak";
            return true;
        }

        /// <summary>Joriy katakdan manzilga qayta marshrut (yo'l yopilganda).</summary>
        public bool ReRoute(TransportEntity e)
        {
            if (e == null) return false;
            List<Point> path = RouteFinder.FindPath(_network, e.CurrentTile, e.RouteTo);
            if (path == null || path.Count < 2)
            {
                e.IsBlocked = true;
                e.AnimationState = TransportAnimationState.Idle;
                e.CurrentSpeed = 0f;
                RouteFailed?.Invoke(e, "Yo'l yopilgan — muqobil yo'l yo'q");
                return false;
            }
            e.RouteTiles = path;
            e.RouteIndex = 1;
            e.Arrived = false;
            e.IsBlocked = false;
            e.AnimationState = TransportAnimationState.Moving;
            return true;
        }

        /// <summary>Transport turgan yo'l segmentining tezlik ko'paytiruvchisi.</summary>
        private float SpeedFactorAt(Point tile)
        {
            RoadSegment s = _network?.SegmentAt(tile.X, tile.Y);
            if (s == null) return 0.8f;
            float cong = s.CongestionLevel;
            float trafficSlow = cong <= 1f ? 1f - cong * 0.45f : 0.55f - (cong - 1f) * 0.35f;
            return Math.Max(0.2f, RoadTypeInfo.SpeedFactor(s.Type) * trafficSlow);
        }

        // ==================================================================
        //  HARAKAT
        // ==================================================================

        /// <summary>
        /// Harakatni yangilaydi. dt — o'yin sekundlari (pauza/speed allaqachon qo'llangan).
        /// Har simulyatsiya qadamida yo'l yuklamalari qaytadan yig'iladi.
        /// </summary>
        public void Update(float dt)
        {
            _network?.ClearLoads();
            if (dt <= 0f)
            {
                Traffic.Sample(_network, _entities.Values);
                return;
            }

            foreach (TransportEntity e in _entities.Values)
            {
                if (e.WaitTimer > 0f)
                {
                    e.WaitTimer = Math.Max(0f, e.WaitTimer - dt);
                    e.CurrentSpeed = 0f;
                    e.AnimationState = e.WaitTimer > 0f ? TransportAnimationState.Loading : TransportAnimationState.Idle;
                    Animator.Update(e, dt);
                    continue;
                }

                if (!e.HasRoute || e.Arrived)
                {
                    e.CurrentSpeed = 0f;
                    if (e.AnimationState == TransportAnimationState.Moving)
                        e.AnimationState = TransportAnimationState.Idle;
                    Animator.Update(e, dt);
                    continue;
                }

                Point nextTile = e.RouteTiles[e.RouteIndex];
                RoadSegment seg = _network?.SegmentAt(nextTile.X, nextTile.Y);
                if (seg != null && seg.IsBlocked)
                {
                    if (!ReRoute(e))
                    {
                        Animator.Update(e, dt);
                        continue;
                    }
                    nextTile = e.RouteTiles[e.RouteIndex];
                }

                Vector2 target = TransportRoute.TileCenter(nextTile);
                Vector2 delta = target - e.Position;
                float dist = delta.Length();

                float factor = SpeedFactorAt(e.CurrentTile);
                float speed = Math.Max(0.15f, e.MaxSpeed * factor);
                e.CurrentSpeed = speed;

                // Piksel/sekund: bir katak ~ 45 piksel yo'l.
                float step = speed * 45f * dt;
                if (step >= dist)
                {
                    e.Position = target;
                    e.CurrentTile = nextTile;
                    e.RouteIndex++;
                    if (e.RouteIndex >= e.RouteTiles.Count)
                    {
                        e.Arrived = true;
                        e.CurrentSpeed = 0f;
                        e.AnimationState = TransportAnimationState.Idle;
                        Arrived?.Invoke(e);
                        Animator.Update(e, dt);
                        continue;
                    }
                    e.AnimationState = TransportAnimationState.Turn;
                }
                else
                {
                    Vector2 dir = delta / Math.Max(0.0001f, dist);
                    e.Position += dir * step;
                    e.FacingAngle = MathF.Atan2(dir.Y, dir.X);
                    e.AnimationState = TransportAnimationState.Moving;
                }

                // Yo'l yuklamasi (tirbandlik modeli): transport + piyoda.
                RoadSegment here = _network?.SegmentAt(e.CurrentTile.X, e.CurrentTile.Y);
                if (here != null)
                {
                    here.CurrentLoad += 1f;
                    if (e.Type == TransportType.CitizenCarrier) here.PedestrianLoad += 1f;
                }

                Animator.Update(e, dt);
            }

            _statsTimer += dt;
            if (_statsTimer >= 1f)
            {
                _statsTimer = 0f;
                Traffic.Sample(_network, _entities.Values);
            }
        }

        // ==================================================================
        //  SAVE / LOAD
        // ==================================================================

        public List<TransportEntity> Snapshot() => new List<TransportEntity>(_entities.Values);

        public void Restore(IEnumerable<TransportEntity> saved)
        {
            Clear();
            if (saved == null) return;
            foreach (TransportEntity t in saved)
            {
                if (t == null) continue;
                _entities[t.Id] = t;
                if (!_byOwner.TryGetValue(t.OwnerId, out List<TransportEntity> list))
                {
                    list = new List<TransportEntity>();
                    _byOwner[t.OwnerId] = list;
                }
                list.Add(t);
                if (t.Id >= _nextId) _nextId = t.Id + 1;
            }
        }
    }
}
