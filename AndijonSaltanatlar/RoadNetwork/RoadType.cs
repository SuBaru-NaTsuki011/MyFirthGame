using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.RoadNetwork
{
    /// <summary>
    /// Yo'l turi. Tarixiy-stilizatsiyalangan muhit: asfalt yoki zamonaviy magistral YO'Q.
    /// </summary>
    public enum RoadType
    {
        None = 0,
        /// <summary>Tuproq yo'l — arzon, sekin, kichik sig'im.</summary>
        Dirt = 1,
        /// <summary>Shag'alli yo'l — o'rtacha narx va tezlik.</summary>
        Paved = 2,
        /// <summary>Tosh yo'l (tosh yotqizilgan) — aravalar uchun qulay.</summary>
        Cobblestone = 3,
        /// <summary>Ko'prik ustidagi yo'l (suv ustidan o'tadi).</summary>
        StoneBridge = 4,
        /// <summary>Eski saqlangan nom — katta ko'cha ma'nosida.</summary>
        KattaShaharchasi = 10,
        /// <summary>Katta shahar ko'chasi — asosiy transport yo'li.</summary>
        MainStreet = 11
    }

    /// <summary>Yo'l turlarining iqtisodiy va harakat xususiyatlari — bitta joyda.</summary>
    public static class RoadTypeInfo
    {
        /// <summary>O'yinchi qurishi mumkin bo'lgan quruqlik yo'llari (arzon → qimmat).</summary>
        public static readonly RoadType[] LandTypes =
        {
            RoadType.Dirt, RoadType.Paved, RoadType.Cobblestone, RoadType.MainStreet
        };

        public static string NameUz(RoadType t)
        {
            switch (t)
            {
                case RoadType.Dirt: return "Tuproq yo'l";
                case RoadType.Paved: return "Shag'alli yo'l";
                case RoadType.Cobblestone: return "Tosh yo'l";
                case RoadType.MainStreet: return "Katta ko'cha";
                case RoadType.KattaShaharchasi: return "Katta ko'cha";
                case RoadType.StoneBridge: return "Tosh ko'prik";
                default: return "Yo'l yo'q";
            }
        }

        /// <summary>Bir katak yo'lning qurilish narxi (tanga).</summary>
        public static int BuildCost(RoadType t)
        {
            switch (t)
            {
                case RoadType.Dirt: return 8;
                case RoadType.Paved: return 18;
                case RoadType.Cobblestone: return 32;
                case RoadType.MainStreet: return 55;
                case RoadType.KattaShaharchasi: return 55;
                case RoadType.StoneBridge: return 70;
                default: return 8;
            }
        }

        /// <summary>Yo'lni keyingi darajaga ko'tarish narxi (farq + 4 tanga ish haqi).</summary>
        public static int UpgradeCost(RoadType from, RoadType to)
        {
            int diff = BuildCost(to) - BuildCost(from);
            return diff <= 0 ? 0 : diff + 4;
        }

        public static RoadType Next(RoadType t)
        {
            switch (t)
            {
                case RoadType.Dirt: return RoadType.Paved;
                case RoadType.Paved: return RoadType.Cobblestone;
                case RoadType.Cobblestone: return RoadType.MainStreet;
                case RoadType.KattaShaharchasi: return RoadType.MainStreet;
                default: return RoadType.None;
            }
        }

        /// <summary>Bir vaqtda yo'lda bo'la oladigan transport sig'imi (birlik).</summary>
        public static float Capacity(RoadType t)
        {
            switch (t)
            {
                case RoadType.Dirt: return 4f;
                case RoadType.Paved: return 8f;
                case RoadType.Cobblestone: return 12f;
                case RoadType.MainStreet: return 22f;
                case RoadType.KattaShaharchasi: return 22f;
                case RoadType.StoneBridge: return 14f;
                default: return 4f;
            }
        }

        /// <summary>Yo'l sirit bo'yicha tezlik ko'paytiruvchisi (1.0 = tosh yo'l).</summary>
        public static float SpeedFactor(RoadType t)
        {
            switch (t)
            {
                case RoadType.Dirt: return 0.55f;
                case RoadType.Paved: return 0.80f;
                case RoadType.Cobblestone: return 1.00f;
                case RoadType.MainStreet: return 1.20f;
                case RoadType.KattaShaharchasi: return 1.20f;
                case RoadType.StoneBridge: return 0.95f;
                default: return 0.60f;
            }
        }

        /// <summary>Pathfinding uchun harakat narxi (kichik = tezroq).</summary>
        public static float MovementCost(RoadType t)
        {
            float s = SpeedFactor(t);
            return s <= 0.01f ? 2f : 1f / s;
        }

        public static float Maintenance(RoadType t)
        {
            switch (t)
            {
                case RoadType.Dirt: return 0.5f;
                case RoadType.Paved: return 1.2f;
                case RoadType.Cobblestone: return 1.5f;
                case RoadType.MainStreet: return 2.0f;
                case RoadType.KattaShaharchasi: return 2.0f;
                case RoadType.StoneBridge: return 1.8f;
                default: return 0.5f;
            }
        }

        public static bool IsBridge(RoadType t) => t == RoadType.StoneBridge;

        /// <summary>Xaritadagi yo'l sirtini ajratish uchun rang (infratuzilma xaritasi).</summary>
        public static Color Tint(RoadType t)
        {
            switch (t)
            {
                case RoadType.Dirt: return new Color(150, 122, 88);
                case RoadType.Paved: return new Color(172, 158, 132);
                case RoadType.Cobblestone: return new Color(196, 190, 178);
                case RoadType.MainStreet: return new Color(224, 204, 160);
                case RoadType.StoneBridge: return new Color(188, 158, 120);
                default: return new Color(140, 140, 140);
            }
        }
    }
}
