using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AndijonSaltanatlar.Core
{
    /// <summary>
    /// 2D kamera: pan (WASD / strelkalar / o'ng yoki o'rta tugma bilan sudrash) va
    /// kursorga qarab silliq zoom (g'ildirak).
    /// </summary>
    public sealed class CameraManager
    {
        private float _targetZoom;
        private Viewport _viewport;

        /// <summary>Kamera qarab turgan nuqta (world piksel).</summary>
        public Vector2 Position;
        public float Zoom { get; private set; }

        // --- Ekran silkinishi (kuchli zarba) ---
        private float _shakeT, _shakeDur, _shakeAmp, _shakeClock;
        private Vector2 _shake;

        /// <summary>Ekranni qisqa silkitadi (piksel amplituda, soniya). Kuchliroq silkinish kuchsizini almashtiradi.</summary>
        public void Shake(float amplitude, float duration)
        {
            float current = _shakeDur > 0f ? _shakeAmp * (_shakeT / _shakeDur) : 0f;
            if (amplitude < current) return;
            _shakeAmp = amplitude; _shakeDur = Math.Max(0.01f, duration); _shakeT = _shakeDur;
        }

        /// <summary>false bo'lsa WASD/strelkalar kamerani surmaydi (masalan, o'yinchi jangchini boshqarayotganda).</summary>
        public bool KeyboardPanEnabled { get; set; } = true;

        /// <summary>Zoom'ni silliq o'zgartirish (masalan, odam ichiga kirganda yaqinlashish).</summary>
        public void SetTargetZoom(float zoom)
        {
            _targetZoom = MathHelper.Clamp(zoom, GameConfig.MinZoom, GameConfig.MaxZoom);
        }

        /// <summary>Kamerani berilgan world nuqtaga silliq yaqinlashtiradi (boshqarilayotgan jangchiga ergashish).</summary>
        public void Follow(Vector2 world, float dt, float sharpness = 8f)
        {
            Position = Vector2.Lerp(Position, world, 1f - MathF.Exp(-sharpness * dt));
            ClampToMap();
        }

        public CameraManager(Viewport viewport, Vector2 startWorld, float zoom)
        {
            _viewport = viewport;
            Position = startWorld;
            Zoom = _targetZoom = MathHelper.Clamp(zoom, GameConfig.MinZoom, GameConfig.MaxZoom);
        }

        public Matrix Transform
        {
            get
            {
                // Pikselga yaxlitlash — sprite'lar "titrab" ko'rinmasligi uchun.
                float tx = MathF.Round(-Position.X * Zoom + _viewport.Width * 0.5f + _shake.X);
                float ty = MathF.Round(-Position.Y * Zoom + _viewport.Height * 0.5f + _shake.Y);
                return Matrix.CreateScale(Zoom, Zoom, 1f) * Matrix.CreateTranslation(tx, ty, 0f);
            }
        }

        public Vector2 ScreenToWorld(Vector2 screen)
        {
            return (screen - _shake - new Vector2(_viewport.Width, _viewport.Height) * 0.5f) / Zoom + Position;
        }

        public Vector2 WorldToScreen(Vector2 world)
        {
            return (world - Position) * Zoom + new Vector2(_viewport.Width, _viewport.Height) * 0.5f + _shake;
        }

        /// <summary>Ekranda ko'rinayotgan world to'rtburchagi (chap, yuqori, o'ng, past).</summary>
        public Vector4 VisibleWorldRect
        {
            get
            {
                Vector2 half = new Vector2(_viewport.Width, _viewport.Height) * 0.5f / Zoom;
                return new Vector4(Position.X - half.X, Position.Y - half.Y, Position.X + half.X, Position.Y + half.Y);
            }
        }

        public void Update(float dt, InputManager input, Viewport viewport)
        {
            _viewport = viewport;
            if (_shakeT > 0f)
            {
                _shakeT = MathF.Max(0f, _shakeT - dt);
                _shakeClock += dt;
                float k = _shakeT / _shakeDur;
                _shake = new Vector2(MathF.Sin(_shakeClock * 83f), MathF.Cos(_shakeClock * 61f)) * _shakeAmp * k * k;
            }
            else _shake = Vector2.Zero;

            // --- Klaviatura bilan pan ---
            Vector2 move = Vector2.Zero;
            if (KeyboardPanEnabled) // jangchini boshqarayotganda klaviatura jangchiga tegishli
            {
                if (input.KeyDown(Keys.A) || input.KeyDown(Keys.Left)) move.X -= 1;
                if (input.KeyDown(Keys.D) || input.KeyDown(Keys.Right)) move.X += 1;
                if (input.KeyDown(Keys.W) || input.KeyDown(Keys.Up)) move.Y -= 1;
                if (input.KeyDown(Keys.S) || input.KeyDown(Keys.Down)) move.Y += 1;
            }
            if (move != Vector2.Zero)
            {
                move.Normalize();
                float speed = (input.KeyDown(Keys.LeftShift) ? 1500f : 800f) / Zoom;
                Position += move * speed * dt;
            }

            // --- Sichqoncha bilan sudrash ---
            if (input.IsPanDragging)
            {
                Point d = input.MouseDelta;
                Position -= new Vector2(d.X, d.Y) / Zoom;
            }

            // --- Zoom (kursor ostidagi nuqta joyida qoladi) ---
            if (input.WheelDelta != 0 && !input.MouseOverUI)
            {
                float factor = MathF.Pow(1.15f, input.WheelDelta / 120f);
                _targetZoom = MathHelper.Clamp(_targetZoom * factor, GameConfig.MinZoom, GameConfig.MaxZoom);
            }
            if (input.KeyPressed(Keys.OemPlus) || input.KeyPressed(Keys.Add))
                _targetZoom = MathHelper.Clamp(_targetZoom * 1.2f, GameConfig.MinZoom, GameConfig.MaxZoom);
            if (input.KeyPressed(Keys.OemMinus) || input.KeyPressed(Keys.Subtract))
                _targetZoom = MathHelper.Clamp(_targetZoom / 1.2f, GameConfig.MinZoom, GameConfig.MaxZoom);

            if (MathF.Abs(_targetZoom - Zoom) > 0.0005f)
            {
                Vector2 anchor = input.MouseVector;
                Vector2 before = ScreenToWorld(anchor);
                Zoom = MathHelper.Lerp(Zoom, _targetZoom, 1f - MathF.Exp(-14f * dt));
                if (MathF.Abs(_targetZoom - Zoom) < 0.0005f) Zoom = _targetZoom;
                Vector2 after = ScreenToWorld(anchor);
                Position += before - after;
            }

            ClampToMap();
        }

        /// <summary>Kamerani darhol berilgan nuqta va zoom'ga o'rnatadi (Home tugmasi, Load uchun).</summary>
        public void Jump(Vector2 position, float zoom)
        {
            Position = position;
            Zoom = _targetZoom = MathHelper.Clamp(zoom, GameConfig.MinZoom, GameConfig.MaxZoom);
            ClampToMap();
        }

        private void ClampToMap()
        {
            float halfW = GameConfig.MapSize * GameConfig.HalfTileW;
            float fullH = GameConfig.MapSize * GameConfig.TileHeight;
            Position.X = MathHelper.Clamp(Position.X, -halfW, halfW);
            Position.Y = MathHelper.Clamp(Position.Y, 0, fullH);
        }
    }
}
