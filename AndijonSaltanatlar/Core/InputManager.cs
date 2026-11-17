using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace AndijonSaltanatlar.Core
{
    /// <summary>
    /// Klaviatura, sichqoncha va SENSORLI EKRAN holatini kadrma-kadr kuzatadi.
    /// Boshqa tizimlar xom MonoGame holatiga emas, shu klassga murojaat qiladi.
    ///
    /// PROMPT 19A (Android): sensor sichqonchaga o'giriladi —
    ///   bir barmoq bilan tegib qo'yib yuborish  → chap tugma bosilishi (tap = tanlash / tugma);
    ///   bir barmoq bilan sudrash               → kamerani surish (IsPanDragging + MouseDelta);
    ///   ikki barmoq (pinch)                   → zoom (WheelDelta).
    /// PC'da sichqoncha va klaviatura avvalgidek ishlaydi.
    /// </summary>
    public sealed class InputManager
    {
        private const int TapSlop = 14;          // shu pikseldan ko'p siljisa — tap emas, sudrash
        private const float PinchToWheel = 3f;   // pinch masofa o'zgarishi -> g'ildirak birligi

        private KeyboardState _kb, _prevKb;
        private MouseState _ms, _prevMs;
        private Point _rightPressPos;
        private bool _rightDragged;

        // --- Sensor holati ---
        private bool _touchMode;
        private bool _touchDown;
        private Point _tPos, _tPrevPos, _tStart;
        private bool _tMoved, _tPanning, _tap;
        private int _pinchDelta;
        private float _pinchDist = -1f;

        // --- Matn kiritish (Window.TextInput yoki Android ekran klaviaturasi) ---
        private readonly Queue<char> _text = new Queue<char>();

        /// <summary>UI har kadr o'rnatadi: sichqoncha panel ustidami?</summary>
        public bool MouseOverUI { get; set; }

        /// <summary>Oxirgi kiritish sensordan keldimi (UI katta tugmalar uchun).</summary>
        public bool IsTouch => _touchMode || ForceTouchUi;

        /// <summary>Sinov: PC'da telefon interfeysini ko'rsatish (katta tugmalar, telefon masshtabi).</summary>
        public bool ForceTouchUi { get; set; }

        /// <summary>
        /// Eski piksel-shriftli panellar virtual o'lchamda chiziladi (telefonda 720p balandlik). Ular yangilanayotganda
        /// Game1 shu koeffitsientni o'rnatadi — kursor/barmoq koordinatalari panel koordinatalariga o'giriladi.
        /// </summary>
        public float PointerScale { get; set; } = 1f;

        private Point RawPosition => _touchMode ? _tPos : _ms.Position;
        public Point MousePosition => PointerScale == 1f ? RawPosition : new Point((int)(RawPosition.X / PointerScale), (int)(RawPosition.Y / PointerScale));
        public Vector2 MouseVector => new Vector2(RawPosition.X, RawPosition.Y) / PointerScale;
        public Point MouseDelta => _touchMode ? (_tPanning ? _tPos - _tPrevPos : Point.Zero) : _ms.Position - _prevMs.Position;
        public int WheelDelta => _touchMode ? _pinchDelta : _ms.ScrollWheelValue - _prevMs.ScrollWheelValue;

        public bool LeftDown => _touchMode ? _tap : _ms.LeftButton == ButtonState.Pressed;
        public bool LeftPressed => _touchMode ? _tap : _ms.LeftButton == ButtonState.Pressed && _prevMs.LeftButton == ButtonState.Released;
        public bool LeftReleased => _touchMode ? _tap : _ms.LeftButton == ButtonState.Released && _prevMs.LeftButton == ButtonState.Pressed;
        public bool RightDown => !_touchMode && _ms.RightButton == ButtonState.Pressed;
        public bool MiddleDown => !_touchMode && _ms.MiddleButton == ButtonState.Pressed;

        /// <summary>O'ng tugma bosilib, sudralmasdan qo'yib yuborildi (bekor qilish uchun).</summary>
        public bool RightClicked { get; private set; }

        /// <summary>O'ng yoki o'rta tugma (yoki bir barmoq) bilan xaritani sudrash davom etyaptimi.</summary>
        public bool IsPanDragging => _touchMode ? _tPanning : (MiddleDown || (RightDown && _rightDragged));

        public void Update(bool windowActive)
        {
            _prevKb = _kb;
            _prevMs = _ms;
            if (windowActive)
            {
                _kb = Keyboard.GetState();
                _ms = Mouse.GetState();
            }
            else
            {
                _kb = default;
                // Sichqoncha pozitsiyasi saqlanadi, tugmalar "qo'yib yuborilgan" deb olinadi.
                _ms = new MouseState(_ms.X, _ms.Y, _ms.ScrollWheelValue,
                    ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
            }

            UpdateTouch(windowActive);
            bool back = false;
            try { back = windowActive && GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed; } catch { }
            BackPressed = back && !_backDown;
            _backDown = back;
            if (LeftPressed) _clickLatch = true;

            RightClicked = false;
            bool rightNow = _ms.RightButton == ButtonState.Pressed;
            bool rightBefore = _prevMs.RightButton == ButtonState.Pressed;
            if (rightNow && !rightBefore)
            {
                _rightPressPos = _ms.Position;
                _rightDragged = false;
            }
            else if (rightNow)
            {
                Point d = _ms.Position - _rightPressPos;
                if (Math.Abs(d.X) + Math.Abs(d.Y) > 6) _rightDragged = true;
            }
            else if (rightBefore)
            {
                RightClicked = !_rightDragged;
                _rightDragged = false;
            }
        }

        private void UpdateTouch(bool windowActive)
        {
            _tap = false;
            _pinchDelta = 0;
            _tPrevPos = _tPos;

            TouchCollection touches;
            try { touches = windowActive ? TouchPanel.GetState() : default; }
            catch { touches = default; } // sensor qo'llab-quvvatlanmaydigan platforma

            int count = 0;
            Vector2 a = Vector2.Zero, b = Vector2.Zero;
            foreach (TouchLocation t in touches)
            {
                if (t.State != TouchLocationState.Pressed && t.State != TouchLocationState.Moved) continue;
                if (count == 0) a = t.Position; else if (count == 1) b = t.Position;
                count++;
            }

            if (count > 0)
            {
                _touchMode = true;
                if (count >= 2)
                {
                    // Ikki barmoq: zoom. Tap bo'lmaydi.
                    float dist = Vector2.Distance(a, b);
                    if (_pinchDist > 0f) _pinchDelta = (int)((dist - _pinchDist) * PinchToWheel);
                    _pinchDist = dist;
                    _tMoved = true;
                    _tPanning = false;
                    Vector2 mid = (a + b) * 0.5f;
                    _tPos = new Point((int)mid.X, (int)mid.Y);
                    _tPrevPos = _tPos;
                }
                else
                {
                    _pinchDist = -1f;
                    var p = new Point((int)a.X, (int)a.Y);
                    if (!_touchDown) { _tStart = p; _tPrevPos = p; _tMoved = false; }
                    _tPos = p;
                    if (!_tMoved && Math.Abs(p.X - _tStart.X) + Math.Abs(p.Y - _tStart.Y) > TapSlop) _tMoved = true;
                    _tPanning = _tMoved;
                }
                _touchDown = true;
            }
            else
            {
                if (_touchDown && !_tMoved) _tap = true; // barmoq siljimasdan ko'tarildi — tap
                _touchDown = false;
                _tPanning = false;
                _pinchDist = -1f;
                // Sichqoncha qimirlasa yoki bosilsa — PC rejimiga qaytamiz.
                if (_touchMode && !_tap && (_ms.Position != _prevMs.Position || _ms.LeftButton == ButtonState.Pressed)) _touchMode = false;
            }
        }

        private bool _clickLatch;

        /// <summary>
        /// PROMPT 19B UI (Draw bosqichida ishlaydigan komponentlar) uchun: oxirgi chizishdan beri bosish bo'ldimi.
        /// Bir kadrga bir nechta Update to'g'ri kelsa ham bosish yo'qolmaydi. O'qilgach tozalanadi.
        /// </summary>
        public bool TakeClick()
        {
            bool c = _clickLatch;
            _clickLatch = false;
            return c;
        }

        public bool KeyDown(Keys k) => _kb.IsKeyDown(k);
        public bool KeyPressed(Keys k) => (_kb.IsKeyDown(k) && !_prevKb.IsKeyDown(k)) || (k == Keys.Escape && BackPressed);

        /// <summary>Android "Orqaga" tugmasi shu kadrda bosildi (Esc kabi ishlaydi: panelni yopadi, o'yindan chiqmaydi).</summary>
        public bool BackPressed { get; private set; }
        private bool _backDown;

        // ---- Matn kiritish ----
        public void PushText(char c)
        {
            if (_text.Count < 64) _text.Enqueue(c);
        }

        public bool TryReadChar(out char c)
        {
            if (_text.Count > 0) { c = _text.Dequeue(); return true; }
            c = '\0';
            return false;
        }

        public void ClearText() => _text.Clear();
    }
}
