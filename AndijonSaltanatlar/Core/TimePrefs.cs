using System;
using System.IO;
using System.Text.Json;
using AndijonSaltanatlar.Environment;

namespace AndijonSaltanatlar.Core
{
    /// <summary>Ko'rinadigan kun vaqti: AVTO — o'yin soati bo'yicha, qolganlari — doim shu payt.</summary>
    public enum TimeLock { Auto, Tong, Kunduz, Shom, Tun }

    /// <summary>
    /// VAQT VA MUHIT sozlamalari — shu qurilmaning shaxsiy ko'rinish tanlovi (onlayn o'yinda boshqa o'yinchilarga
    /// ta'sir qilmaydi). Faylda saqlanadi: AppPaths.Root/vaqt_muhit.json.
    /// </summary>
    public sealed class TimePrefs
    {
        /// <summary>Bir o'yin kuni necha real daqiqa (standart 60).</summary>
        public int DayMinutes { get; set; } = 60;
        public TimeLock Lock { get; set; } = TimeLock.Auto;
        /// <summary>Ko'rinadigan fasl: -1 — avto (haqiqiy fasl), 0..3 — Bahor/Yoz/Kuz/Qish.</summary>
        public int SeasonView { get; set; } = -1;
        /// <summary>Effektlar sifati: 0 past, 1 o'rta, 2 yuqori.</summary>
        public int Quality { get; set; } = 1;
        /// <summary>Yon tomondagi missiya kartasi yashirilgan (o'rnida kichik "MISSIYA" tugmasi qoladi).</summary>
        public bool QuestTrackerHidden { get; set; }

        public static readonly int[] DayPresets = { 10, 30, 60, 120 };

        public static TimePrefs Current { get; private set; } = new TimePrefs();

        /// <summary>Sozlama o'zgargach true — Game1 bir marta qo'llaydi.</summary>
        public static bool Dirty { get; set; } = true;

        private static string PathOf() => System.IO.Path.Combine(AppPaths.Root, "vaqt_muhit.json");

        public static void Load()
        {
            try
            {
                string p = PathOf();
                if (File.Exists(p)) Current = JsonSerializer.Deserialize<TimePrefs>(File.ReadAllText(p)) ?? new TimePrefs();
            }
            catch { Current = new TimePrefs(); }
            Current.DayMinutes = Math.Clamp(Current.DayMinutes, 1, 240);
            Current.SeasonView = Math.Clamp(Current.SeasonView, -1, 3);
            Current.Quality = Math.Clamp(Current.Quality, 0, 2);
            Dirty = true;
        }

        public static void Save()
        {
            Dirty = true;
            try
            {
                Directory.CreateDirectory(AppPaths.Root);
                File.WriteAllText(PathOf(), JsonSerializer.Serialize(Current));
            }
            catch { /* sozlama saqlanmasa ham o'yin davom etadi */ }
        }

        /// <summary>Qulflangan payt uchun kun ulushi (0 — yarim tun).</summary>
        public static float LockedFraction(TimeLock l)
        {
            switch (l)
            {
                case TimeLock.Tong: return 0.27f;
                case TimeLock.Kunduz: return 0.46f;
                case TimeLock.Shom: return 0.70f;
                case TimeLock.Tun: return 0.93f;
                default: return -1f;
            }
        }

        public static string LockName(TimeLock l)
        {
            switch (l)
            {
                case TimeLock.Tong: return "Doim tong";
                case TimeLock.Kunduz: return "Doim kunduz";
                case TimeLock.Shom: return "Doim shom";
                case TimeLock.Tun: return "Doim tun";
                default: return "Avto";
            }
        }

        /// <summary>Ko'rinish uchun kun ulushi: qulf bo'lsa — qulf, aks holda o'yin soati.</summary>
        public static float VisualFraction(float clockFraction)
        {
            float f = LockedFraction(Current.Lock);
            return f >= 0f ? f : clockFraction;
        }

        public static Season VisualSeason(Season real) => Current.SeasonView >= 0 ? (Season)Current.SeasonView : real;

        /// <summary>"08:30" — kun ulushidan soat.</summary>
        public static string ClockText(float dayFraction)
        {
            int minutes = (int)(Math.Clamp(dayFraction, 0f, 0.9999f) * 24 * 60);
            return (minutes / 60).ToString("00") + ":" + (minutes % 60 / 10 * 10).ToString("00");
        }
    }
}
