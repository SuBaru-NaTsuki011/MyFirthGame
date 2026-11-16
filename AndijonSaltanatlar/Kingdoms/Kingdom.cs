using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Kingdoms
{
    public enum GovernmentType { Saltanat, Beglik, Hokimlik }

    /// <summary>AI hukmdorning strategik moyilliklari (0..1). Qarorlar shu vaznlarga bog'liq.</summary>
    public sealed class AIProfile
    {
        public float EconomicFocus { get; set; } = 0.5f;
        public float ExpansionFocus { get; set; } = 0.5f;
        public float DiplomacyFocus { get; set; } = 0.5f;
        public float TechnologyFocus { get; set; } = 0.5f;
        public float StabilityFocus { get; set; } = 0.5f;
        public float RiskTolerance { get; set; } = 0.5f;
    }

    /// <summary>
    /// Saltanat — mustaqil davlat. O'ziga tegishli hududlarni boshqaradi; statistikasi shu
    /// hududlardan yig'iladi. O'yinchi davlati IsAIControlled = false.
    /// </summary>
    public sealed class Kingdom
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string RulerName { get; set; } = "";
        public int CapitalRegionId { get; set; }
        public List<int> OwnedRegionIds { get; } = new List<int>();

        public int Treasury { get; set; }
        public int Population { get; set; }
        public int MilitaryStrength { get; set; } = 50;
        public int EconomicStrength { get; set; } = 50;
        public int Stability { get; set; } = 60;
        public int Reputation { get; set; } = 50;
        public int TechnologyLevel { get; set; } = 1;
        public GovernmentType GovernmentType { get; set; } = GovernmentType.Saltanat;
        public bool IsAIControlled { get; set; } = true;

        /// <summary>Tarixiy dalilsiz — muqobil tarix davlati ekanligini bildiradi.</summary>
        public bool IsFictional { get; set; } = true;

        public AIProfile AI { get; } = new AIProfile();
        public Color MapColor { get; set; } = Color.Gray;

        public int RegionCount => OwnedRegionIds.Count;
    }
}
