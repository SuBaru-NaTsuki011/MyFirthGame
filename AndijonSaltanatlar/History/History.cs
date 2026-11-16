using System.Collections.Generic;

namespace AndijonSaltanatlar.History
{
    /// <summary>Kontentning tarixiy ishonchlilik holati — tasdiqlangan fakt, rekonstruksiya yoki xayoliy.</summary>
    public enum VerificationStatus { Tasdiqlangan, Rekonstruksiya, Xayoliy, Tekshirilmagan }

    public static class VerificationStatusText
    {
        public static string Name(VerificationStatus s)
        {
            switch (s)
            {
                case VerificationStatus.Tasdiqlangan: return "Tasdiqlangan manba";
                case VerificationStatus.Rekonstruksiya: return "Tarixiy rekonstruksiya";
                case VerificationStatus.Xayoliy: return "Xayoliy (o'yin)";
                default: return "Tekshirilmagan";
            }
        }
    }

    public sealed class HistoryEntry
    {
        public string Id { get; set; } = "";
        public string TitleUz { get; set; } = "";
        public string DescriptionUz { get; set; } = "";
        public string HistoricalPeriod { get; set; } = "";
        public string ImagePath { get; set; } = "";
        public VerificationStatus Status { get; set; }
        public List<string> SourceReferences { get; set; } = new List<string>();
        public List<string> RelatedTechnologyIds { get; set; } = new List<string>();
    }

    /// <summary>
    /// TARIX XAZINASI — tarixiy shaxs, joy va voqealar ma'lumotlari. Har bir yozuv ishonchlilik
    /// holati bilan belgilangan. Tasdiqlangan ma'lumotlar manbaga tayangan; qolganlari rekonstruksiya
    /// yoki xayoliy deb aniq ko'rsatilgan — to'qib chiqarilmagan.
    /// </summary>
    public static class HistoryRepository
    {
        public static readonly List<HistoryEntry> Entries = Build();

        private static List<HistoryEntry> Build()
        {
            return new List<HistoryEntry>
            {
                new HistoryEntry
                {
                    Id = "bobur", TitleUz = "Zahiriddin Muhammad Bobur", HistoricalPeriod = "1483-1530",
                    Status = VerificationStatus.Tasdiqlangan,
                    DescriptionUz = "Andijonda tug'ilgan sarkarda, davlat arbobi va shoir. Boburiylar saltanatiga asos solgan. \"Boburnoma\" asari muallifi.",
                    SourceReferences = { "Boburnoma", "O'zbekiston Milliy Entsiklopediyasi" },
                    RelatedTechnologyIds = { "kitobat", "xattotlik" }
                },
                new HistoryEntry
                {
                    Id = "andijon", TitleUz = "Andijonning tarixiy rivojlanishi", HistoricalPeriod = "O'rta asrlar",
                    Status = VerificationStatus.Rekonstruksiya,
                    DescriptionUz = "Farg'ona vodiysidagi qadimiy shahar; Buyuk ipak yo'lidagi savdo va hunarmandchilik markazi.",
                    SourceReferences = { "Umumiy tarixiy manbalar" }
                },
                new HistoryEntry
                {
                    Id = "fargona-madaniyat", TitleUz = "Farg'ona vodiysi shahar madaniyati", HistoricalPeriod = "O'rta asrlar",
                    Status = VerificationStatus.Rekonstruksiya,
                    DescriptionUz = "Mahalla tuzilishi, hovlili uylar, bozor va madrasalarga asoslangan sharqona shahar madaniyati."
                },
                new HistoryEntry
                {
                    Id = "temuriylar-ilm", TitleUz = "Temuriylar davri ilm-fani", HistoricalPeriod = "XIV-XV asr",
                    Status = VerificationStatus.Tasdiqlangan,
                    DescriptionUz = "Ulug'bek rasadxonasi (Samarqand) kabi ilmiy maskanlar; astronomiya va matematika yuksalgan davr.",
                    SourceReferences = { "Tarixiy manbalar" }, RelatedTechnologyIds = { "astronomiya", "hisob-ilmi" }
                },
                new HistoryEntry
                {
                    Id = "ipak-yoli", TitleUz = "Buyuk ipak yo'li savdosi", HistoricalPeriod = "Qadimgi-O'rta asrlar",
                    Status = VerificationStatus.Tasdiqlangan,
                    DescriptionUz = "Sharq va G'arbni bog'lagan savdo yo'llari tarmog'i; karvonsaroylar va bozorlar rivoji.",
                    RelatedTechnologyIds = { "karvon-savdosi" }
                },
                new HistoryEntry
                {
                    Id = "asaka", TitleUz = "Asaka hududi", HistoricalPeriod = "—",
                    Status = VerificationStatus.Tekshirilmagan,
                    DescriptionUz = "O'yinda geografik orientir sifatida ishlatiladi. Aniq o'rta asr chegaralari tarixiy dalil bilan tasdiqlanmagan."
                },
            };
        }
    }
}
