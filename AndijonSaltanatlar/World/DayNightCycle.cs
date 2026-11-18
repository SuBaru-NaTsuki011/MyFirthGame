using System;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Core.Graphics;
using AndijonSaltanatlar.Environment;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.World
{
    /// <summary>
    /// Kun-tun aylanishi: GameClock'ning kun ulushidan (0..1) atrof-muhit rangini (Ambient)
    /// va tun darajasini (NightAmount) hisoblaydi. Tong va shom soatlari FASLGA bog'liq
    /// (SeasonClock): yozda kunduz uzun, qishda qisqa. Ambient butun sahnaga multiply bilan
    /// qo'llanadi; NightAmount mash'ala/chiroq yorug'ligi kuchini belgilaydi.
    /// Zamonaviy elektr chiroqlari yo'q — faqat iliq olov yorug'ligi.
    /// </summary>
    public sealed class DayNightCycle
    {
        // Kun bosqichi ranglari
        private static readonly Color Night = new Color(68, 80, 128);
        private static readonly Color Dawn = new Color(238, 198, 162); // iliq tong
        private static readonly Color Morning = new Color(255, 255, 255);
        private static readonly Color Day = new Color(255, 250, 240);
        private static readonly Color Dusk = new Color(242, 186, 140);  // iliq shom

        /// <summary>Tong/shom atrofidagi o'tish uzunligi (kun ulushida ~1.5 soat).</summary>
        private const float Twilight = 0.06f;

        private readonly (float t, Color col)[] _keys = new (float, Color)[9];

        public Color Ambient { get; private set; } = Color.White;
        public float NightAmount { get; private set; }
        public string PhaseName { get; private set; } = "Kunduz";

        /// <summary>Fasl berilmasa — Bahor (o'rtacha kun) bo'yicha.</summary>
        public void Update(float dayFraction) => Update(dayFraction, Season.Bahor);

        public void Update(float dayFraction, Season season)
        {
            float f = dayFraction - MathF.Floor(dayFraction);
            float sr = SeasonClock.Sunrise(season);
            float ss = SeasonClock.Sunset(season);
            BuildKeys(sr, ss);

            Ambient = Sample(f);
            NightAmount = MathHelper.Clamp(1f - Ambient.G / 255f, 0f, 1f);

            if (f < sr - Twilight || f > ss + Twilight) PhaseName = "Tun";
            else if (f < sr + Twilight) PhaseName = "Tong";
            else if (f < ss - Twilight) PhaseName = "Kunduz";
            else PhaseName = "Shom";
        }

        private void BuildKeys(float sr, float ss)
        {
            float midday = (sr + ss) * 0.5f;
            _keys[0] = (0f, Night);
            _keys[1] = (MathHelper.Clamp(sr - Twilight, 0.02f, 0.98f), Night);
            _keys[2] = (sr, Dawn);
            _keys[3] = (sr + Twilight, Morning);
            _keys[4] = (midday, Day);
            _keys[5] = (MathHelper.Clamp(ss - Twilight, sr + Twilight + 0.01f, 0.99f), Day);
            _keys[6] = (ss, Dusk);
            _keys[7] = (MathHelper.Clamp(ss + Twilight, 0.02f, 0.99f), Night);
            _keys[8] = (1f, Night);
        }

        private Color Sample(float f)
        {
            for (int i = 0; i + 1 < _keys.Length; i++)
            {
                if (f >= _keys[i].t && f <= _keys[i + 1].t)
                {
                    float span = _keys[i + 1].t - _keys[i].t;
                    float u = span <= 0f ? 0f : (f - _keys[i].t) / span;
                    return Canvas.Mix(_keys[i].col, _keys[i + 1].col, u);
                }
            }
            return _keys[_keys.Length - 1].col;
        }
    }
}
