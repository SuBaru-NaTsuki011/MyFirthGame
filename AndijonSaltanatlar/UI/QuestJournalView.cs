using System.Collections.Generic;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Core.Graphics;
using AndijonSaltanatlar.Quests;
using AndijonSaltanatlar.Achievements;
using AndijonSaltanatlar.LandExpansion;
using AndijonSaltanatlar.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.UI
{
    /// <summary>
    /// TOPSHIRIQLAR ekrani (Quest Journal) — PROMPT 13, 15-bo'lim. Bo'limlar: Asosiy hikoya, Qo'shimcha,
    /// Kunlik, Yutuqlar, Kampaniyalar. Har bir kartada nom, tavsif, progress, mukofot va tugmalar:
    /// "MUKOFOTNI OL" (bajarilgan vazifa) va "XARITADA KO'RSAT" (yer vazifalari). J tugmasi bilan ochiladi.
    /// </summary>
    public sealed class QuestJournalView
    {
        private enum Section { Main, Side, Daily, Achievements, Campaigns }

        private static readonly Color Bg = new Color(18, 20, 28);
        private static readonly Color PanelBg = new Color(30, 25, 20, 240);
        private static readonly Color PanelBg2 = new Color(50, 42, 33, 245);
        private static readonly Color Border = new Color(150, 116, 66);
        private static readonly Color Gold = new Color(235, 200, 120);
        private static readonly Color Cream = new Color(234, 224, 202);
        private static readonly Color Muted = new Color(176, 160, 134);
        private static readonly Color Green = new Color(120, 214, 132);
        private static readonly Color Red = new Color(236, 112, 96);
        private static readonly Color Track = new Color(60, 58, 48);

        private readonly AssetManager _assets;
        private readonly PixelFont _font;
        private readonly GameStateManager _state;
        private readonly Texture2D _px;

        public bool BackRequested;
        public bool FocusMapRequested;
        public Vector2 FocusTarget;

        private Section _section = Section.Main;
        private Rectangle _backBtn;
        private readonly Rectangle[] _tabBtns = new Rectangle[5];
        private readonly List<(string questId, Rectangle rect)> _claimBtns = new();
        private readonly List<(string questId, Rectangle rect)> _mapBtns = new();

        private int _scroll;

        public QuestJournalView(AssetManager assets, PixelFont font, GameStateManager state)
        {
            _assets = assets; _font = font; _state = state; _px = assets.Pixel;
        }

        private Story.StoryModeManager Story => _state.Story;

        private void Layout(int vw, int vh)
        {
            _backBtn = new Rectangle(vw - 226, 14, 210, 30);
            int tx = 16, ty = 54, tw = (vw - 32 - 4 * 8) / 5, th = 30;
            for (int i = 0; i < 5; i++) _tabBtns[i] = new Rectangle(tx + i * (tw + 8), ty, tw, th);
        }

        public void Update(InputManager input, int vw, int vh)
        {
            Layout(vw, vh);
            input.MouseOverUI = true;

            int sc = input.WheelDelta;
            if (sc != 0) _scroll = System.Math.Max(0, _scroll - sc / 4);

            if (!input.LeftPressed) return;
            Point m = input.MousePosition;

            if (_backBtn.Contains(m)) { BackRequested = true; return; }
            for (int i = 0; i < 5; i++)
                if (_tabBtns[i].Contains(m)) { _section = (Section)i; _scroll = 0; return; }

            foreach (var (qid, r) in _claimBtns)
                if (r.Contains(m)) { Story?.ClaimReward(qid); return; }

            foreach (var (qid, r) in _mapBtns)
                if (r.Contains(m)) { FocusLandForQuest(qid); return; }
        }

        private void FocusLandForQuest(string questId)
        {
            // Mos uchastkani topamiz; bo'lmasa noto'g'ri joyga yo'naltirmaymiz (PROMPT 13, 15-bo'lim).
            LandParcel target = null;
            foreach (var p in _state.Land.Parcels)
                if (p.State == LandParcelState.Unlockable) { target = p; break; }
            if (target == null)
                foreach (var p in _state.Land.Parcels)
                    if (p.State == LandParcelState.Locked) { target = p; break; }

            if (target == null)
            {
                _state.ShowMessage("Hozircha ochish mumkin bo'lgan yer yo'q — aholi yoki xazinani oshiring", true);
                return;
            }
            var c = target.GridBounds.Center;
            FocusTarget = IsoMath.TileToWorld(new Vector2(c.X + 0.5f, c.Y + 0.5f));
            FocusMapRequested = true;
            BackRequested = true;
        }

        public void Draw(SpriteBatch sb, int vw, int vh)
        {
            Layout(vw, vh);
            Fill(sb, new Rectangle(0, 0, vw, vh), Bg);
            _font.Draw(sb, "MISSIYALAR", new Vector2(16, 14), Gold, 3, 1);
            Panel(sb, _backBtn, PanelBg, Border, 1);
            _font.DrawCentered(sb, "SHAHARGA QAYTISH", _backBtn.Center.ToVector2(), Cream, 2, 1);

            string[] names = { "ASOSIY HIKOYA", "QO'SHIMCHA", "KUNLIK", "YUTUQLAR", "KAMPANIYALAR" };
            for (int i = 0; i < 5; i++)
            {
                bool on = (int)_section == i;
                Panel(sb, _tabBtns[i], on ? PanelBg2 : PanelBg, on ? Gold : Border, on ? 2 : 1);
                _font.DrawCentered(sb, names[i], _tabBtns[i].Center.ToVector2(), on ? Gold : Cream, 2, 1);
            }

            _claimBtns.Clear();
            _mapBtns.Clear();
            var area = new Rectangle(16, 94, vw - 32, vh - 94 - 16);
            Panel(sb, area, PanelBg, Border, 2);

            if (Story == null)
            {
                _font.DrawCentered(sb, "Story tizimi hali ishga tushmagan.", area.Center.ToVector2(), Muted, 2, 1);
                return;
            }

            if (_section == Section.Achievements) DrawAchievements(sb, area);
            else if (_section == Section.Campaigns) DrawCampaigns(sb, area);
            else DrawQuests(sb, area, SectionCategory());
        }

        private QuestCategory SectionCategory() => _section switch
        {
            Section.Side => QuestCategory.SideQuest,
            Section.Daily => QuestCategory.Daily,
            _ => QuestCategory.MainStory
        };

        private void DrawQuests(SpriteBatch sb, Rectangle area, QuestCategory cat)
        {
            int x = area.X + 16, y = area.Y + 14 - _scroll;
            bool any = false;
            foreach (var def in Story.Quests.Catalog)
            {
                if (def.Category != cat) continue;
                var prog = Story.Quests.ProgressOf(def.QuestId);
                if (prog == null || prog.Status == QuestStatus.Locked) continue; // qulflangan vazifa ko'rsatilmaydi
                any = true;

                var card = new Rectangle(x, y, area.Width - 32, 92);
                if (card.Bottom > area.Y && card.Y < area.Bottom)
                {
                    Panel(sb, card, PanelBg2, Border, 1);
                    _font.Draw(sb, def.TitleUz, new Vector2(card.X + 12, card.Y + 10), Gold, 2, 1);
                    string st = StatusText(prog.Status);
                    _font.DrawRight(sb, st, new Vector2(card.Right - 12, card.Y + 10), StatusColor(prog.Status), 2, 1);
                    _font.Draw(sb, Fit(def.DescriptionUz, 72), new Vector2(card.X + 12, card.Y + 30), Cream, 2, 1);

                    // Progress (birinchi objective)
                    if (def.Objectives.Count > 0)
                    {
                        var o0 = def.Objectives[0];
                        int cur = System.Math.Min(prog.GetProgress(0), o0.TargetAmount);
                        var bar = new Rectangle(card.X + 12, card.Y + 50, 260, 14);
                        Fill(sb, bar, Track);
                        float p = o0.TargetAmount > 0 ? (float)cur / o0.TargetAmount : 1f;
                        Fill(sb, new Rectangle(bar.X, bar.Y, (int)(bar.Width * Clamp01(p)), bar.Height), Green);
                        _font.DrawCentered(sb, cur + " / " + o0.TargetAmount, bar.Center.ToVector2(), new Color(20, 24, 20), 2, 1);
                    }

                    _font.Draw(sb, "Mukofot: " + RewardText(def), new Vector2(card.X + 290, card.Y + 50), Muted, 2, 1);

                    // Tugmalar
                    if (prog.Status == QuestStatus.Completed)
                    {
                        var claim = new Rectangle(card.Right - 180, card.Y + 46, 160, 24);
                        Panel(sb, claim, new Color(40, 70, 40, 245), Green, 2);
                        _font.DrawCentered(sb, "MUKOFOTNI OL", claim.Center.ToVector2(), Green, 2, 1);
                        _claimBtns.Add((def.QuestId, claim));
                    }
                    else if (HasLandObjective(def) && prog.Status == QuestStatus.Active)
                    {
                        var map = new Rectangle(card.Right - 180, card.Y + 46, 160, 24);
                        Panel(sb, map, PanelBg, Gold, 1);
                        _font.DrawCentered(sb, "XARITADA KO'RSAT", map.Center.ToVector2(), Gold, 2, 1);
                        _mapBtns.Add((def.QuestId, map));
                    }
                }
                y += 100;
            }
            if (!any)
                _font.DrawCentered(sb, "Bu bo'limda hozircha vazifa yo'q.", area.Center.ToVector2(), Muted, 2, 1);
        }

        private void DrawAchievements(SpriteBatch sb, Rectangle area)
        {
            int cols = 2, cw = (area.Width - 48) / cols, ch = 62, gx = area.X + 16, gy = area.Y + 14 - _scroll;
            int i = 0;
            foreach (var a in Story.Achievements.Catalog)
            {
                var prog = Story.Achievements.ProgressOf(a.Id);
                bool unlocked = prog != null && prog.IsUnlocked;
                if (a.IsHidden && !unlocked) { i++; continue; }

                int col = i % cols, row = i / cols;
                var card = new Rectangle(gx + col * (cw + 16), gy + row * (ch + 10), cw, ch);
                if (card.Bottom > area.Y && card.Y < area.Bottom)
                {
                    Panel(sb, card, unlocked ? new Color(44, 60, 40, 245) : PanelBg2, unlocked ? Green : Border, unlocked ? 2 : 1);
                    _font.Draw(sb, a.TitleUz, new Vector2(card.X + 12, card.Y + 8), unlocked ? Green : Cream, 2, 1);
                    _font.Draw(sb, Fit(a.DescriptionUz, 40), new Vector2(card.X + 12, card.Y + 26), Muted, 2, 1);
                    string tag = unlocked ? "OCHILGAN" : (prog != null ? prog.Progress + "/" + a.RequirementAmount : "0/" + a.RequirementAmount);
                    _font.DrawRight(sb, tag, new Vector2(card.Right - 12, card.Y + 42), unlocked ? Green : Muted, 2, 1);
                }
                i++;
            }
        }

        private void DrawCampaigns(SpriteBatch sb, Rectangle area)
        {
            int x = area.X + 16, y = area.Y + 16;
            var camp = Story.Campaign?.Active;
            if (camp == null)
            {
                _font.Draw(sb, "Hikoya rejimi: Asaka Saltanatining Yuksalishi", new Vector2(x, y), Gold, 2, 1); y += 24;
                _font.Draw(sb, "Erkin Saltanat rejimida o'ynayapsiz. Yangi o'yinni", new Vector2(x, y), Cream, 2, 1); y += 18;
                _font.Draw(sb, "Hikoya rejimida boshlab, kampaniyani o'ynashingiz mumkin.", new Vector2(x, y), Cream, 2, 1);
                return;
            }
            _font.Draw(sb, camp.NameUz, new Vector2(x, y), Gold, 3, 1); y += 30;
            _font.Draw(sb, "Davr: " + camp.HistoricalPeriod, new Vector2(x, y), Muted, 2, 1); y += 18;
            _font.Draw(sb, "Holat: " + VerifText(camp.VerificationStatus), new Vector2(x, y), Muted, 2, 1); y += 26;

            foreach (var ch in camp.NarrativeChapters)
            {
                bool done = ch.QuestIds.TrueForAll(qid =>
                {
                    var p = Story.Quests.ProgressOf(qid);
                    return p != null && (p.Status == QuestStatus.Completed || p.Status == QuestStatus.RewardClaimed);
                });
                bool current = Story.Campaign.CurrentChapter?.ChapterId == ch.ChapterId;
                Color c = done ? Green : current ? Gold : Muted;
                string mark = done ? "[+] " : current ? "[>] " : "[ ] ";
                _font.Draw(sb, mark + ch.TitleUz, new Vector2(x, y), c, 2, 1);
                y += 20;
            }
        }

        // ---- yordamchilar ----
        private bool HasLandObjective(QuestDefinition def)
        {
            foreach (var o in def.Objectives) if (o.Type == ObjectiveType.UnlockLand) return true;
            return false;
        }

        private static string StatusText(QuestStatus s) => s switch
        {
            QuestStatus.Available => "OCHIQ",
            QuestStatus.Active => "DAVOM ETMOQDA",
            QuestStatus.Completed => "BAJARILDI",
            QuestStatus.RewardClaimed => "MUKOFOT OLINDI",
            _ => ""
        };
        private Color StatusColor(QuestStatus s) => s switch
        {
            QuestStatus.Completed => Gold,
            QuestStatus.RewardClaimed => Green,
            QuestStatus.Active => Cream,
            _ => Muted
        };

        private static string RewardText(QuestDefinition def)
        {
            var parts = new List<string>();
            foreach (var r in def.Rewards)
            {
                string n = r.Kind switch
                {
                    RewardKind.Coin => r.Amount + " tanga",
                    RewardKind.Food => r.Amount + " oziq-ovqat",
                    RewardKind.Science => r.Amount + " ilm",
                    RewardKind.Culture => r.Amount + " madaniyat",
                    RewardKind.Experience => r.Amount + " XP",
                    RewardKind.Title => "unvon: " + r.RefKey,
                    RewardKind.Decoration => "dekor: " + r.RefKey,
                    _ => ""
                };
                if (n.Length > 0) parts.Add(n);
            }
            return parts.Count > 0 ? string.Join(", ", parts) : "-";
        }

        private static string VerifText(Campaigns.VerificationStatus s) => s switch
        {
            Campaigns.VerificationStatus.Verified => "Tasdiqlangan",
            Campaigns.VerificationStatus.NeedsHistoricalReview => "Tarixiy tekshiruv kutilmoqda",
            _ => "Badiiy (muqobil tarix)"
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
    }
}
