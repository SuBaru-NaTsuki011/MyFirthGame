using System.Collections.Generic;

namespace AndijonSaltanatlar.Battles
{
    /// <summary>Janglar tarixini saqlaydi va oxirgi jang natijasini ko'rsatish uchun ushlab turadi.</summary>
    public sealed class BattleManager
    {
        public List<BattleResult> History { get; } = new List<BattleResult>();
        public BattleResult Last { get; private set; }

        public void Record(BattleResult r)
        {
            Last = r;
            History.Add(r);
            if (History.Count > 30) History.RemoveAt(0);
        }

        public void ClearLast() => Last = null;
    }
}
