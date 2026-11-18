namespace AndijonSaltanatlar.Shared
{
    /// <summary>
    /// Ilova versiyasi va yangilash manbasi (GitHub Releases) — bitta manba.
    /// Yangi reliz chiqarganda: 1) shu yerdagi Current ni oshiring, 2) Android csproj dagi
    /// ApplicationDisplayVersion/ApplicationVersion ni mos oshiring, 3) GitHub'da shu versiya
    /// nomli (masalan "v0.21.0") reliz yarating va APK'ni ApkAssetName nomi bilan yuklang.
    /// </summary>
    public static class AppVersion
    {
        /// <summary>Joriy ilova versiyasi (Android ApplicationDisplayVersion bilan bir xil bo'lsin).</summary>
        public const string Current = "0.20.0";

        // --- Yangilash manbasi: GitHub Releases ---
        public const string GitHubOwner = "anvarovmuhammadamin011-ops";
        public const string GitHubRepo = "MyFirthGame";

        /// <summary>Har relizda APK aynan shu nom bilan yuklansin — shunda quyidagi doimiy havolalar ishlaydi.</summary>
        public const string ApkAssetName = "AndijonSaltanatlar.apk";

        /// <summary>Eng so'nggi reliz haqida JSON (tag_name solishtiriladi).</summary>
        public static string LatestReleaseApiUrl =>
            "https://api.github.com/repos/" + GitHubOwner + "/" + GitHubRepo + "/releases/latest";

        /// <summary>Doimiy yuklab olish havolasi — har doim eng so'nggi relizning APK'siga ishora qiladi (QR shu manzilga).</summary>
        public static string LatestApkUrl =>
            "https://github.com/" + GitHubOwner + "/" + GitHubRepo + "/releases/latest/download/" + ApkAssetName;

        /// <summary>Relizlar sahifasi (brauzerda ochish uchun zaxira havola).</summary>
        public static string ReleasesPageUrl =>
            "https://github.com/" + GitHubOwner + "/" + GitHubRepo + "/releases/latest";

        /// <summary><paramref name="latest"/> joriy ishlab turgan versiyadan yangiroqmi.</summary>
        public static bool IsNewer(string latest, string current)
        {
            return Compare(latest, current) > 0;
        }

        /// <summary>Semantik versiyalarni solishtiradi. a&lt;b → manfiy. "v0.21.0" va "0.21.0" formatlarini tushunadi.</summary>
        public static int Compare(string a, string b)
        {
            int[] pa = Parse(a);
            int[] pb = Parse(b);
            for (int i = 0; i < 3; i++)
            {
                if (pa[i] != pb[i]) return pa[i] - pb[i];
            }
            return 0;
        }

        private static int[] Parse(string v)
        {
            int[] r = { 0, 0, 0 };
            if (string.IsNullOrWhiteSpace(v)) return r;
            v = v.Trim();
            if (v.Length > 0 && (v[0] == 'v' || v[0] == 'V')) v = v.Substring(1);
            string[] parts = v.Split('.');
            for (int i = 0; i < parts.Length && i < 3; i++)
            {
                int cur = 0;
                foreach (char c in parts[i])
                {
                    if (c >= '0' && c <= '9') cur = cur * 10 + (c - '0');
                    else break; // "1-beta" kabi qo'shimchalarni e'tiborsiz qoldiradi
                }
                r[i] = cur;
            }
            return r;
        }
    }
}
