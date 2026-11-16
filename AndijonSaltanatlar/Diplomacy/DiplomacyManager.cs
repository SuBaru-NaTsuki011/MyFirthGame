using System;
using System.Collections.Generic;

namespace AndijonSaltanatlar.Diplomacy
{
    /// <summary>
    /// Saltanatlararo diplomatik munosabatlar (-100..+100) va shartnomalar (ittifoq, savdo bitimi).
    /// Munosabat juftlik bo'yicha saqlanadi (tartibsiz), TreatyManager vazifasini ham bajaradi.
    /// </summary>
    public sealed class DiplomacyManager
    {
        private readonly Dictionary<int, int> _relations = new Dictionary<int, int>();
        private int _nextTreatyId = 1;

        public List<Treaty> Treaties { get; } = new List<Treaty>();

        private static int Key(int a, int b) => a < b ? a * 1000 + b : b * 1000 + a;

        public int Get(int a, int b) => _relations.TryGetValue(Key(a, b), out int v) ? v : 0;

        public void Set(int a, int b, int value) =>
            _relations[Key(a, b)] = Math.Max(-100, Math.Min(100, value));

        public void Add(int a, int b, int delta) => Set(a, b, Get(a, b) + delta);

        public RelationBand Band(int a, int b) => RelationBands.Of(Get(a, b));

        public bool AreAllied(int a, int b)
        {
            foreach (Treaty t in Treaties)
                if (t.Active && t.Type == TreatyType.Alliance && Pair(t, a, b)) return true;
            return false;
        }

        public bool HasTradePact(int a, int b)
        {
            foreach (Treaty t in Treaties)
                if (t.Active && t.Type == TreatyType.TradePact && Pair(t, a, b)) return true;
            return false;
        }

        private static bool Pair(Treaty t, int a, int b) =>
            (t.KingdomA == a && t.KingdomB == b) || (t.KingdomA == b && t.KingdomB == a);

        public Treaty AddTreaty(TreatyType type, int a, int b, int day)
        {
            if (type == TreatyType.Alliance && AreAllied(a, b)) return null;
            if (type == TreatyType.TradePact && HasTradePact(a, b)) return null;
            var t = new Treaty { Id = _nextTreatyId++, Type = type, KingdomA = a, KingdomB = b, SinceDay = day };
            Treaties.Add(t);
            return t;
        }
    }
}
