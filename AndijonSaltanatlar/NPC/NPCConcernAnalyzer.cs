using System.Collections.Generic;
using AndijonSaltanatlar.AdvancedEconomy;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Population;

namespace AndijonSaltanatlar.NPC
{
    /// <summary>
    /// Fuqaroning muammolarini HAQIQIY o'yin holatidan aniqlaydi. Hech qanday muammo "ssenariy bo'yicha"
    /// berilmaydi: ishsiz deb faqat haqiqatan ishsiz fuqaro, "yo'l yo'q" deb faqat uyi ko'chaga ulanmagan
    /// fuqaro aytadi va h.k. Dialog shartlari, hisobotlar va vazifalar shu tahlilga tayanadi.
    /// </summary>
    public static class NPCConcernAnalyzer
    {
        /// <summary>Fuqaroning faol muammolari, muhimlik bo'yicha saralangan.</summary>
        public static List<string> Analyze(Citizen c, CityLifeManager life, GameStateManager state, BuildingManager buildings)
        {
            var list = new List<string>();
            if (c == null || life == null || state == null) return list;

            Household hh = state.Pop.GetHousehold(c.HouseholdId);
            DistrictStats district = life.Districts.Get(life.Districts.DistrictOfCitizen(c));
            EconomyBalanceConfig cfg = life.Economy.Config;

            // Ishsizlik — shaxsiy holat.
            if (!c.IsEmployed) list.Add(ConcernKeys.Unemployed);

            // Qashshoqlik — oila bugun oziq-ovqatni to'liq sotib ololmadi.
            if (hh != null && (hh.CouldNotAffordFood || hh.FedRatio < 0.99f)) list.Add(ConcernKeys.Poverty);

            // Uy-joy — uyi yo'q yoki uyida sig'imdan ko'p odam yashaydi.
            if (!c.HomeBuildingId.HasValue) list.Add(ConcernKeys.Housing);
            else
            {
                Building home = FindBuilding(buildings, c.HomeBuildingId.Value);
                if (home != null && Residents(state.Pop, home.Id) > home.PopulationCapacity) list.Add(ConcernKeys.Housing);
            }

            // Narx — shahar bozoridagi oziq-ovqat narxi me'yordan ancha yuqori.
            if (life.Economy.Prices.Get(ProductType.Food).PriceRatio >= cfg.HighPriceRatio) list.Add(ConcernKeys.FoodPrice);

            // Soliq.
            if (state.Policy.PopulationTax >= 25) list.Add(ConcernKeys.HighTax);

            // Yo'l — aynan shu fuqaroning uyi ko'chaga ulanmagan.
            if (c.HomeBuildingId.HasValue && !life.Districts.IsHouseConnected(c.HomeBuildingId.Value)) list.Add(ConcernKeys.NoRoad);

            // Mahalla darajasidagi ehtiyojlar.
            if (district != null)
            {
                if (district.Needs.Contains(ConcernKeys.NoMarket)) list.Add(ConcernKeys.NoMarket);
                if (district.Needs.Contains(ConcernKeys.Security)) list.Add(ConcernKeys.Security);
                if (district.Needs.Contains(ConcernKeys.NoSchool)) list.Add(ConcernKeys.NoSchool);
            }

            list.Sort((a, b) => ConcernKeys.Weight(b).CompareTo(ConcernKeys.Weight(a)));
            return list;
        }

        /// <summary>Muammo shu fuqaro uchun hozir ham dolzarbmi.</summary>
        public static bool IsActive(string concern, Citizen c, CityLifeManager life, GameStateManager state, BuildingManager buildings)
            => Analyze(c, life, state, buildings).Contains(concern);

        public static int Residents(PopulationManager pop, int homeBuildingId)
        {
            int n = 0;
            foreach (Citizen x in pop.Citizens) if (x.HomeBuildingId == homeBuildingId) n++;
            return n;
        }

        public static Building FindBuilding(BuildingManager b, int id)
        {
            if (b == null) return null;
            foreach (Building x in b.Buildings) if (x.Id == id) return x;
            return null;
        }
    }
}
