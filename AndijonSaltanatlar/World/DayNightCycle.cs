using System;
using AndijonSaltanatlar.Core.Graphics;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.World
{
    /// <summary>
    /// Kun-tun aylanishi: GameClock'ning kun ulushidan (0..1) atrof-muhit rangini (Ambient)
    /// va tun darajasini (NightAmount) hisoblaydi. Ambient butun sahnaga multiply bilan
    /// qo'llanadi; NightAmount mash'ala/chiroq yorug'ligi kuchini belgilaydi.
    /// Zamonaviy elektr chiroqlari yo'q — faqat iliq olov yorug'ligi.
    /// </summary>
    public sealed class DayNightCycle
    {
        private static readonly (float t, Color col)[] Keys =
        {
            (0.00f, new Color(68, 80, 128)),   // tun
            (0.12f, new Color(74, 86, 130)),   // sahar
            (0.20f, new Color(238, 198, 162)), // tong (iliq)
            (0.30f, new Color(255, 255, 255)), // ertalab yorug'
            (0.58f, new Color(255, 250, 240)), // kunduz
            (0.70f, new Color(242, 186, 140)), // shom (iliq)
            (0.80f, new Color(68, 80, 128)),   // tun boshlanishi
            (1.00f, new Color(68, 80, 128)),   // tun
        };

        public Color Ambient { get; private set; } = Color.White;
        public float NightAmount { get; private set; }
        public string PhaseName { get; private set; } = "Kunduz";

        public void Update(float dayFraction)
        {
            float f = dayFraction - MathF.Floor(dayFraction);
            Ambient = Sample(f);
            NightAmount = MathHelper.Clamp(1f - Ambient.G / 255f, 0f, 1f);
            PhaseName = f < 0.12f ? "Tun" : f < 0.22f ? "Tong" : f < 0.62f ? "Kunduz" : f < 0.80f ? "Shom" : "Tun";
        }

        private static Color Sample(float f)
        {
            for (int i = 0; i + 1 < Keys.Length; i++)
            {
                if (f >= Keys[i].t && f <= Keys[i + 1].t)
                {
                    float span = Keys[i + 1].t - Keys[i].t;
                    float u = span <= 0f ? 0f : (f - Keys[i].t) / span;
                    return Canvas.Mix(Keys[i].col, Keys[i + 1].col, u);
                }
            }
            return Keys[Keys.Length - 1].col;
        }
    }
}
