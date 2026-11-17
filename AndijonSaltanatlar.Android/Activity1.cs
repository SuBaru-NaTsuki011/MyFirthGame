using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.AndroidApp
{
    /// <summary>
    /// Android kirish nuqtasi. Windows'dagi Program.cs o'rniga: o'sha Game1 ni ishga tushiradi.
    /// O'yin "Google bilan kirish" ekrani bilan ochiladi; kirgandan so'ng saltanat serverdan qolgan joyidan yuklanadi.
    /// Google sahifasidagi "O'yinga qaytish" havolasi (uz.andijon.saltanatlar://auth) shu Activity'ni qayta ochadi.
    /// Ilova fonga o'tganda (OnPause) o'yin holati serverga yuboriladi.
    /// </summary>
    [Activity(
        Label = "Andijon: Saltanatlar",
        MainLauncher = true,
        AlwaysRetainTaskState = true,
        LaunchMode = LaunchMode.SingleInstance,
        ScreenOrientation = ScreenOrientation.SensorLandscape,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize)]
    [IntentFilter(new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "uz.andijon.saltanatlar", DataHost = "auth")]
    public class Activity1 : AndroidGameActivity
    {
        private Game1 _game;
        private View _view;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            _game = new Game1(new LaunchOptions());
            // "Google bilan kirish": telefondagi barcha Google akkauntlar ro'yxati (tizim oynasi).
            var picker = new GoogleAccountPicker(this);
            _game.NativeAccountPicker = picker.PickAsync;
            _view = _game.Services.GetService(typeof(View)) as View;
            SetContentView(_view);
            // O'yin paytida ekran o'chib qolmasin.
            Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
            HideSystemBars();
            _game.Run();
        }

        protected override void OnResume()
        {
            base.OnResume();
            HideSystemBars();
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            if (hasFocus) HideSystemBars();
        }

        /// <summary>To'liq ekran (immersive): status va navigatsiya panellari yashiriladi, chetdan surilsa vaqtincha chiqadi.</summary>
        private void HideSystemBars()
        {
            if (_view == null) return;
#pragma warning disable CS0618, CA1422 // SystemUiVisibility barcha Android versiyalarida (API 23+) ishlaydi
            _view.SystemUiVisibility = (StatusBarVisibility)(SystemUiFlags.ImmersiveSticky | SystemUiFlags.Fullscreen |
                SystemUiFlags.HideNavigation | SystemUiFlags.LayoutStable | SystemUiFlags.LayoutHideNavigation | SystemUiFlags.LayoutFullscreen);
#pragma warning restore CS0618, CA1422
        }

        protected override void OnPause()
        {
            _game?.SaveNowInBackground();
            base.OnPause();
        }
    }
}
