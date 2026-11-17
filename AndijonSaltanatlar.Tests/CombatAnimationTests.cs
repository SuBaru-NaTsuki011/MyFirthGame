using System;
using System.Linq;
using AndijonSaltanatlar.Core.Graphics;
using AndijonSaltanatlar.Units;
using Microsoft.Xna.Framework;
using Xunit;

namespace AndijonSaltanatlar.Tests
{
    /// <summary>Jang animatsiyasi: zarba kadrlari, chayqalish va yiqilish.</summary>
    public class CombatAnimationTests
    {
        private static readonly int[] Fighters = { UnitArt.Swordsman, UnitArt.Spearman, UnitArt.Shield, UnitArt.Patrol, UnitArt.Raider, UnitArt.RaiderChief };

        [Fact]
        public void AttackFrames_WeaponMovesBetweenPhases_FeetStayAnchored()
        {
            foreach (int kind in Fighters)
                for (int atk = 0; atk < UnitArt.AttackKinds; atk++)
                {
                    Canvas[] f = Enumerable.Range(0, UnitArt.AttackPhases).Select(p => UnitArt.AttackFrame(kind, atk, p)).ToArray();
                    // Ko'tarish, urish va davomi bir-biridan farq qiladi (qurol haqiqatan harakatlanadi)
                    Assert.True(Diff(f[0], f[1]) > 20, "kind " + kind + " atk " + atk + " 0-1");
                    Assert.True(Diff(f[1], f[2]) > 20, "kind " + kind + " atk " + atk + " 1-2");
                    foreach (Canvas c in f)
                    {
                        // Oyoq osti (soya markazi) kanvas o'rtasida — oddiy kadrlar bilan bir joyda turadi
                        int cx = c.Width / 2, fy = c.Height - UnitArt.FootOffset;
                        Assert.True(c.Pixels[fy * c.Width + cx].A > 0, "soya markazda emas: " + kind);
                    }
                    // Urish bosqichida qurol oldinga cho'zilgan: o'ng yarmida ko'tarishdagidan ko'proq piksel
                    Assert.True(RightMass(f[1]) > RightMass(f[0]), "urishda qurol oldinda emas: kind " + kind + " atk " + atk);
                }
        }

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

        private static int Diff(Canvas a, Canvas b)
        {
            int n = 0;
            int w = Math.Min(a.Width, b.Width), h = Math.Min(a.Height, b.Height);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (a.Pixels[y * a.Width + x] != b.Pixels[y * b.Width + x]) n++;
            return n;
        }

        private static int RightMass(Canvas c)
        {
            int n = 0, mid = c.Width / 2 + 8;
            for (int y = 0; y < c.Height - UnitArt.FootOffset - 4; y++)
                for (int x = mid; x < c.Width; x++)
                    if (c.Pixels[y * c.Width + x].A > 0) n++;
            return n;
        }
    }
}
