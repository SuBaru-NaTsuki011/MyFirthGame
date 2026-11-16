using System;
using System.Collections.Generic;

namespace AndijonSaltanatlar.Transportation
{
    /// <summary>
    /// Transport animatsiyasi holati (har transport uchun alohida):
    ///   - g'ildirak aylanishi tezlikka mos;
    ///   - to'xtaganda g'ildirak aylanishi to'xtaydi va sekin "orqaga qaytib" to'g'irlanadi;
    ///   - burilishda yuzlanish burchagi silliq o'zgaradi;
    ///   - yuk ortish/tushirish holatida yurak tebranishi (bob) to'xtaydi.
    /// Anchor nuqtasi (oyoq/g'ildirak yerga tegadigan joy) hech qachon o'zgarmaydi — tebranish
    /// faqat sprite ichida, draw-time'da qo'llanadi.
    /// </summary>
    public sealed class TransportAnimationController
    {
        private sealed class State
        {
            public float Wheel;
            public float Bob;
            public float SmoothAngle;
            public float LastSpeed;
        }

        private readonly Dictionary<int, State> _states = new Dictionary<int, State>();

        public void Update(TransportEntity e, float dt)
        {
            if (e == null) return;
            if (!_states.TryGetValue(e.Id, out State s))
            {
                s = new State();
                _states[e.Id] = s;
            }

            float speed = e.CurrentSpeed;
            if (e.AnimationState == TransportAnimationState.Moving && speed > 0.001f)
            {
                // Aylanish tezligi chiziqli tezlikka bog'liq: tezroq arava — tezroq g'ildirak.
                s.Wheel += dt * (1.2f + speed * 1.8f);
                s.Bob += dt * (4.0f + speed * 2.2f);
                if (s.Wheel > MathF.PI * 2f) s.Wheel -= MathF.PI * 2f;
            }
            else if (e.AnimationState == TransportAnimationState.Turn)
            {
                s.Wheel += dt * 0.9f;
            }
            else
            {
                // To'xtaganda g'ildirak asta-sekin to'xtaydi (sakrab to'xtamaydi).
                float decay = Math.Max(0f, 1f - dt * 6f);
                s.Wheel += dt * 0.4f * decay;
            }

            // Burilish: yuzlanish burchagi silliq yaqinlashadi.
            float target = e.FacingAngle;
            float diff = target - s.SmoothAngle;
            while (diff > MathF.PI) diff -= MathF.PI * 2f;
            while (diff < -MathF.PI) diff += MathF.PI * 2f;
            s.SmoothAngle += diff * Math.Min(1f, dt * 10f);

            s.LastSpeed = speed;
            e.WheelRotation = s.Wheel;
            e.BobPhase = e.AnimationState == TransportAnimationState.Moving ? s.Bob : 0f;
        }

        /// <summary>Chizishda ishlatiladigan silliq yuzlanish burchagi.</summary>
        public float SmoothAngleOf(int transportId)
            => _states.TryGetValue(transportId, out State s) ? s.SmoothAngle : 0f;

        /// <summary>Harakat tufayli yuzaga keladigan kichik vertikal tebranish (piksel).</summary>
        public float BobOf(int transportId)
            => _states.TryGetValue(transportId, out State s) ? MathF.Sin(s.Bob) * 0.8f : 0f;

        public void Forget(int transportId) => _states.Remove(transportId);

        public void Reset() => _states.Clear();
    }
}
