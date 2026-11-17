using System.IO;

namespace AndijonSaltanatlar.Core
{
    /// <summary>
    /// O'yin ma'lumotlari papkasi (kirish tokeni, sozlamalar, zaxira saqlash).
    /// Standart: %AppData%/AndijonSaltanatlar. Sinovlarda --data-dir bilan alohida papka beriladi —
    /// shunda avtomatik sinov o'yinchining haqiqiy hisobiga tegmaydi.
    /// </summary>
    public static class AppPaths
    {
        private static string _root;

        public static string Root
        {
            get => _root ?? Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "AndijonSaltanatlar");
            set => _root = string.IsNullOrWhiteSpace(value) ? null : Path.GetFullPath(value);
        }

        public static string File(string name) => Path.Combine(Root, name);
    }
}
