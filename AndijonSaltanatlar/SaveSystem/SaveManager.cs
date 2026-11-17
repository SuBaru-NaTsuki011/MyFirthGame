using System;
using System.IO;
using System.Text.Json;

namespace AndijonSaltanatlar.SaveSystem
{
    /// <summary>
    /// O'yinni JSON faylga saqlaydi va yuklaydi. Buzilgan yoki eski versiyali fayl topilsa
    /// o'yin to'xtamaydi — null qaytaradi. Yozish atomar: avval .tmp ga yoziladi, keyin almashtiriladi.
    /// </summary>
    public static class SaveManager
    {
        public const int CurrentVersion = 2;

        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { WriteIndented = true };

        /// <summary>Standart saqlash manzili: %AppData%/AndijonSaltanatlar/save1.json</summary>
        public static string DefaultPath => AndijonSaltanatlar.Core.AppPaths.File("save1.json");

        public static bool Save(string path, SaveGameData data)
        {
            try
            {
                data.Version = CurrentVersion;
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                string json = JsonSerializer.Serialize(data, Options);
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static SaveGameData Load(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                SaveGameData data = JsonSerializer.Deserialize<SaveGameData>(json);
                if (data == null) return null;
                if (data.Version > CurrentVersion) return null; // kelajak versiyasi — o'qimaymiz
                if (data.PlayerBuildings == null) data.PlayerBuildings = new System.Collections.Generic.List<SavedBuilding>();
                return data;
            }
            catch (Exception)
            {
                return null; // buzilgan fayl — o'yin to'xtamaydi
            }
        }

        public static bool Exists(string path) => File.Exists(path);

        /// <summary>Bulutga yuborish uchun JSON (ixcham).</summary>
        public static string ToJson(SaveGameData data)
        {
            data.Version = CurrentVersion;
            return JsonSerializer.Serialize(data);
        }

        /// <summary>Bulutdan kelgan JSON'ni o'qiydi; buzilgan bo'lsa null.</summary>
        public static SaveGameData FromJson(string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) return null;
                SaveGameData data = JsonSerializer.Deserialize<SaveGameData>(json);
                if (data == null || data.Version > CurrentVersion) return null;
                if (data.PlayerBuildings == null) data.PlayerBuildings = new System.Collections.Generic.List<SavedBuilding>();
                return data;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Eski saqlashni joriy xarita o'lchamiga moslaydi: shahar bloki siljigan bo'lsa, binolar, yo'llar,
        /// ish joylari va kamera ham shu qadar suriladi. Siljish o'zgarganda AI reja (Vazir) koordinatalari
        /// eskirgani uchun tashlab yuboriladi.
        /// </summary>
        public static void MigrateLayout(SaveGameData data, int currentOffset, int halfTileH)
        {
            int d = currentOffset - data.MapOffset;
            if (d == 0) return;
            foreach (SavedBuilding b in data.PlayerBuildings) { b.X += d; b.Y += d; }
            if (data.Roads != null) foreach (SavedRoad r in data.Roads) { r.X += d; r.Y += d; }
            if (data.Citizens != null)
                foreach (var c in data.Citizens)
                    if (c.WorkX >= 0 && c.WorkY >= 0) { c.WorkX += d; c.WorkY += d; }
            // Izometrik: (x+d, y+d) siljish ekran X'ni o'zgartirmaydi, Y esa 2*d*halfTileH ga oshadi.
            data.CameraY += 2 * d * halfTileH;
            data.Governor = null;
            data.MapOffset = currentOffset;
        }
    }
}
