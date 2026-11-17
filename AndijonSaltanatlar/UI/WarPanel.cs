using System;
using System.Linq;
using AndijonSaltanatlar.Shared.DTOs;
using AndijonSaltanatlar.Shared.War;
using AndijonSaltanatlar.UI.Design;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.UI
{
    /// <summary>
    /// PROMPT 25 — "HARBIY BOSHQARUV" bo'limi (OnlinePanel ichida). Bo'limlar: QO'SHINLAR, SARKARDALAR, BIRLIKLAR,
    /// HARBIY BINOLAR, MOJAROLAR (AI saltanatlar + tinchlik), PvP, JANG TARIXI.
    /// Panel hech narsani o'zi hisoblamaydi: barcha raqamlar WarOverviewDto dan, har amal — server buyrug'i.
    /// </summary>
    public sealed partial class OnlinePanel
    {
        private static readonly string[] WarTabs = { "Xarita", "Qo'shin", "Sarkarda", "Birliklar", "Binolar", "Mojarolar", "PvP", "Tarix" };
        private int _warTab;
        private int _warArmy;
        private int _warTarget = -1;
        private int _recruitQty = 5;
        private int _pvpStake;
        private int _pvpTarget = -1;
        private bool _warRequested;
        private BattleScene _battleScene;
        private bool _battleOpen;
        private bool _openActiveBattle;

        /// <summary>Jang sahnasi ochiqmi (Game1 butun ekranni panelga beradi).</summary>
        public bool BattleOpen => _battleOpen && _client.Battle != null;

        /// <summary>Tugma eni: yorliq to'liq sig'adigan qilib (maksimum — mavjud joyning ulushi).</summary>
        private int BtnW(string label, int max) => Math.Min(max, (int)_ui.Type.Measure(label, TextStyle.Button).X + 36);

        private ArmyDto SelectedArmy(WarOverviewDto w)
            => w.Armies.FirstOrDefault(a => a.Id == _warArmy) ?? w.Armies.FirstOrDefault();

        private void DrawWar(Rectangle c)
        {
            UiKit ui = _ui; ThemeTokens T = ui.T;
            WarOverviewDto w = _client.War;
            if (w == null)
            {
                if (!_warRequested) { _warRequested = true; _client.LoadWar(); }
                ui.Paragraph("Harbiy ma'lumotlar yuklanmoqda…", c, TextStyle.Body);
                return;
            }
            if (_openActiveBattle && (w.ActiveBattle != null || w.History.Count > 0)) { _openActiveBattle = false; OpenBattle((w.ActiveBattle ?? w.History[0]).Id); }
            int h = ui.ControlH;
            // Donishmand Vazir maslahati (faqat statistikadan; qarorni o'yinchi qabul qiladi).
            string advice = w.AdviceUz.FirstOrDefault() ?? "";
            int advH = (int)(ui.Type.LineHeight(TextStyle.Secondary) * 2 + 14);
            var adv = new Rectangle(c.X, c.Y, c.Width, advH);
            ui.RoundedBorder(adv, Color.Lerp(T.Border, T.Accent, 0.4f), Color.Lerp(T.Surface, T.Accent, 0.07f), 8);
            ui.Paragraph("Vazir: " + advice, new Rectangle(adv.X + 12, adv.Y + 6, adv.Width - 24, advH - 8), TextStyle.Secondary, T.TextPrimary, 2);

            if (w.ActiveBattle != null)
            {
                var ab = new Rectangle(c.X, adv.Bottom + 6, c.Width, h);
                if (ui.Button(ab, "FAOL JANG: " + w.ActiveBattle.AttackerName + " — " + w.ActiveBattle.DefenderName + " · " + w.ActiveBattle.Round + "-raund · SAHNANI OCHISH", ButtonKind.Accent))
                    OpenBattle(w.ActiveBattle.Id);
                adv.Height += h + 6;
            }

            int tabY = adv.Bottom + 8;
            _warTab = ui.TabBar(new Rectangle(c.X, tabY, c.Width, h), WarTabs, _warTab);
            var body = new Rectangle(c.X, tabY + h + ui.Gap, c.Width, c.Bottom - tabY - h - ui.Gap);
            switch (_warTab)
            {
                case 0: DrawWarMap(body, w); break;
                case 1: DrawArmies(body, w); break;
                case 2: DrawCommanders(body, w); break;
                case 3: DrawUnits(body, w); break;
                case 4: DrawFacilities(body, w); break;
                case 5: DrawConflicts(body, w); break;
                case 6: DrawPvp(body, w); break;
                default: DrawBattleHistory(body, w); break;
            }
        }

        private void OpenBattle(int battleId)
        {
            _battleScene ??= new BattleScene(_assets);
            _battleOpen = true;
            _client.LoadBattle(battleId);
        }

        // ------------------------------------------------------------------ QO'SHINLAR
        private void DrawArmies(Rectangle c, WarOverviewDto w)
        {
            UiKit ui = _ui; ThemeTokens T = ui.T;
            int h = ui.ControlH, gap = ui.Gap;
            string cap = "Sig'im: " + w.UnitsTotal + " / " + w.Capacity + " · Qo'shinlar: " + w.Armies.Count + " / " + w.MaxArmies + " · Kunlik xarajat: " + w.DailyUpkeep + " tanga";
            int nb = BtnW("Yangi qo'shin", c.Width / 3);
            ui.TextFit(cap, new Rectangle(c.X, c.Y, c.Width - nb - 10, h), TextStyle.Secondary, T.TextSecondary);
            if (ui.Button(new Rectangle(c.Right - nb, c.Y, nb, h), "Yangi qo'shin", ButtonKind.Secondary, !_client.WarBusy && w.Armies.Count < w.MaxArmies, "Harbiy qarorgoh darajasi qo'shinlar sonini oshiradi"))
                _client.CreateArmy("");
            var list = new Rectangle(c.X, c.Y + h + gap, c.Width, c.Height - h - gap);
            int ch = (int)(ui.Type.LineHeight(TextStyle.PanelTitle) + ui.Type.LineHeight(TextStyle.Secondary) * 3 + 46 + h);
            int scroll = ui.Scroll("armies", list, w.Armies.Count * (ch + gap));
            for (int i = 0; i < w.Armies.Count; i++)
            {
                ArmyDto a = w.Armies[i];
                var r = new Rectangle(list.X, list.Y + i * (ch + gap) - scroll, list.Width - 8, ch);
                if (!UiKit.InView(r, list)) continue;
                bool sel = SelectedArmy(w)?.Id == a.Id;
                if (ui.Card(r, sel, true)) _warArmy = a.Id;
                int x = r.X + 16, y = r.Y + 12, cw = r.Width - 32;
                ui.TextFit(a.Name, new Rectangle(x, y, cw - 160, (int)ui.Type.LineHeight(TextStyle.PanelTitle)), TextStyle.PanelTitle);
                Color sc = a.Status == ArmyStatus.Available ? T.Success : a.Status == ArmyStatus.InBattle ? T.Danger : T.Warning;
                ui.Badge(new Vector2(r.Right - 16 - ui.Type.Measure(a.StatusUz, TextStyle.Caption).X - 20, y + 2), a.StatusUz, sc);
                y += (int)ui.Type.LineHeight(TextStyle.PanelTitle) + 6;
                ui.TextFit("Sarkarda: " + a.CommanderName + (a.CommanderLevel > 0 ? " (" + WarRules.CommanderRank(a.CommanderLevel) + ")" : "") + " · Hudud: " + a.RegionName,
                    new Rectangle(x, y, cw, (int)ui.Type.LineHeight(TextStyle.Secondary)), TextStyle.Secondary, T.TextSecondary);
                y += (int)ui.Type.LineHeight(TextStyle.Secondary) + 4;
                string groups = a.Groups.Count == 0 ? "Birlik yo'q" : string.Join(" · ", a.Groups.Select(g => g.NameUz + " " + g.Count));
                ui.TextFit(groups, new Rectangle(x, y, cw, (int)ui.Type.LineHeight(TextStyle.Secondary)), TextStyle.Secondary, T.TextPrimary);
                y += (int)ui.Type.LineHeight(TextStyle.Secondary) + 8;
                int third = (cw - 2 * gap) / 3;
                Stat(new Rectangle(x, y, third, h), "Kuch", a.Strength.ToString(), 1f, T.Primary);
                Stat(new Rectangle(x + third + gap, y, third, h), "Ruh", a.Morale + "%", a.Morale / 100f, new Color(240, 200, 110));
                Stat(new Rectangle(x + 2 * (third + gap), y, third, h), "Tayyorgarlik", a.Readiness + "%", a.Readiness / 100f, T.Success);
                y += h + 8;
                ui.TextFit(a.UnitCount + " birlik · xarajat " + a.Upkeep + "/kun · " + a.ActiveTaskUz, new Rectangle(x, y, cw, (int)ui.Type.LineHeight(TextStyle.Secondary)), TextStyle.Caption, T.TextSecondary);
            }
        }

        private void Stat(Rectangle r, string label, string value, float v, Color c)
        {
            UiKit ui = _ui;
            ui.TextFit(label + ": " + value, new Rectangle(r.X, r.Y, r.Width, r.Height - 10), TextStyle.Secondary, ui.T.TextPrimary);
            ui.ProgressBar(new Rectangle(r.X, r.Bottom - 8, r.Width, 6), v, c);
        }

        // ------------------------------------------------------------------ SARKARDALAR
        private void DrawCommanders(Rectangle c, WarOverviewDto w)
        {
            UiKit ui = _ui; ThemeTokens T = ui.T;
            int h = ui.ControlH, gap = ui.Gap;
            ArmyDto army = SelectedArmy(w);
            string hire = "Sarkarda yollash · " + WarRules.CommanderHireCost;
            int hb = BtnW(hire, c.Width / 2);
            ui.TextFit("Tanlangan qo'shin: " + (army?.Name ?? "-"), new Rectangle(c.X, c.Y, c.Width - hb - 10, h), TextStyle.Secondary, T.TextSecondary);
            if (ui.Button(new Rectangle(c.Right - hb, c.Y, hb, h), hire, ButtonKind.Secondary, !_client.WarBusy && w.Commanders.Count < WarRules.MaxCommanders))
                _client.HireCommander();
            var list = new Rectangle(c.X, c.Y + h + gap, c.Width, c.Height - h - gap);
            int cols = list.Width >= 900 ? 3 : list.Width >= 560 ? 2 : 1;
            int cw = (list.Width - (cols - 1) * gap - 8) / cols;
            int ch = (int)(ui.Type.LineHeight(TextStyle.PanelTitle) + ui.Type.LineHeight(TextStyle.Secondary) * 4 + 40 + h);
            int rows = (w.Commanders.Count + cols - 1) / cols;
            int scroll = ui.Scroll("cmds", list, rows * (ch + gap));
            for (int i = 0; i < w.Commanders.Count; i++)
            {
                CommanderDto cmd = w.Commanders[i];
                var r = new Rectangle(list.X + (i % cols) * (cw + gap), list.Y + (i / cols) * (ch + gap) - scroll, cw, ch);
                if (!UiKit.InView(r, list)) continue;
                ui.Card(r);
                int ps = (int)(ui.Type.LineHeight(TextStyle.PanelTitle) * 2.2f);
                var portrait = new Rectangle(r.X + 14, r.Y + 14, ps, ps);
                Color pc = new[] { new Color(70, 120, 200), new Color(190, 90, 70), new Color(80, 150, 100), new Color(170, 130, 60), new Color(130, 90, 170), new Color(60, 140, 150) }[cmd.Portrait % 6];
                ui.Rounded(portrait, pc, ps / 2);
                ui.Rounded(new Rectangle(portrait.X + ps / 4, portrait.Y + ps / 6, ps / 2, ps / 2), Color.Lerp(pc, Color.White, 0.55f), ps / 4);
                ui.Rounded(new Rectangle(portrait.X + ps / 6, portrait.Y + ps * 2 / 3, ps * 2 / 3, ps / 3), Color.Lerp(pc, Color.Black, 0.2f), ps / 6);
                int x = portrait.Right + 12, tw = r.Right - 14 - x, y = r.Y + 12;
                ui.TextFit(cmd.Name, new Rectangle(x, y, tw, (int)ui.Type.LineHeight(TextStyle.PanelTitle)), TextStyle.PanelTitle); y += (int)ui.Type.LineHeight(TextStyle.PanelTitle) + 2;
                ui.TextFit(cmd.RankUz + " · " + new string('★', cmd.Level) + new string('☆', 3 - cmd.Level), new Rectangle(x, y, tw, (int)ui.Type.LineHeight(TextStyle.Secondary)), TextStyle.Secondary, T.Accent); y += (int)ui.Type.LineHeight(TextStyle.Secondary) + 6;
                if (cmd.NextLevelXp > 0) ui.ProgressBar(new Rectangle(x, y, tw, 6), cmd.Experience / (float)cmd.NextLevelXp, T.Accent);
                y = Math.Max(y + 14, portrait.Bottom + 10);
                int lx = r.X + 14, lw = r.Width - 28;
                ui.TextFit("Qo'shin kuchi +" + cmd.PowerBonusPct + "% · Ruh +" + cmd.MoraleBonus + " · Boshqaruv +" + cmd.AdministrationBonus, new Rectangle(lx, y, lw, (int)ui.Type.LineHeight(TextStyle.Secondary)), TextStyle.Secondary, T.TextPrimary);
                y += (int)ui.Type.LineHeight(TextStyle.Secondary) + 4;
                string assigned = w.Armies.FirstOrDefault(a => a.Id == cmd.AssignedArmyId)?.Name ?? "Tayinlanmagan";
                ui.TextFit("Qo'shin: " + assigned + (cmd.NextLevelXp > 0 ? " · Tajriba " + cmd.Experience + "/" + cmd.NextLevelXp : ""), new Rectangle(lx, y, lw, (int)ui.Type.LineHeight(TextStyle.Secondary)), TextStyle.Secondary, T.TextSecondary);
                bool can = army != null && cmd.AssignedArmyId != army.Id && army.Status != ArmyStatus.InBattle;
                if (ui.Button(new Rectangle(lx, r.Bottom - h - 12, lw, h), army == null ? "Qo'shin yo'q" : cmd.AssignedArmyId == army.Id ? "Shu qo'shinga tayinlangan" : army.Name + "ga tayinlash", ButtonKind.Secondary, can && !_client.WarBusy))
                    _client.AssignCommander(cmd.Id, army.Id);
            }
        }

        // ------------------------------------------------------------------ BIRLIKLAR
        private void DrawUnits(Rectangle c, WarOverviewDto w)
        {
            UiKit ui = _ui; ThemeTokens T = ui.T;
            int h = ui.ControlH, gap = ui.Gap;
            ArmyDto army = SelectedArmy(w);
            if (army == null) { ui.Paragraph("Qo'shin yo'q.", c, TextStyle.Body); return; }
            int levelOf(FacilityKind k) => w.Facilities.FirstOrDefault(f => f.Kind == k)?.Level ?? 0;

            // Miqdor tanlash + navbat
            int ql = (int)ui.Type.Measure("Miqdor:", TextStyle.Secondary).X + 12;
            ui.TextFit("Miqdor:", new Rectangle(c.X, c.Y, ql, h), TextStyle.Secondary, T.TextSecondary);
            int[] qtys = { 1, 5, 10, 20 };
            int qw = Math.Max(48, Math.Min((int)ui.Type.Measure("20", TextStyle.Secondary).X + 40, (c.Width - ql) / 4 - gap));
            for (int i = 0; i < qtys.Length; i++)
                if (ui.Chip(new Rectangle(c.X + ql + i * (qw + gap), c.Y + 4, qw, h - 8), qtys[i].ToString(), _recruitQty == qtys[i])) _recruitQty = qtys[i];
            ui.TextFit(army.Name, new Rectangle(c.X + ql + 4 * (qw + gap) + 10, c.Y, Math.Max(0, c.Right - (c.X + ql + 4 * (qw + gap) + 10)), h), TextStyle.Secondary, T.TextSecondary);
            int y = c.Y + h + 4;
            if (w.Queue.Count > 0)
            {
                string q = "Navbatda: " + string.Join(" · ", w.Queue.Select(o => o.NameUz + " ×" + o.Count + " (" + o.SecondsLeft + " s)"));
                ui.TextFit(q, new Rectangle(c.X, y, c.Width, (int)ui.Type.LineHeight(TextStyle.Secondary)), TextStyle.Secondary, T.Accent);
                y += (int)ui.Type.LineHeight(TextStyle.Secondary) + 4;
            }
            var list = new Rectangle(c.X, y + gap, c.Width, c.Bottom - y - gap);
            int rh = Math.Max(h + 20, (int)(ui.Type.LineHeight(TextStyle.Button) + ui.Type.LineHeight(TextStyle.Caption) + 24));
            var units = WarRules.Units;
            int scroll = ui.Scroll("units", list, units.Count * (rh + 6));
            for (int i = 0; i < units.Count; i++)
            {
                UnitDefinition u = units[i];
                var r = new Rectangle(list.X, list.Y + i * (rh + 6) - scroll, list.Width - 8, rh);
                if (!UiKit.InView(r, list)) continue;
                bool unlocked = WarRules.Unlocked(u, levelOf);
                ui.Card(r);
                int have = army.Groups.Where(g => g.Line == u.Line && g.Level == u.Level).Sum(g => g.Count);
                int btnW = BtnW("Oshirish ×20 · 2200", r.Width * 2 / 5);
                int tx = r.X + 14, tw = r.Width - btnW - 40;
                string title = u.NameUz + " · " + u.Level + "-daraja" + (have > 0 ? " · bor: " + have : "");
                ui.TextFit(title, new Rectangle(tx, r.Y + 8, tw, (int)ui.Type.LineHeight(TextStyle.Button)), TextStyle.Button, unlocked ? T.TextPrimary : T.TextSecondary);
                string info = u.IsSupport ? (u.Line == UnitLine.Aravasi ? "Ruhni oshiradi (har biri +2, maks. 15)" : "Yo'qotishlarni kamaytiradi (maks. 10%)") : "Kuch " + u.Power + " · Chidamlilik " + u.Resilience;
                info += " · xarajat " + u.Upkeep + "/kun" + (unlocked ? "" : " · Kerak: " + WarRules.RequirementText(u));
                ui.TextFit(info, new Rectangle(tx, r.Y + 10 + (int)ui.Type.LineHeight(TextStyle.Button), tw, (int)ui.Type.LineHeight(TextStyle.Caption)), TextStyle.Caption, unlocked ? T.TextSecondary : T.Warning);
                var br = new Rectangle(r.Right - btnW - 12, r.Y + (rh - h) / 2, btnW, h);
                if (u.Level == 1)
                {
                    if (ui.Button(br, "Yollash ×" + _recruitQty + " · " + u.Cost * _recruitQty, ButtonKind.Primary, unlocked && !_client.WarBusy, "Tanga bir marta yechiladi, birliklar navbatdan keyin qo'shiladi"))
                        _client.Recruit(army.Id, u.Line, _recruitQty);
                }
                else
                {
                    int from = army.Groups.Where(g => g.Line == u.Line && g.Level == u.Level - 1).Sum(g => g.Count);
                    int n = Math.Min(from, _recruitQty);
                    if (ui.Button(br, n > 0 ? "Oshirish ×" + n + " · " + u.Cost * n : "Birlik yo'q", ButtonKind.Secondary, unlocked && n > 0 && !_client.WarBusy))
                        _client.UpgradeUnits(army.Id, u.Line, u.Level - 1, n);
                }
            }
        }

        // ------------------------------------------------------------------ HARBIY BINOLAR
        private void DrawFacilities(Rectangle c, WarOverviewDto w)
        {
            UiKit ui = _ui; ThemeTokens T = ui.T;
            int h = ui.ControlH, gap = ui.Gap;
            int cols = c.Width >= 900 ? 3 : c.Width >= 560 ? 2 : 1;
            int cw = (c.Width - (cols - 1) * gap - 8) / cols;
            int ch = (int)(ui.Type.LineHeight(TextStyle.PanelTitle) + ui.Type.LineHeight(TextStyle.Secondary) * 3 + 36 + h);
            int rows = (w.Facilities.Count + cols - 1) / cols;
            int scroll = ui.Scroll("facilities", c, rows * (ch + gap));
            for (int i = 0; i < w.Facilities.Count; i++)
            {
                FacilityDto f = w.Facilities[i];
                var r = new Rectangle(c.X + (i % cols) * (cw + gap), c.Y + (i / cols) * (ch + gap) - scroll, cw, ch);
                if (!UiKit.InView(r, c)) continue;
                ui.Card(r);
                int x = r.X + 14, tw = r.Width - 28, y = r.Y + 12;
                ui.TextFit(f.NameUz, new Rectangle(x, y, tw, (int)ui.Type.LineHeight(TextStyle.PanelTitle)), TextStyle.PanelTitle); y += (int)ui.Type.LineHeight(TextStyle.PanelTitle) + 4;
                ui.TextFit("Daraja: " + f.Level + " / " + f.MaxLevel + (f.Upkeep > 0 ? " · xarajat " + f.Upkeep + "/kun" : ""), new Rectangle(x, y, tw, (int)ui.Type.LineHeight(TextStyle.Secondary)), TextStyle.Secondary, f.Level > 0 ? T.Success : T.TextSecondary);
                y += (int)ui.Type.LineHeight(TextStyle.Secondary) + 4;
                ui.Paragraph(f.EffectUz, new Rectangle(x, y, tw, (int)(ui.Type.LineHeight(TextStyle.Secondary) * 2 + 4)), TextStyle.Secondary, T.TextSecondary, 2);
                bool max = f.Level >= f.MaxLevel;
                if (ui.Button(new Rectangle(x, r.Bottom - h - 12, tw, h), max ? "Eng yuqori daraja" : (f.Level == 0 ? "Qurish · " : "Rivojlantirish · ") + f.UpgradeCost, ButtonKind.Primary, !max && !_client.WarBusy))
                    _client.BuildFacility(f.Kind);
            }
        }

        // ------------------------------------------------------------------ MOJAROLAR (AI) + TINCHLIK
        private void DrawConflicts(Rectangle c, WarOverviewDto w)
        {
            UiKit ui = _ui; ThemeTokens T = ui.T;
            int h = ui.ControlH, gap = ui.Gap;
            bool side = c.Width >= 820;
            var listR = side ? new Rectangle(c.X, c.Y, c.Width * 52 / 100, c.Height) : new Rectangle(c.X, c.Y, c.Width, c.Height / 2 - gap);
            var info = side ? new Rectangle(listR.Right + gap, c.Y, c.Right - listR.Right - gap, c.Height) : new Rectangle(c.X, listR.Bottom + gap, c.Width, c.Bottom - listR.Bottom - gap);

            int rh = (int)(ui.Type.LineHeight(TextStyle.Button) + ui.Type.LineHeight(TextStyle.Caption) + 22);
            int peaceRows = w.Conflicts.Count;
            int total = w.Targets.Count + peaceRows;
            int scroll = ui.Scroll("targets", listR, total * (rh + 6));
            int idx = 0;
            ArmyDto army = SelectedArmy(w);
            foreach (ConflictDto cf in w.Conflicts)
            {
                var r = new Rectangle(listR.X, listR.Y + idx++ * (rh + 6) - scroll, listR.Width - 8, rh);
                if (!UiKit.InView(r, listR)) continue;
                ui.Card(r);
                string st = cf.Active ? "Faol mojaro · munosabat " + cf.Relation : "Sulh: " + cf.TruceMinutesLeft + " daqiqa qoldi";
                int pb = BtnW("Tinchlik taklifi", r.Width / 2);
                ui.TextFit(cf.KingdomName, new Rectangle(r.X + 12, r.Y + 8, r.Width - pb - 30, (int)ui.Type.LineHeight(TextStyle.Button)), TextStyle.Button);
                ui.TextFit(st, new Rectangle(r.X + 12, r.Y + 10 + (int)ui.Type.LineHeight(TextStyle.Button), r.Width - pb - 30, (int)ui.Type.LineHeight(TextStyle.Caption)), TextStyle.Caption, cf.Active ? T.Danger : T.Success);
                if (cf.Active && ui.Button(new Rectangle(r.Right - pb - 10, r.Y + (rh - h) / 2, pb, h), "Tinchlik taklifi", ButtonKind.Secondary, !_client.WarBusy))
                    _client.ProposePeace(cf.KingdomId);
            }
            foreach (WarTargetDto t in w.Targets)
            {
                var r = new Rectangle(listR.X, listR.Y + idx++ * (rh + 6) - scroll, listR.Width - 8, rh);
                if (!UiKit.InView(r, listR)) continue;
                if (ui.Card(r, _warTarget == t.RegionId, true)) _warTarget = t.RegionId;
                ui.TextFit(t.RegionName + " · " + t.OwnerName, new Rectangle(r.X + 12, r.Y + 8, r.Width - 24, (int)ui.Type.LineHeight(TextStyle.Button)), TextStyle.Button, t.CanAttack ? T.TextPrimary : T.TextSecondary);
                string sub = "Himoya ≈ " + t.EstimatedDefense + " · sizning qo'shin " + (army?.Strength ?? 0) + " · aholi " + t.Population + (t.CanAttack ? "" : " · " + t.BlockReasonUz);
                ui.TextFit(sub, new Rectangle(r.X + 12, r.Y + 10 + (int)ui.Type.LineHeight(TextStyle.Button), r.Width - 24, (int)ui.Type.LineHeight(TextStyle.Caption)), TextStyle.Caption, t.CanAttack ? T.TextSecondary : T.Warning);
            }

            ui.Card(info);
            WarTargetDto sel = w.Targets.FirstOrDefault(t => t.RegionId == _warTarget);
            int x = info.X + 16, y = info.Y + 14, iw = info.Width - 32;
            if (sel == null)
            {
                ui.Paragraph("Ro'yxatdan hududni tanlang. Mojaro boshlashdan oldin uning oqibatlari shu yerda ko'rsatiladi. Real o'yinchilar bilan jang faqat PvP bo'limida, ikki tomon rozi bo'lsa.", new Rectangle(x, y, iw, info.Height - 28), TextStyle.Body);
                return;
            }
            ui.TextFit("SALTANATLAR O'RTASIDAGI MOJARO", new Rectangle(x, y, iw, (int)ui.Type.LineHeight(TextStyle.Caption)), TextStyle.Caption, T.Accent); y += (int)ui.Type.LineHeight(TextStyle.Caption) + 4;
            ui.TextFit(sel.RegionName, new Rectangle(x, y, iw, (int)ui.Type.LineHeight(TextStyle.PanelTitle)), TextStyle.PanelTitle); y += (int)ui.Type.LineHeight(TextStyle.PanelTitle) + 6;
            void Row(string a) { ui.TextFit(a, new Rectangle(x, y, iw, (int)ui.Type.LineHeight(TextStyle.Secondary)), TextStyle.Secondary, T.TextPrimary); y += (int)ui.Type.LineHeight(TextStyle.Secondary) + 4; }
            Row("Egasi: " + sel.OwnerName);
            Row("Munosabat: " + sel.Relation + " → " + sel.RelationAfter);
            Row("Ta'minot xarajati: " + sel.Cost + " tanga");
            Row("Raqib himoyasi ≈ " + sel.EstimatedDefense + " · Sizning qo'shin: " + (army?.Strength ?? 0));
            y += 4;
            float ph = ui.Paragraph(sel.ConsequencesUz, new Rectangle(x, y, iw, (int)(ui.Type.LineHeight(TextStyle.Secondary) * 4 + 8)), TextStyle.Secondary, T.TextSecondary, 4);
            int by = info.Bottom - h - 14;
            bool can = sel.CanAttack && army != null && army.Status == ArmyStatus.Available && w.ActiveBattle == null;
            string label = !sel.CanAttack ? sel.BlockReasonUz : army == null ? "Qo'shin yo'q" : army.Status != ArmyStatus.Available ? army.Name + ": " + army.StatusUz : w.ActiveBattle != null ? "Faol jang tugashini kuting" : "Mojaro e'lon qilish va jangga kirish";
            if (ui.Button(new Rectangle(x, by, iw, h), label, ButtonKind.Danger, can && !_client.WarBusy, "Jang serverda hisoblanadi; raund qarorlarini siz tanlaysiz"))
                _client.StartBattle(army.Id, sel.RegionId);
        }

        // ------------------------------------------------------------------ PvP
        private void DrawPvp(Rectangle c, WarOverviewDto w)
        {
            UiKit ui = _ui; ThemeTokens T = ui.T;
            int h = ui.ControlH, gap = ui.Gap;
            ArmyDto army = SelectedArmy(w);
            string prot = w.Protected ? " · Siz yangi o'yinchi himoyasidasiz: PvP ni yoqsangiz himoya bekor bo'ladi" : "";
            int tgw = BtnW("PvP takliflari: O'CHIQ", c.Width / 2);
            ui.TextFit("PvP faqat ikki tomon rozi bo'lsa boshlanadi. Rad etsangiz hech narsa yo'qotmaysiz." + prot, new Rectangle(c.X, c.Y, c.Width - tgw - 10, h), TextStyle.Secondary, T.TextSecondary);
            string pvpLabel = w.Protected ? "PvP ni yoqish (himoyasiz)" : w.AcceptsPvp ? "PvP takliflari: YOQIQ" : "PvP takliflari: O'CHIQ";
            if (ui.Button(new Rectangle(c.Right - tgw, c.Y, tgw, h), pvpLabel, w.Protected ? ButtonKind.Accent : w.AcceptsPvp ? ButtonKind.Secondary : ButtonKind.Ghost, !_client.WarBusy, "Bosib almashtiring"))
                _client.SetPvp(w.Protected || !w.AcceptsPvp);
            int y = c.Y + h + gap;
            int rh = (int)(ui.Type.LineHeight(TextStyle.Button) + ui.Type.LineHeight(TextStyle.Caption) * 2 + 26);

            // Kiruvchi va chiquvchi takliflar
            foreach (BattleInvitationDto inv in w.Invitations)
            {
                var r = new Rectangle(c.X, y, c.Width - 8, rh);
                ui.RoundedBorder(r, inv.Incoming ? T.Accent : T.Border, Color.Lerp(T.Surface, inv.Incoming ? T.Accent : T.Primary, 0.08f), 8);
                int bw = BtnW("Qo'shin tayyor emas", r.Width / 4);
                string head = inv.Incoming ? inv.FromName + " sizni PvP jangiga taklif qilmoqda" : inv.ToName + " javobi kutilmoqda";
                ui.TextFit(head + " · " + inv.SecondsLeft / 60 + " daq", new Rectangle(r.X + 12, r.Y + 8, r.Width - 2 * bw - 40, (int)ui.Type.LineHeight(TextStyle.Button)), TextStyle.Button);
                ui.Paragraph(inv.TermsUz, new Rectangle(r.X + 12, r.Y + 10 + (int)ui.Type.LineHeight(TextStyle.Button), r.Width - 2 * bw - 40, (int)(ui.Type.LineHeight(TextStyle.Caption) * 2 + 4)), TextStyle.Caption, T.TextSecondary, 2);
                if (inv.Incoming)
                {
                    bool can = army != null && army.Status == ArmyStatus.Available;
                    if (ui.Button(new Rectangle(r.Right - 2 * bw - 20, r.Y + (rh - h) / 2, bw, h), can ? "Qabul qilish" : "Qo'shin tayyor emas", ButtonKind.Primary, can && !_client.WarBusy, "Tanlangan qo'shin bilan: " + (army?.Name ?? "-")))
                        _client.RespondPvp(inv.Id, true, army.Id);
                    if (ui.Button(new Rectangle(r.Right - bw - 12, r.Y + (rh - h) / 2, bw, h), "Rad etish", ButtonKind.Ghost, !_client.WarBusy))
                        _client.RespondPvp(inv.Id, false, 0);
                }
                y += rh + 6;
            }

            ui.TextFit("BOSHQA O'YINCHILAR", new Rectangle(c.X, y + 4, c.Width, (int)ui.Type.LineHeight(TextStyle.Caption)), TextStyle.Caption, T.Accent);
            y += (int)ui.Type.LineHeight(TextStyle.Caption) + 10;
            if (w.Opponents.Count == 0) { ui.Paragraph("Bu dunyoda boshqa real o'yinchi yo'q. Sherigingiz Google bilan kirib, shu serverga ulansin.", new Rectangle(c.X, y, c.Width, 80), TextStyle.Body); return; }
            var list = new Rectangle(c.X, y, c.Width, c.Bottom - y);
            int oh = (int)(ui.Type.LineHeight(TextStyle.Button) + ui.Type.LineHeight(TextStyle.Caption) + h + 30);
            int scroll = ui.Scroll("pvp", list, w.Opponents.Count * (oh + 6));
            for (int i = 0; i < w.Opponents.Count; i++)
            {
                PvpOpponentDto o = w.Opponents[i];
                var r = new Rectangle(list.X, list.Y + i * (oh + 6) - scroll, list.Width - 8, oh);
                if (!UiKit.InView(r, list)) continue;
                if (ui.Card(r, _pvpTarget == o.KingdomId, true)) { _pvpTarget = o.KingdomId; _pvpStake = 0; }
                ui.TextFit(o.Name + " · " + o.RulerName + " · qo'shin kuchi " + o.Strength, new Rectangle(r.X + 12, r.Y + 8, r.Width - 24, (int)ui.Type.LineHeight(TextStyle.Button)), TextStyle.Button);
                ui.TextFit(o.NoteUz.Length > 0 ? o.NoteUz : "Taklif yuborish mumkin", new Rectangle(r.X + 12, r.Y + 10 + (int)ui.Type.LineHeight(TextStyle.Button), r.Width - 24, (int)ui.Type.LineHeight(TextStyle.Caption)), TextStyle.Caption, o.NoteUz.Length > 0 ? T.Warning : T.TextSecondary);
                int cy = r.Bottom - h - 10;
                int sendW = BtnW("Taklif yuborish", r.Width / 3);
                int chipW = Math.Max(60, Math.Min((int)ui.Type.Measure("Garov: Marg'ilon", TextStyle.Secondary).X + 30, (r.Width - sendW - 40) / Math.Max(1, o.StakeRegions.Count + 1) - gap));
                if (ui.Chip(new Rectangle(r.X + 12, cy, chipW, h), "Hududsiz", _pvpTarget == o.KingdomId && _pvpStake == 0)) { _pvpTarget = o.KingdomId; _pvpStake = 0; }
                for (int k = 0; k < o.StakeRegions.Count; k++)
                    if (ui.Chip(new Rectangle(r.X + 12 + (k + 1) * (chipW + gap), cy, chipW, h), "Garov: " + o.StakeRegions[k].Name, _pvpTarget == o.KingdomId && _pvpStake == o.StakeRegions[k].RegionId))
                    { _pvpTarget = o.KingdomId; _pvpStake = o.StakeRegions[k].RegionId; }
                bool can = !o.Protected && o.AcceptsPvp && army != null && army.Status == ArmyStatus.Available;
                if (ui.Button(new Rectangle(r.Right - sendW - 12, cy, sendW, h), "Taklif yuborish", ButtonKind.Primary, can && !_client.WarBusy, "Tanlangan qo'shin: " + (army?.Name ?? "-")))
                    _client.InvitePvp(o.KingdomId, army.Id, _pvpTarget == o.KingdomId ? _pvpStake : 0);
            }
        }

        // ------------------------------------------------------------------ JANG TARIXI
        private void DrawBattleHistory(Rectangle c, WarOverviewDto w)
        {
            UiKit ui = _ui; ThemeTokens T = ui.T;
            if (w.History.Count == 0) { ui.Paragraph("Hali janglar bo'lmagan.", c, TextStyle.Body); return; }
            int h = ui.ControlH;
            int rh = (int)(ui.Type.LineHeight(TextStyle.Button) + ui.Type.LineHeight(TextStyle.Caption) + 24);
            int scroll = ui.Scroll("history", c, w.History.Count * (rh + 6));
            for (int i = 0; i < w.History.Count; i++)
            {
                BattleDto b = w.History[i];
                var r = new Rectangle(c.X, c.Y + i * (rh + 6) - scroll, c.Width - 8, rh);
                if (!UiKit.InView(r, c)) continue;
                ui.Card(r);
                bool won = b.WinnerKingdomId == w.KingdomId;
                string res = b.Peace ? "TINCHLIK" : won ? "G'ALABA" : "MAG'LUBIYAT";
                Color rc = b.Peace ? T.Secondary : won ? T.Success : T.Danger;
                int bw = BtnW("Hisobot", r.Width / 4);
                ui.TextFit(res + " · " + b.AttackerName + " — " + b.DefenderName + " · " + b.RegionName + (b.IsPvp ? " · PvP" : ""), new Rectangle(r.X + 12, r.Y + 8, r.Width - bw - 36, (int)ui.Type.LineHeight(TextStyle.Button)), TextStyle.Button, rc);
                ui.TextFit(b.Rounds.Count + " raund · " + b.ResultUz, new Rectangle(r.X + 12, r.Y + 10 + (int)ui.Type.LineHeight(TextStyle.Button), r.Width - bw - 36, (int)ui.Type.LineHeight(TextStyle.Caption)), TextStyle.Caption, T.TextSecondary);
                if (ui.Button(new Rectangle(r.Right - bw - 12, r.Y + (rh - h) / 2, bw, h), "Hisobot", ButtonKind.Ghost)) OpenBattle(b.Id);
            }
        }

        /// <summary>Jang sahnasini to'liq ekranda chizadi; qaror va tugmalarni serverga yuboradi.</summary>
        private void DrawBattleOverlay(int vw, int vh, float dt)
        {
            UiKit ui = _ui;
            BattleDto b = _client.Battle;
            if (b == null) return;
            ui.Fill(new Rectangle(0, 0, vw, vh), ui.T.Background);
            BattleSceneAction a = _battleScene.Draw(ui, _sbCache, b, vw, vh, dt, _client.WarBusy);
            switch (a)
            {
                case BattleSceneAction.Decide:
                    _client.SubmitDecision(b.Id, b.Round, _battleScene.SelectedDecision);
                    break;
                case BattleSceneAction.Close:
                    _battleOpen = false;
                    break;
                case BattleSceneAction.OpenRegion:
                    _battleOpen = false; _client.CloseBattle(); _section = Section.Kingdom; _kingdomTab = 2; _selectedRegion = b.RegionId; _client.QueueRefresh();
                    break;
                case BattleSceneAction.ManageRegion:
                    _battleOpen = false; _client.CloseBattle(); _section = Section.Kingdom; _kingdomTab = 6; _client.QueueRefresh();
                    break;
            }
        }
    }
}
