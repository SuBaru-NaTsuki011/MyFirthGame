using System;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Geography
{
    /// <summary>
    /// WGS84 geografik koordinatalarni (lat/lon) o'yin xaritasining planar (normal 0..1) koordinatalariga
    /// o'tkazadi — lokal ekvidistant (equirectangular) proyeksiya. Bu MonoGame ekran koordinatalaridan
    /// ALOHIDA tizim: ekranga aylantirish WorldMap kamerasida qilinadi.
    ///
    /// Andijon viloyati uchun lokal chegaralar. Kattaroq (O'zbekiston/Osiyo) xaritalar uchun boshqa
    /// chegara va masshtab bilan alohida konverter yaratiladi.
    /// </summary>
    public static class CoordinateConverter
    {
        // Andijon viloyatini qamrab oluvchi taxminiy chegaralar
        public const double MinLat = 40.40, MaxLat = 40.98;
        public const double MinLon = 71.60, MaxLon = 72.85;

        /// <summary>Lat/Lon -> normal (0..1), shimol tepada. Chegaradan tashqari qiymat ham qaytariladi (clamp yo'q).</summary>
        public static Vector2 ToNormalized(double lat, double lon)
        {
            double x = (lon - MinLon) / (MaxLon - MinLon);
            double y = (MaxLat - lat) / (MaxLat - MinLat);
            return new Vector2((float)x, (float)y);
        }

        /// <summary>Teskari: normal (0..1) -> lat/lon.</summary>
        public static void ToGeographic(float nx, float ny, out double lat, out double lon)
        {
            lon = MinLon + nx * (MaxLon - MinLon);
            lat = MaxLat - ny * (MaxLat - MinLat);
        }

        /// <summary>Ikki geografik nuqta orasidagi taxminiy masofa (km) — haversine.</summary>
        public static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }
    }
}
