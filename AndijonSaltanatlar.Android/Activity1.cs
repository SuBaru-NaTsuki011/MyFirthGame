using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.AndroidApp
{
    /// <summary>
    /// Android kirish nuqtasi. Windows'dagi Program.cs o'rniga: o'sha Game1 ni ishga tushiradi.
    /// Telefonda o'yin darhol ONLINE panel bilan ochiladi (server manzili + login), chunki maqsad —
    /// kompyuterdagi o'yinchi bilan bitta dunyoda savdo qilish.
    /// </summary>
    [Activity(
        Label = "Andijon: Saltanatlar",
        MainLauncher = true,
        AlwaysRetainTaskState = true,
        LaunchMode = LaunchMode.SingleInstance,
        ScreenOrientation = ScreenOrientation.SensorLandscape,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize)]
    public class Activity1 : AndroidGameActivity
    {
        private Game1 _game;
        private View _view;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            _game = new Game1(new LaunchOptions { Online = true });
            _view = _game.Services.GetService(typeof(View)) as View;
            SetContentView(_view);
            _game.Run();
        }
    }
}
