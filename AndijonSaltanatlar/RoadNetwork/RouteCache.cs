using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.RoadNetwork
{
    /// <summary>
    /// Marshrut keshi. Kalit: (manba katak, manzil katak). Graf versiyasi o'zgarsa (yo'l qurildi,
    /// buzildi, ko'prik qo'shildi) butun kesh bekor qilinadi — eskirgan marshrut ishlatilmaydi.
    /// </summary>
    public sealed class RouteCache
    {
        private readonly Dictionary<long, Entry> _entries = new Dictionary<long, Entry>();
        public const int MaxEntries = 512;

        private sealed class Entry
        {
            public int Version;
            public List<Point> Path;
        }

        public int Count => _entries.Count;

        private static long Key(Point a, Point b)
            => ((long)a.X * 100003L + a.Y) * 1000003L + (b.X * 1013L + b.Y);

        public bool TryGet(Point from, Point to, int version, out List<Point> path)
        {
            path = null;
            if (!_entries.TryGetValue(Key(from, to), out Entry e)) return false;
            if (e.Version != version) return false;
            path = e.Path;
            return path != null;
        }

        public void Store(Point from, Point to, int version, List<Point> path)
        {
            if (path == null) return;
            if (_entries.Count >= MaxEntries) _entries.Clear();
            _entries[Key(from, to)] = new Entry { Version = version, Path = path };
        }

        /// <summary>Graf o'zgardi — eskirgan yozuvlarni tashlaydi.</summary>
        public void Invalidate(int newVersion)
        {
            if (_entries.Count == 0) return;
            var stale = new List<long>();
            foreach (var kv in _entries)
                if (kv.Value.Version != newVersion) stale.Add(kv.Key);
            foreach (long k in stale) _entries.Remove(k);
        }

        public void Clear() => _entries.Clear();
    }
}
