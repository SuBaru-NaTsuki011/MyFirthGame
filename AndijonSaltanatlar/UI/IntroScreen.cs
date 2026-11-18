using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace AndijonSaltanatlar.UI
{
    /// <summary>
    /// O'yin ochilganda ko'rsatiladigan kirish videosi (studio intro, ovozli).
    /// MonoGame DesktopGL MP4 o'ynatmagani uchun video oldindan kadrlar (Content/Intro/frame_XXXX.jpg)
    /// va ovoz (Content/Intro/intro.wav) ko'rinishiga aylantirilgan. Kadrlar o'yin paytida birma-bir
    /// yuklanadi (xotira kam). Istalgan tugma/bosish/teginish bilan o'tkazib yuborish mumkin.
    /// </summary>
    public sealed class IntroScreen : IDisposable
    {
        private const int Fps = 15;
        private const int FrameCount = 165;                 // ffmpeg: 11s * 15fps
        private static readonly float Duration = FrameCount / (float)Fps;
        private const float FadeIn = 0.4f, FadeOut = 0.6f;

        private readonly GraphicsDevice _gd;
        private readonly Texture2D _pixel;

        private float _time;
        private int _shownIndex = -1;
        private Texture2D _frame;
        private SoundEffect _snd;
        private SoundEffectInstance _sndInst;
        private bool _audioTried;
        private bool _done;

        private MouseState _prevMouse;
        private KeyboardState _prevKeys;

        public bool Done => _done;

        public IntroScreen(GraphicsDevice gd, Texture2D pixel)
        {
            _gd = gd;
            _pixel = pixel;
            _prevMouse = Mouse.GetState();
            _prevKeys = Keyboard.GetState();
        }

        public void Update(float dt, bool focused)
        {
            if (_done) return;
            if (!_audioTried) { _audioTried = true; TryStartAudio(); }
            _time += dt;
            if (focused && SkipPressed()) { Finish(); return; }
            if (_time >= Duration + 0.2f) Finish();
        }

        private bool SkipPressed()
        {
            bool skip = false;

            MouseState m = Mouse.GetState();
            if (m.LeftButton == ButtonState.Released && _prevMouse.LeftButton == ButtonState.Pressed) skip = true;
            _prevMouse = m;

            KeyboardState k = Keyboard.GetState();
            if (k.GetPressedKeys().Length > 0 && _prevKeys.GetPressedKeys().Length == 0) skip = true;
            _prevKeys = k;

            try
            {
                TouchCollection t = TouchPanel.GetState();
                foreach (TouchLocation tl in t)
                    if (tl.State == TouchLocationState.Pressed) skip = true;
            }
            catch { /* teginish paneli yo'q */ }

            return skip;
        }

        private void TryStartAudio()
        {
            try
            {
                using Stream s = TitleContainer.OpenStream("Content/Intro/intro.wav");
                _snd = SoundEffect.FromStream(s);
                _sndInst = _snd.CreateInstance();
                _sndInst.Volume = 1f;
                _sndInst.Play();
            }
            catch { /* ovoz bo'lmasa ham intro ko'rinadi */ }
        }

        public void Draw(SpriteBatch sb, int vw, int vh)
        {
            int idx = (int)(_time * Fps) + 1;
            if (idx < 1) idx = 1;
            if (idx > FrameCount) idx = FrameCount;
            if (idx != _shownIndex) { LoadFrame(idx); _shownIndex = idx; }

            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
            sb.Draw(_pixel, new Rectangle(0, 0, vw, vh), Color.Black);
            if (_frame != null)
                sb.Draw(_frame, Fit(vw, vh, _frame.Width, _frame.Height), Color.White);
            float fade = FadeAlpha();
            if (fade > 0f) sb.Draw(_pixel, new Rectangle(0, 0, vw, vh), Color.Black * fade);
            sb.End();
        }

        private float FadeAlpha()
        {
            if (_time < FadeIn) return 1f - _time / FadeIn;
            float toEnd = Duration - _time;
            if (toEnd < FadeOut) return MathHelper.Clamp(1f - toEnd / FadeOut, 0f, 1f);
            return 0f;
        }

        private static Rectangle Fit(int vw, int vh, int iw, int ih)
        {
            float scale = Math.Min(vw / (float)iw, vh / (float)ih);
            int w = (int)(iw * scale), h = (int)(ih * scale);
            return new Rectangle((vw - w) / 2, (vh - h) / 2, w, h);
        }

        private void LoadFrame(int idx)
        {
            try
            {
                string name = "Content/Intro/frame_" + idx.ToString("0000") + ".jpg";
                using Stream s = TitleContainer.OpenStream(name);
                Texture2D tex = Texture2D.FromStream(_gd, s);
                _frame?.Dispose();
                _frame = tex;
            }
            catch { /* kadr topilmasa oldingisi qoladi */ }
        }

        private void Finish()
        {
            if (_done) return;
            _done = true;
            try { _sndInst?.Stop(); } catch { }
        }

        public void Dispose()
        {
            try { _sndInst?.Dispose(); } catch { }
            try { _snd?.Dispose(); } catch { }
            try { _frame?.Dispose(); } catch { }
        }
    }
}
