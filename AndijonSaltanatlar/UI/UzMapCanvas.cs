using System;
using System.Collections.Generic;
using System.Linq;
using AndijonSaltanatlar.Shared.Geo;
using AndijonSaltanatlar.UI.Design;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.UI
{
    /// <summary>Xaritadagi hudud (hudud hududlari bo'yalishi uchun).</summary>
    public readonly struct MapCell
    {
        public readonly int Id;
        public readonly float X, Y;
        public readonly int Owner;
        public readonly Color Color;
        public readonly string Province;
        public readonly bool Mine;

        public MapCell(int id, float x, float y, int owner, Color color, string province, bool mine)
        {
            Id = id; X = x; Y = y; Owner = owner; Color = color; Province = province ?? ""; Mine = mine;
        }
    }

    /// <summary>
    /// O'ZBEKISTON eski xaritasi (pergament, siyoh): chegara, Orol dengizi, daryolar, tog'lar, cho'llar, viloyat
    /// yozuvlari va hududlar (har hudud — eng yaqin shahar atrofidagi maydon, egasi rangida, chegaralari siyohda).
    /// Kattalashtirish: g'ildirak / ikki barmoq / + − tugmalar. Surish: sichqoncha bilan sudrash yoki barmoq.
    /// Hudud maydonlari ko'rinib turgan qism uchun kichik tekstura sifatida hisoblanadi (piksel uslubi) va ko'rinish
    /// o'zgargach qayta chiziladi; surish paytida eski tekstura joyida siljib turadi.
    /// </summary>
    public sealed class UzMapCanvas
    {
        public static readonly Color Ink = new Color(70, 48, 28);
        public static readonly Color Paper = new Color(224, 204, 158);
        private static readonly Color SeaColor = new Color(148, 172, 166);
        private static readonly Color RiverInk = new Color(48, 76, 96);

        private static readonly float WKm = (float)UzMap.WidthKm, HKm = (float)UzMap.HeightKm;
        private static readonly Vector2[] OutlineKm = UzMap.Outline.Select(p => Km(UzMap.X(p.Lon), UzMap.Y(p.Lat))).ToArray();
        private static readonly Vector2[] AralKm = UzMap.Aral.Select(p => Km(UzMap.X(p.Lon), UzMap.Y(p.Lat))).ToArray();
        private static readonly Vector2[][] RiversKm = UzMap.Rivers.Select(r => r.Select(p => Km(UzMap.X(p.Lon), UzMap.Y(p.Lat))).ToArray()).ToArray();
        private static readonly Vector2[] PeaksKm = BuildPeaks();
        private static readonly Vector2[] DesertKm = BuildDesert();
        private static readonly Vector2[] RipplesKm = BuildRipples();

        private static readonly (string Name, double Lon, double Lat)[] Neighbors =
        {
            ("QOZOG'ISTON", 64.2, 45.2), ("TURKMANISTON", 59.6, 39.6), ("TOJIKISTON", 69.6, 38.9), ("QIRG'IZISTON", 72.2, 41.85), ("AFG'ONISTON", 65.2, 37.35),
        };

        private readonly UiKit _ui;
        private Rectangle _area;
        private float _zoom = 1f, _basePx = 1f, _pxPerKm = 1f;
        private Vector2 _center;
        private bool _centered, _dragging;
        private float _lastViewChange = -10f;
        private readonly List<Rectangle> _labels = new List<Rectangle>();
        private Rectangle _btnIn, _btnOut, _btnHome;

        // Hudud maydonlari teksturasi (ko'rinib turgan qism uchun)
        private Texture2D _tex;
        private Vector2 _texKmTL, _texKmBR;
        private int _texKey;
        private float _texZoom;
        private Vector2 _texCenter;
        private Rectangle _texArea;

        public UzMapCanvas(UiKit ui) { _ui = ui; }

        public float Zoom => _zoom;
        public float PxPerKm => _pxPerKm;
        public Rectangle Area => _area;
        /// <summary>Belgilar (qasr, bayroq) kattaligi: uzoqdan kichik, yaqindan katta.</summary>
        /// <summary>
        /// Belgilar (qasr, bayroq) kattaligi: xaritadagi masofaga mos (qo'shni tumanlar ~15 km — qasrlar bir-birini
        /// yopmaydi). Uzoqdan kichik, yaqindan katta.
        /// </summary>
        public float MarkerScale => Math.Clamp(_pxPerKm * 0.42f, 0.36f, 1.45f) * (_ui.IsTouch ? 1.12f : 1f);

        /// <summary>Hudud chegarasi shahardan shuncha km dan uzoqqa cho'zilmaydi (undan narisi — egasiz cho'l/tog').</summary>
        private const float MaxClaimKm = 85f;
        public bool Focused => _centered;

        private static Vector2 Km(float x, float y) => new Vector2(x / 100f * WKm, y / 100f * HKm);

        /// <summary>Ko'rinishni nuqtaga qaratish (masalan o'yinchining poytaxtiga).</summary>
        public void FocusOn(float x, float y, float zoom)
        {
            _center = Km(x, y);
            _zoom = Math.Clamp(zoom, 1f, 14f);
            _centered = true;
            _lastViewChange = -10f;
        }

        public Vector2 ToScreen(float x, float y)
        {
            Vector2 k = Km(x, y);
            return new Vector2(_area.Center.X, _area.Center.Y) + (k - _center) * _pxPerKm;
        }

        private Vector2 KmToScreen(Vector2 k) => new Vector2(_area.Center.X, _area.Center.Y) + (k - _center) * _pxPerKm;
        private Vector2 ScreenToKm(Vector2 s) => _center + (s - new Vector2(_area.Center.X, _area.Center.Y)) / _pxPerKm;

        public bool InView(Vector2 p, int margin = 30)
            => p.X > _area.X - margin && p.X < _area.Right + margin && p.Y > _area.Y - margin && p.Y < _area.Bottom + margin;

        // ------------------------------------------------------------------ Kiritish
        /// <summary>Kadr boshida: maydon, kattalashtirish/surish va tugmalar (bosish shu yerda "yutiladi").</summary>
        public void Begin(Rectangle area)
        {
            _area = area;
            _labels.Clear();
            _basePx = Math.Min(area.Width / WKm, area.Height / HKm) * 0.98f;
            if (!_centered) { _center = new Vector2(WKm / 2, HKm / 2); _centered = true; }
            _pxPerKm = _basePx * _zoom;

            int b = _ui.IsTouch ? (int)(40 * _ui.Type.UiScale) : 30;
            _btnIn = new Rectangle(area.Right - b - 8, area.Y + 8, b, b);
            _btnOut = new Rectangle(area.Right - b - 8, area.Y + 12 + b, b, b);
            _btnHome = new Rectangle(area.Right - b - 8, area.Y + 16 + 2 * b, b, b);
            Vector2 mid = new Vector2(area.Center.X, area.Center.Y);
            if (_ui.Clicked(_btnIn)) ZoomAt(mid, 1.6f);
            if (_ui.Clicked(_btnOut)) ZoomAt(mid, 1f / 1.6f);
            if (_ui.Clicked(_btnHome)) { _zoom = 1f; _center = new Vector2(WKm / 2, HKm / 2); Changed(); }

            Core.InputManager inp = _ui.Input;
            if (inp != null)
            {
                Point m = inp.MousePosition;
                bool over = area.Contains(m);
                if (over && inp.WheelDelta != 0) ZoomAt(new Vector2(m.X, m.Y), (float)Math.Pow(1.0016, inp.WheelDelta));
                if (inp.IsPanDragging) { if (over || _dragging) Pan(inp.MouseDelta); }
                else if (!inp.IsTouch)
                {
                    if (inp.LeftPressed && over) _dragging = true;
                    if (!inp.LeftDown) _dragging = false;
                    if (_dragging && inp.MouseDelta != Point.Zero) Pan(inp.MouseDelta);
                }
            }
            _pxPerKm = _basePx * _zoom;
            ClampCenter();
        }

        private void Changed() => _lastViewChange = _ui.Time;

        private void Pan(Point d)
        {
            if (d == Point.Zero) return;
            _center -= new Vector2(d.X, d.Y) / _pxPerKm;
            Changed();
        }

        private void ZoomAt(Vector2 screen, float factor)
        {
            Vector2 before = ScreenToKm(screen);
            _zoom = Math.Clamp(_zoom * factor, 1f, 14f);
            _pxPerKm = _basePx * _zoom;
            Vector2 after = ScreenToKm(screen);
            _center += before - after;
            Changed();
        }

        private void ClampCenter()
        {
            float hw = _area.Width / 2f / _pxPerKm, hh = _area.Height / 2f / _pxPerKm;
            _center.X = hw * 2 >= WKm ? WKm / 2 : Math.Clamp(_center.X, hw, WKm - hw);
            _center.Y = hh * 2 >= HKm ? HKm / 2 : Math.Clamp(_center.Y, hh, HKm - hh);
        }

        /// <summary>Xaritaga bosildimi (belgilar va tugmalardan keyin chaqiriladi): bosilgan joyga eng yaqin hudud.</summary>
        public int PickNearest(IEnumerable<(int Id, Vector2 Screen)> items, float radius)
        {
            if (!_ui.Clicked(_area)) return -1;
            Point m = _ui.Input.MousePosition;
            int best = -1; float bd = radius * radius;
            foreach ((int id, Vector2 p) in items)
            {
                float d = Vector2.DistanceSquared(p, new Vector2(m.X, m.Y));
                if (d < bd) { bd = d; best = id; }
            }
            return best;
        }

        /// <summary>Yozuv joylashtirish: boshqa yozuv bilan ustma-ust tushsa — chizilmaydi.</summary>
        public bool PlaceLabel(Rectangle r, bool force = false)
        {
            if (!force)
                foreach (Rectangle o in _labels)
                    if (o.Intersects(r)) return false;
            _labels.Add(r);
            return true;
        }

        // ------------------------------------------------------------------ Chizish
        /// <summary>Fon: pergament, hudud maydonlari, dengiz, daryolar, tog'lar, cho'llar, yozuvlar. Kesish (clip) boshlanadi.</summary>
        public void DrawBase(IReadOnlyList<MapCell> cells, int highlightId, Color highlight)
        {
            UiKit ui = _ui;
            ui.BeginClip(_area);
            ui.Fill(_area, Paper);
            // Pergament dog'lari (xaritaga "yopishgan" — surilganda birga suriladi)
            for (int i = 0; i < 26; i++)
            {
                float fx = (i * 0.618f + 0.11f) % 1f, fy = (i * 0.431f + 0.27f) % 1f;
                Vector2 p = KmToScreen(new Vector2(fx * WKm, fy * HKm));
                int rr = (int)((40 + (i * 37) % 70) * _pxPerKm * 0.5f) + 6;
                if (!InView(p, rr)) continue;
                ui.Rounded(new Rectangle((int)p.X - rr, (int)p.Y - rr, rr * 2, rr * 2), new Color(170, 130, 80) * 0.07f, rr);
            }

            EnsureTexture(cells, highlightId, highlight);
            if (_tex != null)
            {
                Vector2 a = KmToScreen(_texKmTL), b = KmToScreen(_texKmBR);
                ui.Batch.Draw(_tex, new Rectangle((int)Math.Round(a.X), (int)Math.Round(a.Y), (int)Math.Round(b.X - a.X), (int)Math.Round(b.Y - a.Y)), Color.White);
            }

            // Dengiz to'lqinchalari
            foreach (Vector2 k in RipplesKm)
            {
                Vector2 p = KmToScreen(k);
                if (!InView(p)) continue;
                int w = Math.Clamp((int)(14 * _pxPerKm), 6, 26);
                ui.Fill(new Rectangle((int)p.X - w / 2, (int)p.Y, w / 3, 1), RiverInk * 0.5f);
                ui.Fill(new Rectangle((int)p.X - w / 6, (int)p.Y - 1, w / 3, 1), RiverInk * 0.5f);
                ui.Fill(new Rectangle((int)p.X + w / 6, (int)p.Y, w / 3, 1), RiverInk * 0.5f);
            }

            // Cho'l (nuqtali shtrix)
            int ds = _zoom > 3f ? 3 : 2;
            foreach (Vector2 k in DesertKm)
            {
                Vector2 p = KmToScreen(k);
                if (InView(p, 4)) ui.Fill(new Rectangle((int)p.X, (int)p.Y, ds, ds), Ink * 0.32f);
            }

            // Daryolar
            int rt = Math.Clamp((int)(1.6f + MathF.Sqrt(_zoom) * 0.9f), 2, 5);
            foreach (Vector2[] river in RiversKm)
                for (int i = 1; i < river.Length; i++)
                    ThickLine(KmToScreen(river[i - 1]), KmToScreen(river[i]), RiverInk * 0.85f, rt);

            // Tog'lar
            int mh = Math.Clamp((int)(13 * _pxPerKm), 7, 30);
            foreach (Vector2 k in PeaksKm)
            {
                Vector2 p = KmToScreen(k);
                if (!InView(p, mh)) continue;
                Mountain(p, (int)(mh * 1.1f), mh);
            }

            // Qo'shni davlatlar va viloyatlar nomi
            if (_zoom < 2.6f)
                foreach ((string n, double lon, double lat) in Neighbors)
                {
                    Vector2 p = ToScreen(UzMap.X(lon), UzMap.Y(lat));
                    Vector2 m = ui.Type.Measure(n, TextStyle.Caption);
                    if (InView(p)) ui.Text(n, p - m / 2, TextStyle.Caption, Ink * 0.38f);
                }
            if (_zoom < 3.0f)
            {
                TextStyle st = _zoom < 1.8f ? TextStyle.Caption : TextStyle.Secondary;
                foreach (MapProvince pr in UzMap.Provinces)
                {
                    string n = pr.Name.ToUpperInvariant();
                    Vector2 p = ToScreen(UzMap.X(pr.LabelLon), UzMap.Y(pr.LabelLat));
                    Vector2 m = ui.Type.Measure(n, st);
                    var r = new Rectangle((int)(p.X - m.X / 2), (int)(p.Y - m.Y / 2), (int)m.X, (int)m.Y);
                    if (!InView(p, (int)m.X) || !PlaceLabel(r)) continue;
                    ui.Text(n, new Vector2(r.X, r.Y), st, Ink * 0.62f);
                }
            }
        }

        /// <summary>Ustki qatlam: ramka, kartush, kompas, masshtab chizig'i va tugmalar. Kesish (clip) tugaydi.</summary>
        public void DrawOverlay(string title)
        {
            UiKit ui = _ui;
            // Chetlari qoraygan
            for (int i = 0; i < 5; i++)
            {
                Color e = new Color(120, 86, 46) * (0.2f - i * 0.035f);
                int k = 5 + i * 6;
                ui.Fill(new Rectangle(_area.X, _area.Y + i * 6, _area.Width, 6), e);
                ui.Fill(new Rectangle(_area.X, _area.Bottom - k, _area.Width, 6), e);
                ui.Fill(new Rectangle(_area.X + i * 6, _area.Y, 6, _area.Height), e);
                ui.Fill(new Rectangle(_area.Right - k, _area.Y, 6, _area.Height), e);
            }
            // Kartush
            Vector2 tm = ui.Type.Measure(title, TextStyle.Secondary);
            var cr = new Rectangle(_area.X + 14, _area.Y + 12, (int)tm.X + 28, (int)ui.Type.LineHeight(TextStyle.Secondary) + 12);
            ui.Fill(new Rectangle(cr.X + 3, cr.Y + 3, cr.Width, cr.Height), Ink * 0.25f);
            ui.Frame(cr, new Color(238, 224, 186), Ink, 2, false);
            ui.Frame(new Rectangle(cr.X + 4, cr.Y + 4, cr.Width - 8, cr.Height - 8), Color.Transparent, Ink * 0.6f, 1, false);
            ui.Type.DrawFitted(ui.Batch, title, cr, TextStyle.Secondary, Ink, 1);

            // Masshtab chizig'i (km)
            int[] nice = { 10, 20, 50, 100, 200, 300, 500 };
            int km = nice.FirstOrDefault(n => n * _pxPerKm >= 70);
            if (km == 0) km = 500;
            int len = (int)(km * _pxPerKm);
            int sx = _area.X + 16, sy = _area.Bottom - 22;
            ui.Fill(new Rectangle(sx - 4, sy - (int)ui.Type.LineHeight(TextStyle.Caption) - 6, len + 8, (int)ui.Type.LineHeight(TextStyle.Caption) + 16), new Color(238, 224, 186) * 0.85f);
            ui.Fill(new Rectangle(sx, sy, len, 3), Ink);
            ui.Fill(new Rectangle(sx, sy - 4, 2, 7), Ink);
            ui.Fill(new Rectangle(sx + len / 2, sy - 3, 2, 5), Ink);
            ui.Fill(new Rectangle(sx + len - 2, sy - 4, 2, 7), Ink);
            ui.Text(km + " km", new Vector2(sx, sy - ui.Type.LineHeight(TextStyle.Caption) - 3), TextStyle.Caption, Ink);

            // Kompas
            float kc = Math.Clamp(_area.Height / 520f, 0.8f, 1.8f);
            Vector2 c = new Vector2(_area.Right - 44 * kc - (_btnIn.Width + 16), _area.Bottom - 48 * kc);
            int cl = (int)(24 * kc), cw = Math.Max(2, (int)(3 * kc));
            ui.Rounded(new Rectangle((int)c.X - cl / 2, (int)c.Y - cl / 2, cl, cl), Ink * 0.15f, cl / 2);
            ui.Fill(new Rectangle((int)c.X - cw / 2, (int)c.Y - cl, cw, cl * 2), Ink);
            ui.Fill(new Rectangle((int)c.X - cl, (int)c.Y - cw / 2, cl * 2, cw), Ink * 0.7f);
            ui.Fill(new Rectangle((int)c.X - cw, (int)c.Y - cl, cw * 2, cl / 2), new Color(160, 40, 30));
            ui.Type.DrawFitted(ui.Batch, "Sh", new Rectangle((int)c.X - 20, (int)c.Y - cl - (int)ui.Type.LineHeight(TextStyle.Caption) - 2, 40, (int)ui.Type.LineHeight(TextStyle.Caption)), TextStyle.Caption, Ink, 1);

            ui.EndClip();
            // Ramka va kattalashtirish tugmalari (kesishdan tashqarida — doim to'liq ko'rinadi)
            ui.Frame(_area, Color.Transparent, new Color(64, 42, 22), 3, false);
            ui.Frame(new Rectangle(_area.X + 6, _area.Y + 6, _area.Width - 12, _area.Height - 12), Color.Transparent, Ink * 0.7f, 1, false);
            MapButton(_btnIn, "+");
            MapButton(_btnOut, "-");
            MapButton(_btnHome, "O");
        }

        private void MapButton(Rectangle r, string glyph)
        {
            UiKit ui = _ui;
            bool hover = ui.Hover(r);
            ui.Frame(r, hover ? new Color(246, 234, 200) : new Color(236, 220, 180), Ink, 2, false);
            ui.Type.DrawFitted(ui.Batch, glyph, r, TextStyle.Button, Ink, 1);
        }

        // ------------------------------------------------------------------ Belgilar uchun yordamchilar
        /// <summary>Ramkali siyoh yozuv (joy bo'lsa). anchor — yozuvning yuqori markazi.</summary>
        public bool Label(string text, Vector2 anchor, Color border, Color fg, bool force = false)
        {
            UiKit ui = _ui;
            Vector2 m = ui.Type.Measure(text, TextStyle.Caption);
            var lr = new Rectangle((int)(anchor.X - m.X / 2 - 5), (int)anchor.Y, (int)m.X + 10, (int)ui.Type.LineHeight(TextStyle.Caption) + 2);
            if (!PlaceLabel(lr, force)) return false;
            ui.Frame(lr, new Color(238, 224, 186), border, 1, false);
            ui.Type.DrawFitted(ui.Batch, text, lr, TextStyle.Caption, fg, 1);
            return true;
        }

        public void ThickLine(Vector2 a, Vector2 b, Color c, int thick)
        {
            float len = Vector2.Distance(a, b);
            if (len < 0.5f) return;
            if (!InView(a, 50) && !InView(b, 50) && !SegmentCrossesArea(a, b)) return;
            Vector2 d = (b - a) / len;
            float step = Math.Max(1f, thick * 0.6f);
            for (float t = 0; t <= len; t += step)
            {
                Vector2 p = a + d * t;
                _ui.Fill(new Rectangle((int)p.X - thick / 2, (int)p.Y - thick / 2, thick, thick), c);
            }
        }

        public void DashedLine(Vector2 a, Vector2 b, Color c, int thick, int dash, int gap)
        {
            float len = Vector2.Distance(a, b);
            if (len < 1) return;
            Vector2 d = (b - a) / len;
            for (float t = 0; t < len; t += dash + gap)
            {
                float e = Math.Min(len, t + dash);
                for (float u = t; u < e; u += 2)
                {
                    Vector2 p = a + d * u;
                    _ui.Fill(new Rectangle((int)p.X - thick / 2, (int)p.Y - thick / 2, thick, thick), c);
                }
            }
        }

        private bool SegmentCrossesArea(Vector2 a, Vector2 b)
        {
            float minX = Math.Min(a.X, b.X), maxX = Math.Max(a.X, b.X), minY = Math.Min(a.Y, b.Y), maxY = Math.Max(a.Y, b.Y);
            return maxX >= _area.X && minX <= _area.Right && maxY >= _area.Y && minY <= _area.Bottom;
        }

        /// <summary>Siyohda chizilgan tog' cho'qqisi: chap qiyalik yorug', o'ng qiyalik shtrixlangan.</summary>
        private void Mountain(Vector2 baseCenter, int w, int h)
        {
            UiKit ui = _ui;
            int step = h > 16 ? 1 : 1;
            for (int row = 0; row < h; row += step)
            {
                float t = row / (float)h;
                int half = (int)(w / 2f * t);
                int y = (int)baseCenter.Y - h + row;
                int cx = (int)baseCenter.X;
                ui.Fill(new Rectangle(cx - half, y, half, step), new Color(214, 192, 146));
                if (row % 2 == 0) ui.Fill(new Rectangle(cx, y, half, 1), Ink * 0.45f);
                ui.Fill(new Rectangle(cx - half - 1, y, 2, step), Ink);
                ui.Fill(new Rectangle(cx + half - 1, y, 2, step), Ink);
            }
        }

        // ------------------------------------------------------------------ Hudud maydonlari teksturasi
        private void EnsureTexture(IReadOnlyList<MapCell> cells, int highlightId, Color highlight)
        {
            int key = 17;
            unchecked
            {
                foreach (MapCell c in cells) key = key * 31 + c.Id * 7 + c.Owner * 131 + (int)c.Color.PackedValue + (c.Mine ? 1 : 0);
                key = key * 31 + highlightId;
                key = key * 31 + (int)highlight.PackedValue;
            }
            bool viewMoved = _tex == null || Math.Abs(_texZoom - _zoom) > 0.001f || Vector2.Distance(_texCenter, _center) > 0.5f || _texArea != _area;
            bool settled = _ui.Time - _lastViewChange > 0.18f;
            if (_tex != null && key == _texKey && !(viewMoved && settled)) return;
            Generate(cells, highlightId, highlight);
            _texKey = key;
        }

        private void Generate(IReadOnlyList<MapCell> cells, int highlightId, Color highlight)
        {
            int tw = Math.Max(64, _area.Width / 3), th = Math.Max(48, _area.Height / 3);
            Vector2 tl = ScreenToKm(new Vector2(_area.X, _area.Y)), br = ScreenToKm(new Vector2(_area.Right, _area.Bottom));
            float sxk = (br.X - tl.X) / tw, syk = (br.Y - tl.Y) / th;
            int n = cells.Count;
            var cx = new float[n]; var cy = new float[n];
            for (int i = 0; i < n; i++) { Vector2 k = Km(cells[i].X, cells[i].Y); cx[i] = k.X; cy[i] = k.Y; }

            var idx = new short[tw * th];
            var xs = new List<float>(16);
            var sea = new List<float>(16);
            for (int j = 0; j < th; j++)
            {
                float ky = tl.Y + (j + 0.5f) * syk;
                Crossings(OutlineKm, ky, xs);
                Crossings(AralKm, ky, sea);
                for (int i = 0; i < tw; i++)
                {
                    float kx = tl.X + (i + 0.5f) * sxk;
                    short v;
                    if (Inside(sea, kx)) v = -2;
                    else if (!Inside(xs, kx) || n == 0) v = -1;
                    else
                    {
                        int best = 0; float bd = float.MaxValue;
                        for (int c = 0; c < n; c++)
                        {
                            float dx = cx[c] - kx, dy = cy[c] - ky, d = dx * dx + dy * dy;
                            if (d < bd) { bd = d; best = c; }
                        }
                        v = bd > MaxClaimKm * MaxClaimKm ? (short)-3 : (short)best;
                    }
                    idx[j * tw + i] = v;
                }
            }

            var data = new Color[tw * th];
            Color outside = new Color(246, 236, 210) * 0.35f;
            for (int j = 0; j < th; j++)
                for (int i = 0; i < tw; i++)
                {
                    int p = j * tw + i;
                    short v = idx[p];
                    if (v == -2) { data[p] = SeaColor * 0.9f; continue; }
                    if (v == -1) { data[p] = outside; continue; }
                    if (v == -3)
                    {
                        // Egasiz yerlar: faqat mamlakat chegarasi chiziladi
                        bool edge = false;
                        for (int q = 0; q < 4 && !edge; q++)
                        {
                            int ni = i + (q == 0 ? 1 : q == 1 ? -1 : 0), nj = j + (q == 2 ? 1 : q == 3 ? -1 : 0);
                            if (ni < 0 || nj < 0 || ni >= tw || nj >= th) continue;
                            short o = idx[nj * tw + ni];
                            edge = o == -1 || o == -2;
                        }
                        data[p] = edge ? Ink : Color.Transparent;
                        continue;
                    }
                    MapCell c = cells[v];
                    Color tint = c.Id == highlightId ? highlight * 0.6f
                        : c.Owner == 0 ? new Color(150, 118, 70) * 0.10f
                        : c.Color * (c.Mine ? 0.46f : 0.32f);
                    float border = 0f;
                    for (int q = 0; q < 4; q++)
                    {
                        int ni = i + (q == 0 ? 1 : q == 1 ? -1 : 0), nj = j + (q == 2 ? 1 : q == 3 ? -1 : 0);
                        if (ni < 0 || nj < 0 || ni >= tw || nj >= th) continue;
                        short o = idx[nj * tw + ni];
                        if (o == v) continue;
                        float s;
                        if (o == -1) s = 1f;
                        else if (o == -2) s = 0.85f;
                        else if (o == -3) s = c.Owner != 0 ? 0.6f : 0.22f;
                        else
                        {
                            MapCell oc = cells[o];
                            if (oc.Owner != c.Owner) s = 0.9f;
                            else if (oc.Province != c.Province) s = o < v ? 0.55f : 0f;
                            else s = o < v ? 0.24f : 0f;
                        }
                        if (s > border) border = s;
                    }
                    data[p] = border > 0f ? Ink * border : tint;
                }

            if (_tex == null || _tex.Width != tw || _tex.Height != th)
            {
                _tex?.Dispose();
                _tex = new Texture2D(_ui.Batch.GraphicsDevice, tw, th);
            }
            _tex.SetData(data);
            _texKmTL = tl; _texKmBR = br;
            _texZoom = _zoom; _texCenter = _center; _texArea = _area;
        }

        private static void Crossings(Vector2[] poly, float y, List<float> xs)
        {
            xs.Clear();
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                Vector2 a = poly[i], b = poly[j];
                if ((a.Y > y) != (b.Y > y)) xs.Add(a.X + (y - a.Y) * (b.X - a.X) / (b.Y - a.Y));
            }
            xs.Sort();
        }

        private static bool Inside(List<float> xs, float x)
        {
            int k = 0;
            for (int i = 0; i < xs.Count; i++) if (xs[i] < x) k++;
            return (k & 1) == 1;
        }

        // ------------------------------------------------------------------ Statik bezaklar (bir marta hisoblanadi)
        private static Vector2[] BuildPeaks()
        {
            var list = new List<Vector2>();
            int seed = 0;
            foreach (var ridge in UzMap.Ridges)
            {
                Vector2[] pts = ridge.Select(p => Km(UzMap.X(p.Lon), UzMap.Y(p.Lat))).ToArray();
                for (int i = 1; i < pts.Length; i++)
                {
                    float len = Vector2.Distance(pts[i - 1], pts[i]);
                    int steps = Math.Max(1, (int)(len / 24f));
                    for (int s = 0; s < steps; s++)
                    {
                        seed++;
                        Vector2 p = Vector2.Lerp(pts[i - 1], pts[i], s / (float)steps);
                        p += new Vector2(((seed * 37) % 11 - 5) * 1.2f, ((seed * 53) % 9 - 4) * 1.5f);
                        list.Add(p);
                    }
                }
            }
            return list.OrderBy(p => p.Y).ToArray();
        }

        private static Vector2[] BuildDesert()
        {
            var list = new List<Vector2>();
            int seed = 1;
            foreach (var d in UzMap.Deserts)
            {
                Vector2 a = Km(UzMap.X(d.LonMin), UzMap.Y(d.LatMax)), b = Km(UzMap.X(d.LonMax), UzMap.Y(d.LatMin));
                for (float y = a.Y; y < b.Y; y += 16f)
                    for (float x = a.X; x < b.X; x += 16f)
                    {
                        seed = seed * 1103515245 + 12345;
                        float jx = ((seed >> 8) & 15) - 7.5f, jy = ((seed >> 16) & 15) - 7.5f;
                        var p = new Vector2(x + jx, y + jy);
                        if (UzMap.InsideCountry(p.X / WKm * 100f, p.Y / HKm * 100f)) list.Add(p);
                    }
            }
            return list.ToArray();
        }

        private static Vector2[] BuildRipples()
        {
            var list = new List<Vector2>();
            var poly = UzMap.Aral.Select(p => (UzMap.X(p.Lon), UzMap.Y(p.Lat))).ToArray();
            int row = 0;
            for (float y = 0; y < HKm; y += 22f, row++)
                for (float x = (row % 2) * 18f; x < WKm; x += 36f)
                    if (UzMap.InPolygon(poly, x / WKm * 100f, y / HKm * 100f)) list.Add(new Vector2(x, y));
            return list.ToArray();
        }
    }
}
