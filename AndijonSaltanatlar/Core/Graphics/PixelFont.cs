using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.Core.Graphics
{
    /// <summary>
    /// Kodda chizilgan 5x7 piksel shrift. Hech qanday TTF fayl kerak emas — barcha gliflar
    /// o'yin ochilganda bir marta bitta atlasga chiziladi va keyin SpriteBatch bilan tez chiziladi.
    /// O'zbekcha maxsus belgilar (ʻ, ‘, —, × ...) oddiy ASCII ko'rinishga moslanadi.
    /// Kichik harflar bosh harf glifi bilan chiziladi (small-caps uslubi).
    /// </summary>
    public sealed class PixelFont
    {
        public const int GW = 5;   // glif eni (piksel)
        public const int GH = 7;   // glif bo'yi (piksel)

        private readonly Texture2D _atlas;
        private readonly Dictionary<char, int> _index = new Dictionary<char, int>();

        public PixelFont(GraphicsDevice gd)
        {
            Dictionary<char, string[]> table = Glyphs();
            var canvas = new Canvas(table.Count * GW, GH);
            int i = 0;
            foreach (var kv in table)
            {
                _index[kv.Key] = i;
                string[] rows = kv.Value;
                for (int y = 0; y < GH && y < rows.Length; y++)
                {
                    string row = rows[y];
                    for (int x = 0; x < GW && x < row.Length; x++)
                        if (row[x] == '#')
                            canvas.Pixels[y * canvas.Width + (i * GW + x)] = Color.White;
                }
                i++;
            }
            _atlas = canvas.ToTexture(gd);
        }

        /// <summary>Matn o'lchami (piksel) berilgan masshtabda.</summary>
        public Vector2 Measure(string text, int scale = 2, int tracking = 1)
        {
            int n = string.IsNullOrEmpty(text) ? 0 : text.Length;
            int adv = (GW + tracking) * scale;
            float w = n > 0 ? n * adv - tracking * scale : 0;
            return new Vector2(w, GH * scale);
        }

        /// <summary>Matnni chap-yuqori nuqtadan chizadi. Chizilgan kenglikni qaytaradi.</summary>
        public float Draw(SpriteBatch sb, string text, Vector2 pos, Color color, int scale = 2, int tracking = 1)
        {
            if (string.IsNullOrEmpty(text)) return 0f;
            float x = pos.X;
            int adv = (GW + tracking) * scale;
            foreach (char ch in text)
            {
                char c = Normalize(ch);
                if (c != ' ' && _index.TryGetValue(c, out int gi))
                    sb.Draw(_atlas, new Vector2(x, pos.Y), new Rectangle(gi * GW, 0, GW, GH),
                        color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                x += adv;
            }
            return x - pos.X;
        }

        public void DrawCentered(SpriteBatch sb, string text, Vector2 center, Color color, int scale = 2, int tracking = 1)
        {
            Vector2 size = Measure(text, scale, tracking);
            Draw(sb, text, new Vector2(center.X - size.X / 2f, center.Y - size.Y / 2f), color, scale, tracking);
        }

        public void DrawRight(SpriteBatch sb, string text, Vector2 rightTop, Color color, int scale = 2, int tracking = 1)
        {
            Vector2 size = Measure(text, scale, tracking);
            Draw(sb, text, new Vector2(rightTop.X - size.X, rightTop.Y), color, scale, tracking);
        }

        private static char Normalize(char c)
        {
            switch (c)
            {
                case 'ʻ': case '‘': case '’': case '`': case '´': return '\'';
                case '“': case '”': return '"';
                case '—': case '–': case '−': return '-';
                case '…': return '.';
                case '×': return 'X';
                case ' ': return ' ';
            }
            if (c >= 'a' && c <= 'z') return (char)(c - 32);
            return c;
        }

        private static void G(Dictionary<char, string[]> d, char key, params string[] rows) => d[key] = rows;

        private static Dictionary<char, string[]> Glyphs()
        {
            var d = new Dictionary<char, string[]>();

            G(d, '0', ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###.");
            G(d, '1', "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###.");
            G(d, '2', ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####");
            G(d, '3', "#####", "...#.", "..#..", "...#.", "....#", "#...#", ".###.");
            G(d, '4', "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#.");
            G(d, '5', "#####", "#....", "####.", "....#", "....#", "#...#", ".###.");
            G(d, '6', "..##.", ".#...", "#....", "####.", "#...#", "#...#", ".###.");
            G(d, '7', "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#...");
            G(d, '8', ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###.");
            G(d, '9', ".###.", "#...#", "#...#", ".####", "....#", "...#.", ".##..");

            G(d, 'A', ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#");
            G(d, 'B', "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####.");
            G(d, 'C', ".####", "#....", "#....", "#....", "#....", "#....", ".####");
            G(d, 'D', "###..", "#..#.", "#...#", "#...#", "#...#", "#..#.", "###..");
            G(d, 'E', "#####", "#....", "#....", "####.", "#....", "#....", "#####");
            G(d, 'F', "#####", "#....", "#....", "####.", "#....", "#....", "#....");
            G(d, 'G', ".####", "#....", "#....", "#.###", "#...#", "#...#", ".####");
            G(d, 'H', "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#");
            G(d, 'I', ".###.", "..#..", "..#..", "..#..", "..#..", "..#..", ".###.");
            G(d, 'J', "..###", "...#.", "...#.", "...#.", "#..#.", "#..#.", ".##..");
            G(d, 'K', "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#");
            G(d, 'L', "#....", "#....", "#....", "#....", "#....", "#....", "#####");
            G(d, 'M', "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#");
            G(d, 'N', "#...#", "##..#", "##..#", "#.#.#", "#..##", "#..##", "#...#");
            G(d, 'O', ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###.");
            G(d, 'P', "####.", "#...#", "#...#", "####.", "#....", "#....", "#....");
            G(d, 'Q', ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#");
            G(d, 'R', "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#");
            G(d, 'S', ".####", "#....", "#....", ".###.", "....#", "....#", "####.");
            G(d, 'T', "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#..");
            G(d, 'U', "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###.");
            G(d, 'V', "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#..");
            G(d, 'W', "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#");
            G(d, 'X', "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#");
            G(d, 'Y', "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#..");
            G(d, 'Z', "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####");

            G(d, '.', ".....", ".....", ".....", ".....", ".....", ".##..", ".##..");
            G(d, ',', ".....", ".....", ".....", ".....", ".##..", ".##..", ".#...");
            G(d, ':', ".....", ".##..", ".##..", ".....", ".##..", ".##..", ".....");
            G(d, ';', ".....", ".##..", ".##..", ".....", ".##..", ".##..", ".#...");
            G(d, '!', "..#..", "..#..", "..#..", "..#..", "..#..", ".....", "..#..");
            G(d, '?', ".###.", "#...#", "...#.", "..#..", "..#..", ".....", "..#..");
            G(d, '\'', "..#..", "..#..", ".#...", ".....", ".....", ".....", ".....");
            G(d, '"', ".#.#.", ".#.#.", ".#.#.", ".....", ".....", ".....", ".....");
            G(d, '-', ".....", ".....", ".....", "#####", ".....", ".....", ".....");
            G(d, '+', ".....", "..#..", "..#..", "#####", "..#..", "..#..", ".....");
            G(d, '(', "..#..", ".#...", ".#...", ".#...", ".#...", ".#...", "..#..");
            G(d, ')', "..#..", "...#.", "...#.", "...#.", "...#.", "...#.", "..#..");
            G(d, '/', "....#", "....#", "...#.", "..#..", ".#...", "#....", "#....");
            G(d, '%', "##..#", "##.#.", "..#..", ".#...", "#.##.", "..##.", ".....");
            // PROMPT 19A: login, parol va taklif raqamlari uchun
            G(d, '_', ".....", ".....", ".....", ".....", ".....", ".....", "#####");
            G(d, '*', ".....", "#.#.#", ".###.", "#####", ".###.", "#.#.#", ".....");
            G(d, '#', ".#.#.", "#####", ".#.#.", ".#.#.", "#####", ".#.#.", ".....");
            G(d, '@', ".###.", "#...#", "#.###", "#.#.#", "#.###", "#....", ".###.");
            G(d, '=', ".....", ".....", "#####", ".....", "#####", ".....", ".....");
            G(d, '>', ".#...", "..#..", "...#.", "....#", "...#.", "..#..", ".#...");

            return d;
        }
    }
}
