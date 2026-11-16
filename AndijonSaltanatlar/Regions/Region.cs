namespace AndijonSaltanatlar.Regions
{
    /// <summary>
    /// Strategik xaritadagi bitta hudud (Andijon viloyati shaharlari ruhida). Faol shahar (Asaka)
    /// batafsil simulyatsiya qilinadi; qolgan hududlar statistik model bilan boshqariladi.
    /// X,Y — strategik xaritadagi normal joylashuv (0..1), taxminiy geografiyaga mos.
    /// </summary>
    public sealed class Region
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public float X { get; set; }   // normal planar (0..1) — geografik koordinatadan hisoblangan
        public float Y { get; set; }

        // --- Haqiqiy geografiya qatlami (o'yin kontentidan ajratilgan) ---
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Source { get; set; } = "";
        public string GeographicId { get; set; } = "";

        /// <summary>Egasi (Kingdom.Id). 0 — mustaqil/neytral.</summary>
        public int OwnerKingdomId { get; set; }
        public bool IsCapital { get; set; }
        /// <summary>Faol (batafsil simulyatsiya qilinadigan) shahar — Asaka.</summary>
        public bool IsActiveCity { get; set; }

        public int Population { get; set; }
        public int Food { get; set; }
        public int Production { get; set; }
        public int TaxIncome { get; set; }
        public int Trade { get; set; }
        public int Security { get; set; }
        public int Happiness { get; set; } = 60;
        public int Governance { get; set; } = 50;
        /// <summary>Rivojlanish darajasi (0..100) — daromad va aholi salohiyatini belgilaydi.</summary>
        public int Development { get; set; } = 30;

        /// <summary>Statistik hududning bir kunlik sof daromadi (rivojlanish + aholiga bog'liq).</summary>
        public int DailyIncome => (Development * 2 + Population / 10 + Trade) * (Happiness >= 40 ? 1 : 0) + 1;
    }
}
