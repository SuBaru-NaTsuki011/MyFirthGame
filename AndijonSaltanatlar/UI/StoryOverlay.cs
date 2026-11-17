using System.Collections.Generic;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Core.Graphics;
using AndijonSaltanatlar.Narrative;
using AndijonSaltanatlar.Quests;
using AndijonSaltanatlar.Achievements;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.UI
{
    /// <summary>
    /// O'yin ustidagi hikoya qatlami — PROMPT 13, 11, 15 & 16-bo'lim:
    ///  - Dialog paneli (personaj + matn + tanlovlar),
    ///  - Vazifa kuzatuvchisi (tracker HUD) — eng muhim faol vazifa,
    ///  - Yutuq bildirishnomasi (qisqa, oltin ramkali, navbat bilan).
    /// </summary>
    public sealed class StoryOverlay
    {
        /// <summary>O'ng paneldan qoldiriladigan joy (Game1 tanlov paneli ochiq bo'lsa o'rnatadi).</summary>
        public int RightInset { get; set; }

        private static readonly Color PanelBg = new Color(26, 22, 16, 245);
        private static readonly Color Border = new Color(150, 116, 66);
        private static readonly Color Gold = new Color(235, 200, 120);
        private static readonly Color Cream = new Color(234, 224, 202);
        private static readonly Color Muted = new Color(176, 160, 134);
        private static readonly Color Green = new Color(120, 214, 132);
        private static readonly Color Track = new Color(60, 58, 48);

        private readonly AssetManager _assets;
        private readonly PixelFont _font;
        private readonly GameStateManager _state;
        private readonly Texture2D _px;

        private readonly List<Rectangle> _choiceRects = new();
        private Rectangle _continueRect;

        // Missiya kartasi: X — yashirish, yashirilganda "MISSIYA" tugmasi, kartani bosish — MISSIYALAR ro'yxati
        private Rectangle _trackerBox, _trackerClose, _trackerPill;
        /// <summary>Kursor missiya kartasi/tugmasi ustida — orqadagi xaritaga bosish o'tmaydi.</summary>
        public bool PointerOverTracker { get; private set; }
        /// <summary>MISSIYALAR ro'yxatini ochish so'raldi (Game1 ochadi).</summary>
        public bool JournalRequested { get; set; }
        /// <summary>Katta oyna ochiq — missiya kartasi chizilmaydi va bosilmaydi.</summary>
        public bool HideTracker { get; set; }

        // yutuq bildirishnomasi
        private AchievementDefinition _toast;
        private float _toastTimer;

        public StoryOverlay(AssetManager assets, PixelFont font, GameStateManager state)
        {
            _assets = assets; _font = font; _state = state; _px = assets.Pixel;
        }

        private Story.StoryModeManager Story => _state.Story;
        public bool DialogueOpen => Story != null && Story.Dialogue != null && Story.Dialogue.IsOpen;

        public void Update(InputManager input, float dt, int vw, int vh)
        {
            // Bildirishnoma navbati (bir vaqtda bittadan — ko'payib ketmasligi uchun).
            if (_toast == null && Story != null && Story.Achievements.HasPendingNotification)
            {
                _toast = Story.Achievements.DequeueNotification();
                _toastTimer = 3.5f;
            }
            if (_toast != null)
            {
                _toastTimer -= dt;
                if (_toastTimer <= 0) _toast = null;
            }

            if (!DialogueOpen)
            {
                Point mp = input.MousePosition;
                PointerOverTracker = !HideTracker && (_trackerBox.Contains(mp) || _trackerPill.Contains(mp));
                if (!input.LeftPressed || !PointerOverTracker) return;
                if (_trackerClose.Contains(mp)) { TimePrefs.Current.QuestTrackerHidden = true; TimePrefs.Save(); }
                else if (_trackerPill.Contains(mp)) { TimePrefs.Current.QuestTrackerHidden = false; TimePrefs.Save(); }
                else JournalRequested = true;
                return;
            }
            PointerOverTracker = false;
            input.MouseOverUI = true; // dialog ochiq — orqadagi bosishlar o'tmaydi
            if (!input.LeftPressed) return;
            Point m = input.MousePosition;

            var d = Story.Dialogue.Current;
            if (d.Choices != null && d.Choices.Count > 0)
            {
                for (int i = 0; i < _choiceRects.Count; i++)
                    if (_choiceRects[i].Contains(m)) { Story.Dialogue.Choose(i); return; }
            }
            else if (_continueRect.Contains(m))
            {
                Story.Dialogue.Choose(0);
            }
        }

        public void Draw(SpriteBatch sb, int vw, int vh)
        {
            if (Story == null) return;
            if (DialogueOpen) DrawDialogue(sb, vw, vh);
            else if (!HideTracker) DrawTracker(sb, vw, vh);
            else _trackerBox = _trackerClose = _trackerPill = Rectangle.Empty;
            DrawToast(sb, vw, vh);
        }

        private void DrawTracker(SpriteBatch sb, int vw, int vh)
        {
            _trackerBox = _trackerClose = _trackerPill = Rectangle.Empty;
            var q = Story.Quests.TopTrackedQuest();
            if (q == null) return;
            var prog = Story.Quests.ProgressOf(q.QuestId);
            if (prog == null) return;

            // Yashirilgan: faqat kichik "MISSIYA" tugmasi (bosilsa karta qaytadi)
            if (TimePrefs.Current.QuestTrackerHidden)
            {
                _trackerPill = new Rectangle(vw - 16 - 132 - RightInset, 92, 132, 30);
                Panel(sb, _trackerPill, PanelBg, Gold, 1);
                Fill(sb, new Rectangle(_trackerPill.X + 10, _trackerPill.Y + 11, 8, 8), Gold);
                _font.Draw(sb, "MISSIYA", new Vector2(_trackerPill.X + 26, _trackerPill.Y + 8), Gold, 2, 1);
                return;
            }

            // O'ng tomonda tanlangan obyekt paneli ochiq bo'lsa, vazifa kartasi uning chap tomoniga suriladi (ustma-ust tushmaydi).
            // Vazifa nomi ikki qatorgacha o'raladi (so'z o'rtasida kesilmaydi); karta balandligi shunga moslashadi
            var titleLines = Wrap(q.TitleUz ?? "", 264, 2);
            if (titleLines.Count > 2) { titleLines = titleLines.GetRange(0, 2); titleLines[1] = Fit(titleLines[1], 20); }
            int extra = (System.Math.Max(1, titleLines.Count) - 1) * 20;
            var box = new Rectangle(vw - 300 - RightInset, 92, 284, 84 + extra);
            _trackerBox = box;
            Panel(sb, box, PanelBg, Border, 1);
            _font.Draw(sb, "JORIY MISSIYA", new Vector2(box.X + 10, box.Y + 8), Gold, 2, 1);
            _trackerClose = new Rectangle(box.Right - 28, box.Y + 4, 24, 22);
            Panel(sb, _trackerClose, new Color(40, 33, 26), Border, 1);
            _font.DrawCentered(sb, "X", _trackerClose.Center.ToVector2(), Cream, 2, 1);
            for (int li = 0; li < titleLines.Count; li++)
                _font.Draw(sb, titleLines[li], new Vector2(box.X + 10, box.Y + 28 + li * 20), Cream, 2, 1);

            if (q.Objectives.Count > 0)
            {
                var o0 = q.Objectives[0];
                int cur = System.Math.Min(prog.GetProgress(0), o0.TargetAmount);
                var bar = new Rectangle(box.X + 10, box.Y + 52 + extra, box.Width - 20, 20);
                Fill(sb, bar, Track);
                float p = o0.TargetAmount > 0 ? (float)cur / o0.TargetAmount : 1f;
                Fill(sb, new Rectangle(bar.X, bar.Y, (int)(bar.Width * Clamp01(p)), bar.Height), Green);
                string bt = cur + " / " + o0.TargetAmount + "  (bosing — ro'yxat)";
                if (_font.Measure(bt, 2, 1).X > bar.Width - 8) bt = cur + " / " + o0.TargetAmount;
                _font.DrawCentered(sb, bt, bar.Center.ToVector2(), new Color(20, 24, 20), 2, 1);
            }
        }

        private void DrawDialogue(SpriteBatch sb, int vw, int vh)
        {
            // Yarim shaffof fon
            Fill(sb, new Rectangle(0, 0, vw, vh), new Color(0, 0, 0, 120));

            var d = Story.Dialogue.Current;
            int panelH = 190;
            var box = new Rectangle(40, vh - panelH - 30, vw - 80, panelH);
            Panel(sb, box, PanelBg, Gold, 2);

            _font.Draw(sb, SpeakerName(d.Speaker), new Vector2(box.X + 18, box.Y + 14), Gold, 3, 1);

            int y = box.Y + 50;
            foreach (string line in Wrap(d.TextUz, box.Width - 40, 2))
            {
                _font.Draw(sb, line, new Vector2(box.X + 18, y), Cream, 2, 1);
                y += 18;
            }

            _choiceRects.Clear();
            if (d.Choices != null && d.Choices.Count > 0)
            {
                int by = box.Bottom - 12 - d.Choices.Count * 26;
                for (int i = 0; i < d.Choices.Count; i++)
                {
                    var r = new Rectangle(box.X + 18, by + i * 26, box.Width - 36, 22);
                    Panel(sb, r, new Color(44, 38, 28, 245), Border, 1);
                    _font.Draw(sb, "> " + d.Choices[i].TextUz, new Vector2(r.X + 10, r.Y + 4), Cream, 2, 1);
                    _choiceRects.Add(r);
                }
            }
            else
            {
                _continueRect = new Rectangle(box.Right - 180, box.Bottom - 34, 160, 24);
                Panel(sb, _continueRect, new Color(44, 38, 28, 245), Gold, 1);
                _font.DrawCentered(sb, "DAVOM ETISH", _continueRect.Center.ToVector2(), Gold, 2, 1);
            }
        }

        private void DrawToast(SpriteBatch sb, int vw, int vh)
        {
            if (_toast == null) return;
            float a = Clamp01(_toastTimer > 3f ? (3.5f - _toastTimer) / 0.5f : (_toastTimer < 0.6f ? _toastTimer / 0.6f : 1f));
            byte alpha = (byte)(a * 255);
            var box = new Rectangle(vw / 2 - 180, 80, 360, 60);
            Fill(sb, box, new Color((byte)26, (byte)22, (byte)16, (byte)(alpha * 0.96f)));
            var gold = new Color(Gold.R, Gold.G, Gold.B, alpha);
            Fill(sb, new Rectangle(box.X, box.Y, box.Width, 2), gold);
            Fill(sb, new Rectangle(box.X, box.Bottom - 2, box.Width, 2), gold);
            Fill(sb, new Rectangle(box.X, box.Y, 2, box.Height), gold);
            Fill(sb, new Rectangle(box.Right - 2, box.Y, 2, box.Height), gold);

            _font.DrawCentered(sb, "YUTUQ OCHILDI!", new Vector2(box.Center.X, box.Y + 16), gold, 2, 1);
            _font.DrawCentered(sb, _toast.TitleUz, new Vector2(box.Center.X, box.Y + 38), new Color(Cream.R, Cream.G, Cream.B, alpha), 2, 1);
        }

        private static string SpeakerName(Speaker s) => s switch
        {
            Speaker.Vazir => "DONISHMAND VAZIR",
            Speaker.Savdogar => "SAVDOGAR",
            Speaker.Memor => "ME'MOR",
            Speaker.Muallim => "MUALLIM",
            Speaker.Dehqon => "DEHQON",
            Speaker.Elchi => "ELCHI",
            _ => ""
        };

        private static float Clamp01(float v) => v < 0 ? 0 : (v > 1 ? 1 : v);
        private static string Fit(string s, int max) => s != null && s.Length > max ? s.Substring(0, max) : s ?? "";

        private void Fill(SpriteBatch sb, Rectangle r, Color c) => sb.Draw(_px, r, c);
        private void Panel(SpriteBatch sb, Rectangle r, Color bg, Color b, int t)
        {
            Fill(sb, r, bg);
            Fill(sb, new Rectangle(r.X, r.Y, r.Width, t), b);
            Fill(sb, new Rectangle(r.X, r.Bottom - t, r.Width, t), b);
            Fill(sb, new Rectangle(r.X, r.Y, t, r.Height), b);
            Fill(sb, new Rectangle(r.Right - t, r.Y, t, r.Height), b);
        }

        private List<string> Wrap(string text, int maxPx, int scale)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(text)) return lines;
            foreach (string word in text.Split(' '))
            {
                if (lines.Count == 0) { lines.Add(word); continue; }
                string test = lines[^1] + " " + word;
                if (_font.Measure(test, scale, 1).X > maxPx) lines.Add(word);
                else lines[^1] = test;
            }
            return lines;
        }
    }
}
