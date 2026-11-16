using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Core.Graphics
{
    /// <summary>Placeholder san'at uchun umumiy ranglar palitrasi (sharqona loy-g'isht, feruza, zarhal).</summary>
    public static class Pal
    {
        public static readonly Color Mud = new Color(200, 166, 120);
        public static readonly Color MudDark = new Color(178, 142, 100);
        public static readonly Color Brick = new Color(190, 138, 94);
        public static readonly Color BrickPale = new Color(210, 174, 128);
        public static readonly Color Plaster = new Color(226, 208, 172);
        public static readonly Color Paving = new Color(206, 178, 134);
        public static readonly Color Turquoise = new Color(40, 170, 178);
        public static readonly Color Blue = new Color(42, 98, 176);
        public static readonly Color DeepBlue = new Color(26, 58, 124);
        public static readonly Color Gold = new Color(222, 182, 64);
        public static readonly Color Wood = new Color(118, 78, 44);
        public static readonly Color Door = new Color(54, 36, 24);
        public static readonly Color Leaf = new Color(78, 136, 64);
        public static readonly Color LeafDark = new Color(54, 108, 56);
        public static readonly Color LeafLight = new Color(118, 166, 76);
        public static readonly Color Trunk = new Color(98, 70, 46);
        public static readonly Color Water = new Color(66, 150, 186);
        public static readonly Color Stone = new Color(156, 150, 140);
        public static readonly Color Red = new Color(174, 50, 42);
        public static readonly Color Cream = new Color(236, 220, 184);
        public static readonly Color Ink = new Color(46, 30, 22);
        public static readonly Color Steel = new Color(180, 186, 194);
    }

    /// <summary>
    /// Izometrik "g'isht terish" yordamchisi: n x n tile izli sprite ichida (x, y, z) koordinatada
    /// quti, gumbaz, minora, ravoq kabi shakllarni chizadi.
    ///   x, y — tile birligida (0..n), z — balandlik pikselda.
    /// Ko'rinadigan yuzalar: tepa, SW (y = const, chap-old) va SE (x = const, o'ng-old).
    /// Yorug'lik chapdan tushadi: SW yorug', SE soyada.
    /// </summary>
    public sealed class IsoCanvas
    {
        public readonly Canvas C;
        public readonly int Size;
        private readonly float _ox, _oy;

        public IsoCanvas(int size, int height, int padX = 0)
        {
            Size = size;
            C = new Canvas(size * GameConfig.TileWidth + padX * 2, size * GameConfig.TileHeight + height);
            _ox = C.Width / 2f;
            _oy = height;
        }

        public Vector2 P(float x, float y, float z)
        {
            return new Vector2(_ox + (x - y) * GameConfig.HalfTileW, _oy + (x + y) * GameConfig.HalfTileH - z);
        }

        // ---------- Tekis yuzalar ----------

        public void Top(float x0, float y0, float x1, float y1, float z, Color col, float noise = 0.05f)
        {
            C.FillPolygon(col, noise, P(x0, y0, z), P(x1, y0, z), P(x1, y1, z), P(x0, y1, z));
        }

        /// <summary>y = const tekisligidagi to'rtburchak (rang o'zgartirilmaydi).</summary>
        public void QuadSW(float x0, float x1, float y, float z0, float z1, Color col, float noise = 0f)
        {
            C.FillPolygon(col, noise, P(x0, y, z1), P(x1, y, z1), P(x1, y, z0), P(x0, y, z0));
        }

        /// <summary>x = const tekisligidagi to'rtburchak (rang o'zgartirilmaydi).</summary>
        public void QuadSE(float x, float y0, float y1, float z0, float z1, Color col, float noise = 0f)
        {
            C.FillPolygon(col, noise, P(x, y0, z1), P(x, y1, z1), P(x, y1, z0), P(x, y0, z0));
        }

        public void FaceSW(float x0, float x1, float y, float z0, float z1, Color col, float noise = 0.05f, int brick = 0)
        {
            Color c = Canvas.Shade(col, 0.93f);
            QuadSW(x0, x1, y, z0, z1, c, noise);
            if (brick > 0)
            {
                Color line = Canvas.Alpha(Canvas.Shade(c, 0.72f), 110);
                for (float z = z0 + brick; z < z1 - 1; z += brick)
                    C.Line(P(x0, y, z), P(x1, y, z) - new Vector2(1, 0), line);
            }
        }

        public void FaceSE(float x, float y0, float y1, float z0, float z1, Color col, float noise = 0.05f, int brick = 0)
        {
            Color c = Canvas.Shade(col, 0.70f);
            QuadSE(x, y0, y1, z0, z1, c, noise);
            if (brick > 0)
            {
                Color line = Canvas.Alpha(Canvas.Shade(c, 0.72f), 110);
                for (float z = z0 + brick; z < z1 - 1; z += brick)
                    C.Line(P(x, y1, z), P(x, y0, z) - new Vector2(1, 0), line);
            }
        }

        /// <summary>To'g'ri burchakli quti: ikki old yuza + tepa.</summary>
        public void Box(float x0, float y0, float x1, float y1, float z0, float z1, Color col,
            int brick = 0, Color? top = null, float noise = 0.05f)
        {
            FaceSW(x0, x1, y1, z0, z1, col, noise, brick);
            FaceSE(x1, y0, y1, z0, z1, col, noise, brick);
            Top(x0, y0, x1, y1, z1, Canvas.Shade(top ?? col, 1.08f), noise);
        }

        /// <summary>Quti tepasida chetlab o'tgan pastroq "parapet" effekti.</summary>
        public void Parapet(float x0, float y0, float x1, float y1, float z, Color roof, float inset = 0.1f)
        {
            Top(x0 + inset, y0 + inset, x1 - inset, y1 - inset, z, Canvas.Shade(roof, 0.9f));
        }

        // ---------- Ravoq (sharqona o'tkir uchli arka) ----------

        /// <param name="sw">true: y = plane tekisligida (center — x); false: x = plane tekisligida (center — y).</param>
        public void Arch(bool sw, float center, float plane, float halfW, float z0, float z1, Color col)
        {
            float rise = MathF.Min(halfW * GameConfig.HalfTileW * 1.25f, z1 - z0);
            float zs = z1 - rise;
            var pts = new List<Vector2>();
            Vector2 F(float u, float z) => sw ? P(u, plane, z) : P(plane, u, z);
            pts.Add(F(center - halfW, z0));
            pts.Add(F(center - halfW, zs));
            const int n = 5;
            for (int i = 1; i < n; i++)
            {
                float a = i / (float)n * MathHelper.PiOver2;
                pts.Add(F(center - halfW * MathF.Cos(a), zs + rise * MathF.Pow(MathF.Sin(a), 0.75f)));
            }
            pts.Add(F(center, z1));
            for (int i = n - 1; i >= 1; i--)
            {
                float a = i / (float)n * MathHelper.PiOver2;
                pts.Add(F(center + halfW * MathF.Cos(a), zs + rise * MathF.Pow(MathF.Sin(a), 0.75f)));
            }
            pts.Add(F(center + halfW, zs));
            pts.Add(F(center + halfW, z0));
            C.FillPolygon(col, 0f, pts.ToArray());
        }

        /// <summary>Koshinli ramka ichidagi qorong'i ravoq.</summary>
        public void FramedArch(bool sw, float center, float plane, float halfW, float z0, float z1, Color frame, Color inner)
        {
            Arch(sw, center, plane, halfW, z0, z1, sw ? frame : Canvas.Shade(frame, 0.78f));
            Arch(sw, center, plane, halfW * 0.72f, z0, z1 - 4, sw ? inner : Canvas.Shade(inner, 0.8f));
        }

        // ---------- Aylana shakllar ----------

        private static float SideLight(float u) => 1.12f - 0.46f * (u + 1f) * 0.5f;

        /// <summary>Vertikal silindr (minora tanasi, gumbaz barabani). r — ekran pikselida.</summary>
        public void Cylinder(float cx, float cy, float z0, float z1, float r, Color col, int brick = 0)
        {
            Vector2 c = P(cx, cy, 0);
            int ir = (int)MathF.Ceiling(r);
            for (int dx = -ir; dx <= ir; dx++)
            {
                float u = (dx + 0.5f) / r;
                if (MathF.Abs(u) > 1f) continue;
                float e = r * 0.5f * MathF.Sqrt(1f - u * u);
                int yTop = (int)MathF.Round(c.Y - z1);
                int yBot = (int)MathF.Round(c.Y - z0 + e);
                Color sc = Canvas.Shade(col, SideLight(u));
                for (int y = yTop; y < yBot; y++)
                {
                    Color pc = sc;
                    if (brick > 0 && ((int)(c.Y - y) % brick) == 0) pc = Canvas.Shade(sc, 0.86f);
                    C.Blend((int)MathF.Floor(c.X + dx), y, pc, 0.04f);
                }
            }
            C.FillEllipse(c.X, c.Y - z1, r, r * 0.5f, Canvas.Shade(col, 1.1f), 0.04f);
        }

        /// <summary>Silindrni o'rab turgan rangli halqa (koshin belbog').</summary>
        public void Band(float cx, float cy, float z0, float z1, float r, Color col)
        {
            Vector2 c = P(cx, cy, 0);
            int ir = (int)MathF.Ceiling(r);
            for (int dx = -ir; dx <= ir; dx++)
            {
                float u = (dx + 0.5f) / r;
                if (MathF.Abs(u) > 1f) continue;
                float e = r * 0.5f * MathF.Sqrt(1f - u * u);
                Color sc = Canvas.Shade(col, SideLight(u));
                for (int y = (int)MathF.Round(c.Y - z1 + e); y < (int)MathF.Round(c.Y - z0 + e); y++)
                    C.Blend((int)MathF.Floor(c.X + dx), y, sc);
            }
        }

        /// <summary>Sharqona (biroz o'tkir uchli, "piyozsimon") gumbaz.</summary>
        public void Dome(float cx, float cy, float z0, float r, float h, Color col, int ribs = 0, bool finial = true)
        {
            Vector2 c = P(cx, cy, z0);
            // Asosning pastki yarim ellipsi
            for (int dy = 0; dy <= (int)(r * 0.5f); dy++)
            {
                float hw = r * MathF.Sqrt(MathF.Max(0f, 1f - MathF.Pow(dy / (r * 0.5f), 2)));
                DomeRow(c.X, (int)MathF.Round(c.Y) + dy, hw, col, ribs, 0f);
            }
            for (int t = 1; t <= (int)h; t++)
            {
                float f = t / h;
                float hw = r * MathF.Pow(MathF.Cos(f * MathHelper.PiOver2), 0.72f) * (1f + 0.13f * MathF.Sin(MathHelper.Pi * f));
                DomeRow(c.X, (int)MathF.Round(c.Y) - t, hw, col, ribs, f);
            }
            if (finial)
            {
                Vector2 top = new Vector2(c.X, c.Y - h);
                C.Line(top, top - new Vector2(0, 8), Pal.Gold);
                C.FillEllipse(top.X + 0.5f, top.Y - 3.5f, 1.8f, 1.8f, Pal.Gold);
            }
        }

        private void DomeRow(float cx, int y, float hw, Color col, int ribs, float f)
        {
            if (hw < 0.5f) return;
            int ih = (int)MathF.Ceiling(hw);
            for (int dx = -ih; dx <= ih; dx++)
            {
                float u = (dx + 0.5f) / hw;
                if (MathF.Abs(u) > 1f) continue;
                float light = SideLight(u) + 0.10f * f;
                // Yaltiroq dog'
                float hx = u + 0.42f, hy = f - 0.55f;
                light += 0.22f * MathF.Max(0f, 1f - (hx * hx * 6f + hy * hy * 8f));
                if (ribs > 0)
                {
                    float a = MathF.Asin(Math.Clamp(u, -1f, 1f)) / MathHelper.Pi * ribs + 0.5f;
                    if (a - MathF.Floor(a) < 0.16f) light *= 0.86f;
                }
                C.Blend((int)MathF.Floor(cx + dx), y, Canvas.Shade(col, light), 0.03f);
            }
        }

        /// <summary>Konus tom.</summary>
        public void Cone(float cx, float cy, float z0, float r, float h, Color col)
        {
            Vector2 c = P(cx, cy, z0);
            for (int dy = 0; dy <= (int)(r * 0.5f); dy++)
            {
                float hw = r * MathF.Sqrt(MathF.Max(0f, 1f - MathF.Pow(dy / (r * 0.5f), 2)));
                DomeRow(c.X, (int)MathF.Round(c.Y) + dy, hw, col, 0, 0f);
            }
            for (int t = 1; t <= (int)h; t++)
                DomeRow(c.X, (int)MathF.Round(c.Y) - t, r * (1f - t / h), col, 0, 0f);
        }

        /// <summary>Ingichka ustun / xoda.</summary>
        public void Pole(float x, float y, float z0, float z1, Color col, int thickness = 2)
        {
            Vector2 a = P(x, y, z0), b = P(x, y, z1);
            C.FillRect((int)a.X - thickness / 2, (int)b.Y, thickness, (int)(a.Y - b.Y), col);
        }

        /// <summary>Xodaga osilgan bayroq.</summary>
        public void Flag(float x, float y, float zTop, Color col, int w = 12, int h = 8)
        {
            Vector2 p = P(x, y, zTop);
            C.FillPolygon(col, 0.04f, p, p + new Vector2(w, 2), p + new Vector2(w - 3, h * 0.5f + 1), p + new Vector2(w, h), p + new Vector2(0, h - 1));
        }

        /// <summary>Devor tepasidagi kungura (tish) — kichik quti.</summary>
        public void Merlon(float x, float y, float s, float z0, float z1, Color col)
        {
            Box(x, y, x + s, y + s, z0, z1, col);
        }
    }
}
