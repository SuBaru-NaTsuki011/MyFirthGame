using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.UI.Design;
using AndijonSaltanatlar.Online;
using AndijonSaltanatlar.Shared;

namespace AndijonSaltanatlar.UI
{
    /// <summary>
    /// "Yangi versiya mavjud" modal oynasi. GitHub'da yangiroq reliz topilsa ko'rsatiladi.
    /// YUKLAB OLISH — brauzerda doimiy APK havolasini ochadi (Android — yuklab olib o'rnatadi).
    /// Akkaunt va o'yin holati bulutda saqlangani uchun yangilashdan keyin o'sha joydan davom etadi.
    /// </summary>
    public sealed class UpdateScreen
    {
        private readonly UpdateInfo _info;
        private InputManager _input;

        public UiKit Ui { get; set; }

        /// <summary>"Keyinroq" bosilgach.</summary>
        public Action Dismissed;
        /// <summary>"Yuklab olish" bosilib, havola ochilgach.</summary>
        public Action Downloaded;

        private static readonly Color Gold = new Color(222, 178, 92);
        private static readonly Color SoftBlue = new Color(120, 168, 226);

        public UpdateScreen(UpdateInfo info)
        {
            _info = info ?? new UpdateInfo();
        }

        public void Update(InputManager input)
        {
            _input = input;
            input.MouseOverUI = true;
        }

        public void Draw(SpriteBatch sb, int vw, int vh, float time)
        {
            if (Ui == null || _input == null) return;
            UiKit ui = Ui;
            ui.Begin(sb, _input, vw, vh, time);

            // Fonni qoraytirish (modal)
            ui.Fill(new Rectangle(0, 0, vw, vh), new Color(8, 10, 16) * 0.72f);

            int w = Math.Min(vw - 32, 560);
            int h = Math.Min(vh - 32, ui.IsTouch ? 420 : 340);
            var card = new Rectangle((vw - w) / 2, (vh - h) / 2, w, h);
            ui.Panel(card);
            ui.Rounded(new Rectangle(card.X, card.Y, card.Width, 5), Gold, 3);

            int pad = 24;
            int x = card.X + pad;
            int innerW = card.Width - pad * 2;
            int y = card.Y + 20;

            float titleH = ui.Type.LineHeight(TextStyle.MainTitle);
            ui.Type.DrawFitted(sb, "Yangi versiya mavjud", new Rectangle(x, y, innerW, (int)titleH), TextStyle.MainTitle, Gold, 1);
            y += (int)titleH + 8;

            float bodyH = ui.Type.LineHeight(TextStyle.Body);
            string verLine = "Joriy: " + AppVersion.Current + "   →   Yangi: " + _info.LatestVersion;
            ui.Type.DrawFitted(sb, verLine, new Rectangle(x, y, innerW, (int)bodyH), TextStyle.Body, SoftBlue, 1);
            y += (int)bodyH + 10;

            string note = "Yangi imkoniyatlardan foydalanish uchun ilovani yangilang. " +
                          "Akkauntingiz va o'yin holatingiz bulutda saqlangan — yangilagach o'sha " +
                          "joydan davom etasiz.";
            var noteRect = new Rectangle(x, y, innerW, (int)(bodyH * 5));
            float used = ui.Paragraph(note, noteRect, TextStyle.Body, null, 6);
            y += (int)used + 16;

            // Tugmalar (pastki qism)
            int bh = ui.ControlH;
            int by = card.Bottom - pad - bh;
            int gap = ui.Gap;
            int bw = (innerW - gap) / 2;

            if (ui.Button(new Rectangle(x, by, bw, bh), "KEYINROQ", ButtonKind.Ghost))
            {
                Dismissed?.Invoke();
            }
            if (ui.Button(new Rectangle(x + bw + gap, by, bw, bh), "YUKLAB OLISH", ButtonKind.Primary))
            {
                try { CloudSession.OpenUrl(_info.DownloadUrl); } catch { }
                Downloaded?.Invoke();
            }

            ui.End();
        }
    }
}
