using System;
using System.Net.Http;
using System.Threading.Tasks;
using AndijonSaltanatlar.Shared;

namespace AndijonSaltanatlar.Online
{
    /// <summary>Yangilash haqida ma'lumot (GitHub'dagi eng so'nggi reliz).</summary>
    public sealed class UpdateInfo
    {
        public bool Available;
        public string LatestVersion = "";
        public string DownloadUrl = AppVersion.LatestApkUrl;
        public string ReleaseUrl = AppVersion.ReleasesPageUrl;
    }

    /// <summary>
    /// GitHub Releases'dan eng so'nggi versiyani tekshiradi.
    /// Internet yo'q, server javob bermasa yoki reliz bo'lmasa — jim o'tadi (null), o'yinni bloklamaydi.
    /// </summary>
    public static class UpdateChecker
    {
        /// <summary>Yangilash bo'lsa UpdateInfo, aks holda null qaytaradi. Hech qachon exception tashlamaydi.</summary>
        public static async Task<UpdateInfo> CheckAsync()
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
                // GitHub API User-Agent talab qiladi.
                http.DefaultRequestHeaders.UserAgent.ParseAdd("AndijonSaltanatlar/" + AppVersion.Current);
                http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

                string json = await http.GetStringAsync(AppVersion.LatestReleaseApiUrl).ConfigureAwait(false);
                string tag = ExtractString(json, "tag_name");
                if (string.IsNullOrEmpty(tag)) return null;

                if (AppVersion.IsNewer(tag, AppVersion.Current))
                {
                    return new UpdateInfo { Available = true, LatestVersion = tag };
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>JSON'dan "key":"value" ni qo'lda ajratadi (kichik bog'liqliksiz parser).</summary>
        private static string ExtractString(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return null;
            string needle = "\"" + key + "\"";
            int i = json.IndexOf(needle, StringComparison.Ordinal);
            if (i < 0) return null;
            i = json.IndexOf(':', i + needle.Length);
            if (i < 0) return null;
            int q1 = json.IndexOf('"', i + 1);
            if (q1 < 0) return null;
            int q2 = json.IndexOf('"', q1 + 1);
            if (q2 < 0) return null;
            return json.Substring(q1 + 1, q2 - q1 - 1);
        }
    }
}
