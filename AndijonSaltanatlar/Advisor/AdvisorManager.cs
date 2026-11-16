using System.Collections.Generic;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Environment;
using AndijonSaltanatlar.LandExpansion;

namespace AndijonSaltanatlar.Advisor
{
    /// <summary>
    /// DONISHMAND VAZIR — shahar holatini tahlil qilib (CityAnalyzer vazifasi), muhimlik bo'yicha
    /// saralangan tavsiyalar beradi. Faqat haqiqiy GameState ma'lumotlaridan foydalanadi; mavjud
    /// bo'lmagan bino yoki yetmaydigan resursni tavsiya qilmaydi (narxni aytadi). Qaror o'yinchiniki.
    /// </summary>
    public sealed class AdvisorManager
    {
        public List<AdvisorMessage> Messages { get; } = new List<AdvisorMessage>();
        public AdvisorMessage Current => Messages.Count > 0 ? Messages[0] : null;
        public bool HasUrgent => Current != null && Current.Priority >= 90;

        public void Analyze(GameStateManager s)
        {
            Messages.Clear();
            var pop = s.Pop;

            if (pop.HousingUsed > pop.HousingCapacity)
                Add(AdvisorCategory.Muhim, "Uy-joy tanqisligi",
                    "Hazratim, " + (pop.HousingUsed - pop.HousingCapacity) + " nafar fuqaroga uy yetishmayapti. Yangi uy qurishni tavsiya qilaman.",
                    "Uy qurish", 1, 100);

            if (s.Food < s.Population)
                Add(AdvisorCategory.Muhim, "Oziq-ovqat kamaymoqda",
                    "Hazratim, oziq-ovqat zaxiramiz ehtiyojdan kam. Bog' qurib hosilni oshiraylik.",
                    "Bog' qurish", 2, 95);

            if (pop.Count > 0 && pop.Unemployed > pop.Count * 0.35f)
                Add(AdvisorCategory.Tavsiya, "Ishsizlik ortmoqda",
                    "Ish izlayotgan fuqarolar ko'paymoqda. Bozor qurish yangi ish o'rinlari yaratadi.",
                    "Bozor qurish", 3, 80);

            if (pop.AvgHappiness < 45)
                Add(AdvisorCategory.Ogohlantirish, "Mamnunlik pasaymoqda",
                    "Hazratim, aholining roziligi pasaymoqda. Soliqlarni kamaytirishni tavsiya qilaman.",
                    "Soliqni kamaytirish", 5, 70);

            foreach (LandParcel p in s.Land.Parcels)
                if (p.State == LandParcelState.Unlockable && s.Coins >= p.UnlockCost)
                {
                    Add(AdvisorCategory.Tavsiya, "Yangi hudud imkoniyati",
                        "Hazratim, " + p.NameUz + " hududini ochish mumkin (" + p.UnlockCost + " tanga). Shaharni kengaytiraylik.",
                        "Yopiq yerni tanlang", 4, 60);
                    break;
                }

            if (s.Weather.Current == Weather.Snowy)
                Add(AdvisorCategory.Ogohlantirish, "Qishki ob-havo",
                    "Hazratim, qor yog'moqda — ishlab chiqarish sekinlashdi. Oziq-ovqat zaxirasini nazorat qiling.", "", 0, 50);

            if (s.Season == Season.Kuz)
                Add(AdvisorCategory.Tavsiya, "Hosil mavsumi",
                    "Hazratim, kuz keldi — hosil davri. Bog'lar ko'proq oziq-ovqat beradi.", "Bog' qurish", 2, 45);

            if (Messages.Count == 0)
                Add(AdvisorCategory.Tavsiya, "Shahar barqaror",
                    "Hazratim, ishlar joyida ketmoqda. Rivojlanishda davom etamiz.", "", 0, 10);

            Messages.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        private void Add(AdvisorCategory c, string title, string body, string action, int actionId, int priority)
            => Messages.Add(new AdvisorMessage { Category = c, Title = title, Body = body, ActionLabel = action, ActionId = actionId, Priority = priority });
    }
}
