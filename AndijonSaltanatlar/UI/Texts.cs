namespace AndijonSaltanatlar.UI
{
    /// <summary>
    /// Interfeysdagi barcha o'zbekcha matnlar bitta joyda. Keyinchalik til almashtirish
    /// (tarjima) shu klass orqali qilinadi.
    /// </summary>
    public static class Texts
    {
        public const string Title = "ANDIJON: TAXTLAR JANGI";

        // Bino nomlari
        public const string House = "Uy";
        public const string Tower = "Minora";
        public const string Market = "Bozor";
        public const string Wall = "Devor";
        public const string Palace = "Saroy";

        // Resurslar
        public const string Coin = "Tanga";
        public const string Food = "Oziq-ovqat";
        public const string Population = "Aholi";
        public const string Day = "Kun";

        // Pastki panel bo'limlari
        public const string TabBuild = "QURISH";
        public const string TabCity = "SHAHAR";
        public const string TabPopulation = "AHOLI";
        public const string TabState = "DAVLAT";
        public const string TabArmy = "QO'SHIN";
        public const string TabSettings = "SOZLAMALAR";
        public const string TabCivilization = "TARAQQIYOT";

        // Amallar
        public const string Build = "Qurish";
        public const string Cancel = "Bekor qilish";
        public const string BuildMode = "QURILISH REJIMI";
        public const string Paused = "TO'XTATILDI";
        public const string Help = "YORDAM";
        public const string Save = "Saqlash (F5)";
        public const string Load = "Yuklash (F9)";

        // Panel yorliqlari
        public const string Cost = "Narx";
        public const string HitPoints = "Mustahkamlik";
        public const string Attack = "Hujum";
        public const string Capacity = "Sig'im";

        // Hukmdor / murojaat
        public const string Petition = "Murojaat";
        public const string Decree = "Farmon";
        public const string TaxPopulation = "Aholi solig'i";
        public const string TaxTrade = "Savdo solig'i";
        public const string TaxProduction = "Ishlab chiqarish solig'i";
        public const string Speed = "Tezlik";

        // Xabarlar
        public const string CannotBuildHere = "Bu joyga qurib bo'lmaydi";
        public const string NotEnoughCoins = "Tanga yetarli emas";
        public const string Saved = "O'yin saqlandi";
        public const string SaveFailed = "Saqlab bo'lmadi";
        public const string Loaded = "O'yin yuklandi";
        public const string NoSave = "Saqlangan o'yin topilmadi";

        // Yordam ekrani qatorlari
        public static readonly string[] HelpLines =
        {
            "Kamera: W A S D yoki o'q tugmalar",
            "Surish: o'ng yoki o'rta tugmani bosib torting",
            "Zoom: sichqoncha g'ildiragi yoki + / -",
            "Markazga qaytish: Home",
            "Qurish: pastdagi QURISH paneli yoki 1 2 3 4",
            "Joylashtirish: chap tugma  (devorni sudrash mumkin)",
            "Bekor qilish: o'ng tugma yoki Esc",
            "Tanlash: obyekt ustiga chap tugma bilan bosing",
            "Saqlash / Yuklash: F5 / F9",
            "To'xtatish: Bo'sh joy (Space)  |  Yordam: H",
            "Ichiga kirish: jangchini tanlab F (Q/E almashtirish)",
            "Fuqaro: tanlab F bilan boshqaring; E — ish/suhbat; F — qaytish",
            "Ferma: dehqon bilan E — urug' ekish yoki hosil o'rish; bozorga borib sotish",
            "Uy/saroyga kirish: odam ichida bino yonida ENTER",
            "Jang: SPACE kombo (kesish-chopish-sanchish), R aylanma",
            "Shohni chiqarish: saroyda shoh yonida F",
            "Bosqin chaqirish (sinov): B",
            "Keyingi fasl (ko'rish uchun): N",
            "Aholi solig'i 30% dan oshsa xalq saroyga tosh otadi"
        };
    }
}
