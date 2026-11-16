using System.Collections.Generic;

namespace AndijonSaltanatlar.Economy
{
    /// <summary>
    /// Barcha resurslarni bitta joyda saqlaydi va ularni sarflash/qo'shish qoidalarini beradi.
    /// Resurslar hech qachon manfiy bo'lmaydi.
    /// </summary>
    public sealed class ResourceManager
    {
        private readonly Dictionary<ResourceType, int> _values = new Dictionary<ResourceType, int>();

        public ResourceManager(int coins, int food)
        {
            _values[ResourceType.Coin] = coins;
            _values[ResourceType.Food] = food;
            _values[ResourceType.Population] = 0;
        }

        public int Get(ResourceType type) => _values.TryGetValue(type, out int v) ? v : 0;

        public void Set(ResourceType type, int value) => _values[type] = value < 0 ? 0 : value;

        public void Add(ResourceType type, int delta) => Set(type, Get(type) + delta);

        public bool CanAfford(ResourceType type, int cost) => Get(type) >= cost;

        /// <summary>Yetarli bo'lsa sarflaydi va true qaytaradi; aks holda hech narsani o'zgartirmaydi.</summary>
        public bool Spend(ResourceType type, int cost)
        {
            if (!CanAfford(type, cost)) return false;
            Set(type, Get(type) - cost);
            return true;
        }
    }
}
