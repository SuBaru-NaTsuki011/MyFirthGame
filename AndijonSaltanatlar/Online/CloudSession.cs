using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AndijonSaltanatlar.Shared.DTOs;

namespace AndijonSaltanatlar.Online
{
    public enum CloudLoginState { SignedOut, CheckingToken, WaitingForBrowser, SignedIn, Error }

    /// <summary>Qurilmada saqlanadigan kirish ma'lumoti (parol yo'q — faqat server bergan token).</summary>
    public sealed class StoredAuth
    {
        public string ServerUrl { get; set; } = "";
        public string Token { get; set; } = "";
        public string Username { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Email { get; set; } = "";

        private static string FilePath => AndijonSaltanatlar.Core.AppPaths.File("auth.json");

        public static StoredAuth Load()
        {
            try { if (File.Exists(FilePath)) return JsonSerializer.Deserialize<StoredAuth>(File.ReadAllText(FilePath)); }
            catch { }
            return null;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
            }
            catch { }
        }

        public static void Clear()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
        }
    }

    /// <summary>
    /// Doimiy onlayn sessiya: Google orqali kirish va bulutdagi saqlash.
    ///   - Kirish: server /api/auth/google/start → tizim brauzeri (Google) → /api/auth/google/poll (har 2 soniya).
    ///   - Token qurilmada saqlanadi: keyingi safar o'yin avtomatik kiradi (/api/auth/me bilan tekshiriladi).
    ///   - Saqlash: GET/PUT /api/save (versiya nazorati bilan) — o'yin istalgan qurilmada qolgan joyidan davom etadi.
    /// Barcha tarmoq ishlari fonda; natijalar <see cref="Pump"/> orqali o'yin oqimiga o'tadi.
    /// </summary>
    public sealed class CloudSession
    {
        private static readonly JsonSerializerOptions Json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        private readonly ConcurrentQueue<Action> _main = new ConcurrentQueue<Action>();
        private CancellationTokenSource _loginCts;
        private HttpClient _http;
        private int _uploading;

        public CloudLoginState State { get; private set; } = CloudLoginState.SignedOut;
        public string ServerUrl { get; private set; } = "";
        public string Token { get; private set; }
        public string Username { get; private set; } = "";
        public string DisplayName { get; private set; } = "";
        public string Email { get; private set; } = "";
        public string Message { get; private set; } = "";
        public bool MessageIsError { get; private set; }
        public string LoginUrl { get; private set; } = "";

        /// <summary>Oxirgi muvaffaqiyatli saqlash versiyasi (server tasdiqlagan).</summary>
        public long SaveVersion { get; private set; }
        public DateTime LastSavedUtc { get; private set; }
        public bool LastSaveFailed { get; private set; }
        public bool Uploading => _uploading != 0;

        /// <summary>Kirish muvaffaqiyatli bo'lganda (asosiy oqimda) chaqiriladi.</summary>
        public Action SignedIn;
        /// <summary>Boshqa qurilma yangiroq saqlagan (409) — o'yin bulutdan qayta yuklanishi kerak.</summary>
        public Action SaveConflict;

        /// <summary>
        /// Android: telefondagi Google akkauntlar ro'yxati (tizim oynasi). Web Client ID oladi, tanlangan akkauntning
        /// Google ID tokenini qaytaradi; foydalanuvchi oynani yopsa OperationCanceledException. null — brauzer orqali.
        /// </summary>
        public Func<string, Task<string>> NativeAccountPicker { get; set; }

        /// <summary>Hozir telefonning akkaunt tanlash oynasi ochiq (kirish ekrani matni uchun).</summary>
        public bool UsingNativePicker { get; private set; }

        /// <summary>Sinov uchun: brauzer ochilmaydi, login manzili shu faylga yoziladi (Playwright o'zi ochadi).</summary>
        public string LoginUrlFile { get; set; }

        public string AccountLabel => string.IsNullOrEmpty(Email) ? DisplayName : DisplayName + " (" + Email + ")";

        public void Pump()
        {
            while (_main.TryDequeue(out Action a))
            {
                try { a(); } catch { }
            }
        }

        private void Post(Action a) => _main.Enqueue(a);
        private void SetMessage(string text, bool error) => Post(() => { Message = text ?? ""; MessageIsError = error; });

        public static string NormalizeUrl(string url)
        {
            url = (url ?? "").Trim().TrimEnd('/');
            if (url.Length > 0 && !url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                url = (LooksLocal(url) ? "http://" : "https://") + url;
            return url;
        }

        /// <summary>IP manzil / localhost / port ko'rsatilgan bo'lsa — uy tarmog'i (http). Domen nomi (masalan
        /// xxx.trycloudflare.com) — internet manzili, standart https.</summary>
        public static bool LooksLocal(string hostAndRest)
        {
            string host = hostAndRest.Split('/')[0];
            if (host.Contains(':')) return true;
            if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;
            return System.Net.IPAddress.TryParse(host, out _);
        }

        private HttpClient Http(string url)
        {
            HttpClient h = _http;
            if (h == null || !string.Equals(h.BaseAddress.ToString().TrimEnd('/'), url, StringComparison.OrdinalIgnoreCase))
                _http = h = new HttpClient { BaseAddress = new Uri(url + "/"), Timeout = TimeSpan.FromSeconds(15) };
            return h;
        }

        /// <summary>So'rov yuboradi; token har so'rovga alohida qo'shiladi (umumiy header'lar oqimlar orasida o'zgarmaydi).</summary>
        private Task<HttpResponseMessage> Send(string url, HttpMethod method, string path, HttpContent content = null,
                                               string token = null, CancellationToken ct = default)
        {
            var req = new HttpRequestMessage(method, path) { Content = content };
            if (token != null) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return Http(url).SendAsync(req, ct);
        }

        // ------------------------------------------------------------------
        // Avtomatik kirish (saqlangan token)
        // ------------------------------------------------------------------
        public void TryResume(string defaultServer)
        {
            StoredAuth a = StoredAuth.Load();
            ServerUrl = NormalizeUrl(a?.ServerUrl ?? defaultServer);
            if (a == null || string.IsNullOrEmpty(a.Token) || ServerUrl.Length == 0) { State = CloudLoginState.SignedOut; return; }
            State = CloudLoginState.CheckingToken;
            Message = "Serverga ulanmoqda...";
            MessageIsError = false;
            string url = ServerUrl, token = a.Token;
            Task.Run(async () =>
            {
                try
                {
                    HttpResponseMessage r = await Send(url, HttpMethod.Get, "api/auth/me", token: token);
                    if (r.StatusCode == HttpStatusCode.Unauthorized || r.StatusCode == HttpStatusCode.Forbidden)
                    {
                        StoredAuth.Clear();
                        Post(() => { State = CloudLoginState.SignedOut; Message = "Kirish muddati tugadi — Google bilan qayta kiring."; MessageIsError = false; });
                        return;
                    }
                    r.EnsureSuccessStatusCode();
                    MeResponse me = await r.Content.ReadFromJsonAsync<MeResponse>(Json);
                    Post(() => FinishSignIn(url, token, me.Username, me.DisplayName, me.Email));
                }
                catch (Exception ex)
                {
                    Post(() => { State = CloudLoginState.Error; Message = "Serverga ulanib bo'lmadi. Internetni tekshirib, qayta urinib ko'ring." + Detail(ex); MessageIsError = true; });
                }
            });
        }

        // ------------------------------------------------------------------
        // Google bilan kirish
        // ------------------------------------------------------------------
        public void StartGoogleLogin(string serverUrl)
        {
            string url = NormalizeUrl(serverUrl);
            if (url.Length == 0) { Message = "Server manzilini kiriting"; MessageIsError = true; return; }
            CancelLogin();
            ServerUrl = url;
            State = CloudLoginState.WaitingForBrowser;
            UsingNativePicker = NativeAccountPicker != null;
            Message = UsingNativePicker ? "Google akkauntlari ochilmoqda..." : "Brauzer ochilmoqda...";
            MessageIsError = false;
            var cts = _loginCts = new CancellationTokenSource();
            Task.Run(async () =>
            {
                try
                {
                    // 1) Telefon: tizimning Google akkaunt tanlash oynasi (telefondagi barcha akkauntlar).
                    if (NativeAccountPicker != null && await TryNativeLoginAsync(url, cts.Token)) return;
                    Post(() => UsingNativePicker = false);

                    // 2) Brauzer orqali Google (kompyuter yoki telefon oynasi ishlamasa).
                    HttpResponseMessage r = await Send(url, HttpMethod.Post, "api/auth/google/start", ct: cts.Token);
                    if (!r.IsSuccessStatusCode) { await FailFrom(r, "Google kirishni boshlab bo'lmadi"); return; }
                    GoogleLoginStartResponse start = await r.Content.ReadFromJsonAsync<GoogleLoginStartResponse>(Json, cts.Token);
                    Post(() => LoginUrl = start.LoginUrl);
                    OpenBrowser(start.LoginUrl);
                    SetMessage("Brauzerda Google hisobingizni tanlang, so'ng o'yinga qayting.", false);

                    DateTime until = DateTime.UtcNow.AddSeconds(Math.Max(60, start.ExpiresInSeconds));
                    while (DateTime.UtcNow < until)
                    {
                        await Task.Delay(2000, cts.Token);
                        HttpResponseMessage p;
                        try { p = await Send(url, HttpMethod.Get, "api/auth/google/poll?s=" + Uri.EscapeDataString(start.SessionId) + "&k=" + Uri.EscapeDataString(start.PollKey), ct: cts.Token); }
                        catch (HttpRequestException) { continue; } // vaqtincha tarmoq uzilishi — qayta urinamiz
                        if (!p.IsSuccessStatusCode) continue;
                        GoogleLoginPollResponse res = await p.Content.ReadFromJsonAsync<GoogleLoginPollResponse>(Json, cts.Token);
                        if (res.Status == "pending") continue;
                        if (res.Status == "ok" && res.Auth != null)
                        {
                            string token = res.Auth.Token;
                            Post(() => FinishSignIn(url, token, res.Auth.Username, res.DisplayName, res.Email));
                            return;
                        }
                        Post(() => { State = CloudLoginState.Error; Message = string.IsNullOrEmpty(res.MessageUz) ? "Kirib bo'lmadi" : res.MessageUz; MessageIsError = true; });
                        return;
                    }
                    Post(() => { State = CloudLoginState.Error; Message = "Kirish vaqti tugadi. Qaytadan urinib ko'ring."; MessageIsError = true; });
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Post(() => { State = CloudLoginState.Error; Message = "Serverga ulanib bo'lmadi." + Detail(ex); MessageIsError = true; });
                }
            });
        }

        /// <summary>
        /// Telefondagi akkaunt tanlash oynasi orqali kirish. true — jarayon tugadi (kirildi, bekor qilindi yoki xato
        /// ko'rsatildi); false — bu usul mavjud emas (server Google sozlanmagan / Play xizmatlari yo'q), brauzerga o'tiladi.
        /// </summary>
        private async Task<bool> TryNativeLoginAsync(string url, CancellationToken ct)
        {
            GoogleClientConfig cfg;
            HttpResponseMessage c = await Send(url, HttpMethod.Get, "api/auth/google/config", ct: ct);
            if (!c.IsSuccessStatusCode) return false;
            cfg = await c.Content.ReadFromJsonAsync<GoogleClientConfig>(Json, ct);
            if (cfg == null || !cfg.Configured || string.IsNullOrEmpty(cfg.WebClientId)) return false;

            SetMessage("Telefoningizdagi Google akkauntlardan birini tanlang.", false);
            string idToken;
            try
            {
                idToken = await NativeAccountPicker(cfg.WebClientId);
            }
            catch (OperationCanceledException)
            {
                Post(() => { State = CloudLoginState.SignedOut; UsingNativePicker = false; Message = "Kirish bekor qilindi."; MessageIsError = false; });
                return true;
            }
            catch (Exception ex)
            {
                // Masalan, Google Play xizmatlari yo'q yoki ilova Google Cloud'da ro'yxatdan o'tmagan — brauzer orqali davom etamiz.
                SetMessage("Telefon akkaunt oynasi ochilmadi, brauzer orqali davom etamiz." + Detail(ex), false);
                return false;
            }

            HttpResponseMessage r = await Send(url, HttpMethod.Post, "api/auth/google/idtoken",
                JsonContent.Create(new GoogleIdTokenRequest { IdToken = idToken }, options: Json), ct: ct);
            if (!r.IsSuccessStatusCode) { await FailFrom(r, "Google hisobini tasdiqlab bo'lmadi"); return true; }
            GoogleLoginPollResponse res = await r.Content.ReadFromJsonAsync<GoogleLoginPollResponse>(Json, ct);
            Post(() => { UsingNativePicker = false; FinishSignIn(url, res.Auth.Token, res.Auth.Username, res.DisplayName, res.Email); });
            return true;
        }

        public void CancelLogin()
        {
            try { _loginCts?.Cancel(); } catch { }
            _loginCts = null;
            if (State == CloudLoginState.WaitingForBrowser) { State = CloudLoginState.SignedOut; Message = ""; }
        }

        private void FinishSignIn(string url, string token, string username, string displayName, string email)
        {
            ServerUrl = url;
            Token = token;
            Username = username ?? "";
            DisplayName = string.IsNullOrEmpty(displayName) ? Username : displayName;
            Email = email ?? "";
            State = CloudLoginState.SignedIn;
            Message = "Xush kelibsiz, " + DisplayName + "!";
            MessageIsError = false;
            new StoredAuth { ServerUrl = url, Token = token, Username = Username, DisplayName = DisplayName, Email = Email }.Save();
            SignedIn?.Invoke();
        }

        /// <summary>Kirgandan keyingi xato (masalan saqlashni yuklab bo'lmadi) — kirish ekrani xabar va "Qayta urinish" ko'rsatadi.</summary>
        public void ReportError(string message)
        {
            State = CloudLoginState.Error;
            Message = message ?? "Xato";
            MessageIsError = true;
        }

        public void SignOut()
        {
            CancelLogin();
            StoredAuth.Clear();
            Token = null;
            SaveVersion = 0;
            State = CloudLoginState.SignedOut;
            Message = "Hisobdan chiqdingiz.";
            MessageIsError = false;
        }

        private void OpenBrowser(string url)
        {
            if (!string.IsNullOrEmpty(LoginUrlFile))
            {
                try { File.WriteAllText(LoginUrlFile, url); } catch { }
                return;
            }
            try
            {
#if ANDROID
                var intent = new Android.Content.Intent(Android.Content.Intent.ActionView, Android.Net.Uri.Parse(url));
                intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                Android.App.Application.Context.StartActivity(intent);
#else
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
#endif
            }
            catch (Exception ex)
            {
                SetMessage("Brauzerni ochib bo'lmadi: " + ex.Message, true);
            }
        }

        private async Task FailFrom(HttpResponseMessage r, string fallback)
        {
            string msg = fallback + " (" + (int)r.StatusCode + ")";
            try
            {
                using JsonDocument doc = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
                if (doc.RootElement.TryGetProperty("messageUz", out JsonElement m)) msg = m.GetString();
            }
            catch { }
            Post(() => { State = CloudLoginState.Error; Message = msg; MessageIsError = true; });
        }

        private static string Detail(Exception ex)
        {
            string m = ex.GetBaseException().Message ?? "";
            return m.Length == 0 ? "" : " (" + (m.Length > 60 ? m.Substring(0, 60) : m) + ")";
        }

        // ------------------------------------------------------------------
        // Bulutdagi saqlash
        // ------------------------------------------------------------------
        /// <summary>Bulutdagi o'yinni yuklaydi. onDone(json yoki null — saqlash yo'q, xato bo'lsa error).</summary>
        public void LoadSave(Action<string, string> onDone)
        {
            string url = ServerUrl, token = Token;
            Task.Run(async () =>
            {
                try
                {
                    HttpResponseMessage r = await Send(url, HttpMethod.Get, "api/save", token: token);
                    if (r.StatusCode == HttpStatusCode.NotFound) { Post(() => { SaveVersion = 0; onDone(null, null); }); return; }
                    if (r.StatusCode == HttpStatusCode.Unauthorized) { Post(() => onDone(null, "unauthorized")); return; }
                    r.EnsureSuccessStatusCode();
                    CloudSaveDto dto = await r.Content.ReadFromJsonAsync<CloudSaveDto>(Json);
                    Post(() => { SaveVersion = dto.Version; LastSavedUtc = dto.UpdatedAt; onDone(dto.Json, null); });
                }
                catch (Exception ex)
                {
                    Post(() => onDone(null, "Saqlangan o'yinni yuklab bo'lmadi." + Detail(ex)));
                }
            });
        }

        /// <summary>O'yinni bulutga yuboradi (fonda; bir vaqtda faqat bitta yuborish).</summary>
        public void UploadSave(string json, Action<bool> onDone = null)
        {
            if (State != CloudLoginState.SignedIn || string.IsNullOrEmpty(json)) { onDone?.Invoke(false); return; }
            if (Interlocked.Exchange(ref _uploading, 1) == 1) { onDone?.Invoke(false); return; }
            string url = ServerUrl, token = Token;
            long baseVersion = SaveVersion;
#if ANDROID
            const string device = "Android";
#else
            const string device = "Windows";
#endif
            Task.Run(async () =>
            {
                try
                {
                    HttpResponseMessage r = await Send(url, HttpMethod.Put, "api/save",
                        JsonContent.Create(new CloudSaveUploadRequest { Json = json, BaseVersion = baseVersion, Device = device }, options: Json), token);
                    if (r.StatusCode == HttpStatusCode.Conflict)
                    {
                        Post(() => { LastSaveFailed = true; SaveConflict?.Invoke(); onDone?.Invoke(false); });
                        return;
                    }
                    r.EnsureSuccessStatusCode();
                    CloudSaveDto dto = await r.Content.ReadFromJsonAsync<CloudSaveDto>(Json);
                    Post(() => { SaveVersion = dto.Version; LastSavedUtc = DateTime.UtcNow; LastSaveFailed = false; onDone?.Invoke(true); });
                }
                catch (Exception)
                {
                    Post(() => { LastSaveFailed = true; onDone?.Invoke(false); });
                }
                finally { Interlocked.Exchange(ref _uploading, 0); }
            });
        }

        /// <summary>Ilova yopilayotganda: oxirgi holatni sinxron (qisqa kutish bilan) yuboradi.</summary>
        public bool UploadSaveBlocking(string json, int timeoutMs = 4000)
        {
            if (State != CloudLoginState.SignedIn || string.IsNullOrEmpty(json)) return false;
            try
            {
                var task = Send(ServerUrl, HttpMethod.Put, "api/save",
                    JsonContent.Create(new CloudSaveUploadRequest { Json = json, BaseVersion = SaveVersion, Device = "exit" }, options: Json), Token);
                if (!task.Wait(timeoutMs) || !task.Result.IsSuccessStatusCode) return false;
                CloudSaveDto dto = task.Result.Content.ReadFromJsonAsync<CloudSaveDto>(Json).Result;
                SaveVersion = dto.Version;
                return true;
            }
            catch { return false; }
        }
    }
}
