using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.Core.Graphics
{
    /// <summary>
    /// Oddiy dasturiy (CPU) rasm chizgich. Placeholder sprite'lar o'yin ochilganda bir marta
    /// shu yerda chizilib, keyin oddiy Texture2D ga aylanadi — ish vaqtida hech qanday yuk yo'q.
    /// Piksellar "to'g'ri" (premultiply qilinmagan) alfa bilan saqlanadi.
    /// </summary>
    public sealed class Canvas
    {
        public readonly int Width;
        public readonly int Height;
        public readonly Color[] Pixels;
        private readonly float[] _xs = new float[64];

        public Canvas(int width, int height)
        {
            Width = width;
            Height = height;
            Pixels = new Color[width * height];
        }

        // ---------- Rang yordamchilari ----------

        public static Color Shade(Color c, float f)
        {
            return new Color(
                (int)Math.Clamp(c.R * f, 0, 255),
                (int)Math.Clamp(c.G * f, 0, 255),
                (int)Math.Clamp(c.B * f, 0, 255),
                (int)c.A);
        }

        public static Color Alpha(Color c, int a) => new Color((int)c.R, (int)c.G, (int)c.B, a);

        public static Color Mix(Color a, Color b, float t)
        {
            return new Color(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t),
                (int)(a.A + (b.A - a.A) * t));
        }

        /// <summary>Deterministik [0..1) "shovqin".</summary>
        public static float Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFF) / 65536f;
            }
        }

        // ---------- Asosiy primitivlar ----------

        public void Blend(int x, int y, Color c, float noise = 0f)
        {
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height || c.A == 0) return;
            if (noise > 0f) c = Shade(c, 1f + (Hash(x, y) - 0.5f) * 2f * noise);
            int i = y * Width + x;
            if (c.A == 255) { Pixels[i] = c; return; }
            Color d = Pixels[i];
            float sa = c.A / 255f, da = d.A / 255f;
            float oa = sa + da * (1 - sa);
            if (oa <= 0.001f) return;
            Pixels[i] = new Color(
                (int)((c.R * sa + d.R * da * (1 - sa)) / oa),
                (int)((c.G * sa + d.G * da * (1 - sa)) / oa),
                (int)((c.B * sa + d.B * da * (1 - sa)) / oa),
                (int)(oa * 255f));
        }

        public void FillRect(int x, int y, int w, int h, Color c, float noise = 0f)
        {
            for (int j = y; j < y + h; j++)
                for (int i = x; i < x + w; i++)
                    Blend(i, j, c, noise);
        }

        /// <summary>Scanline usulida ko'pburchak to'ldirish.</summary>
        public void FillPolygon(Color c, float noise, params Vector2[] p)
        {
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < p.Length; i++)
            {
                if (p[i].Y < minY) minY = p[i].Y;
                if (p[i].Y > maxY) maxY = p[i].Y;
            }
            int y0 = Math.Max(0, (int)MathF.Floor(minY));
            int y1 = Math.Min(Height - 1, (int)MathF.Ceiling(maxY));
            for (int y = y0; y <= y1; y++)
            {
                float sy = y + 0.5f;
                int n = 0;
                for (int i = 0; i < p.Length && n < _xs.Length; i++)
                {
                    Vector2 a = p[i], b = p[(i + 1) % p.Length];
                    if ((a.Y <= sy && b.Y > sy) || (b.Y <= sy && a.Y > sy))
                        _xs[n++] = a.X + (sy - a.Y) / (b.Y - a.Y) * (b.X - a.X);
                }
                Array.Sort(_xs, 0, n);
                for (int k = 0; k + 1 < n; k += 2)
                {
                    int xa = (int)MathF.Round(_xs[k]);
                    int xb = (int)MathF.Round(_xs[k + 1]);
                    for (int x = xa; x < xb; x++) Blend(x, y, c, noise);
                }
            }
        }

        public void FillEllipse(float cx, float cy, float rx, float ry, Color c, float noise = 0f)
        {
            int x0 = (int)MathF.Floor(cx - rx), x1 = (int)MathF.Ceiling(cx + rx);
            int y0 = (int)MathF.Floor(cy - ry), y1 = (int)MathF.Ceiling(cy + ry);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) Blend(x, y, c, noise);
                }
        }

        /// <summary>Hajmli ko'rinadigan "shar": chap-yuqoridan yoritilgan ellips (daraxt shoxlari uchun).</summary>
        public void FillBlob(float cx, float cy, float rx, float ry, Color c, float noise = 0.08f)
        {
            int x0 = (int)MathF.Floor(cx - rx), x1 = (int)MathF.Ceiling(cx + rx);
            int y0 = (int)MathF.Floor(cy - ry), y1 = (int)MathF.Ceiling(cy + ry);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    if (dx * dx + dy * dy > 1f) continue;
                    float light = 1f - 0.22f * dx - 0.30f * dy;
                    Blend(x, y, Shade(c, light), noise);
                }
        }

        public void Line(Vector2 a, Vector2 b, Color c, int thickness = 1)
        {
            float len = MathF.Max(MathF.Abs(b.X - a.X), MathF.Abs(b.Y - a.Y));
            int steps = Math.Max(1, (int)MathF.Ceiling(len));
            int lastX = int.MinValue, lastY = int.MinValue;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int x = (int)MathF.Floor(a.X + (b.X - a.X) * t);
                int y = (int)MathF.Floor(a.Y + (b.Y - a.Y) * t);
                if (x == lastX && y == lastY) continue;
                lastX = x; lastY = y;
                for (int j = 0; j < thickness; j++)
                    for (int k = 0; k < thickness; k++)
                        Blend(x + k, y + j, c);
            }
        }

        /// <summary>
        /// Siluet chetlarini qoraytiradi — sprite'lar yer fonida aniq ajralib turadi.
        /// </summary>
        public void Outline(Color col, float strength = 0.55f)
        {
            var edge = new bool[Pixels.Length];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    if (Pixels[y * Width + x].A < 160) continue;
                    if (IsClear(x - 1, y) || IsClear(x + 1, y) || IsClear(x, y - 1) || IsClear(x, y + 1))
                        edge[y * Width + x] = true;
                }
            for (int i = 0; i < edge.Length; i++)
                if (edge[i])
                {
                    Color m = Mix(Pixels[i], col, strength);
                    Pixels[i] = new Color((int)m.R, (int)m.G, (int)m.B, 255);
                }
        }

        private bool IsClear(int x, int y)
        {
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height) return true;
            return Pixels[y * Width + x].A < 60;
        }

        // ---------- Texture bilan ishlash ----------

        /// <summary>
        /// Mipmapli premultiplied tekstura: katta (yuqori aniqlikdagi) rasm kichik chizilganda ham silliq — piksel
        /// bo'lib ko'rinmaydi (PointClamp mip darajasini tanlaydi). Android'da mipmapsiz (GLES cheklovlari).
        /// </summary>
        public Texture2D ToTextureMipmapped(GraphicsDevice gd)
        {
#if ANDROID
            return ToTexture(gd);
#else
            int w = Width, h = Height;
            var tex = new Texture2D(gd, w, h, true, SurfaceFormat.Color);
            var lvl = new Vector4[w * h];
            for (int i = 0; i < lvl.Length; i++)
            {
                Color p = Pixels[i];
                float a = p.A / 255f;
                lvl[i] = new Vector4(p.R / 255f * a, p.G / 255f * a, p.B / 255f * a, a);
            }
            for (int level = 0; level < tex.LevelCount; level++)
            {
                var data = new Color[w * h];
                for (int i = 0; i < data.Length; i++) data[i] = new Color(lvl[i]);
                tex.SetData(level, null, data, 0, data.Length);
                int nw = Math.Max(1, w / 2), nh = Math.Max(1, h / 2);
                var next = new Vector4[nw * nh];
                for (int y = 0; y < nh; y++)
                    for (int x = 0; x < nw; x++)
                    {
                        int x0 = Math.Min(w - 1, x * 2), x1 = Math.Min(w - 1, x * 2 + 1), y0 = Math.Min(h - 1, y * 2), y1 = Math.Min(h - 1, y * 2 + 1);
                        next[y * nw + x] = (lvl[y0 * w + x0] + lvl[y0 * w + x1] + lvl[y1 * w + x0] + lvl[y1 * w + x1]) * 0.25f;
                    }
                lvl = next; w = nw; h = nh;
            }
            return tex;
#endif
        }

        /// <summary>Premultiplied-alpha Texture2D yaratadi (SpriteBatch AlphaBlend uchun).</summary>
        public Texture2D ToTexture(GraphicsDevice gd)
        {
            var data = new Color[Pixels.Length];
            for (int i = 0; i < data.Length; i++)
            {
                Color p = Pixels[i];
                data[i] = Color.FromNonPremultiplied(p.R, p.G, p.B, p.A);
            }
            var tex = new Texture2D(gd, Width, Height, false, SurfaceFormat.Color);
            tex.SetData(data);
            return tex;
        }

        public static Canvas FromPng(GraphicsDevice gd, string path)
        {
            using var fs = File.OpenRead(path);
            using var tex = Texture2D.FromStream(gd, fs);
            var c = new Canvas(tex.Width, tex.Height);
            tex.GetData(c.Pixels);
            return c;
        }

        public void SavePng(GraphicsDevice gd, string path)
        {
            using var tex = new Texture2D(gd, Width, Height, false, SurfaceFormat.Color);
            tex.SetData(Pixels);
            using var fs = File.Create(path);
            tex.SaveAsPng(fs, Width, Height);
        }
    }
}
