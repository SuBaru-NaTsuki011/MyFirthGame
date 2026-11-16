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
        public const int CurrentVersion = 1;

        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { WriteIndented = true };

        /// <summary>Standart saqlash manzili: %AppData%/AndijonSaltanatlar/save1.json</summary>
        public static string DefaultPath => Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            "AndijonSaltanatlar", "save1.json");

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
    }
}
