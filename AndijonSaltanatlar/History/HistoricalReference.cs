using System.Collections.Generic;

namespace AndijonSaltanatlar.History
{
    /// <summary>
    /// Tarixiy manba yozuvi — PROMPT 13, 10 & 17-bo'lim. Har bir tarixiy da'vo manbaga bog'lanadi;
    /// manba tekshirilmagan bo'lsa kontent NeedsHistoricalReview holatida saqlanadi.
    /// </summary>
    public class HistoricalReference
    {
        public string Id { get; set; } = "";
        public string TitleUz { get; set; } = "";
        /// <summary>Manba turi: ilmiy nashr, muzey materiali, tadqiqot...</summary>
        public string SourceType { get; set; } = "";
        public string Citation { get; set; } = "";
        public Campaigns.VerificationStatus Status { get; set; } = Campaigns.VerificationStatus.NeedsHistoricalReview;
    }

    /// <summary>
    /// Tarixiy kontentni tekshiruvchi — PROMPT 13, 10-bo'lim. Manbasiz da'volarni jonli kontent sifatida
    /// chiqarishga yo'l qo'ymaydi (metadata validatsiyasi).
    /// </summary>
    public static class HistoricalContentValidator
    {
        /// <summary>Yozuv jonli (o'yinchiga tasdiqlangan fakt sifatida) ko'rsatilishi mumkinmi?</summary>
        public static bool CanPresentAsFact(HistoricalReference reference)
        {
            if (reference == null) return false;
            return reference.Status == Campaigns.VerificationStatus.Verified
                   && !string.IsNullOrWhiteSpace(reference.Citation);
        }

        /// <summary>Manbalar ro'yxatini tekshiradi; tekshiruv talab qiladiganlar ro'yxatini qaytaradi.</summary>
        public static List<string> FindUnverified(IEnumerable<HistoricalReference> references)
        {
            var result = new List<string>();
            if (references == null) return result;
            foreach (var r in references)
                if (!CanPresentAsFact(r)) result.Add(r.Id);
            return result;
        }
    }
}
