using System;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Core.Graphics;
using AndijonSaltanatlar.UI.Design;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.UI
{
    public enum MainMenuAction { None, Start, Continue, Exit, History, Online }

    /// <summary>
    /// Bosh menyu (PROMPT 19B dizayn tizimi). Orqa fonda tirik Asaka shahri (Game1 chizadi); markazda
    /// sarlavha karta va tugmalar. PC: markazlashgan ustun; telefon: kattaroq tugmalar, gorizontal joylashuv.
    /// Barcha rang — ThemeManager, barcha matn — Typography (Noto Sans).
    /// </summary>
    public sealed class MainMenuScreen
    {
        private readonly (string label, MainMenuAction action, ButtonKind kind, string hint)[] _items =
        {
            ("Onlayn o'yin", MainMenuAction.Online, ButtonKind.Primary, "Server: PC va telefon bitta dunyoda"),
            ("Hikoya rejimi", MainMenuAction.Start, ButtonKind.Secondary, "Asakadan boshlanadigan kampaniya"),
            ("Davom ettirish", MainMenuAction.Continue, ButtonKind.Secondary, "Oxirgi saqlangan o'yin"),
            ("Tarix xazinasi", MainMenuAction.History, ButtonKind.Secondary, "Texnologiyalar va sivilizatsiya"),
            ("Chiqish", MainMenuAction.Exit, ButtonKind.Ghost, null),
        };

        private InputManager _input;
        private MainMenuAction _pending;

        /// <summary>Yangi dizayn tizimi (Game1 o'rnatadi).</summary>
        public UiKit Ui { get; set; }

        public MainMenuScreen(AssetManager assets, PixelFont font) { }

        public MainMenuAction Update(InputManager input, int vw, int vh)
        {
            _input = input;
            input.MouseOverUI = true;
            MainMenuAction a = _pending;
            _pending = MainMenuAction.None;
            return a;
        }

        public void SetMouse(Point m) { }

        public void Draw(SpriteBatch sb, int vw, int vh)
        {
            if (Ui == null || _input == null) return;
            UiKit ui = Ui;
            ui.Begin(sb, _input, vw, vh, (float)(DateTime.UtcNow.TimeOfDay.TotalSeconds));
            ThemeTokens T = ui.T;
            ui.Fill(new Rectangle(0, 0, vw, vh), T.Scrim);

            int h = ui.ControlH + (ui.IsTouch ? 4 : 6), gap = ui.Gap;
            // Karta eni sarlavhaga moslashadi (piksel shrift keng) — sarlavha kartadan chiqmaydi.
            int titleW = (int)ui.Type.Measure("ANDIJON: TAXTLAR JANGI", TextStyle.MainTitle).X + 64;
            int cardW = Math.Min(vw - 32, Math.Max(titleW, ui.Compact && vw > vh ? 760 : 520));
            bool twoCols = ui.Compact && vw > vh && cardW >= 700;
            int rows = twoCols ? (_items.Length + 1) / 2 : _items.Length;
            int titleH = (int)ui.Type.LineHeight(TextStyle.MainTitle) + (int)ui.Type.LineHeight(TextStyle.Body) + 40;
            int cardH = Math.Min(vh - 24, titleH + rows * (h + gap) + 70);
            var card = new Rectangle((vw - cardW) / 2, (vh - cardH) / 2, cardW, cardH);
            ui.Panel(card);
            ui.Rounded(new Rectangle(card.X, card.Y, card.Width, 6), T.Accent, 3);

            int x = card.X + 28, y = card.Y + 26, w = card.Width - 56;
            ui.Type.DrawFitted(sb, "ANDIJON: TAXTLAR JANGI", new Rectangle(card.X + 16, y, card.Width - 32, (int)ui.Type.LineHeight(TextStyle.MainTitle)), TextStyle.MainTitle, T.Primary, 1);
            y += (int)ui.Type.LineHeight(TextStyle.MainTitle) + 4;
            ui.Type.DrawFitted(sb, "Bir shahardan buyuk saltanat sari", new Rectangle(card.X + 16, y, card.Width - 32, (int)ui.Type.LineHeight(TextStyle.Body)), TextStyle.Body, T.TextSecondary, 1);
            y += (int)ui.Type.LineHeight(TextStyle.Body) + 24;

            int colW = twoCols ? (w - gap) / 2 : w;
            for (int i = 0; i < _items.Length; i++)
            {
                int col = twoCols ? i % 2 : 0, row = twoCols ? i / 2 : i;
                var r = new Rectangle(x + col * (colW + gap), y + row * (h + gap), colW, h);
                if (ui.Button(r, _items[i].label, _items[i].kind, true, _items[i].hint)) _pending = _items[i].action;
            }

            // Mavzu almashtirgich (Sozlamalar): tungi / yorug'
            int tbw = Math.Min(card.Width / 2 - 28, (int)ui.Type.Measure("Mavzu: Yorug'", TextStyle.Button).X + 30);
            var tr = new Rectangle(card.Right - 20 - tbw, card.Bottom - 46, tbw, 32);
            if (ui.Button(tr, "Mavzu: " + ThemeManager.Current.Name, ButtonKind.Ghost, true, "Interfeys mavzusini almashtirish")) ThemeManager.Toggle();
            ui.TextFit("v0.25 · Oʻzbekcha", new Rectangle(card.X + 24, card.Bottom - 46, tr.X - card.X - 36, 32), TextStyle.Caption, T.TextSecondary);
            ui.End();
        }
    }
}
