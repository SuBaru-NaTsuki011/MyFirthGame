namespace AndijonSaltanatlar.Core
{
    /// <summary>Matnni tizim buferiga (clipboard) nusxalash — Windows (SDL2) va Android uchun. Imkonsiz bo'lsa false.</summary>
    public static class Clipboard
    {
        public static bool TrySet(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            try
            {
#if ANDROID
                var ctx = Android.App.Application.Context;
                var cm = (Android.Content.ClipboardManager)ctx.GetSystemService(Android.Content.Context.ClipboardService);
                if (cm == null) return false;
                cm.PrimaryClip = Android.Content.ClipData.NewPlainText("havola", text);
                return true;
#else
                return SDL_SetClipboardText(text) == 0;
#endif
            }
            catch
            {
                return false;
            }
        }

#if !ANDROID
        [System.Runtime.InteropServices.DllImport("SDL2.dll",
            CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl,
            CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
        private static extern int SDL_SetClipboardText(string text);
#endif
    }
}
