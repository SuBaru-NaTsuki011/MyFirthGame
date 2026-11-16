using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AndijonSaltanatlar.Geography
{
    /// <summary>
    /// GeoJSON Point FeatureCollection'dan geografik nuqtalarni yuklovchi pipeline (ixtiyoriy).
    /// Fayl mavjud bo'lmasa null qaytaradi — o'yin ichki GeographyDatabase paketidan foydalanadi.
    /// Katta fayllar to'liq RAMga yuklanmasligi uchun faqat kerakli xususiyatlar o'qiladi.
    /// Attribution/litsenziya talablari saqlanishi kerak (manba GeographicPoint.Source'da).
    /// </summary>
    public static class GeoJsonLoader
    {
        public static List<GeographicPoint> TryLoad(string path, string source, string version)
        {
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                using JsonDocument doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("features", out JsonElement features)) return null;

                var list = new List<GeographicPoint>();
                foreach (JsonElement f in features.EnumerateArray())
                {
                    if (!f.TryGetProperty("geometry", out JsonElement g)) continue;
                    if (!g.TryGetProperty("type", out JsonElement gt) || gt.GetString() != "Point") continue;
                    if (!g.TryGetProperty("coordinates", out JsonElement c) || c.GetArrayLength() < 2) continue;

                    double lon = c[0].GetDouble();
                    double lat = c[1].GetDouble();
                    string name = "", id = "";
                    if (f.TryGetProperty("properties", out JsonElement props))
                    {
                        if (props.TryGetProperty("name", out JsonElement n)) name = n.GetString() ?? "";
                        if (props.TryGetProperty("id", out JsonElement idp)) id = idp.GetString() ?? "";
                    }
                    list.Add(new GeographicPoint { GeographicId = id, NameUz = name, Latitude = lat, Longitude = lon, Source = source, DataVersion = version });
                }
                return list.Count > 0 ? list : null;
            }
            catch (Exception)
            {
                return null; // buzilgan fayl — ichki paketga qaytamiz
            }
        }
    }

    /// <summary>Geografik koordinatalarning asosiy to'g'riligini tekshiradi.</summary>
    public static class GeographyValidator
    {
        public static bool IsValid(double lat, double lon)
            => lat >= -90 && lat <= 90 && lon >= -180 && lon <= 180;
    }
}
