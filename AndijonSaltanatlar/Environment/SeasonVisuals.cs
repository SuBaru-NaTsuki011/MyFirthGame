using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Core.Graphics;
using AndijonSaltanatlar.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.Environment
{
    /// <summary>
    /// FASLLAR KO'RINISHI: yer, daraxt va binolar faslga qarab o'zgaradi.
    ///  - BAHOR: o'rik oppoq-pushti gullaydi, o'tlar yam-yashil, yerda gullar.
    ///  - YOZ:  asl ko'rinish.
    ///  - KUZ:  terak sarg'ayadi, chinor qizg'ish-to'q sariq, yerda xazon.
    ///  - QISH: hamma yoq qor, ariq muzlagan, bargli daraxtlar yalang'och, tomlar va archalarda qor.
    /// Asl teksturalar o'zgartirilmaydi: har bir fasl uchun nusxa bir marta (birinchi chizilganda) yaratiladi va keshlanadi.
    /// O'yin mantig'iga ta'sir qilmaydi (tanlash/bosish asl teksturalar bo'yicha ishlaydi).
    /// </summary>
    public sealed class SeasonVisuals
    {
        private enum ObjKind { None, Poplar, Chinor, Apricot, Cypress, Bush, Flowers, Garden, Prop, Building }

        private readonly GraphicsDevice _gd;
        private readonly Texture2D _baseAtlas;
        private readonly Texture2D[] _atlas = new Texture2D[4];
        private readonly Dictionary<Texture2D, Texture2D>[] _cache =
        {
            new Dictionary<Texture2D, Texture2D>(), new Dictionary<Texture2D, Texture2D>(),
            new Dictionary<Texture2D, Texture2D>(), new Dictionary<Texture2D, Texture2D>()
        };

        public Season Current { get; set; } = Season.Yoz;

        private static readonly Color Snow = new Color(236, 240, 247);

        public SeasonVisuals(GraphicsDevice gd, Texture2D tilesAtlas)
        {
            _gd = gd;
            _baseAtlas = tilesAtlas;
        }

        /// <summary>Joriy fasl uchun yer atlasi (yozda — asl atlas).</summary>
        public Texture2D TileAtlas
        {
            get
            {
                if (Current == Season.Yoz || _baseAtlas == null) return _baseAtlas;
                int i = (int)Current;
                return _atlas[i] ??= BuildAtlas(Current);
            }
        }

        /// <summary>Ekranga umumiy fasl tusi (kun/tun tusiga ko'paytiriladi).</summary>
        public Color AmbientTint => Current switch
        {
            Season.Bahor => new Color(250, 255, 246),
            Season.Kuz => new Color(255, 242, 222),
            Season.Qish => new Color(226, 234, 250),
            _ => Color.White
        };

        /// <summary>Bino / bezak teksturasining faslga mos nusxasi. Odamlar va jonivorlar o'zgarmaydi.</summary>
        public Texture2D ForObject(IWorldObject o, Texture2D tex)
        {
            if (tex == null || Current == Season.Yoz) return tex;
            ObjKind kind = Classify(o);
            if (kind == ObjKind.None) return tex;
            if (Current != Season.Qish && (kind == ObjKind.Prop || kind == ObjKind.Building)) return tex;
            var cache = _cache[(int)Current];
            if (cache.TryGetValue(tex, out Texture2D t)) return t;
            t = Transform(tex, kind, Current);
            cache[tex] = t;
            return t;
        }

        private static ObjKind Classify(IWorldObject o)
        {
            if (o is Decoration d)
            {
                switch (d.Type)
                {
                    case DecorationType.TreePoplar: return ObjKind.Poplar;
                    case DecorationType.TreeChinor: return ObjKind.Chinor;
                    case DecorationType.TreeApricot: return ObjKind.Apricot;
                    case DecorationType.TreeCypress: return ObjKind.Cypress;
                    case DecorationType.Bush: return ObjKind.Bush;
                    case DecorationType.Flowers: return ObjKind.Flowers;
                    default: return ObjKind.Prop;
                }
            }
            if (o is Building b) return b.Type == BuildingType.Garden ? ObjKind.Garden : ObjKind.Building;
            return ObjKind.None;
        }

        // ==================================================================
        // Yordamchilar
        // ==================================================================

        private static Color[] ReadStraight(Texture2D tex)
        {
            var data = new Color[tex.Width * tex.Height];
            tex.GetData(data);
            for (int i = 0; i < data.Length; i++)
            {
                Color p = data[i];
                if (p.A == 0 || p.A == 255) continue;
                float k = 255f / p.A;
                data[i] = new Color((int)Math.Min(255, p.R * k), (int)Math.Min(255, p.G * k), (int)Math.Min(255, p.B * k), (int)p.A);
            }
            return data;
        }

        private Texture2D Upload(Color[] straight, int w, int h)
        {
            var data = new Color[straight.Length];
            for (int i = 0; i < data.Length; i++)
            {
                Color p = straight[i];
                data[i] = Color.FromNonPremultiplied(p.R, p.G, p.B, p.A);
            }
            var tex = new Texture2D(_gd, w, h, false, SurfaceFormat.Color);
            tex.SetData(data);
            return tex;
        }

        private static float Lum(Color c) => (0.3f * c.R + 0.59f * c.G + 0.11f * c.B) / 255f;

        private static Color Scale(Color c, float f, int a) => new Color(
            (int)Math.Clamp(c.R * f, 0, 255), (int)Math.Clamp(c.G * f, 0, 255), (int)Math.Clamp(c.B * f, 0, 255), a);

        private static bool IsFoliage(Color c) => c.A > 0 && c.G > c.R + 6 && c.G > c.B + 6;

        // ==================================================================
        // Yer atlasi
        // ==================================================================

        private Texture2D BuildAtlas(Season s)
        {
            int w = _baseAtlas.Width, h = _baseAtlas.Height;
            Color[] px = ReadStraight(_baseAtlas);
            for (int y = 0; y < h; y++)
            {
                int row = y / TileArt.CellH;
                if (row >= TileArt.RowOverlay) continue; // highlight romblari o'zgarmaydi
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    Color c = px[i];
                    if (c.A == 0) continue;
                    float n = Canvas.Hash(x * 3 + 11, y * 7 + 5);
                    float n2 = Canvas.Hash(x * 5 + 101, y * 3 + 17);
                    px[i] = TilePixel(s, row, c, n, n2);
                }
            }
            return Upload(px, w, h);
        }

        private static Color TilePixel(Season s, int row, Color c, float n, float n2)
        {
            float shade = Lum(c) / 0.55f;
            switch (s)
            {
                case Season.Bahor:
                    if (row == 3)
                    {
                        if (n < 0.010f) return new Color(238, 150, 172, (int)c.A);  // lola
                        if (n < 0.018f) return new Color(248, 246, 236, (int)c.A);  // oq gul
                        if (n < 0.024f) return new Color(248, 220, 92, (int)c.A);   // sariq gul
                        return Canvas.Mix(c, Scale(new Color(104, 182, 80), shade, c.A), 0.35f);
                    }
                    if (row == 0 && n < 0.03f) return Scale(new Color(126, 172, 82), 0.9f + n2 * 0.2f, c.A); // maysa
                    if (row == RowBanks) return Canvas.Mix(c, Scale(new Color(112, 170, 80), shade, c.A), 0.2f);
                    return c;

                case Season.Kuz:
                    if (IsLeafSpot(row, n)) return Leaf(n2, c.A);
                    if (row == 3) return Canvas.Mix(c, Scale(new Color(182, 152, 74), shade, c.A), 0.55f);
                    if (row == RowBanks) return Canvas.Mix(c, Scale(new Color(170, 140, 80), shade, c.A), 0.3f);
                    return c;

                case Season.Qish:
                    switch (row)
                    {
                        case 0:
                        case 3:
                        case RowBanks:
                            return Canvas.Mix(c, Scale(Snow, 0.93f + n * 0.07f, c.A), 0.86f);
                        case 1: // oyoq ostida ezilgan qor
                            return Canvas.Mix(c, Scale(new Color(214, 214, 220), 0.95f + n * 0.08f, c.A), 0.5f);
                        case 2: // qisman supurilgan maydon
                            return Canvas.Mix(c, Scale(Snow, 0.95f + n * 0.05f, c.A), n2 < 0.5f ? 0.65f : 0.45f);
                        case 4:
                        case 5: // muzlagan ariq
                            return Canvas.Mix(c, Scale(new Color(198, 224, 238), 0.96f + n * 0.06f, c.A), 0.62f);
                    }
                    return c;
            }
            return c;
        }

        private const int RowBanks = TileArt.RowBanks;

        private static bool IsLeafSpot(int row, float n) => row switch
        {
            3 => n < 0.04f,
            0 => n < 0.022f,
            1 => n < 0.012f,
            2 => n < 0.008f,
            _ => false
        };

        private static Color Leaf(float n, int a) => n < 0.33f ? new Color(214, 122, 42, a)
                                                    : n < 0.66f ? new Color(186, 70, 38, a)
                                                    : new Color(226, 184, 64, a);

        // ==================================================================
        // Daraxt / bino teksturalari
        // ==================================================================

        private Texture2D Transform(Texture2D tex, ObjKind kind, Season s)
        {
            int w = tex.Width, h = tex.Height;
            Color[] px = ReadStraight(tex);
            // Bino va buyumlar (rasta soyaboni, feruza gumbaz) barg emas — ularga faqat qor qatlami
            bool foliage = kind != ObjKind.Building && kind != ObjKind.Prop;
            for (int y = 0; y < h && foliage; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    Color c = px[i];
                    if (!IsFoliage(c)) continue;
                    // Barg to'dalari: qo'shni piksellar bir xil rangda bo'lsin (2x2 bo'lak)
                    float clump = Canvas.Hash(x / 2 * 13 + 7, y / 2 * 17 + 3);
                    float fine = Canvas.Hash(x * 7 + 1, y * 11 + 9);
                    px[i] = FoliagePixel(kind, s, c, clump, fine);
                }
            if (s == Season.Qish) SnowCap(px, w, h, kind == ObjKind.Building || kind == ObjKind.Prop ? 2 : 1);
            return Upload(px, w, h);
        }

        private static Color FoliagePixel(ObjKind kind, Season s, Color c, float clump, float fine)
        {
            float l = Lum(c);
            float shade = 0.55f + l * 1.1f;
            switch (s)
            {
                case Season.Bahor:
                    if (kind == ObjKind.Apricot && clump < 0.6f)
                        return Scale(fine < 0.5f ? new Color(252, 242, 246) : new Color(246, 192, 210), 0.75f + l * 0.5f, c.A); // o'rik guli
                    if (kind == ObjKind.Cypress) return c;
                    return Canvas.Mix(c, Scale(new Color(122, 198, 92), shade * 0.85f, c.A), 0.3f);

                case Season.Kuz:
                {
                    Color a, b;
                    switch (kind)
                    {
                        case ObjKind.Poplar: a = new Color(232, 190, 58); b = new Color(212, 150, 42); break;
                        case ObjKind.Chinor: a = new Color(214, 110, 40); b = new Color(184, 70, 36); break;
                        case ObjKind.Apricot: a = new Color(228, 152, 54); b = new Color(198, 98, 40); break;
                        case ObjKind.Bush: a = new Color(172, 86, 48); b = new Color(150, 112, 52); break;
                        case ObjKind.Flowers: a = new Color(150, 122, 62); b = new Color(128, 104, 56); break;
                        case ObjKind.Garden: a = new Color(220, 150, 50); b = new Color(176, 92, 44); break;
                        default: return c; // archa doim yashil
                    }
                    Color p = Canvas.Mix(a, b, clump);
                    // Bir oz yashil barg qoladi
                    if (fine < 0.08f) return c;
                    return Scale(p, shade * 0.9f, c.A);
                }

                case Season.Qish:
                    switch (kind)
                    {
                        case ObjKind.Cypress:
                            return fine < 0.22f ? Scale(Snow, 0.9f + l * 0.2f, c.A) : Scale(c, 0.85f, c.A);
                        case ObjKind.Bush:
                        case ObjKind.Flowers:
                        case ObjKind.Garden:
                            // Qor bosgan buta: oppoq, soyasi ko'kimtir
                            return Scale(Canvas.Mix(new Color(198, 210, 228), Snow, l * 1.6f), 1f, c.A);
                        default:
                            // Bargli daraxt yalang'och: shox-shabba va shoxlardagi qor qoladi
                            if (clump < 0.16f) return new Color(96, 74, 58, (int)c.A);
                            if (clump < 0.28f) return Scale(Snow, 0.92f + fine * 0.08f, c.A);
                            return new Color(0, 0, 0, 0);
                    }
            }
            return c;
        }

        /// <summary>Yuqoriga qaragan chetlarga (tom, shox, archa uchi) qor qatlami.</summary>
        private static void SnowCap(Color[] px, int w, int h, int depth)
        {
            var cap = new bool[px.Length];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (px[i].A < 128) continue;
                    if (y > 0 && px[i - w].A >= 64) continue;
                    for (int d = 0; d <= depth && y + d < h; d++)
                    {
                        int j = (y + d) * w + x;
                        if (px[j].A < 128) break;
                        cap[j] = true;
                    }
                }
            for (int i = 0; i < px.Length; i++)
            {
                if (!cap[i]) continue;
                float n = Canvas.Hash(i * 3 + 5, i / w + 7);
                px[i] = Scale(Snow, 0.92f + n * 0.08f, px[i].A);
            }
        }
    }
}
