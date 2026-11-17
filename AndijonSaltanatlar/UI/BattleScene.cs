using System;
using System.Collections.Generic;
using System.Linq;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Core.Graphics;
using AndijonSaltanatlar.Shared.DTOs;
using AndijonSaltanatlar.Shared.War;
using AndijonSaltanatlar.UI.Design;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.UI
{
    public enum BattleSceneAction { None, Close, Decide, OpenRegion, ManageRegion }

    /// <summary>
    /// PROMPT 25 — 2.5D izometrik jang sahnasi. Barcha raqamlar va natija — serverdan (BattleDto); sahna faqat ko'rsatadi.
    ///
    /// Animatsiyalar (grafik zo'ravonliksiz, stilizatsiyalangan):
    ///  - Birliklar: maydonga kirib kelish (March), Idle (har birining fazasi va tezligi har xil — bir xil takrorlanmaydi),
    ///    Ready, mavhum harakat (markazga qisqa intilish), Reaction (silkinish), Victory (sakrash), Withdraw (orqaga, xiralashib).
    ///  - Sarkardalar: Idle, CommandGesture (raund hal bo'lganda), Victory, Return.
    ///  - Muhit: hilpiragan bayroqlar, yengil chang, sahna kirish/chiqish o'tishlari.
    /// Sprite anchor (oyoq nuqtasi) o'zgarmaydi — faqat siljish beriladi; kadrlar almashinuvi sekin va silliq.
    /// Katta qo'shin minglab NPC emas: guruh ikonkalari (bitta figura ≈ bir necha birlik) bilan ko'rsatiladi.
    /// </summary>
    public sealed class BattleScene
    {
        private readonly AssetManager _assets;
        private int _battleId = -1;
        private int _seenRounds;
        private float _enterT;          // 0..1 kirish animatsiyasi
        private float _actionT = 1f;    // 0..1 — oxirgi raund harakati
        private float _sceneTime;
        private int _lastAttackerLoss, _lastDefenderLoss;
        private BattleDecision? _selected;
        private bool _showReport;
        private readonly List<Vector3> _dust = new List<Vector3>(); // x, y, yosh

        public BattleDecision SelectedDecision => _selected ?? BattleDecision.Muvozanatli;

        public BattleScene(AssetManager assets) { _assets = assets; }

        private struct Figure
        {
            public Vector2 Home;
            public string Sprite;
            public Color Tint;
            public float Phase, Speed, Scale;
            public bool Support;
        }

        public void Reset() { _battleId = -1; }

        public BattleSceneAction Draw(UiKit ui, SpriteBatch sb, BattleDto b, int vw, int vh, float dt, bool busy)
        {
            if (b == null) return BattleSceneAction.None;
            if (b.Id != _battleId)
            {
                _battleId = b.Id; _seenRounds = b.Rounds.Count; _enterT = 0f; _battleEndT = null; _actionT = 1f; _selected = null; _showReport = false; _dust.Clear();
            }
            _sceneTime += dt;
            _enterT = Math.Min(1f, _enterT + dt / 1.4f);
            if (b.Rounds.Count > _seenRounds)
            {
                // Yangi raund hal bo'ldi — harakat animatsiyasi va chang.
                _seenRounds = b.Rounds.Count;
                BattleRoundDto last = b.Rounds[b.Rounds.Count - 1];
                _lastAttackerLoss = last.AttackerLoss; _lastDefenderLoss = last.DefenderLoss;
                _actionT = 0f;
                _selected = null;
                var rng = new Random(b.Id * 31 + _seenRounds);
                for (int i = 0; i < 26; i++) _dust.Add(new Vector3((float)rng.NextDouble(), (float)rng.NextDouble(), 0f));
            }
            _actionT = Math.Min(1f, _actionT + dt / 1.1f);

            ThemeTokens T = ui.T;
            BattleSceneAction act = BattleSceneAction.None;
            float s = Math.Max(1f, vh / 720f);         // sahna masshtabi (telefon/PC)
            int topH = (int)(ui.Type.LineHeight(TextStyle.PanelTitle) * 2.6f + 24);
            int panelH = b.Status == BattleStatus.Completed ? 0 : (int)(ui.ControlH * 2.2f + ui.Gap * 3 + ui.Type.LineHeight(TextStyle.Secondary) + 10);
            var field = new Rectangle(0, topH, vw, vh - topH - panelH);

            // ---- Fon: osmon, uzoq tog'lar, shahar siluetlari ----
            DrawBackdrop(ui, field, vw);

            // ---- Izometrik jang maydoni va qo'shinlar (piksel-art: PointClamp) ----
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            DrawField(sb, field, s);
            DrawArmies(sb, b, field, s);
            DrawFlags(ui, sb, b, field, s);
            DrawDust(ui, field, s, dt);
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

            // ---- Yuqori panel: saltanatlar, kuch va ruh, raund ----
            DrawHeader(ui, b, new Rectangle(0, 0, vw, topH), b.Status != BattleStatus.Completed ? ui.ControlH + 16 : 0);

            // ---- Pastki qaror paneli yoki natija oynasi ----
            if (b.Status != BattleStatus.Completed)
                act = DrawDecisionPanel(ui, b, new Rectangle(0, vh - panelH, vw, panelH), busy);
            else
                act = DrawResult(ui, b, vw, vh);

            // Yopish (sahnadan chiqish — jang serverda davom etadi, qayta ochish mumkin)
            int h = ui.ControlH;
            if (b.Status != BattleStatus.Completed && ui.IconButton(new Rectangle(vw - h - 8, 8, h, h), "×", "Sahnani yopish (jang serverda davom etadi)"))
                act = BattleSceneAction.Close;
            return act;
        }

        // ------------------------------------------------------------------ Fon
        private static void DrawBackdrop(UiKit ui, Rectangle f, int vw)
        {
            // Osmon gradienti (oqshom ranglari)
            int bands = 12;
            for (int i = 0; i < bands; i++)
            {
                Color c = Color.Lerp(new Color(44, 58, 92), new Color(214, 150, 98), i / (float)(bands - 1));
                int y0 = f.Y + f.Height * i / (bands * 2), y1 = f.Y + f.Height * (i + 1) / (bands * 2);
                ui.Fill(new Rectangle(0, y0, vw, y1 - y0 + 1), c);
            }
            // Uzoq tog'lar (Farg'ona vodiysi tizmalari)
            int horizon = f.Y + f.Height / 2;
            for (int x = 0; x < vw; x += 6)
            {
                float m = 0.5f + 0.5f * MathF.Sin(x * 0.008f) * MathF.Cos(x * 0.0031f + 1.3f);
                int top = horizon - (int)(f.Height * (0.08f + 0.14f * m));
                ui.Fill(new Rectangle(x, top, 6, horizon - top), new Color(70, 78, 104));
            }
            // Shahar siluetlari: gumbaz va minoralar (ikki tomonda)
            Color sil = new Color(52, 56, 78);
            for (int side = 0; side < 2; side++)
            {
                int cx = side == 0 ? vw / 8 : vw * 7 / 8;
                int baseY = horizon;
                ui.Fill(new Rectangle(cx - 70, baseY - 26, 140, 26), sil);
                ui.Rounded(new Rectangle(cx - 22, baseY - 58, 44, 44), sil, 22);
                ui.Fill(new Rectangle(cx - 52, baseY - 70, 9, 70), sil);
                ui.Fill(new Rectangle(cx + 44, baseY - 64, 9, 64), sil);
            }
            ui.Fill(new Rectangle(0, horizon, vw, f.Bottom - horizon), new Color(86, 104, 66));
        }

        private void DrawField(SpriteBatch sb, Rectangle f, float s)
        {
            Texture2D atlas = _assets.Get(SpriteKeys.Tiles);
            float tile = 1.15f * s;
            int n = 9;
            Vector2 c = new Vector2(f.Center.X, f.Y + f.Height * 0.52f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float wx = (x - y) * 32 * tile, wy = (x + y - n + 1) * 16 * tile;
                    int row = ((x * 7 + y * 13) % 5 == 0) ? 0 : 3; // asosan o't, orasida tuproq
                    var pos = c + new Vector2(wx + TileArt.DrawOffsetX * tile, wy + TileArt.DrawOffsetY * tile);
                    sb.Draw(atlas, pos, TileArt.Cell(row, (x + y) % TileArt.Columns), Color.White, 0f, Vector2.Zero, tile, SpriteEffects.None, 0f);
                }
        }

        // ------------------------------------------------------------------ Qo'shinlar
        private static string SpriteFor(UnitLine line, int level)
        {
            switch (line)
            {
                case UnitLine.Otliq: return SpriteKeys.UnitPatrol;
                case UnitLine.Masofaviy: return SpriteKeys.UnitSwordsman;
                case UnitLine.Aravasi:
                case UnitLine.Lager: return SpriteKeys.UnitCitizen + "2";
                default: return level >= 3 ? SpriteKeys.UnitShield : level == 2 ? SpriteKeys.UnitSwordsman : SpriteKeys.UnitSpearman;
            }
        }

        private static Color TintFor(UnitLine line, int level, Color kingdom)
        {
            Color baseC = line == UnitLine.Otliq ? new Color(255, 236, 200) : line == UnitLine.Masofaviy ? new Color(210, 240, 210) : Color.White;
            if (level >= 3) baseC = Color.Lerp(baseC, new Color(255, 220, 140), 0.35f);
            return Color.Lerp(baseC, kingdom, 0.18f);
        }

        private static List<Figure> Formation(List<UnitGroupDto> groups, bool left, Vector2 anchor, float s, int seed, Color kingdom)
        {
            var figs = new List<Figure>();
            int total = Math.Max(1, groups.Sum(g => g.Count));
            int maxFigs = 22;
            float per = Math.Max(1f, total / (float)maxFigs);
            var rng = new Random(seed);
            int i = 0;
            foreach (UnitGroupDto g in groups.OrderBy(g => g.Line == UnitLine.Aravasi || g.Line == UnitLine.Lager ? 1 : 0).ThenByDescending(g => g.Line == UnitLine.Otliq ? 0 : 1))
            {
                int n = Math.Max(g.Count > 0 ? 1 : 0, (int)Math.Round(g.Count / per));
                for (int k = 0; k < n && figs.Count < maxFigs + 4; k++, i++)
                {
                    int col = i / 5, row = i % 5;
                    float dx = (left ? -1 : 1) * col * 30 * s + (row % 2) * 8 * s * (left ? -1 : 1);
                    float dy = (row - 2) * 15 * s + col * 7.5f * s;
                    figs.Add(new Figure
                    {
                        Home = anchor + new Vector2(dx + (left ? -1 : 1) * col * 0f, dy),
                        Sprite = SpriteFor(g.Line, g.Level),
                        Tint = TintFor(g.Line, g.Level, kingdom),
                        Phase = (float)rng.NextDouble() * MathHelper.TwoPi,
                        Speed = 1.6f + (float)rng.NextDouble() * 1.4f,
                        Scale = (g.Line == UnitLine.Otliq ? 1.25f : 1.1f) * s,
                        Support = g.Line == UnitLine.Aravasi || g.Line == UnitLine.Lager
                    });
                }
            }
            figs.Sort((a, b) => a.Home.Y.CompareTo(b.Home.Y)); // chuqurlik bo'yicha chizish
            return figs;
        }

        private void DrawArmies(SpriteBatch sb, BattleDto b, Rectangle f, float s)
        {
            Color ac = ToColor(b.AttackerColor), dc = ToColor(b.DefenderColor);
            float midY = f.Y + f.Height * 0.56f;
            var aAnchor = new Vector2(f.Center.X - f.Width * 0.09f, midY);
            var dAnchor = new Vector2(f.Center.X + f.Width * 0.09f, midY);
            bool done = b.Status == BattleStatus.Completed;
            int aWin = done && b.WinnerKingdomId == b.AttackerKingdomId ? 1 : done && b.WinnerKingdomId != 0 ? -1 : 0;
            DrawSide(sb, Formation(b.AttackerGroups, true, aAnchor, s, b.Id * 7 + 1, ac), true, s, f, aWin, _lastAttackerLoss > 0, done && b.Peace);
            DrawSide(sb, Formation(b.DefenderGroups, false, dAnchor, s, b.Id * 7 + 2, dc), false, s, f, -aWin, _lastDefenderLoss > 0, done && b.Peace);
            // Sarkardalar (qo'shin orqasida, kattaroq) — CommandGesture raund hal bo'lganda.
            DrawCommander(sb, aAnchor + new Vector2(-150 * s, -10 * s), true, s, ac, aWin);
            DrawCommander(sb, dAnchor + new Vector2(150 * s, -10 * s), false, s, dc, -aWin);
        }

        private void DrawSide(SpriteBatch sb, List<Figure> figs, bool left, float s, Rectangle f, int outcome, bool hit, bool peace)
        {
            float dir = left ? 1f : -1f;
            float t = _sceneTime;
            // March: maydon chetidan o'z joyiga (ease-out), har figura biroz kechikib.
            for (int i = 0; i < figs.Count; i++)
            {
                Figure g = figs[i];
                float delay = (i % 7) * 0.05f;
                float e = MathHelper.Clamp((_enterT - delay) / (1f - 0.35f), 0f, 1f);
                e = 1f - (1f - e) * (1f - e) * (1f - e);
                Vector2 pos = Vector2.Lerp(g.Home - new Vector2(dir * f.Width * 0.45f, 0), g.Home, e);

                // Idle: yengil tebranish (har biri o'z fazasida)
                float bob = MathF.Sin(t * g.Speed + g.Phase) * 1.4f * s;
                // Mavhum harakat: markazga qisqa intilish va qaytish (support orqada qoladi)
                float a = _actionT < 1f ? MathF.Sin(_actionT * MathF.PI) : 0f;
                if (!g.Support) pos.X += dir * a * 26 * s * (0.7f + 0.3f * MathF.Sin(g.Phase));
                // Reaction: yo'qotishda qisqa silkinish
                if (hit && _actionT < 0.6f) pos.X += MathF.Sin(_actionT * 60f + g.Phase) * 2f * s * (1f - _actionT / 0.6f);
                float alpha = 1f;
                // Yakun: g'olib sakraydi (Victory), mag'lub orqaga chekinadi (Withdraw); tinchlikda — joyida turadi.
                if (outcome > 0 && !peace) bob -= MathF.Max(0f, MathF.Sin(t * 5f + g.Phase)) * 6f * s;
                if (outcome < 0 && !peace)
                {
                    float w = MathHelper.Clamp((t - (_battleEndT ?? t)) / 2.5f, 0f, 1f);
                    pos.X -= dir * w * 120 * s;
                    alpha = 1f - 0.6f * w;
                }
                pos.Y += bob;

                int frame = ((int)(t * 2.2f + g.Phase * 3) % 3 + 3) % 3; // sekin kadr almashinuvi
                Texture2D tex = _assets.Get(g.Sprite + "_" + frame) ?? _assets.Get(g.Sprite + "_0");
                if (tex == null) continue;
                // Anchor — oyoq ostida (pastki markaz) va o'zgarmaydi.
                var origin = new Vector2(tex.Width / 2f, tex.Height - 2);
                Color col = g.Tint * alpha;
                if (hit && _actionT < 0.35f) col = Color.Lerp(col, new Color(255, 200, 190), 0.5f * (1f - _actionT / 0.35f));
                sb.Draw(_assets.Pixel, new Rectangle((int)(pos.X - 9 * s), (int)(pos.Y - 2 * s), (int)(18 * s), (int)(5 * s)), new Color(0, 0, 0, 60) * alpha); // soya
                sb.Draw(tex, pos, null, col, 0f, origin, g.Scale, left ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0f);
            }
        }

        private float? _battleEndT;

        private void DrawCommander(SpriteBatch sb, Vector2 pos, bool left, float s, Color kingdom, int outcome)
        {
            float t = _sceneTime;
            float gesture = _actionT < 1f ? MathF.Sin(_actionT * MathF.PI) : 0f;
            pos.Y += MathF.Sin(t * 1.3f + (left ? 0 : 1.7f)) * 1.2f * s - gesture * 5 * s;
            if (outcome > 0) pos.Y -= MathF.Max(0f, MathF.Sin(t * 4f)) * 5 * s;
            Texture2D tex = _assets.Get(SpriteKeys.UnitShield + "_" + ((int)(t * 1.5f) % 3)) ?? _assets.Get(SpriteKeys.UnitShield + "_0");
            if (tex == null) return;
            var origin = new Vector2(tex.Width / 2f, tex.Height - 2);
            sb.Draw(_assets.Pixel, new Rectangle((int)(pos.X - 12 * s), (int)(pos.Y - 2 * s), (int)(24 * s), (int)(6 * s)), new Color(0, 0, 0, 70));
            sb.Draw(tex, pos, null, Color.Lerp(Color.White, kingdom, 0.25f), 0f, origin, 1.6f * s, left ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0f);
            // Sarkarda tug'i (CommandGesture paytida ko'tariladi)
            float poleX = pos.X + (left ? -14 : 14) * s, top = pos.Y - (58 + gesture * 8) * s;
            sb.Draw(_assets.Pixel, new Rectangle((int)poleX, (int)top, Math.Max(1, (int)(2 * s)), (int)(44 * s)), new Color(70, 52, 32));
            for (int i = 0; i < 6; i++)
            {
                float wave = MathF.Sin(t * 5f - i * 0.8f) * 2f * s;
                sb.Draw(_assets.Pixel, new Rectangle((int)(poleX + (left ? 2 : -2 - 16 * s) + (left ? i : -i) * 0f), (int)(top + i * 3 * s + wave), (int)(16 * s), (int)(3 * s) + 1), kingdom);
            }
        }

        private void DrawFlags(UiKit ui, SpriteBatch sb, BattleDto b, Rectangle f, float s)
        {
            float t = _sceneTime;
            for (int side = 0; side < 2; side++)
            {
                Color c = ToColor(side == 0 ? b.AttackerColor : b.DefenderColor);
                float x = side == 0 ? f.X + f.Width * 0.12f : f.Right - f.Width * 0.12f;
                float baseY = f.Y + f.Height * 0.62f;
                int poleH = (int)(f.Height * 0.42f);
                sb.Draw(_assets.Pixel, new Rectangle((int)x, (int)(baseY - poleH), Math.Max(2, (int)(3 * s)), poleH), new Color(92, 70, 44));
                sb.Draw(_assets.Pixel, new Rectangle((int)x - 3, (int)(baseY - poleH) - 4, 9, 6), new Color(230, 190, 90));
                // Saltanat bayrog'i (o'yinchi o'zi yaratgan): hilpiraydi, o'ng tomondagisi dastaga ko'zgu tarzida bog'langan
                int clothW = (int)(72 * s), clothH = (int)(clothW / Shared.Heraldry.FlagSpec.Aspect);
                var spec = FlagRenderer.Spec(side == 0 ? b.AttackerFlag : b.DefenderFlag, c);
                var cloth = side == 0 ? new Rectangle((int)x + 3, (int)(baseY - poleH) + 2, clothW, clothH) : new Rectangle((int)x - clothW, (int)(baseY - poleH) + 2, clothW, clothH);
                FlagRenderer.Draw(ui, cloth, spec, 18, 4f * s, t * 4.2f + side, 1f, side == 1);
            }
        }

        private void DrawDust(UiKit ui, Rectangle f, float s, float dt)
        {
            if (_dust.Count == 0) return;
            for (int i = _dust.Count - 1; i >= 0; i--)
            {
                Vector3 d = _dust[i];
                d.Z += dt;
                if (d.Z > 1.6f) { _dust.RemoveAt(i); continue; }
                _dust[i] = d;
                float x = f.Center.X + (d.X - 0.5f) * f.Width * 0.3f + d.Z * 18 * (d.X - 0.5f) * s;
                float y = f.Y + f.Height * (0.5f + d.Y * 0.14f) - d.Z * 22 * s;
                float r = (6 + 10 * d.Z) * s;
                float alpha = 0.35f * (1f - d.Z / 1.6f);
                ui.Rounded(new Rectangle((int)(x - r), (int)(y - r / 2), (int)(r * 2), (int)r), new Color(214, 196, 160) * alpha, (int)(r / 2));
            }
        }

        // ------------------------------------------------------------------ Yuqori panel
        private static void DrawHeader(UiKit ui, BattleDto b, Rectangle r, int rightInset)
        {
            ThemeTokens T = ui.T;
            ui.Fill(r, T.Surface * 0.94f);
            ui.Fill(new Rectangle(r.X, r.Bottom - 1, r.Width, 1), T.Border);
            int pad = 16, mid = r.Center.X;
            float th = ui.Type.LineHeight(TextStyle.PanelTitle);
            string round = b.Status == BattleStatus.Completed ? "JANG YAKUNLANDI" : b.Round + "-RAUND / " + b.MaxRounds;
            ui.Type.DrawCentered(ui.Batch, round, new Rectangle(mid - 160, r.Y + 6, 320, (int)th), TextStyle.PanelTitle, T.Accent);
            ui.Type.DrawCentered(ui.Batch, ui.Type.Fit(b.RegionName + " uchun" + (b.IsPvp ? " · PvP" : ""), 300, TextStyle.Secondary), new Rectangle(mid - 160, r.Y + 8 + (int)th, 320, 22), TextStyle.Secondary, T.TextSecondary);

            int sideW = Math.Max(160, mid - 180 - pad - rightInset);
            Side(ui, new Rectangle(r.X + pad, r.Y + 8, sideW, r.Height - 16), b.AttackerName, b.AttackerCommander, b.AttackerPower, b.AttackerStartPower, b.AttackerMorale, ToColor(b.AttackerColor), true, b.IAmAttacker && b.IAmParticipant);
            Side(ui, new Rectangle(r.Right - pad - rightInset - sideW, r.Y + 8, sideW, r.Height - 16), b.DefenderName, b.DefenderCommander, b.DefenderPower, b.DefenderStartPower, b.DefenderMorale, ToColor(b.DefenderColor), false, !b.IAmAttacker && b.IAmParticipant);
        }

        private static void Side(UiKit ui, Rectangle r, string name, string commander, int power, int start, int morale, Color c, bool left, bool mine)
        {
            ThemeTokens T = ui.T;
            float lh = ui.Type.LineHeight(TextStyle.Button), sh = ui.Type.LineHeight(TextStyle.Caption);
            ui.Rounded(new Rectangle(left ? r.X : r.Right - 6, r.Y + 2, 6, r.Height - 4), c, 3);
            int x = left ? r.X + 14 : r.X, w = r.Width - 14;
            string title = name + (mine ? " (siz)" : "");
            var tr = new Rectangle(x, r.Y, w, (int)lh);
            if (left) ui.TextFit(title, tr, TextStyle.Button); else ui.Type.DrawRight(ui.Batch, ui.Type.Fit(title, w, TextStyle.Button), new Vector2(r.Right - 14, r.Y), TextStyle.Button, T.TextPrimary);
            int y = r.Y + (int)lh + 2;
            string cmd = string.IsNullOrEmpty(commander) ? "" : commander;
            if (cmd.Length > 0)
            {
                if (left) ui.TextFit(cmd, new Rectangle(x, y, w, (int)sh), TextStyle.Caption, T.TextSecondary);
                else ui.Type.DrawRight(ui.Batch, ui.Type.Fit(cmd, w, TextStyle.Caption), new Vector2(r.Right - 14, y), TextStyle.Caption, T.TextSecondary);
                y += (int)sh + 3;
            }
            int barH = Math.Max(8, (int)(sh * 0.7f));
            Bar(ui, new Rectangle(x, y, w, barH), power / (float)Math.Max(1, start), c, "Kuch " + power, left);
            y += barH + (int)sh + 4;
            Bar(ui, new Rectangle(x, y, w, barH), morale / 100f, new Color(240, 200, 110), "Ruh " + morale, left);
        }

        private static void Bar(UiKit ui, Rectangle r, float v, Color c, string label, bool left)
        {
            ui.Rounded(r, ui.T.Disabled, r.Height / 2);
            int w = (int)(r.Width * MathHelper.Clamp(v, 0f, 1f));
            if (w > 0) ui.Rounded(left ? new Rectangle(r.X, r.Y, w, r.Height) : new Rectangle(r.Right - w, r.Y, w, r.Height), c, r.Height / 2);
            var lr = new Rectangle(r.X, r.Bottom + 1, r.Width, (int)ui.Type.LineHeight(TextStyle.Caption));
            if (left) ui.Text(label, new Vector2(lr.X, lr.Y), TextStyle.Caption, ui.T.TextSecondary);
            else ui.Type.DrawRight(ui.Batch, label, new Vector2(lr.Right, lr.Y), TextStyle.Caption, ui.T.TextSecondary);
        }

        // ------------------------------------------------------------------ Qaror paneli
        private BattleSceneAction DrawDecisionPanel(UiKit ui, BattleDto b, Rectangle r, bool busy)
        {
            ThemeTokens T = ui.T;
            ui.Fill(r, T.Surface * 0.96f);
            ui.Fill(new Rectangle(r.X, r.Y, r.Width, 1), T.Border);
            int pad = 14, gap = ui.Gap, h = ui.ControlH;
            int x = r.X + pad, w = r.Width - 2 * pad, y = r.Y + 8;
            string status;
            if (!b.IAmParticipant) status = "Siz bu jangni kuzatyapsiz.";
            else if (b.MyDecisionSubmitted) status = b.IsPvp ? "Qaroringiz yuborildi. Raqib qarori kutilmoqda" + (b.RoundSecondsLeft > 0 ? " (" + b.RoundSecondsLeft + " s)" : "") + "…" : "Server raundni hisoblamoqda…";
            else status = b.Rounds.Count > 0 ? b.Rounds[b.Rounds.Count - 1].SummaryUz + "  Keyingi qaroringiz:" : "Raund uchun strategik yondashuvni tanlang:";
            ui.TextFit(status, new Rectangle(x, y, w, (int)ui.Type.LineHeight(TextStyle.Secondary)), TextStyle.Secondary, b.MyDecisionSubmitted ? T.Accent : T.TextSecondary);
            y += (int)ui.Type.LineHeight(TextStyle.Secondary) + 6;
            if (!b.IAmParticipant) return BattleSceneAction.None;

            var decisions = (BattleDecision[])Enum.GetValues(typeof(BattleDecision));
            int confirmW = Math.Min(260, w / 4);
            int bw = (w - confirmW - gap - gap * (decisions.Length - 1)) / decisions.Length;
            bool locked = b.MyDecisionSubmitted || busy;
            for (int i = 0; i < decisions.Length; i++)
            {
                BattleDecision d = decisions[i];
                var br = new Rectangle(x + i * (bw + gap), y, bw, (int)(h * 1.5f));
                bool sel = _selected == d;
                if (ui.Card(br, sel, !locked) && !locked) _selected = d;
                ui.Type.DrawCentered(ui.Batch, ui.Type.Fit(WarRules.DecisionName(d), br.Width - 12, TextStyle.Button), new Rectangle(br.X, br.Y + 4, br.Width, (int)(br.Height * 0.55f)), TextStyle.Button, sel ? T.Primary : T.TextPrimary);
                ui.Type.DrawCentered(ui.Batch, ui.Type.Fit(WarRules.DecisionHint(d), br.Width - 12, TextStyle.Caption), new Rectangle(br.X, br.Y + (int)(br.Height * 0.5f), br.Width, (int)(br.Height * 0.45f)), TextStyle.Caption, T.TextSecondary);
            }
            var cr = new Rectangle(x + w - confirmW, y, confirmW, (int)(h * 1.5f));
            if (ui.Button(cr, locked ? "Kutilmoqda…" : "TASDIQLASH", ButtonKind.Primary, !locked && _selected != null, "Qarorni serverga yuborish"))
                return BattleSceneAction.Decide;
            return BattleSceneAction.None;
        }

        // ------------------------------------------------------------------ Natija oynasi
        private BattleSceneAction DrawResult(UiKit ui, BattleDto b, int vw, int vh)
        {
            if (_battleEndT == null) _battleEndT = _sceneTime;
            ThemeTokens T = ui.T;
            float appear = MathHelper.Clamp((_sceneTime - _battleEndT.Value - 0.9f) / 0.5f, 0f, 1f);
            if (appear <= 0f) return BattleSceneAction.None;
            ui.Fill(new Rectangle(0, 0, vw, vh), T.Scrim * (0.6f * appear));
            int h = ui.ControlH, gap = ui.Gap;
            // Telefonda shrift kattaroq (UiScale) — oyna ham mos ravishda kengayadi, tugma matnlari qisqarmaydi.
            string[] labels = { "XARITAGA QAYTISH", "HUDUDNI BOSHQARISH", _showReport ? "XULOSA" : "JANG HISOBOTI" };
            int needBtns = 0;
            foreach (string l in labels) needBtns += (int)ui.Type.Measure(l, TextStyle.Button).X + 36;
            int w = Math.Min(vw - 32, Math.Max((int)(640 * Math.Max(1f, ui.Type.UiScale)), needBtns + 2 * gap + 48));
            float lh = ui.Type.LineHeight(TextStyle.Secondary);
            int lines = _showReport ? Math.Max(5, b.Rounds.Count + 2) : 7;
            int mh = Math.Min(vh - 24, (int)(ui.Type.LineHeight(TextStyle.ScreenTitle) + ui.Type.LineHeight(TextStyle.PanelTitle) + lines * (lh + 6) + h * 2 + gap * 4 + 50));
            var m = new Rectangle((vw - w) / 2, (vh - mh) / 2 + (int)((1f - appear) * 30), w, mh);
            ui.Panel(m);
            ui.Rounded(new Rectangle(m.X, m.Y, m.Width, 6), T.Accent, 3);
            int x = m.X + 24, y = m.Y + 18, cw = m.Width - 48;
            ui.Text("JANG YAKUNLANDI", new Vector2(x, y), TextStyle.ScreenTitle, T.Accent);
            y += (int)ui.Type.LineHeight(TextStyle.ScreenTitle) + 4;
            int me = b.IAmAttacker ? b.AttackerKingdomId : b.DefenderKingdomId;
            string outcome = b.Peace ? "Tinchlik" : !b.IAmParticipant ? (b.WinnerKingdomId == b.AttackerKingdomId ? b.AttackerName : b.DefenderName) + " g'alabasi" : b.WinnerKingdomId == me ? "G'alaba" : "Mag'lubiyat";
            Color oc = b.Peace ? T.Secondary : b.WinnerKingdomId == me ? T.Success : T.Danger;
            ui.Text("Natija: " + outcome, new Vector2(x, y), TextStyle.PanelTitle, oc);
            y += (int)ui.Type.LineHeight(TextStyle.PanelTitle) + 8;

            void Line(string a) { ui.TextFit(a, new Rectangle(x, y, cw, (int)lh), TextStyle.Secondary, T.TextPrimary); y += (int)lh + 6; }
            if (!_showReport)
            {
                Line("Hudud: " + b.RegionName + (b.TerritoryTransferred ? " — saltanatga qo'shildi" : ""));
                y += (int)ui.Paragraph("Ishtirokchilar: " + b.AttackerName + " va " + b.DefenderName, new Rectangle(x, y, cw, (int)(lh * 2 + 6)), TextStyle.Secondary, T.TextPrimary, 2) + 6;
                Line("Jang raundlari: " + b.Rounds.Count);
                Line("Yakuniy kuch: " + b.AttackerPower + " / " + b.AttackerStartPower + "  —  " + b.DefenderPower + " / " + b.DefenderStartPower);
                Line("Resurs xarajati: " + b.CostCoins + " tanga");
                y += (int)ui.Paragraph("Diplomatiya: " + b.DiplomacyUz, new Rectangle(x, y, cw, (int)(lh * 2 + 6)), TextStyle.Secondary, T.TextSecondary, 2) + 4;
            }
            else
            {
                foreach (BattleRoundDto r in b.Rounds)
                    Line(r.Round + ". " + WarRules.DecisionName(r.AttackerDecision) + " / " + WarRules.DecisionName(r.DefenderDecision) + " → −" + r.AttackerLoss + " / −" + r.DefenderLoss);
                y += (int)ui.Paragraph(b.ResultUz, new Rectangle(x, y, cw, (int)(lh * 2 + 6)), TextStyle.Secondary, T.TextSecondary, 2) + 4;
            }

            int by = m.Bottom - 16 - h;
            // Tugmalar eni yorliq uzunligiga mutanosib — hech biri qisqarib ketmaydi.
            float k = Math.Min(1f, (cw - 2 * gap) / (float)Math.Max(1, needBtns));
            int extra = Math.Max(0, (cw - 2 * gap - needBtns) / 3);
            int[] bws = new int[3];
            for (int i = 0; i < 3; i++) bws[i] = (int)((ui.Type.Measure(labels[i], TextStyle.Button).X + 36) * k) + extra;
            BattleSceneAction act = BattleSceneAction.None;
            int bx0 = x;
            if (ui.Button(new Rectangle(bx0, by, bws[0], h), labels[0], ButtonKind.Primary, true, "Dunyo xaritasiga qaytish")) act = BattleSceneAction.OpenRegion;
            bx0 += bws[0] + gap;
            if (ui.Button(new Rectangle(bx0, by, bws[1], h), labels[1], ButtonKind.Secondary, b.TerritoryTransferred && b.WinnerKingdomId == me)) act = BattleSceneAction.ManageRegion;
            bx0 += bws[1] + gap;
            if (ui.Button(new Rectangle(bx0, by, bws[2], h), labels[2], ButtonKind.Ghost)) _showReport = !_showReport;
            if (act != BattleSceneAction.None) { _battleEndT = null; }
            return act;
        }

        // ------------------------------------------------------------------ yordamchilar
        private static SpriteBatch _sbStatic;
        private static SpriteBatch SbOf(UiKit ui) => _sbStatic;
        public static void Bind(SpriteBatch sb) => _sbStatic = sb;

        private static Color ToColor(int[] c) => c != null && c.Length >= 3 ? new Color(c[0], c[1], c[2]) : Color.Gray;
    }
}
