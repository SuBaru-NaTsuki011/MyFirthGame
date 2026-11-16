using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.RoadNetwork
{
    /// <summary>Yo'l tarmog'i bog'langanlik hisoboti.</summary>
    public sealed class ConnectivityReport
    {
        public int ComponentCount { get; set; }
        public int LargestComponent { get; set; }
        public int NodeCount { get; set; }
        public List<int> IsolatedNodes { get; set; } = new List<int>();

        /// <summary>Tarmoqning katta qismiga ulanmagan tugunlar ulushi.</summary>
        public float IsolatedShare => NodeCount <= 0 ? 0f : IsolatedNodes.Count / (float)NodeCount;
        public bool IsSingleNetwork => ComponentCount <= 1;
    }

    /// <summary>
    /// Yo'l tarmog'i bog'langanligini tekshiradi: ajralib qolgan yo'l bo'laklari (masalan ko'priksiz
    /// kesilgan ariq ortidagi ko'cha) aniqlanadi. Donishmand Vazir shu hisobotga tayanadi.
    /// </summary>
    public static class RoadConnectivityValidator
    {
        public static ConnectivityReport Analyze(RoadGraph graph)
        {
            var report = new ConnectivityReport { NodeCount = graph.NodeCount };
            if (graph.NodeCount == 0) return report;

            var visited = new HashSet<int>();
            var components = new List<List<int>>();

            foreach (int id in graph.Nodes.Keys)
            {
                if (visited.Contains(id)) continue;
                var comp = new List<int>();
                FloodFill(graph, id, visited, comp);
                components.Add(comp);
            }
            report.ComponentCount = components.Count;
            foreach (List<int> comp in components)
                if (comp.Count > report.LargestComponent) report.LargestComponent = comp.Count;

            // Katta tarmoqqa ulanmagan kichik bo'laklarning chekka tugunlari — ajralgan yo'llar.
            if (components.Count > 1)
                foreach (List<int> comp in components)
                {
                    if (comp.Count >= report.LargestComponent) continue;
                    foreach (int id in comp)
                        if (graph.TryGetNode(id, out RoadNode n) && n.Degree <= 1)
                            report.IsolatedNodes.Add(id);
                }
            report.IsolatedNodes.Sort();
            return report;
        }

        private static void FloodFill(RoadGraph graph, int start, HashSet<int> visited, List<int> comp)
        {
            if (!visited.Add(start)) return;
            var stack = new Stack<int>();
            stack.Push(start);
            while (stack.Count > 0)
            {
                int cur = stack.Pop();
                comp.Add(cur);
                foreach (int n in graph.GetNeighbors(cur))
                    if (visited.Add(n)) stack.Push(n);
            }
        }

        /// <summary>Bino yo'l tarmog'iga ulanganmi (radius ichida yo'l bor).</summary>
        public static bool IsBuildingConnected(RoadNetworkManager net, Building b, int radius = 3)
            => net != null && net.HasRoadAccess(b, radius);

        /// <summary>Ikki bino o'rtasida yo'l marshruti bormi.</summary>
        public static bool AreConnected(RoadNetworkManager net, Building a, Building b, int maxApproach = 6)
        {
            if (net == null || a == null || b == null) return false;
            return RouteFinder.FindPath(net, Center(a), Center(b), maxApproach) != null;
        }

        public static Point Center(Building b)
            => b == null ? Point.Zero : new Point(b.Position.X + b.Size.X / 2, b.Position.Y + b.Size.Y / 2);
    }
}
