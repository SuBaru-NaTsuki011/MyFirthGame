using System;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using AndroidX.Credentials;
using Google.Android.Libraries.Identity.GoogleId;

namespace AndijonSaltanatlar.AndroidApp
{
    /// <summary>
    /// Android'ning tizim "Google bilan kirish" oynasi (Credential Manager): pastdan telefondagi BARCHA Google akkauntlar
    /// ro'yxati chiqadi, o'yinchi bittasini tanlaydi va Google ID token qaytadi. Token serverda imzosi bilan tekshiriladi.
    ///
    /// Ishlashi uchun Google Cloud'da: Web Client ID (serverClientId, server "Google:ClientId") va shu ilova uchun
    /// Android Client ID (paket nomi uz.andijon.saltanatlar + APK imzosining SHA-1) yaratilgan bo'lishi kerak.
    /// </summary>
    public sealed class GoogleAccountPicker
    {
        /// <summary>Foydalanuvchi oynani yopdi (o'yin oddiy holatga qaytadi, xato ko'rsatilmaydi).</summary>
        public sealed class CancelledException : System.OperationCanceledException
        {
            public CancelledException() : base("Bekor qilindi") { }
        }

        private readonly Activity _activity;

        public GoogleAccountPicker(Activity activity) { _activity = activity; }

        /// <summary>Akkaunt tanlash oynasini ko'rsatadi va tanlangan akkauntning Google ID tokenini qaytaradi.</summary>
        public Task<string> PickAsync(string webClientId)
        {
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _activity.RunOnUiThread(() =>
            {
                try
                {
                    // filterByAuthorizedAccounts(false): faqat avval kirgan emas, telefondagi barcha akkauntlar ko'rsatiladi.
                    GetGoogleIdOption option = new GetGoogleIdOption.Builder()
                        .SetFilterByAuthorizedAccounts(false)
                        .SetServerClientId(webClientId)
                        .SetAutoSelectEnabled(false)
                        .Build();
                    GetCredentialRequest request = new GetCredentialRequest.Builder()
                        .AddCredentialOption(option)
                        .Build();
                    ICredentialManager manager = CredentialManager.Create(_activity);
                    manager.GetCredentialAsync(_activity, request, new CancellationSignal(), new DirectExecutor(), new Callback(tcs));
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });
            return tcs.Task;
        }

        private sealed class DirectExecutor : Java.Lang.Object, Java.Util.Concurrent.IExecutor
        {
            public void Execute(Java.Lang.IRunnable command) => command.Run();
        }

        private sealed class Callback : Java.Lang.Object, ICredentialManagerCallback
        {
            private readonly TaskCompletionSource<string> _tcs;
            public Callback(TaskCompletionSource<string> tcs) { _tcs = tcs; }

            public void OnResult(Java.Lang.Object result)
            {
                try
                {
                    var response = (GetCredentialResponse)result;
                    Credential credential = response.Credential;
                    if (credential is CustomCredential custom && custom.Type == GoogleIdTokenCredential.TypeGoogleIdTokenCredential)
                    {
                        GoogleIdTokenCredential google = GoogleIdTokenCredential.CreateFrom(custom.Data);
                        _tcs.TrySetResult(google.IdToken);
                        return;
                    }
                    _tcs.TrySetException(new InvalidOperationException("Google akkaunti tanlanmadi"));
                }
                catch (Exception ex)
                {
                    _tcs.TrySetException(ex);
                }
            }

            public void OnError(Java.Lang.Object e)
            {
                // Xato Java istisnosi sifatida keladi: turini Java klass nomidan aniqlaymiz.
                string cls = e?.Class?.Name ?? "";
                string msg = "";
                try { msg = Java.Lang.Object.GetObject<Java.Lang.Throwable>(e.Handle, Android.Runtime.JniHandleOwnership.DoNotTransfer)?.Message ?? ""; } catch { }
                if (cls.EndsWith("GetCredentialCancellationException")) { _tcs.TrySetException(new CancelledException()); return; }
                if (cls.EndsWith("NoCredentialException")) { _tcs.TrySetException(new InvalidOperationException("Telefonda Google akkaunt topilmadi")); return; }
                _tcs.TrySetException(new InvalidOperationException(string.IsNullOrEmpty(msg) ? "Google oynasini ochib bo'lmadi" : msg));
            }
        }
    }
}
