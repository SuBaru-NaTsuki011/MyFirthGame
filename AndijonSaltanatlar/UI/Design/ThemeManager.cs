using System;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.UI.Design
{
    /// <summary>
    /// Dizayn tokenlari (PROMPT 19B, 11-bo'lim). Barcha yangi panel va tugmalar FAQAT shu ranglardan foydalanadi —
    /// shuning uchun interfeys yagona ko'rinishga ega va mavzuni bitta joyda almashtirish mumkin.
    /// </summary>
    public sealed class ThemeTokens
    {
        public string Name;
        public Color Background, Surface, SurfaceAlt, Primary, PrimaryHover, PrimaryPressed, Secondary, Accent;
        public Color TextPrimary, TextSecondary, TextOnPrimary, Border, Focus;
        public Color Success, Warning, Danger, Disabled, DisabledText, Scrim, Shadow;
    }

    public static class ThemeManager
    {
        private static Color Hex(string hex, int alpha = 255)
        {
            hex = hex.TrimStart('#');
            return new Color(Convert.ToInt32(hex.Substring(0, 2), 16), Convert.ToInt32(hex.Substring(2, 2), 16), Convert.ToInt32(hex.Substring(4, 2), 16), alpha);
        }

        public static readonly ThemeTokens Light = new ThemeTokens
        {
            Name = "Yorug'",
            Background = Hex("F7F9FC"), Surface = Hex("FFFFFF"), SurfaceAlt = Hex("EEF3F8"),
            Primary = Hex("326B96"), PrimaryHover = Hex("3B7BAB"), PrimaryPressed = Hex("285A80"),
            Secondary = Hex("6E9FC2"), Accent = Hex("C59A4B"),
            TextPrimary = Hex("182634"), TextSecondary = Hex("627487"), TextOnPrimary = Hex("FFFFFF"),
            Border = Hex("DFE7EF"), Focus = Hex("C59A4B"),
            Success = Hex("2E8B57"), Warning = Hex("C88A1E"), Danger = Hex("C0453A"),
            Disabled = Hex("E6EBF1"), DisabledText = Hex("A3B0BD"), Scrim = new Color(10, 18, 28, 120), Shadow = new Color(16, 32, 52, 40)
        };

        public static readonly ThemeTokens Dark = new ThemeTokens
        {
            Name = "Tungi",
            Background = Hex("111B29"), Surface = Hex("1B293B"), SurfaceAlt = Hex("223349"),
            Primary = Hex("76B3DF"), PrimaryHover = Hex("8CC2E8"), PrimaryPressed = Hex("5F9DCB"),
            Secondary = Hex("4E7EA3"), Accent = Hex("D6AE67"),
            TextPrimary = Hex("F3F7FB"), TextSecondary = Hex("A9BBCB"), TextOnPrimary = Hex("0E1824"),
            Border = Hex("2E4258"), Focus = Hex("D6AE67"),
            Success = Hex("5CC28A"), Warning = Hex("E0A84A"), Danger = Hex("E56F63"),
            Disabled = Hex("26364A"), DisabledText = Hex("5E7186"), Scrim = new Color(4, 8, 14, 150), Shadow = new Color(0, 0, 0, 90)
        };

        /// <summary>Joriy mavzu (standart — tungi: o'yin dunyosi fonida yaxshi o'qiladi).</summary>
        public static ThemeTokens Current { get; private set; } = Dark;

        public static void Toggle() => Current = Current == Dark ? Light : Dark;
        public static void Set(bool dark) => Current = dark ? Dark : Light;
    }
}
