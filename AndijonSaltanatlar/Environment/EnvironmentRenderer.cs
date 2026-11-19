using System;
using AndijonSaltanatlar.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.Environment
{
    /// <summary>
    /// Ob-havo vizual effektlari — yengil, ekran bo'ylab zarralar (yomg'ir chiziqlari, qor nuqtalari).
    /// Zarralar soni cheklangan (kuchsiz GPU uchun). Pixel teksturasidan foydalanadi.
    /// </summary>
    public static class EnvironmentRenderer
    {
        public static void Draw(SpriteBatch sb, Texture2D px, Weather w, int vw, int vh, float time)
        {
            int count = GraphicsQualityManager.ParticleCount;
            if (w == Weather.Rainy) DrawRain(sb, px, vw, vh, time, count);
            else if (w == Weather.Snowy) DrawSnow(sb, px, vw, vh, time, count);
            else if (w == Weather.Cloudy) sb.Draw(px, new Rectangle(0, 0, vw, vh), new Color(40, 44, 60, 40));
            else if (w == Weather.Storm)
            {
                // Bo'ron: quyuq, qorong'i osmon + kuchli yomg'ir
                sb.Draw(px, new Rectangle(0, 0, vw, vh), new Color(24, 28, 42, 95));
                DrawRain(sb, px, vw, vh, time, (int)(count * 1.6f));
            }
            else if (w == Weather.Hot)
            {
                // Jazirama: issiq sarg'ish parda (quyosh jazirasi)
                sb.Draw(px, new Rectangle(0, 0, vw, vh), new Color(255, 196, 96, 26));
            }
        }

        /// <summary> Havolaning shimoli (wind) bilan silkinishi — bayroqlar uchun. </summary>
        public static void DrawFlag(SpriteBatch sb, Texture2D flag, Vector2 pos, float ang)
        {
            sb.Draw(flag, pos, null, Color.White, ang, new Vector2(flag.Width * 0.5f, flag.Height * 0.5f), 1f, SpriteEffects.None, 0f);
        }

        /// <summary> Daraxt shakllari uchun ba'zi yengil silkinish animatsiyasi.
        /// Shunchaki asosiy cheklovlar bilan ko'rsatiladi. </summary>
        public static void DrawTreeSway(SpriteBatch sb, Texture2D sprite, Vector2 pos, float ang, float swayAmount)
        {
            sb.Draw(sprite, pos, null, Color.White, ang * 0.05f * swayAmount, new Vector2(sprite.Width * 0.5f, sprite.Height * 0.5f), 1f, SpriteEffects.None, 0f);
        }

        /// <summary> Shiregi animatsiyasi (ohlash orqali).
        /// Bu shunchaki cheklovli cheklovli nurlanish hisoblanadi — real FX bilan almashtirilishi mumkin. </summary>
        public static void DrawLanternShimmer(SpriteBatch sb, Texture2D lantern, Vector2 pos, float time)
        {
            float glow = 0.7f + 0.3f * (float)Math.Sin(time * 2.5f);
            sb.Draw(lantern, pos, null, Color.White * (0.5f * glow), 0f, new Vector2(0, 0), 1f, SpriteEffects.None, 0f);
        }

        private static void DrawRain(SpriteBatch sb, Texture2D px, int vw, int vh, float time, int count)
        {
            var col = new Color(170, 190, 230, 120);
            for (int i = 0; i < count; i++)
            {
                float sx = Hash(i * 2 + 1) * vw;
                float phase = (time * 520f + Hash(i) * vh) % (vh + 40);
                float y = phase - 20;
                float x = (sx + y * 0.25f) % vw;
                sb.Draw(px, new Vector2(x, y), null, col, 0.35f, Vector2.Zero, new Vector2(2, 12), SpriteEffects.None, 0f);
            }
        }

        private static void DrawSnow(SpriteBatch sb, Texture2D px, int vw, int vh, float time, int count)
        {
            var col = new Color(235, 240, 250, 180);
            for (int i = 0; i < count; i++)
            {
                float sx = Hash(i * 3 + 2) * vw;
                float phase = (time * 70f + Hash(i) * vh) % (vh + 20);
                float y = phase - 10;
                float x = (sx + (float)Math.Sin((time + i) * 0.8f) * 14f + vw) % vw;
                int s = 2 + (int)(Hash(i * 5) * 2);
                sb.Draw(px, new Rectangle((int)x, (int)y, s, s), col);
            }
        }

        private static float Hash(int n)
        {
            unchecked
            {
                uint h = (uint)(n * 374761393 + 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFF) / 65536f;
            }
        }
    }
}
