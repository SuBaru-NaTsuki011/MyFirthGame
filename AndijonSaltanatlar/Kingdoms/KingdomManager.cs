using System.Collections.Generic;
using AndijonSaltanatlar.Regions;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Kingdoms
{
    /// <summary>
    /// Barcha saltanatlarni yaratadi va statistikasini hududlardan yig'adi. Boshlang'ich holat:
    /// Asaka Saltanati (o'yinchi), Andijon Begligi (AI), Shahrixon Hokimligi (AI). Qolgan hududlar neytral.
    /// </summary>
    public sealed class KingdomManager
    {
        public List<Kingdom> Kingdoms { get; } = new List<Kingdom>();
        public int PlayerKingdomId { get; private set; } = 1;

        public Kingdom Player => Get(PlayerKingdomId);

        public void Initialize(RegionManager regions)
        {
            Kingdoms.Clear();

            var asaka = new Kingdom
            {
                Id = 1, Name = "Asaka Saltanati", RulerName = "Hokim Zahiriddin",
                CapitalRegionId = 1, IsAIControlled = false, IsFictional = false,
                GovernmentType = GovernmentType.Saltanat, MapColor = new Color(70, 130, 220),
                Treasury = 500, MilitaryStrength = 55, EconomicStrength = 55, Stability = 65, Reputation = 55
            };
            asaka.OwnedRegionIds.Add(1);

            var andijon = new Kingdom
            {
                Id = 2, Name = "Andijon Begligi", RulerName = "Mirzo Ulug'",
                CapitalRegionId = 2, IsAIControlled = true, MapColor = new Color(210, 170, 70),
                Treasury = 650, MilitaryStrength = 50, EconomicStrength = 65, Stability = 60, Reputation = 55
            };
            andijon.OwnedRegionIds.Add(2);
            andijon.AI.EconomicFocus = 0.8f; andijon.AI.DiplomacyFocus = 0.7f;
            andijon.AI.ExpansionFocus = 0.35f; andijon.AI.StabilityFocus = 0.6f; andijon.AI.RiskTolerance = 0.4f;

            var shahrixon = new Kingdom
            {
                Id = 3, Name = "Shahrixon Hokimligi", RulerName = "Qosimbek",
                CapitalRegionId = 3, IsAIControlled = true, MapColor = new Color(200, 80, 80),
                Treasury = 480, MilitaryStrength = 70, EconomicStrength = 45, Stability = 55, Reputation = 45
            };
            shahrixon.OwnedRegionIds.Add(3);
            shahrixon.AI.ExpansionFocus = 0.85f; shahrixon.AI.RiskTolerance = 0.7f;
            shahrixon.AI.EconomicFocus = 0.45f; shahrixon.AI.DiplomacyFocus = 0.35f; shahrixon.AI.StabilityFocus = 0.5f;

            Kingdoms.Add(asaka);
            Kingdoms.Add(andijon);
            Kingdoms.Add(shahrixon);

            // Hududlarga egalik
            regions.Get(1).OwnerKingdomId = 1; regions.Get(1).IsCapital = true; regions.Get(1).IsActiveCity = true;
            regions.Get(2).OwnerKingdomId = 2; regions.Get(2).IsCapital = true;
            regions.Get(3).OwnerKingdomId = 3; regions.Get(3).IsCapital = true;
            // 4..9 — neytral (OwnerKingdomId = 0)

            RecomputeStats(regions);
        }

        public Kingdom Get(int id)
        {
            foreach (Kingdom k in Kingdoms) if (k.Id == id) return k;
            return null;
        }

        public IEnumerable<Kingdom> AIKingdoms()
        {
            foreach (Kingdom k in Kingdoms) if (k.IsAIControlled) yield return k;
        }

        /// <summary>Har bir saltanat statistikasini o'z hududlaridan qayta hisoblaydi.</summary>
        public void RecomputeStats(RegionManager regions)
        {
            foreach (Kingdom k in Kingdoms)
            {
                int pop = 0, econ = 0;
                k.OwnedRegionIds.Clear();
                foreach (Region r in regions.Regions)
                    if (r.OwnerKingdomId == k.Id)
                    {
                        k.OwnedRegionIds.Add(r.Id);
                        pop += r.Population;
                        econ += r.Development;
                    }
                k.Population = pop;
                k.EconomicStrength = k.OwnedRegionIds.Count > 0 ? econ / k.OwnedRegionIds.Count : econ;
            }
        }
    }
}
