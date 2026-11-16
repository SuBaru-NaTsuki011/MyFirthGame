namespace AndijonSaltanatlar.Geography
{
    /// <summary>
    /// Haqiqiy geografik nuqta (WGS84). Manba va versiya bilan — o'yin uchun generatsiya qilingan
    /// kontentdan ajratilgan. Koordinatalar taxminiy (ochiq ma'lumotlardan) va shunday belgilangan.
    /// </summary>
    public sealed class GeographicPoint
    {
        public string GeographicId { get; set; } = "";
        public string NameUz { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Source { get; set; } = "";
        public string DataVersion { get; set; } = "";
    }
}
