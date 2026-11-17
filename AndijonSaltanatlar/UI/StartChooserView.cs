using System;
using System.Collections.Generic;
using System.Linq;
using AndijonSaltanatlar.Shared.DTOs;
using AndijonSaltanatlar.Shared.Geo;
using AndijonSaltanatlar.Shared.Heraldry;
using AndijonSaltanatlar.UI.Design;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.UI
{
    /// <summary>
    /// YURT TANLASH (yangi o'yinchi): butun O'zbekiston xaritasidan istalgan mustaqil hududni tanlaydi, keyin saltanat
    /// nomi va o'z BAYROG'ini yaratadi. Band hududlar (AI saltanatlar va boshqa o'yinchilar) bayrog'i bilan ko'rinadi —
    /// ularni keyinroq urush yoki diplomatiya bilan olish mumkin. Bayroq muharriri "Saltanat → Bayroq" bo'limida ham bor.
    /// </summary>
    public sealed partial class OnlinePanel
    {
        private UzMapCanvas _startCanvas;
        private int _startRegion = -1;
        private int _startStep;
        private FlagSpec _flagDraft;
        private string _nameDraft;
        private float _startFetch = -100f;
        private readonly List<MapCell> _startCells = new List<MapCell>();
        private static readonly Random FlagRandom = new Random();

        private void DrawStartChooser(Rectangle win)
        {
            UiKit ui = _ui; ThemeTokens T = ui.T;
            int h = ui.ControlH, gap = ui.Gap, pad = ui.Compact ? 12 : 20;
            StartRegionsDto d = _client.StartRegions;
            if (d == null || ui.Time - _startFetch > 8f) { if (ui.Time - _startFetch > 2f) { _startFetch = ui.Time; _client.LoadStartRegions(); } }

            int th = (int)ui.Type.LineHeight(TextStyle.ScreenTitle);
            ui.Text(_startStep == 0 ? "Yurtingizni tanlang" : "Saltanatingiz bayrog'i", new Vector2(win.X + pad, win.Y + 12), TextStyle.ScreenTitle, T.Accent);
            int sub = (int)ui.Type.LineHeight(TextStyle.Secondary);
            ui.TextFit(_startStep == 0
                    ? "O'zbekistonning istalgan mustaqil hududida saltanat quring. Keyin qo'shnilarni bosib oling!"
                    : "Bayroq qasrlaringiz, qo'shinlaringiz va jang maydonida hilpiraydi.",
                new Rectangle(win.X + pad, win.Y + 16 + th, win.Width - 2 * pad, sub), TextStyle.Secondary, T.TextSecondary);
            var c = new Rectangle(win.X + pad, win.Y + 24 + th + sub, win.Width - 2 * pad, win.Bottom - (win.Y + 24 + th + sub) - pad);

            if (d == null) { ui.Paragraph("Xarita yuklanmoqda…", c, TextStyle.Body); return; }
            if (_flagDraft == null) _flagDraft = FlagRenderer.Spec(d.Flag, new Color(d.Color[0], d.Color[1], d.Color[2])).Clone();
            if (_nameDraft == null) _nameDraft = d.SuggestedName ?? "";
            if (_startRegion == -2) _startRegion = (d.Regions.FirstOrDefault(r => r.Free && r.Name == "Urgut") ?? d.Regions.FirstOrDefault(r => r.Free))?.RegionId ?? -1;
            StartRegionDto sel = d.Regions.FirstOrDefault(r => r.RegionId == _startRegion);

            if (_startStep == 1 && sel != null && sel.Free) { DrawStartFlagStep(c, sel); return; }
            _startStep = 0;

            bool side = c.Width >= 860;
            int infoW = side ? Math.Min((int)(360 * Math.Max(1f, ui.Type.UiScale)), c.Width * 2 / 5) : c.Width;
            var map = side ? new Rectangle(c.X, c.Y, c.Width - infoW - gap, c.Height) : new Rectangle(c.X, c.Y, c.Width, c.Height * 62 / 100);
            var info = side ? new Rectangle(map.Right + gap, c.Y, infoW, c.Height) : new Rectangle(c.X, map.Bottom + gap, c.Width, c.Bottom - map.Bottom - gap);

            _startCanvas ??= new UzMapCanvas(ui);
            UzMapCanvas cv = _startCanvas;
            cv.Begin(map);
            _startCells.Clear();
            foreach (StartRegionDto r in d.Regions)
                _startCells.Add(new MapCell(r.RegionId, r.MapX, r.MapY, r.OwnerKingdomId, new Color(r.Color[0], r.Color[1], r.Color[2]), r.Province, false));
            cv.DrawBase(_startCells, _startRegion, new Color(70, 150, 70));

            float s = cv.MarkerScale;
            foreach (StartRegionDto r in d.Regions.OrderBy(x => x.MapY))
            {
                Vector2 p = cv.ToScreen(r.MapX, r.MapY);
                if (!cv.InView(p, 40)) continue;
                Color col = new Color(r.Color[0], r.Color[1], r.Color[2]);
                int dd = Math.Max(4, (int)(7 * s));
                if (r.Free)
                {
                    // Bo'sh hudud: yashil halqa (pulsatsiya) — shu yerda saltanat qurish mumkin
                    int g = dd + 3 + (int)(2 * MathF.Sin(ui.Time * 3 + r.RegionId));
                    ui.Rounded(new Rectangle((int)p.X - g, (int)p.Y - g, 2 * g, 2 * g), new Color(60, 140, 60) * 0.35f, g);
                    ui.Rounded(new Rectangle((int)p.X - dd, (int)p.Y - dd, 2 * dd, 2 * dd), Ink, dd);
                    ui.Rounded(new Rectangle((int)p.X - dd + 2, (int)p.Y - dd + 2, 2 * dd - 4, 2 * dd - 4), new Color(236, 224, 186), dd - 2);
                }
                else
                {
                    ui.Rounded(new Rectangle((int)p.X - dd, (int)p.Y - dd / 2, dd * 2, dd), Ink, dd / 2);
                    FlagRenderer.DrawOnPole(ui, p, (int)(24 * s), (int)(20 * s), FlagRenderer.Spec(r.Flag, col), ui.Time, r.RegionId);
                }
            }
            bool showAll = cv.Zoom >= 2.2f;
            foreach (StartRegionDto r in d.Regions.OrderByDescending(x => x.RegionId == _startRegion).ThenByDescending(x => x.Population))
            {
                if (!showAll && r.RegionId != _startRegion) continue;
                Vector2 p = cv.ToScreen(r.MapX, r.MapY);
                if (!cv.InView(p)) continue;
                cv.Label(r.Name, p + new Vector2(0, 8), r.Free ? new Color(40, 100, 40) : Ink, Ink, r.RegionId == _startRegion);
            }
            int picked = cv.PickNearest(d.Regions.Select(r => (r.RegionId, cv.ToScreen(r.MapX, r.MapY))), 30 * s + 16);
            if (picked >= 0) _startRegion = picked;
            cv.DrawOverlay("O'zbekiston");

            // Tanlangan hudud kartasi
            ui.Card(info);
            int x = info.X + 14, y = info.Y + 12, w = info.Width - 28;
            float lh = ui.Type.LineHeight(TextStyle.Secondary);
            int free = d.Regions.Count(r => r.Free);
            if (sel == null)
            {
                y += (int)ui.Paragraph("Xaritadan hududni tanlang. Yashil halqali hududlar bo'sh (" + free + " ta). Bayroqli hududlar band: ularni keyinroq urush yoki diplomatiya bilan olish mumkin.",
                    new Rectangle(x, y, w, (int)(lh * 8 + 10)), TextStyle.Secondary, T.TextPrimary, 8) + 8;
                ui.Paragraph("Kattalashtirish: g'ildirak, ikki barmoq yoki + tugmasi. Surish: sudrang.", new Rectangle(x, y, w, (int)(lh * 3 + 6)), TextStyle.Caption, T.TextSecondary, 3);
                return;
            }
            ui.TextFit(sel.Name, new Rectangle(x, y, w, (int)ui.Type.LineHeight(TextStyle.PanelTitle)), TextStyle.PanelTitle); y += (int)ui.Type.LineHeight(TextStyle.PanelTitle) + 4;
            void Row(string a, Color? col = null) { ui.TextFit(a, new Rectangle(x, y, w, (int)lh), TextStyle.Secondary, col ?? T.TextPrimary); y += (int)lh + 4; }
            Row("Viloyat: " + UzMap.ProvinceName(sel.Province), T.TextSecondary);
            Row("Aholi: " + sel.Population);
            if (sel.Free) Row("Holati: bo'sh — shu yerda boshlash mumkin", T.Success);
            else
            {
                Row("Holati: band — " + sel.OwnerName, T.Danger);
                int fw = Math.Min(w / 2, (int)(90 * Math.Max(1f, ui.Type.UiScale)));
                FlagRenderer.DrawFramed(ui, new Rectangle(x + 2, y + 4, fw, (int)(fw / FlagSpec.Aspect)), FlagRenderer.Spec(sel.Flag, new Color(sel.Color[0], sel.Color[1], sel.Color[2])), Ink);
                y += (int)(fw / FlagSpec.Aspect) + 12;
            }
            int by = info.Bottom - h - 12;
            if (!sel.Free) ui.Paragraph("Boshlash uchun bo'sh hudud tanlang. Bu hududni keyinroq urushda olishingiz mumkin.", new Rectangle(x, Math.Max(y, by - (int)(lh * 3 + 10)), w, (int)(lh * 3 + 6)), TextStyle.Caption, T.Warning, 3);
            if (ui.Button(new Rectangle(x, by, w, h), sel.Free ? "Davom etish: bayroq" : "Hudud band", ButtonKind.Primary, sel.Free && !_client.Busy))
                _startStep = 1;
        }

        private void DrawStartFlagStep(Rectangle c, StartRegionDto sel)
        {
            UiKit ui = _ui; ThemeTokens T = ui.T;
            int h = ui.ControlH, gap = ui.Gap;
            int lh = (int)ui.Type.LineHeight(TextStyle.Secondary);
            // Pastda: nom + tugmalar
            int bottomH = lh + 4 + h + gap + h;
            var editor = new Rectangle(c.X, c.Y, c.Width, c.Height - bottomH - gap);
            DrawFlagEditor(editor, _flagDraft);

            int y = editor.Bottom + gap;
            ui.TextFit("Saltanat nomi", new Rectangle(c.X, y, c.Width, lh), TextStyle.Secondary, T.TextSecondary); y += lh + 4;
            _nameDraft = ui.TextInput(new Rectangle(c.X, y, c.Width, h), "kname", _nameDraft ?? "", "Masalan: " + sel.Name + " sultonligi", false,
                () => AndroidKeyboard("kname", "Saltanat nomi", _nameDraft ?? "", false));
            y += h + gap;
            int bw = Math.Min(c.Width / 3, (int)ui.Type.Measure("Orqaga", TextStyle.Button).X + 48);
            if (ui.Button(new Rectangle(c.X, y, bw, h), "Orqaga", ButtonKind.Secondary)) _startStep = 0;
            string go = sel.Name.ToUpperInvariant() + "DA SALTANAT QURISH";
            bool nameOk = string.IsNullOrWhiteSpace(_nameDraft) || _nameDraft.Trim().Length >= 3;
            if (ui.Button(new Rectangle(c.X + bw + gap, y, c.Width - bw - gap, h), go, ButtonKind.Primary, nameOk && !_client.Busy, "Yurt, nom va bayroq saqlanadi"))
            {
                _ui.FocusId = null;
                _client.ChooseStart(sel.RegionId, (_nameDraft ?? "").Trim(), _flagDraft.Encode());
            }
        }

        // ------------------------------------------------------------------ Bayroq muharriri
        private static readonly int[] ThreeColorPatterns = { 2, 4, 8, 10 };

        /// <summary>Bayroq muharriri: katta ko'rinish + naqsh, ranglar, ramz va ramz rangi. f joyida o'zgaradi.</summary>
        private void DrawFlagEditor(Rectangle r, FlagSpec f)
        {
            UiKit ui = _ui; ThemeTokens T = ui.T;
            int h = ui.ControlH, gap = ui.Gap;
            int lh = (int)ui.Type.LineHeight(TextStyle.Secondary);
            bool wide = r.Width >= 760;
            int rowH = Math.Max(h - 6, 30);
            bool three = Array.IndexOf(ThreeColorPatterns, f.Pattern) >= 0;
            int rows = three ? 6 : 5;
            int controlsH = rows * (lh + 2 + rowH + 6) + h;

            Rectangle preview, controls;
            if (wide)
            {
                int pw = Math.Min(r.Width * 2 / 5, (int)((r.Height - 30) * FlagSpec.Aspect));
                preview = new Rectangle(r.X, r.Y, pw, (int)(pw / FlagSpec.Aspect));
                controls = new Rectangle(r.X + pw + gap * 2, r.Y, r.Width - pw - gap * 2, r.Height);
            }
            else
            {
                int ph = Math.Max(50, Math.Min((int)(r.Width / FlagSpec.Aspect / 2), r.Height - controlsH - gap));
                int pw = (int)(ph * FlagSpec.Aspect);
                preview = new Rectangle(r.X + (r.Width - pw) / 2, r.Y, pw, ph);
                controls = new Rectangle(r.X, preview.Bottom + gap, r.Width, r.Bottom - preview.Bottom - gap);
            }
            // Bayroq (dastasi bilan, hilpirab)
            ui.Fill(new Rectangle(preview.X - 6, preview.Y - 4, 4, preview.Height + 30), new Color(92, 70, 44));
            ui.Fill(new Rectangle(preview.X - 8, preview.Y - 8, 8, 6), new Color(214, 172, 62));
            FlagRenderer.Draw(ui, preview, f, Math.Clamp(preview.Width / 6, 18, 60), Math.Max(2f, preview.Height * 0.04f), ui.Time * 3f);

            int x = controls.X, y = controls.Y, w = controls.Width;
            void Caption(string t) { ui.TextFit(t, new Rectangle(x, y, w, lh), TextStyle.Secondary, T.TextSecondary); y += lh + 2; }
            int Cycle(string label, string[] names, int value)
            {
                Caption(label);
                int aw = rowH + 10;
                if (ui.Button(new Rectangle(x, y, aw, rowH), "<", ButtonKind.Secondary)) value = (value + names.Length - 1) % names.Length;
                ui.Frame(new Rectangle(x + aw + 4, y, w - 2 * aw - 8, rowH), T.Background, T.Border, 1, false);
                ui.Type.DrawFitted(ui.Batch, names[value], new Rectangle(x + aw + 4, y, w - 2 * aw - 8, rowH), TextStyle.Body, T.TextPrimary, 1);
                if (ui.Button(new Rectangle(x + w - aw, y, aw, rowH), ">", ButtonKind.Secondary)) value = (value + 1) % names.Length;
                y += rowH + 6;
                return value;
            }
            int Palette(string label, int value)
            {
                Caption(label);
                int n = FlagSpec.Palette.Length;
                int cw = Math.Max(16, (w - (n - 1) * 3) / n);
                for (int i = 0; i < n; i++)
                {
                    var cr = new Rectangle(x + i * (cw + 3), y, cw, rowH);
                    bool on = i == value;
                    ui.Fill(new Rectangle(cr.X - (on ? 3 : 1), cr.Y - (on ? 3 : 1), cr.Width + (on ? 6 : 2), cr.Height + (on ? 6 : 2)), on ? T.Focus : Ink);
                    ui.Fill(cr, FlagRenderer.ToColor(i));
                    if (ui.Clicked(cr)) value = i;
                }
                y += rowH + 6;
                return value;
            }

            f.Pattern = Cycle("Naqsh", FlagSpec.PatternNames, f.Pattern);
            f.C1 = Palette("Asosiy rang", f.C1);
            f.C2 = Palette("Ikkinchi rang", f.C2);
            if (Array.IndexOf(ThreeColorPatterns, f.Pattern) >= 0) f.C3 = Palette("Uchinchi rang", f.C3);
            f.Emblem = Cycle("Ramz", FlagSpec.EmblemNames, f.Emblem);
            if (f.Emblem != 0) f.EmblemColor = Palette("Ramz rangi", f.EmblemColor);
            int rw = Math.Min(w, (int)ui.Type.Measure("Tasodifiy bayroq", TextStyle.Button).X + 40);
            if (y + h <= r.Bottom && ui.Button(new Rectangle(x, y, rw, h), "Tasodifiy bayroq", ButtonKind.Ghost)) RandomizeFlag(f);
        }

        private static void RandomizeFlag(FlagSpec f)
        {
            int n = FlagSpec.Palette.Length;
            f.Pattern = FlagRandom.Next(FlagSpec.PatternNames.Length);
            f.C1 = FlagRandom.Next(n);
            do f.C2 = FlagRandom.Next(n); while (f.C2 == f.C1);
            f.C3 = FlagRandom.Next(n);
            f.Emblem = FlagRandom.Next(FlagSpec.EmblemNames.Length);
            do f.EmblemColor = FlagRandom.Next(n); while (f.EmblemColor == f.C1);
        }

        // ------------------------------------------------------------------ Saltanat → Bayroq bo'limi
        private FlagSpec _flagEdit;
        private string _flagEditBase;

        private void DrawFlagTab(Rectangle c, KingdomOverviewDto k)
        {
            UiKit ui = _ui; ThemeTokens T = ui.T;
            int h = ui.ControlH, gap = ui.Gap;
            if (_flagEdit == null || _flagEditBase != k.Flag)
            {
                Color kc = k.Color != null && k.Color.Length >= 3 ? new Color(k.Color[0], k.Color[1], k.Color[2]) : T.Primary;
                _flagEdit = FlagRenderer.Spec(k.Flag, kc).Clone();
                _flagEditBase = k.Flag;
            }
            DrawFlagEditor(new Rectangle(c.X, c.Y, c.Width, c.Height - h - gap), _flagEdit);
            bool dirty = _flagEdit.Encode() != (k.Flag ?? "");
            int bw = Math.Min(c.Width, (int)ui.Type.Measure("Bayroqni saqlash", TextStyle.Button).X + 60);
            if (ui.Button(new Rectangle(c.Right - bw, c.Bottom - h, bw, h), "Bayroqni saqlash", ButtonKind.Primary, dirty && !_client.Busy, "Bayroq barcha o'yinchilarga ko'rinadi"))
                _client.SetFlag(_flagEdit.Encode());
            if (dirty)
            {
                int rw = Math.Min(c.Width - bw - gap, (int)ui.Type.Measure("Bekor qilish", TextStyle.Button).X + 40);
                if (rw > 40 && ui.Button(new Rectangle(c.Right - bw - gap - rw, c.Bottom - h, rw, h), "Bekor qilish", ButtonKind.Ghost)) _flagEdit = null;
            }
        }
    }
}
