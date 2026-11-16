using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.RoadNetwork
{
    /// <summary>
    /// Yo'l topuvchi: bino/katak → yo'l tuguni → A* → kataklar ro'yxati.
    /// Natija RouteCache'ga yoziladi; graf o'zgarmaguncha qayta hisoblanmaydi.
    /// </summary>
    public static class RouteFinder
    {
        /// <summary>
        /// Manba va manzil o'rtasidagi yo'l marshruti (kataklar). Yo'l yo'q bo'lsa null.
        /// Manba/manzil binosi yo'lga ulanmagan bo'lsa ham, eng yaqin yo'l katagi olinadi.
        /// </summary>
        public static List<Point> FindPath(RoadNetworkManager net, Point from, Point to, int maxApproach = 6)
        {
            if (net == null) return null;

            Point fromTile = ResolveTile(net, from, maxApproach);
            Point toTile = ResolveTile(net, to, maxApproach);
            if (fromTile.X < 0 || toTile.X < 0) return null;

            int version = net.Version;
            if (net.Cache.TryGet(fromTile, toTile, version, out List<Point> cached))
                return cached;

            int fromNode = net.Graph.FindNodeAt(fromTile, WidthOf(net));
            int toNode = net.Graph.FindNodeAt(toTile, WidthOf(net));
            if (fromNode < 0 || toNode < 0) return null;

            List<int> nodes = net.Graph.ShortestPath(fromNode, toNode);
            if (nodes == null || nodes.Count == 0) return null;

            var path = new List<Point>(nodes.Count);
            foreach (int n in nodes)
            {
                Point? p = net.Graph.LocateNode(n);
                if (p.HasValue) path.Add(p.Value);
            }
            if (path.Count == 0) return null;

            net.Cache.Store(fromTile, toTile, version, path);
            return path;
        }

        /// <summary>Ikki nuqta orasida yo'l bormi (tirbandlikni hisobga olmagan holda).</summary>
        public static bool IsReachable(RoadNetworkManager net, Point from, Point to, int maxApproach = 6)
            => FindPath(net, from, to, maxApproach) != null;

        /// <summary>Marshrut uzunligi (kataklarda).</summary>
        public static int PathLength(List<Point> path) => path == null ? 0 : Math.Max(0, path.Count - 1);

        private static Point ResolveTile(RoadNetworkManager net, Point p, int maxApproach)
        {
            RoadSegment s = net.SegmentAt(p.X, p.Y);
            if (s != null && !s.IsBlocked) return s.GridPosition;
            s = net.NearestSegment(p.X, p.Y, maxApproach);
            return s != null ? s.GridPosition : new Point(-1, -1);
        }

        /// <summary>Graf o'lchami uchun xarita eni (RouteCache kaliti bilan bir xil).</summary>
        private static int WidthOf(RoadNetworkManager net)
        {
            // Graph.FindNodeAt katak indeksini biladi; eni RoadNetworkManager'da saqlanadi.
            return net.MapWidth;
        }
    }
}
