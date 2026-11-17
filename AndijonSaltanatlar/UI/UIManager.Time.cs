using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Environment;
using AndijonSaltanatlar.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.UI
{
    /// <summary>
    /// Yuqori paneldagi soat ("08:30 | BAHOR | 3-KUN") va uni bosganda ochiladigan VAQT VA MUHIT oynasi:
    /// kun vaqtini qulflash (doim tong / kunduz / shom / tun yoki avto), ko'rinadigan fasl, kun uzunligi va effektlar sifati.
    /// Tanlovlar faqat shu qurilmadagi ko'rinishga ta'sir qiladi.
    /// </summary>
    public sealed partial class UIManager
    {
        public bool TimePanelOpen { get; private set; }
        private Rectangle _clockRect, _timeBox, _timeClose;
        private readonly List<(Rectangle r, int group, int value)> _timeOpts = new List<(Rectangle, int, int)>();

        private static readonly string[] SeasonOpts = { "AVTO", "BAHOR", "YOZ", "KUZ", "QISH" };
        private static readonly string[] LockOpts = { "AVTO", "TONG", "KUNDUZ", "SHOM", "TUN" };
        private static readonly string[] QualityOpts = { "PAST", "O'RTA", "YUQORI" };

        public void ToggleTimePanel() => TimePanelOpen = !TimePanelOpen;

        /// <summary>Soat matni: haqiqiy o'yin vaqti; ko'rinish qulflangan bo'lsa qulf nomi ham.</summary>
        private string ClockLabel(bool withDay)
        {
            string t = TimePrefs.ClockText(_state.Clock.DayFraction) + " | " + SeasonManager.Name(_state.Season).ToUpperInvariant();
            if (withDay) t += " | " + _state.Day + "-KUN";
            return t;
        }

        private void LayoutTimePanel(int vw, int vh)
        {
            _timeOpts.Clear();
            if (!TimePanelOpen) return;
            int w = Math.Min(vw - 40, 640), h = 400;
            _timeBox = new Rectangle(vw / 2 - w / 2, Math.Max(TopH + 8, vh / 2 - h / 2), w, h);
            int bh = Touch ? 36 : 30;
            _timeClose = new Rectangle(_timeBox.Right - 12 - bh, _timeBox.Y + 10, bh, bh);
            int x = _timeBox.X + 20, y = _timeBox.Y + 100, cw = w - 40, g = 6;

            void Row(int group, int n, int yy)
            {
                int bw = (cw - g * (n - 1)) / n;
                for (int i = 0; i < n; i++) _timeOpts.Add((new Rectangle(x + i * (bw + g), yy, bw, bh), group, i));
            }
            Row(0, LockOpts.Length, y);
            Row(1, SeasonOpts.Length, y + 74);
            Row(2, TimePrefs.DayPresets.Length, y + 148);
            Row(3, QualityOpts.Length, y + 222);
        }

        /// <summary>Soat yoki oyna bosildi. Oyna ochiq bo'lsa true (tashqi bosishlar e'tiborsiz).</summary>
        private bool ClickTime(Point m)
        {
            if (!TimePanelOpen)
            {
                if (_clockRect.Contains(m)) { TimePanelOpen = true; return true; }
                return false;
            }
            if (_timeClose.Contains(m) || !_timeBox.Contains(m)) { TimePanelOpen = false; return true; }
            TimePrefs p = TimePrefs.Current;
            foreach (var (r, group, value) in _timeOpts)
            {
                if (!r.Contains(m)) continue;
                switch (group)
                {
                    case 0: p.Lock = (TimeLock)value; break;
                    case 1: p.SeasonView = value - 1; break;
                    case 2: p.DayMinutes = TimePrefs.DayPresets[value]; break;
                    case 3: p.Quality = value; GraphicsQualityManager.Current = (GraphicsQuality)value; break;
                }
                TimePrefs.Save();
                return true;
            }
            return true;
        }

        private void DrawTimePanel(SpriteBatch sb, int vw, int vh)
        {
            if (!TimePanelOpen) return;
            Fill(sb, new Rectangle(0, 0, vw, vh), new Color(0, 0, 0, 140));
            Panel(sb, _timeBox, PanelBg, Gold, 2);
            int x = _timeBox.X + 20;
            _font.Draw(sb, "VAQT VA MUHIT", new Vector2(x, _timeBox.Y + 14), Gold, 3, 1);
            Panel(sb, _timeClose, PanelBg2, Border, 1);
            DrawButtonText(sb, "X", _timeClose, Cream);
            _font.Draw(sb, "Hozir: " + TimePrefs.ClockText(_state.Clock.DayFraction) + "  " + SeasonManager.Name(_state.Season) + ", " + _state.Day + "-kun  (" +
                           SeasonVisuals.ComfortName(SeasonVisuals.ComfortOf(_state.Season, _state.Weather.Current, _state.Clock.DayFraction)) + ")",
                       new Vector2(x, _timeBox.Y + 50), Cream, 2, 1);

            TimePrefs p = TimePrefs.Current;
            string[] titles = { "KO'RINADIGAN VAQT", "KO'RINADIGAN FASL", "1 KUN UZUNLIGI (REAL DAQIQA)", "EFFEKTLAR SIFATI" };
            int lastGroup = -1;
            foreach (var (r, group, value) in _timeOpts)
            {
                if (group != lastGroup)
                {
                    _font.Draw(sb, titles[group], new Vector2(x, r.Y - 22), Gold, 2, 1);
                    lastGroup = group;
                }
                bool on = group == 0 ? (int)p.Lock == value
                        : group == 1 ? p.SeasonView == value - 1
                        : group == 2 ? p.DayMinutes == TimePrefs.DayPresets[value]
                        : p.Quality == value;
                string label = group == 0 ? LockOpts[value] : group == 1 ? SeasonOpts[value] : group == 2 ? TimePrefs.DayPresets[value] + " DAQ" : QualityOpts[value];
                Panel(sb, r, on ? new Color(70, 54, 24, 245) : PanelBg2, on ? Gold : Border, on ? 2 : 1);
                DrawButtonText(sb, label, r, on ? Gold : Cream);
            }
            _font.Draw(sb, FitText("Tanlov faqat sizning ekraningizga ta'sir qiladi.", _timeBox.Width - 40), new Vector2(x, _timeBox.Bottom - 30), Muted, 2, 1);
        }
    }
}
