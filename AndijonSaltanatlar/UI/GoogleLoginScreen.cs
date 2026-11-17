using System;
using System.Threading.Tasks;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Online;
using AndijonSaltanatlar.UI.Design;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#if ANDROID
using Microsoft.Xna.Framework.Input;
#endif

namespace AndijonSaltanatlar.UI
{
    /// <summary>
    /// O'yinga kirish ekrani — yagona usul: Google hisobi. Orqa fonda tirik Asaka shahri (Game1 chizadi).
    /// Holatlar: kirilmagan → "Google bilan kirish"; brauzer kutilmoqda → ko'rsatma + "Bekor qilish";
    /// token tekshirilmoqda / o'yin yuklanmoqda → jarayon; xato → xabar + "Qayta urinish".
    /// Server manzili kichik "Server" qatorida (telefonda kompyuter/domen manzili kiritiladi).
    /// </summary>
    public sealed class GoogleLoginScreen
    {
        private readonly CloudSession _cloud;
        private InputManager _input;
        private string _server;
        private bool _editServer;
        private string _pendingKeyboard;

        public UiKit Ui { get; set; }
        /// <summary>Kirgandan keyin o'yin yuklanayotgan payt (Game1 o'rnatadi).</summary>
        public string Busy { get; set; }

        public GoogleLoginScreen(CloudSession cloud, string defaultServer)
        {
            _cloud = cloud;
            _server = string.IsNullOrEmpty(cloud.ServerUrl) ? defaultServer ?? "" : cloud.ServerUrl;
            _editServer = string.IsNullOrEmpty(_server);
        }

        public void Update(InputManager input)
        {
            _input = input;
            input.MouseOverUI = true;
            string kb = _pendingKeyboard;
            if (kb != null) { _server = kb; _pendingKeyboard = null; }
        }

        public void Draw(SpriteBatch sb, int vw, int vh, float time)
        {
            if (Ui == null || _input == null) return;
            UiKit ui = Ui;
            ui.Begin(sb, _input, vw, vh, time);
            ThemeTokens T = ui.T;
            ui.Fill(new Rectangle(0, 0, vw, vh), T.Scrim);

            int h = ui.ControlH + (ui.IsTouch ? 4 : 6), gap = ui.Gap;
            int titleW = (int)ui.Type.Measure("ANDIJON: SALTANATLAR", TextStyle.MainTitle).X + 64;
            int cardW = Math.Min(vw - 32, Math.Max(titleW, ui.Compact ? 620 : 560));
            float titleH = ui.Type.LineHeight(TextStyle.MainTitle), bodyH = ui.Type.LineHeight(TextStyle.Body), capH = ui.Type.LineHeight(TextStyle.Secondary);
            // Karta balandligi mazmunga qarab: asosiy tugma + xabar + server qatori (+ xato/kutish holatida qo'shimcha tugma).
            bool extraRow = _cloud.State == CloudLoginState.Error || _cloud.State == CloudLoginState.WaitingForBrowser;
            int cardH = (int)(titleH + bodyH + 2 * capH) + (extraRow ? 3 : 2) * h + 6 * gap + 56;
            cardH = Math.Min(vh - 24, cardH);
            var card = new Rectangle((vw - cardW) / 2, (vh - cardH) / 2, cardW, cardH);
            ui.Panel(card);
            ui.Rounded(new Rectangle(card.X, card.Y, card.Width, 6), T.Accent, 3);

            int x = card.X + 28, w = card.Width - 56, y = card.Y + 22;
            ui.Type.DrawFitted(sb, "ANDIJON: SALTANATLAR", new Rectangle(card.X + 16, y, card.Width - 32, (int)titleH), TextStyle.MainTitle, T.Primary, 1);
            y += (int)titleH + 2;
            ui.Type.DrawFitted(sb, "Saltanatingiz serverda saqlanadi",
                new Rectangle(card.X + 16, y, card.Width - 32, (int)bodyH), TextStyle.Secondary, T.TextSecondary, 1);
            y += (int)bodyH + gap * 2;

            CloudLoginState st = _cloud.State;
            bool working = Busy != null || st == CloudLoginState.CheckingToken || st == CloudLoginState.SignedIn;
            var main = new Rectangle(x, y, w, h);

            if (working)
            {
                string text = Busy ?? (st == CloudLoginState.CheckingToken ? "Serverga ulanmoqda…" : "Saltanat yuklanmoqda…");
                ui.RoundedBorder(main, T.Border, T.SurfaceAlt, 8);
                ui.Type.DrawFitted(sb, text + Dots(time), new Rectangle(main.X + 8, main.Y, main.Width - 16, main.Height), TextStyle.Button, T.TextPrimary, 1);
                y += h + gap;
            }
            else if (st == CloudLoginState.WaitingForBrowser)
            {
                ui.RoundedBorder(main, T.Primary, T.SurfaceAlt, 8);
                string wait = _cloud.UsingNativePicker ? "Google akkauntini tanlang" : "Google oynasi kutilmoqda";
                ui.Type.DrawFitted(sb, wait + Dots(time), new Rectangle(main.X + 8, main.Y, main.Width - 16, main.Height), TextStyle.Button, T.TextPrimary, 1);
                y += h + gap;
                if (_cloud.UsingNativePicker)
                {
                    if (ui.Button(new Rectangle(x, y, w, h - 6), "Bekor qilish", ButtonKind.Ghost, true)) _cloud.CancelLogin();
                }
                else
                {
                    int bw = (w - gap) / 2;
                    if (ui.Button(new Rectangle(x, y, bw, h - 6), "Brauzerni qayta ochish", ButtonKind.Secondary, true)) _cloud.StartGoogleLogin(_server);
                    if (ui.Button(new Rectangle(x + bw + gap, y, bw, h - 6), "Bekor qilish", ButtonKind.Ghost, true)) _cloud.CancelLogin();
                }
                y += h + gap;
            }
            else
            {
                if (GoogleButton(ui, sb, main, "Google bilan kirish")) { ui.FocusId = null; _cloud.StartGoogleLogin(_server); }
                y += h + gap;
            }

            // Holat / xato xabari (bir necha qatorga bo'linadi, kartadan chiqmaydi).
            string msg = _cloud.Message;
            if (string.IsNullOrEmpty(msg) && st == CloudLoginState.SignedOut)
                msg = "Kirish faqat Google hisobi orqali. Parol kerak emas.";
            if (!string.IsNullOrEmpty(msg))
                y += (int)ui.Paragraph(msg, new Rectangle(x, y, w, (int)(capH * 3)), TextStyle.Secondary, _cloud.MessageIsError ? T.Danger : T.TextSecondary, 3) + gap;

            if (st == CloudLoginState.Error && !working)
            {
                if (ui.Button(new Rectangle(x, y, w, h - 6), "Qayta urinish", ButtonKind.Secondary, true))
                {
                    if (!string.IsNullOrEmpty(StoredAuth.Load()?.Token)) _cloud.TryResume(_server);
                    else _cloud.StartGoogleLogin(_server);
                }
                y += h + gap;
            }

            // Server manzili (pastki qator): odatda yashirin, "o'zgartirish" bilan ochiladi.
            int sy = card.Bottom - h - 12;
            if (!working && st != CloudLoginState.WaitingForBrowser && sy > y - 4)
            {
                if (_editServer)
                {
                    int lw = (int)ui.Type.Measure("Server:", TextStyle.Secondary).X + 10;
                    ui.Text("Server:", new Vector2(x, sy + (h - 6 - capH) / 2), TextStyle.Secondary, T.TextSecondary);
                    _server = ui.TextInput(new Rectangle(x + lw, sy, w - lw, h - 6), "login_server", _server, "https://server-manzili yoki 192.168.1.5:5080", false, AndroidKeyboard);
                }
                else
                {
                    string label = "Server: " + (string.IsNullOrEmpty(_server) ? "-" : _server);
                    int bw = Math.Min(w / 2, (int)ui.Type.Measure("O'zgartirish", TextStyle.Button).X + 28);
                    ui.TextFit(label, new Rectangle(x, sy, w - bw - gap, h - 6), TextStyle.Secondary, T.TextSecondary);
                    if (ui.Button(new Rectangle(x + w - bw, sy, bw, h - 6), "O'zgartirish", ButtonKind.Ghost, true)) { _editServer = true; ui.FocusId = "login_server"; }
                }
            }
            ui.End();
        }

