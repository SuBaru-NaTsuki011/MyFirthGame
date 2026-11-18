using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Online;
using AndijonSaltanatlar.Shared.DTOs;
using AndijonSaltanatlar.UI.Design;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.UI
{
    /// <summary>
    /// Kirgandan keyingi DUNYONI TANLASH va YURTNI TANLASH ekranlari (yangi hukmdor uchun).
    ///   - Dunyolar serverdan olinadi; qaytgan o'yinchining saltanati qaysi dunyoda ekani ko'rsatiladi.
    ///   - Yangi o'yinchi dunyoni tanlaydi, so'ng bo'sh hududni tanlab saltanat quradi.
    /// Barcha tarmoq ishlari fonda; ekran muzlamaydi va takroriy bosishga qarshi bloklanadi.
    /// </summary>
    public sealed class WorldSelectionScreen
    {
        private enum Stage { Loading, Worlds, Regions, Error }

        private readonly CloudSession _cloud;
        private InputManager _input;
        private Stage _stage = Stage.Loading;
        private Stage _lastStage = (Stage)(-1);
        private float _stageTime = -1f;
        private WorldsResponse _worlds;
        private StartRegionsDto _starts;
        private int _worldId = -1;
        private int _regionId = -1;
        private string _kingdomName = "";
        private string _error;
        private bool _busy;
        private string _pendingKeyboard;
        private bool _retryRegions;

        public UiKit Ui { get; set; }
        /// <summary>Mavjud saltanat bilan o'yinga qaytish.</summary>
        public Action ContinueRequested;
        /// <summary>Dunyo va yurt tanlandi (parametr: WorldId).</summary>
        public Action<int> WorldChosen;
        /// <summary>Keyinroq tanlash — o'yin odatdagi oqimda boshlanadi.</summary>
        public Action CancelRequested;
        /// <summary>Tanlangan (allaqachon qo'shilgan) dunyo; 0 — hali qo'shilmagan.</summary>
        public int JoinedWorldId { get; private set; }

        private static readonly Color Gold = new Color(222, 178, 92);
        private static readonly Color SoftBlue = new Color(120, 168, 226);

        public WorldSelectionScreen(CloudSession cloud) { _cloud = cloud; }

        public void Begin()
        {
            _starts = null;
            _error = null;
            _busy = true;
            _stage = Stage.Loading;
            JoinedWorldId = 0;
            _retryRegions = false;
            _cloud.LoadWorlds((d, e) =>
            {
                _busy = false;
                if (e != null || d == null) { Fail(e ?? "DUNYOLARNI YUKLAB BO'LMADI."); return; }
                _worlds = d;
                if (_worldId < 0 && d.Worlds.Count > 0)
                    _worldId = d.MyWorldId ?? d.Worlds[0].Id;
                _stage = Stage.Worlds;
            });
        }

        public void Update(InputManager input)
        {
            _input = input;
            input.MouseOverUI = true;
            string kb = _pendingKeyboard;
            if (kb != null)
            {
                _kingdomName = kb;
                _pendingKeyboard = null;
            }
        }

        public void Draw(SpriteBatch sb, int vw, int vh, float time)
        {
            if (Ui == null || _input == null) return;
            UiKit ui = Ui;
            ui.Begin(sb, _input, vw, vh, time);
            if (_stage != _lastStage) { _lastStage = _stage; _stageTime = time; }
            float appear = MathHelper.Clamp((time - _stageTime) / 0.3f, 0f, 1f);

            ui.Fill(new Rectangle(0, 0, vw, vh), new Color(10, 12, 18) * 0.62f);
            ui.Fill(new Rectangle(0, vh / 2, vw, vh / 2), new Color(10, 12, 18) * 0.24f);

            int w = Math.Min(vw - 32, ui.Compact ? 880 : 820);
            int h = Math.Min(vh - 24, ui.Compact ? 640 : 600);
            var card = new Rectangle((vw - w) / 2, (vh - h) / 2 + (int)((1f - appear) * 22), w, h);
            ui.Panel(card);
            ui.Rounded(new Rectangle(card.X, card.Y, card.Width, 5), Gold, 3);
            ui.Fill(new Rectangle(card.X + 24, card.Y + 6, card.Width - 48, 2), SoftBlue * 0.6f);

            switch (_stage)
            {
                case Stage.Loading: DrawLoading(ui, sb, card, time); break;
                case Stage.Regions: DrawRegions(ui, sb, card, time); break;
                case Stage.Error: DrawError(ui, sb, card, time); break;
                default: DrawWorlds(ui, sb, card, time); break;
            }
            ui.End();
        }

        // ------------------------------------------------------------------ Dunyolar
        private void DrawWorlds(UiKit ui, SpriteBatch sb, Rectangle card, float time)
        {
            ThemeTokens T = ui.T;
            int h = ui.ControlH + (ui.IsTouch ? 8 : 10), gap = ui.Gap;
            int x = card.X + 24, w = card.Width - 48;
            float capH = ui.Type.LineHeight(TextStyle.Secondary);
            int y = card.Y + 20;

            Title(ui, sb, card, ref y, "DUNYONI TANLASH", "Saltanatingiz qaysi dunyoda yashaydi?");
            y += gap;

            // Hukmdor (haqiqiy Google ismi/emaili ko'rsatilmaydi)
            string who = string.IsNullOrEmpty(_cloud.DisplayName) ? "HUKMDOR" : _cloud.DisplayName;
            ui.TextFit(who, new Rectangle(x, y, w, (int)ui.Type.LineHeight(TextStyle.PanelTitle)), TextStyle.PanelTitle, Gold);
            y += (int)ui.Type.LineHeight(TextStyle.PanelTitle) + 2;
            bool joined = _worlds?.MyKingdomId != null;
            string note = joined && !string.IsNullOrWhiteSpace(_worlds.MyKingdomName)
                ? "Saltanatingiz: " + _worlds.MyKingdomName + " · Google emailingiz boshqalarga ko'rinmaydi."
                : "Hisob: Google. Taxallusingiz boshqa o'yinchilarga ko'rinadi.";
            if (_cloud.IsVip) note = "VIP HUKMDOR · " + note;
            ui.TextFit(note, new Rectangle(x, y, w, (int)capH), TextStyle.Caption, _cloud.IsVip ? Gold : T.TextSecondary);
            y += (int)capH + gap;

            int footerH = h + gap * 2 + (int)capH * 2;
            var view = new Rectangle(x, y, w, card.Bottom - footerH - y - 8);
            var worlds = _worlds?.Worlds ?? new List<WorldSummaryDto>();
            int rowH = Math.Max(h + 14, (int)(ui.IsTouch ? 78 : 68));
            int contentH = worlds.Count * (rowH + gap);
            int scroll = ui.Scroll("worlds", view, contentH);
            ui.BeginClip(view);
            for (int i = 0; i < worlds.Count; i++)
            {
                WorldSummaryDto d = worlds[i];
                var row = new Rectangle(view.X, view.Y + i * (rowH + gap) - scroll, view.Width, rowH);
                if (!UiKit.InView(row, view)) continue;
                bool sel = d.Id == _worldId;
                bool mine = d.IsMine || _worlds.MyWorldId == d.Id;
                bool selectable = !joined || mine;   // bir hisob — bitta saltanat; boshqa dunyoga o'tib bo'lmaydi
                if (ui.Card(row, sel, selectable) && selectable) _worldId = d.Id;
                ui.Fill(new Rectangle(row.X + 1, row.Y + 1, 4, row.Height - 2), mine ? T.Success : (sel ? Gold : T.Border));
                int tx = row.X + 16, tw = row.Width - 32;
                ui.TextFit(d.Name, new Rectangle(tx, row.Y + 8, tw - 150, (int)ui.Type.LineHeight(TextStyle.PanelTitle)), TextStyle.PanelTitle, sel ? Gold : selectable ? T.TextPrimary : T.TextSecondary);
                string meta = d.Day + "-kun  ·  " + d.PlayerCount + " o'yinchi  ·  " + d.FreeRegionCount + " bo'sh hudud";
                ui.TextFit(meta, new Rectangle(tx, row.Y + 12 + (int)ui.Type.LineHeight(TextStyle.PanelTitle), tw - 150, (int)capH), TextStyle.Caption, T.TextSecondary);
                if (mine)
                {
                    string badge = "SALTANATINGIZ";
                    float bw = ui.Type.Measure(badge, TextStyle.Caption).X + 18;
                    var br = new Rectangle(row.Right - 16 - (int)bw, row.Center.Y - (int)(capH / 2) - 4, (int)bw, (int)capH + 8);
                    ui.Rounded(br, T.Success * 0.18f, 6);
                    ui.Type.DrawFitted(sb, badge, br, TextStyle.Caption, T.Success, 0);
                }
            }
            ui.EndClip();
            if (worlds.Count == 0) ui.Paragraph("Hozircha dunyo yo'q. Server administratori dunyo yaratishi kerak.", view, TextStyle.Body, T.TextSecondary, 3);

            int fy = card.Bottom - h - 16;
            string label = joined ? "O'YINNI DAVOM ETTIRISH" : "DUNYONI TANLASH";
            bool can = !_busy && _worldId > 0 && (joined || worlds.Count > 0);
            if (ui.Button(new Rectangle(x, fy, w, h), _busy ? "YUKLANMOQDA..." : label, ButtonKind.Primary, can))
            {
                if (joined) ContinueRequested?.Invoke();
                else JoinWorld();
            }
            if (ui.Button(new Rectangle(x, fy + h + gap, w, h - 10), "KEYINROQ", ButtonKind.Ghost, !_busy)) CancelRequested?.Invoke();
        }

        // ------------------------------------------------------------------ Hududlar
        private void DrawRegions(UiKit ui, SpriteBatch sb, Rectangle card, float time)
        {
            ThemeTokens T = ui.T;
            int h = ui.ControlH + (ui.IsTouch ? 8 : 10), gap = ui.Gap;
            int x = card.X + 24, w = card.Width - 48;
            float capH = ui.Type.LineHeight(TextStyle.Secondary);
            int y = card.Y + 20;

            Title(ui, sb, card, ref y, "YURTNI TANLASH", "Bo'sh hududda saltanatingizni quring");
            y += gap;
            string worldName = _worlds?.Worlds.FirstOrDefault(v => v.Id == JoinedWorldId)?.Name ?? "Tanlangan dunyo";
            ui.TextFit(worldName, new Rectangle(x, y, w, (int)ui.Type.LineHeight(TextStyle.PanelTitle)), TextStyle.PanelTitle, Gold);
            y += (int)ui.Type.LineHeight(TextStyle.PanelTitle) + 2;
            ui.TextFit("Yashil belgili hududlar bo'sh. Band hududlarni keyinroq urush yoki diplomatiya bilan olish mumkin.", new Rectangle(x, y, w, (int)capH), TextStyle.Caption, T.TextSecondary);
            y += (int)capH + gap;

            bool wide = w >= 640;
            int footerH = (wide ? h : h * 2 + gap) + gap + (int)capH * 2 + 18;
            var view = new Rectangle(x, y, w, card.Bottom - footerH - y - 8);
            var free = (_starts?.Regions ?? new List<StartRegionDto>())
                .Where(r => r.Free).OrderByDescending(r => r.Population).ThenBy(r => r.RegionId).ToList();
            int rowH = Math.Max(h + 10, (int)(ui.IsTouch ? 66 : 58));
            int contentH = free.Count * (rowH + 6);
            int scroll = ui.Scroll("regions", view, contentH);
            ui.BeginClip(view);
            for (int i = 0; i < free.Count; i++)
            {
                StartRegionDto r = free[i];
                var row = new Rectangle(view.X, view.Y + i * (rowH + 6) - scroll, view.Width, rowH);
                if (!UiKit.InView(row, view)) continue;
                bool sel = r.RegionId == _regionId;
                if (ui.Card(row, sel, true)) _regionId = r.RegionId;
                ui.Fill(new Rectangle(row.X + 1, row.Y + 1, 4, row.Height - 2), sel ? Gold : T.Success);
                float ph = ui.Type.LineHeight(TextStyle.PanelTitle);
                ui.TextFit(r.Name, new Rectangle(row.X + 16, row.Y + 7, row.Width - 32, (int)ph), TextStyle.PanelTitle, sel ? Gold : T.TextPrimary);
                string meta = AndijonSaltanatlar.Shared.Geo.UzMap.ProvinceName(r.Province) + "  ·  aholi: " + r.Population;
                ui.TextFit(meta, new Rectangle(row.X + 16, row.Y + 9 + (int)ph, row.Width - 32, (int)capH), TextStyle.Caption, T.TextSecondary);
            }
            ui.EndClip();
            if (free.Count == 0) ui.Paragraph("Bo'sh hudud qolmagan. Boshqa dunyoni tanlang yoki keyinroq qayta urinib ko'ring.", view, TextStyle.Body, T.Warning, 3);

            int by = card.Bottom - (wide ? h : h * 2 + gap) - (int)capH * 2 - 24;
            ui.TextFit("Saltanat nomi", new Rectangle(x, by, w, (int)capH), TextStyle.Secondary, T.TextSecondary);
            by += (int)capH + 4;
            if (wide)
            {
                int iw = Math.Max(200, w * 55 / 100);
                _kingdomName = ui.TextInput(new Rectangle(x, by, iw, h), "kingdom_name", _kingdomName, "Masalan: Asaka sultonligi", false, AndroidKeyboard) ?? "";
                if (_kingdomName.Length > 40) _kingdomName = _kingdomName.Substring(0, 40);
                if (ui.Button(new Rectangle(x + iw + gap, by, w - iw - gap, h), _busy ? "QURILMOQDA..." : "DAVOM ETISH",
                        ButtonKind.Primary, !_busy && _regionId > 0 && NameOk())) BuildKingdom();
            }
            else
            {
                _kingdomName = ui.TextInput(new Rectangle(x, by, w, h), "kingdom_name", _kingdomName, "Masalan: Asaka sultonligi", false, AndroidKeyboard) ?? "";
                if (_kingdomName.Length > 40) _kingdomName = _kingdomName.Substring(0, 40);
                if (ui.Button(new Rectangle(x, by + h + gap, w, h), _busy ? "QURILMOQDA..." : "DAVOM ETISH",
                        ButtonKind.Primary, !_busy && _regionId > 0 && NameOk())) BuildKingdom();
            }
            if (_error != null)
                ui.Paragraph(_error, new Rectangle(x, card.Bottom - 16 - (int)capH, w, (int)capH), TextStyle.Caption, T.Danger, 1);
        }

        // ------------------------------------------------------------------ Holatlar
        private void DrawLoading(UiKit ui, SpriteBatch sb, Rectangle card, float time)
        {
            ThemeTokens T = ui.T;
            int y = card.Center.Y - 46;
            ui.Type.DrawFitted(sb, "DUNYONI TANLASH", new Rectangle(card.X + 24, y, card.Width - 48, (int)ui.Type.LineHeight(TextStyle.MainTitle)), TextStyle.MainTitle, Gold, 1);
            y += (int)ui.Type.LineHeight(TextStyle.MainTitle) + 14;
            Spinner(ui, new Vector2(card.Center.X, y + 12), time);
            ui.Type.DrawFitted(sb, "Dunyolar yuklanmoqda" + Dots(time), new Rectangle(card.X + 24, y + 30, card.Width - 48, (int)ui.Type.LineHeight(TextStyle.Body)), TextStyle.Body, T.TextPrimary, 1);
        }

        private void DrawError(UiKit ui, SpriteBatch sb, Rectangle card, float time)
        {
            ThemeTokens T = ui.T;
            int h = ui.ControlH + (ui.IsTouch ? 8 : 10), gap = ui.Gap;
            int x = card.X + 24, w = card.Width - 48;
            int y = card.Y + 24;
            Title(ui, sb, card, ref y, "DUNYONI TANLASH", null);
            y += gap * 2;
            ui.Paragraph(_error ?? "Xato", new Rectangle(x, y, w, (int)ui.Type.LineHeight(TextStyle.Body) * 3), TextStyle.Body, T.Danger, 3);
            int fy = card.Bottom - h * 2 - gap - 16;
            if (ui.Button(new Rectangle(x, fy, w, h), "QAYTA URINISH", ButtonKind.Primary, !_busy)) Retry();
            if (ui.Button(new Rectangle(x, fy + h + gap, w, h - 10), "KEYINROQ", ButtonKind.Ghost, !_busy)) CancelRequested?.Invoke();
        }

        private void Fail(string message)
        {
            _error = message;
            _busy = false;
            _stage = Stage.Error;
        }

        private void Retry()
        {
            _error = null;
            if (_retryRegions && JoinedWorldId > 0)
            {
                _busy = true;
                _stage = Stage.Loading;
                _cloud.LoadStartRegions((d, e) =>
                {
                    _busy = false;
                    if (e != null || d == null) { _retryRegions = true; Fail(e ?? "HUDUDLARNI YUKLAB BO'LMADI."); return; }
                    _starts = d;
                    InitRegions();
                    _stage = Stage.Regions;
                });
                return;
            }
            Begin();
        }

        // ------------------------------------------------------------------ Amallar
        private void JoinWorld()
        {
            if (_busy || _worldId <= 0) return;
            _busy = true;
            _error = null;
            _cloud.JoinWorld(_worldId, (state, e) =>
            {
                if (e != null || state == null) { Fail(e ?? "DUNYOGA QO'SHILIB BO'LMADI."); return; }
                JoinedWorldId = state.WorldId;
                _worldId = state.WorldId;
                _stage = Stage.Loading;
                _cloud.LoadStartRegions((d, err) =>
                {
                    _busy = false;
                    if (err != null || d == null) { _retryRegions = true; Fail(err ?? "HUDUDLARNI YUKLAB BO'LMADI."); return; }
                    _starts = d;
                    InitRegions();
                    _stage = Stage.Regions;
                });
            });
        }

        private void InitRegions()
        {
            _regionId = -1;
            _retryRegions = false;
            string suggested = string.IsNullOrWhiteSpace(_starts?.SuggestedName)
                ? (string.IsNullOrWhiteSpace(_cloud.DisplayName) ? "Yangi saltanat" : _cloud.DisplayName + " saltanati")
                : _starts.SuggestedName;
            _kingdomName = suggested ?? "";
        }

        private bool NameOk()
        {
            string n = (_kingdomName ?? "").Trim();
            return n.Length >= 3 && n.Length <= 40;
        }

        private void BuildKingdom()
        {
            if (_busy || _regionId <= 0) return;
            _busy = true;
            _error = null;
            _cloud.ChooseStart(_regionId, (_kingdomName ?? "").Trim(), _starts?.Flag ?? "", (state, e, taken) =>
            {
                _busy = false;
                if (e != null || state == null)
                {
                    _error = e ?? "HUDUDNI TANLAB BO'LMADI.";
                    if (taken) RefreshRegions();   // hudud boshqa o'yinchiga o'tdi — ro'yxatni yangilaymiz
                    return;
                }
                WorldChosen?.Invoke(state.WorldId);
            });
        }

        /// <summary>Band hudud xatosidan keyin bo'sh hududlar ro'yxatini qayta yuklaydi (nom saqlanadi).</summary>
        private void RefreshRegions()
        {
            _cloud.LoadStartRegions((d, e) =>
            {
                if (e != null || d == null) return;
                _starts = d;
                _regionId = -1;
                _retryRegions = false;
            });
        }

        private void AndroidKeyboard()
        {
#if ANDROID
            Task<string> t = KeyboardInput.Show("Saltanat nomi", "3-40 harf", _kingdomName, false);
            t.ContinueWith(r => { if (r.Status == TaskStatus.RanToCompletion && r.Result != null) _pendingKeyboard = r.Result; });
#endif
        }

        // ------------------------------------------------------------------ Yordamchilar
        private static void Title(UiKit ui, SpriteBatch sb, Rectangle card, ref int y, string title, string sub)
        {
            float th = ui.Type.LineHeight(TextStyle.MainTitle), bh = ui.Type.LineHeight(TextStyle.Body);
            ui.Type.DrawFitted(sb, title, new Rectangle(card.X + 16, y, card.Width - 32, (int)th), TextStyle.MainTitle, new Color(222, 178, 92), 1);
            y += (int)th + 2;
            if (sub == null) return;
            ui.Type.DrawFitted(sb, sub, new Rectangle(card.X + 16, y, card.Width - 32, (int)bh), TextStyle.Body, new Color(120, 168, 226), 1);
            y += (int)bh;
        }

        private static string Dots(float time) => new string('.', 1 + (int)(time * 2.5f) % 3);

        private static void Spinner(UiKit ui, Vector2 c, float time)
        {
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * MathF.PI * 2f + time * 5f;
                var p = c + new Vector2(MathF.Cos(a), MathF.Sin(a)) * 8f;
                ui.Fill(new Rectangle((int)p.X - 2, (int)p.Y - 2, 4, 4), Gold * ((i + 1) / 8f));
            }
        }
    }
}
