using System;
using AndijonSaltanatlar.Units;
using Microsoft.Xna.Framework;
using Xunit;

namespace AndijonSaltanatlar.Tests
{
    /// <summary>Jang animatsiyasi: zarba kadrlari, chayqalish va yiqilish.</summary>
    public class CombatAnimationTests
    {
        [Fact]
        public void HitReaction_LeansAwayThenRecovers()
        {
            var u = new RaiderUnit(new Vector2(5, 5), false);
            u.TakeHitVisual(new Vector2(1, 0));
            Assert.True(u.FlashTimer > 0f && u.HitReactTimer > 0f && u.HurtTimer > 0f);
            u.HitReactTimer = Unit.HitReactTime * 0.75f;     // erta bosqich: orqaga og'gan
            u.GetHitPose(out Vector2 off, out float rot);
            Assert.True(off.X > 0f && rot > 0f);
            u.HitReactTimer = 0f;                               // tugadi: joyida
            u.GetHitPose(out off, out rot);
            Assert.Equal(Vector2.Zero, off);
            Assert.Equal(0f, rot);
        }

        [Fact]
        public void Fall_RotatesTo90Degrees_WithSmallBounce()
        {
            var u = new RaiderUnit(new Vector2(5, 5), false);
            u.HitPoints = 0;
            float prev = 0f, max = 0f;
            for (int i = 0; i < 40; i++)
            {
                u.Update(0.016f);
                float a = u.FallAngle;
                Assert.InRange(a, 0f, MathHelper.PiOver2 + 0.0001f);
                max = Math.Max(max, a);
                prev = a;
            }
            Assert.Equal(MathHelper.PiOver2, prev, 3);          // oxirida to'liq yotadi
            u.Revive(1f);
            u.Update(0.016f);
            Assert.Equal(0f, u.FallAngle);                      // o'rnidan turdi
        }
    }
}