        private static string Dots(float time) => new string('.', 1 + (int)(time * 2.5f) % 3);

        /// <summary>Google uslubidagi oq tugma: rangli "G" belgisi + matn.</summary>
        private static bool GoogleButton(UiKit ui, SpriteBatch sb, Rectangle r, string label)
        {
            bool hover = ui.Hover(r);
            Color fill = hover ? new Color(240, 243, 248) : Color.White;
            ui.RoundedBorder(r, hover ? new Color(66, 133, 244) : new Color(218, 220, 224), fill, 8);
            int g = Math.Max(18, (int)(r.Height * 0.5f));
            float tw = Math.Min(ui.Type.Measure(label, TextStyle.Button).X, r.Width - g - 40);
            float total = g + 12 + tw;
            float sx = r.X + (r.Width - total) / 2f;
            DrawGLogo(ui, new Rectangle((int)sx, r.Y + (r.Height - g) / 2, g, g));
            ui.Type.DrawFitted(sb, label, new Rectangle((int)(sx + g + 12), r.Y, (int)(r.Right - 12 - (sx + g + 12)), r.Height), TextStyle.Button, new Color(31, 31, 31), 0);
            return ui.Clicked(r);
        }

        /// <summary>Soddalashtirilgan to'rt rangli "G" (piksel to'rtburchaklardan).</summary>
        private static void DrawGLogo(UiKit ui, Rectangle r)
        {
            int s = r.Width, t = Math.Max(3, s / 5);
            Color red = new Color(234, 67, 53), yellow = new Color(251, 188, 5), green = new Color(52, 168, 83), blue = new Color(66, 133, 244);
            ui.Fill(new Rectangle(r.X + t, r.Y, s - 2 * t, t), red);                 // tepa
            ui.Fill(new Rectangle(r.X, r.Y + t, t, s - 2 * t), yellow);              // chap
            ui.Fill(new Rectangle(r.X + t, r.Bottom - t, s - 2 * t, t), green);      // past
            ui.Fill(new Rectangle(r.Right - t, r.Y + s / 2, t, s / 2 - t), blue);    // o'ng-past
            ui.Fill(new Rectangle(r.X + s / 2, r.Y + s / 2 - t / 2, s / 2, t), blue); // ko'ndalang
        }

        private void AndroidKeyboard()
        {
#if ANDROID
            Task<string> t = KeyboardInput.Show("Server manzili", "", _server, false);
            t.ContinueWith(r => { if (r.Status == TaskStatus.RanToCompletion && r.Result != null) _pendingKeyboard = r.Result; });
#endif
        }
    }
}
