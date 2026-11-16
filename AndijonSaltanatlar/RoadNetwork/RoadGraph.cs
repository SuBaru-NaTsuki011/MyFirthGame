using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.RoadNetwork
{
    /// <summary>
    /// Yo'l grafi: tugunlar (yo'l kataklari) va qirralar (qo'shni kataklar orasidagi harakat).
    /// Graf faqat yo'l tarmog'i o'zgarganda qayta quriladi (RoadNetworkManager.RebuildGraph).
    /// </summary>
    public sealed class RoadGraph
    {
        private readonly Dictionary<int, RoadNode> _nodes = new Dictionary<int, RoadNode>();
        private readonly Dictionary<int, RoadEdge> _edges = new Dictionary<int, RoadEdge>();
        /// <summary>Katak kaliti (y*width+x) → tugun Id. Qidiruv O(1).</summary>
        private readonly Dictionary<long, int> _byTile = new Dictionary<long, int>();
        private int _nextEdgeId = 1;

        public int Version { get; private set; }
        public event Action OnGraphChanged;

        public IReadOnlyDictionary<int, RoadNode> Nodes => _nodes;
        public IReadOnlyDictionary<int, RoadEdge> Edges => _edges;
        public int NodeCount => _nodes.Count;
        public int EdgeCount => _edges.Count;

        public long TileKey(int x, int y, int width) => (long)y * width + x;

        public void Clear()
        {
            _nodes.Clear();
            _edges.Clear();
            _byTile.Clear();
            _nextEdgeId = 1;
            Bump();
        }

        /// <summary>Graf versiyasini oshiradi — marshrut keshi shu raqamga qarab bekor qilinadi.</summary>
        public void Bump()
        {
            Version++;
            OnGraphChanged?.Invoke();
        }

        public bool TryAddNode(int nodeId, RoadType type, Point grid, float capacity = 5f, int width = 0)
        {
            if (nodeId <= 0 || _nodes.ContainsKey(nodeId)) return false;
            _nodes[nodeId] = new RoadNode(nodeId, type, grid, capacity);
            if (width > 0) _byTile[TileKey(grid.X, grid.Y, width)] = nodeId;
            return true;
        }

        public bool TryAddEdge(int source, int target, float movementCost, out int edgeId)
        {
            edgeId = 0;
            if (source == target) return false;
            if (!_nodes.ContainsKey(source) || !_nodes.ContainsKey(target)) return false;
            // Ikki tugun orasida bitta qirra yetarli.
            foreach (var kv in _nodes[source].Outgoing)
                if (kv.Key == target) { edgeId = kv.Value; return false; }

            edgeId = _nextEdgeId++;
            _edges[edgeId] = new RoadEdge(edgeId, source, target, movementCost);
            _nodes[source].AddOutgoing(target, edgeId);
            _nodes[target].AddOutgoing(source, edgeId);
            return true;
        }

        public bool TryGetNode(int nodeId, out RoadNode node) => _nodes.TryGetValue(nodeId, out node);
        public bool TryGetEdge(int edgeId, out RoadEdge edge) => _edges.TryGetValue(edgeId, out edge);
        public bool HasNode(int nodeId) => _nodes.ContainsKey(nodeId);

        public bool TryFindNodeAt(Point grid, int width, out int nodeId)
            => _byTile.TryGetValue(TileKey(grid.X, grid.Y, width), out nodeId);

        /// <summary>Grafdagi tugun koordinatasi (O(1) indeks orqali).</summary>
        public int FindNodeAt(Point grid, int width)
            => _byTile.TryGetValue(TileKey(grid.X, grid.Y, width), out int id) ? id : -1;

        public Point? LocateNode(int nodeId)
            => _nodes.TryGetValue(nodeId, out RoadNode n) ? n.GridPosition : (Point?)null;

        public IReadOnlyList<int> GetNeighbors(int nodeId)
            => _nodes.TryGetValue(nodeId, out RoadNode n) ? new List<int>(n.Outgoing.Keys) : (IReadOnlyList<int>)Array.Empty<int>();

        public float EdgeCost(int edgeId)
            => _edges.TryGetValue(edgeId, out RoadEdge e) ? e.MovementCost : 1f;

        /// <summary>
        /// A* eng qisqa yo'l. Topilmasa null. Qirralar harakat narxi (tuproq yo'l qimmatroq) va
        /// tirbandlikka ko'ra hisoblanadi — RoadNetworkManager qirra narxini yangilab turadi.
        /// </summary>
        public List<int> ShortestPath(int start, int goal)
        {
            if (!_nodes.ContainsKey(start) || !_nodes.ContainsKey(goal)) return null;
            if (start == goal) return new List<int> { start };

            var gScore = new Dictionary<int, float>(Math.Min(512, _nodes.Count));
            var cameFrom = new Dictionary<int, int>();
            var closed = new HashSet<int>();
            var open = new PriorityQueue<int, float>();

            gScore[start] = 0f;
            open.Enqueue(start, Heuristic(start, goal));

            while (open.TryDequeue(out int current, out float priority))
            {
                if (closed.Contains(current)) continue;
                if (current == goal) return Reconstruct(cameFrom, start, goal);

                float known = gScore.TryGetValue(current, out float g) ? g : float.MaxValue;
                // Eskirgan navbat yozuvi (yaxshiroq yo'l allaqachon topilgan).
                if (priority > known + Heuristic(current, goal) + 0.001f) continue;
                closed.Add(current);

                if (!_nodes.TryGetValue(current, out RoadNode node)) continue;
                foreach (var kv in node.Outgoing)
                {
                    int next = kv.Key;
                    if (closed.Contains(next)) continue;
                    if (!_edges.TryGetValue(kv.Value, out RoadEdge edge) || edge.Blocked) continue;

                    float tentative = known + Math.Max(0.02f, edge.MovementCost);
                    if (gScore.TryGetValue(next, out float existing) && tentative >= existing) continue;
                    gScore[next] = tentative;
                    cameFrom[next] = current;
                    open.Enqueue(next, tentative + Heuristic(next, goal));
                }
            }
            return null;
        }

        private static List<int> Reconstruct(Dictionary<int, int> cameFrom, int start, int goal)
        {
            var path = new List<int> { goal };
            int cur = goal;
            int guard = 0;
            while (cur != start && cameFrom.TryGetValue(cur, out int prev) && guard++ < 100000)
            {
                path.Add(prev);
                cur = prev;
            }
            path.Reverse();
            return path.Count > 0 && path[0] == start ? path : null;
        }

        /// <summary>Manhattan masofasi (izometrik kataklarda) — A* evristikasi.</summary>
        private float Heuristic(int a, int b)
        {
            if (!_nodes.TryGetValue(a, out RoadNode na) || !_nodes.TryGetValue(b, out RoadNode nb)) return 0f;
            return Math.Abs(na.GridPosition.X - nb.GridPosition.X) + Math.Abs(na.GridPosition.Y - nb.GridPosition.Y);
        }
    }
}
