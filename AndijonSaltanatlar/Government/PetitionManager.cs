using System.Collections.Generic;
using AndijonSaltanatlar.Economy;
using AndijonSaltanatlar.Population;

namespace AndijonSaltanatlar.Government
{
    /// <summary>
    /// Fuqarolar murojaatlarini boshqaradi. Har o'yin kunida shahar holatini tekshiradi va
    /// haqiqiy muammo bo'lsa (oziq-ovqat, uy-joy, ishsizlik, soliq, sog'liq, xavfsizlik) murojaat
    /// yaratadi. Har turda cooldown bor — bir xil murojaat takror-takror chiqmaydi. Bir vaqtda
    /// bitta faol murojaat. Hukmdor qaroriga qarab oqibat qo'llanadi.
    /// </summary>
    public sealed class PetitionManager
    {
        private readonly Dictionary<PetitionType, int> _cooldownUntil = new Dictionary<PetitionType, int>();
        private int _lastCheckedDay = -1;

        public Petition Active { get; private set; }
        public bool HasActive => Active != null;
        public string LastOutcome { get; private set; } = "";
        public int ResolvedCount { get; private set; }

        public void Update(int day, PopulationManager pop, ResourceManager res, GovernmentPolicy policy)
        {
            if (Active != null || day == _lastCheckedDay) return;
            _lastCheckedDay = day;

            PetitionType? pick = null;
            if (res.Get(ResourceType.Food) < pop.Count) pick = PetitionType.FoodShortage;
            else if (pop.Count > 0 && pop.HousingUsed >= pop.HousingCapacity) pick = PetitionType.HousingShortage;
            else if (pop.Count > 0 && pop.Unemployed > pop.Count * 0.35f) pick = PetitionType.Unemployment;
            else if (policy.PopulationTax >= 25 && pop.AvgHappiness < 50) pick = PetitionType.HighTaxes;
            else if (pop.Count > 0 && pop.AvgHealth < 55) pick = PetitionType.HealthProblem;
            else if (pop.Count > 0 && pop.AvgSecurity < 20) pick = PetitionType.SecurityProblem;

            if (pick == null) return;
            PetitionType type = pick.Value;
            if (_cooldownUntil.TryGetValue(type, out int until) && day < until) return;

            _cooldownUntil[type] = day + 20;
            Active = Build(type);
        }

        public void Resolve(int choice, PopulationManager pop, ResourceManager res, GovernmentPolicy policy)
        {
            if (Active == null) return;
            switch (Active.Type)
            {
                case PetitionType.FoodShortage:
                    if (choice == 0) { res.Add(ResourceType.Food, pop.Count * 3); pop.AdjustHappiness(+6); LastOutcome = "Ombor ochildi, oziq-ovqat tarqatildi."; }
                    else if (choice == 1) { res.Add(ResourceType.Coin, -200); res.Add(ResourceType.Food, 150); pop.AdjustHappiness(+4); LastOutcome = "Tashqaridan oziq-ovqat sotib olindi."; }
                    else { pop.AdjustHappiness(-10); LastOutcome = "Rad etildi. Aholi norozi."; }
                    break;
                case PetitionType.HousingShortage:
                    if (choice == 0) { pop.AdjustHappiness(+5); LastOutcome = "Mahalla qurishga ruxsat berildi — yangi uy quring."; }
                    else if (choice == 1) { res.Add(ResourceType.Coin, -300); pop.AdjustHappiness(+8); LastOutcome = "Qurilishga mablag' ajratildi."; }
                    else { pop.AdjustHappiness(-8); LastOutcome = "Rad etildi."; }
                    break;
                case PetitionType.Unemployment:
                    if (choice == 0) { res.Add(ResourceType.Coin, -250); pop.AdjustHappiness(+6); LastOutcome = "Ish o'rinlari uchun mablag' ajratildi — bozor/ustaxona quring."; }
                    else if (choice == 1) { res.Add(ResourceType.Coin, -120); pop.AdjustHappiness(+3); LastOutcome = "Jamoat ishlariga odam jalb qilindi."; }
                    else { pop.AdjustHappiness(-6); LastOutcome = "Rad etildi."; }
                    break;
                case PetitionType.HighTaxes:
                    if (choice == 0) { policy.AdjustPopulation(-10); pop.AdjustHappiness(+8); LastOutcome = "Aholi solig'i kamaytirildi."; }
                    else { pop.AdjustHappiness(-6); LastOutcome = "Soliq o'z holicha qoldirildi."; }
                    break;
                case PetitionType.HealthProblem:
                    if (choice == 0) { res.Add(ResourceType.Coin, -200); pop.AdjustHappiness(+5); LastOutcome = "Tabiblar jalb qilindi."; }
                    else { pop.AdjustHappiness(-5); LastOutcome = "Rad etildi."; }
                    break;
                default: // SecurityProblem
                    if (choice == 0) { pop.AdjustHappiness(+5); LastOutcome = "Soqchilar safi kuchaytirildi — qarorgoh/minora quring."; }
                    else { pop.AdjustHappiness(-5); LastOutcome = "Rad etildi."; }
                    break;
            }
            ResolvedCount++;
            Active = null;
        }

        private static Petition Build(PetitionType t)
        {
            switch (t)
            {
                case PetitionType.FoodShortage:
                    return new Petition(t, "Oziq-ovqat tanqisligi",
                        "Hazratim, omborlarimiz bo'shab bormoqda, oilalar och qolmoqda. Madad so'raymiz.",
                        new[] { "Omborni ochish", "Tashqaridan sotib olish (-200)", "Rad etish" });
                case PetitionType.HousingShortage:
                    return new Petition(t, "Uy-joy tanqisligi",
                        "Hazratim, oilalar ko'paydi, ammo uylar yetishmaydi. Yangi mahalla qurishga ruxsat bering.",
                        new[] { "Ruxsat berish", "Mablag' ajratish (-300)", "Rad etish" });
                case PetitionType.Unemployment:
                    return new Petition(t, "Ishsizlik",
                        "Hazratim, ko'p yoshlar ishsiz yuribdi. Yangi ish o'rinlari zarur.",
                        new[] { "Mablag' ajratish (-250)", "Jamoat ishlari (-120)", "Rad etish" });
                case PetitionType.HighTaxes:
                    return new Petition(t, "Og'ir soliqlar",
                        "Hazratim, soliqlar og'irlik qilmoqda, savdo so'nmoqda. Yengillik so'raymiz.",
                        new[] { "Soliqni kamaytirish", "O'z holicha qoldirish" });
                case PetitionType.HealthProblem:
                    return new Petition(t, "Sog'liq muammosi",
                        "Hazratim, kasallik tarqalmoqda, tabiblar yetishmaydi.",
                        new[] { "Tabiblarni jalb qilish (-200)", "Rad etish" });
                default:
                    return new Petition(t, "Xavfsizlik muammosi",
                        "Hazratim, ko'chalar notinch, soqchilar yetishmaydi.",
                        new[] { "Soqchilarni ko'paytirish", "Rad etish" });
            }
        }
    }
}
