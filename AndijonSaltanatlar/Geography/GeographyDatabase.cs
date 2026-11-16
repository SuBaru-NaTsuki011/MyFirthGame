using System.Collections.Generic;

namespace AndijonSaltanatlar.Geography
{
    /// <summary>
    /// Andijon viloyati shaharlarining TAXMINIY geografik koordinatalari (ochiq ma'lumotlar asosida).
    /// Bu haqiqiy joylashuv orientiri; o'rta asr siyosiy chegaralari EMAS. Har bir yozuvda manba bor.
    /// Kelajakda GeoJSON fayllaridan (GeoJsonLoader) yuklanadi — hozir o'yinga shu paket kiritilgan.
    /// </summary>
    public static class GeographyDatabase
    {
        public const string Source = "OpenStreetMap (taxminiy)";
        public const string DataVersion = "and-2024.1";

        private static readonly Dictionary<string, GeographicPoint> _points = Build();

        public static GeographicPoint Get(string nameUz)
            => _points.TryGetValue(nameUz, out GeographicPoint p) ? p : null;

        private static Dictionary<string, GeographicPoint> Build()
        {
            var d = new Dictionary<string, GeographicPoint>();
            void P(string id, string name, double lat, double lon)
                => d[name] = new GeographicPoint { GeographicId = id, NameUz = name, Latitude = lat, Longitude = lon, Source = Source, DataVersion = DataVersion };

            // Taxminiy koordinatalar (lat, lon)
            P("uz-and-asaka", "Asaka", 40.642, 72.237);
            P("uz-and-andijon", "Andijon", 40.783, 72.344);
            P("uz-and-shahrixon", "Shahrixon", 40.711, 72.059);
            P("uz-and-baliqchi", "Baliqchi", 40.878, 71.717);
            P("uz-and-marhamat", "Marhamat", 40.470, 72.320);
            P("uz-and-xojaobod", "Xo'jaobod", 40.657, 72.558);
            P("uz-and-qorgontepa", "Qo'rg'ontepa", 40.735, 72.700);
            P("uz-and-paxtaobod", "Paxtaobod", 40.912, 72.368);
            P("uz-and-izboskan", "Izboskan", 40.918, 72.100);
            P("uz-and-boston", "Bo'ston", 40.600, 72.450);

            return d;
        }
    }
}
