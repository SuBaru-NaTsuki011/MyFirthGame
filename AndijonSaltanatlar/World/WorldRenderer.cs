using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Core.Graphics;
using AndijonSaltanatlar.Units;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.World
{
    /// <summary>
    /// Dunyodagi barcha "tik" obyektlarni (bino, bezak, jangchi) chuqurlik bo'yicha saralab chizadi
    /// va sichqoncha ostidagi obyektni piksel aniqligida topadi.
    /// </summary>
    public sealed class WorldRenderer
    {
        private readonly List<IWorldObject> _buffer = new List<IWorldObject>(1024);
        private readonly AssetManager _assets;
        private readonly PixelFont _font;

        public int LastDrawnCount { get; private set; }

        /// <summary>
        /// Bino/bezak teksturasini chizishdan oldin almashtirish (masalan fasl: qor, xazon, gul).
        /// Natija asl tekstura bilan bir xil o'lchamda bo'lishi kerak. Odamlarga qo'llanmaydi.
        /// </summary>
        public Func<IWorldObject, Texture2D, Texture2D> TextureFilter { get; set; }

        public WorldRenderer(AssetManager assets, PixelFont font)
        {
            _assets = assets;
            _font = font;
        }

        private static int Compare(IWorldObject a, IWorldObject b)
        {
            int c = a.Depth.CompareTo(b.Depth);
            if (c != 0) return c;
            c = a.SortLayer.CompareTo(b.SortLayer);
            if (c != 0) return c;
            return a.DrawPosition.X.CompareTo(b.DrawPosition.X);
        }

        private static bool Visible(IWorldObject o, Vector4 view)
        {
            Vector2 p = o.DrawPosition;
            return p.X + o.Texture.Width >= view.X && p.X <= view.Z && p.Y + o.Texture.Height >= view.Y && p.Y <= view.W;
        }

        /// <param name="extra">Qo'shimcha obyektlar (masalan, yerdagi qushlar) — binolar bilan birga saralanadi.</param>
        public void Draw(SpriteBatch sb, Vector4 view, MapManager map, BuildingManager buildings, UnitManager units,
            IReadOnlyList<IWorldObject> extra = null)
        {
            _buffer.Clear();
            foreach (var b in buildings.Buildings) if (Visible(b, view)) _buffer.Add(b);
            foreach (var d in map.Decorations) if (Visible(d, view)) _buffer.Add(d);
            foreach (var u in units.Units) if (!u.Hidden && Visible(u, view)) _buffer.Add(u);
            if (extra != null) foreach (var e in extra) if (Visible(e, view)) _buffer.Add(e);
            _buffer.Sort(Compare);

            foreach (var o in _buffer)
            {
                Color tint = Color.White;
                Building bb = o as Building;
                if (bb != null && bb.BuildProgress < 1f)
                    tint = new Color(150, 180, 255) * (0.45f + 0.55f * bb.BuildProgress); // qurilayotgan bino

                if (o is Unit cu)
                {
                    // Zarba yegan: qizarish asta so'nadi
                    if (cu.HurtTimer > 0f) tint = Color.Lerp(Color.White, new Color(255, 116, 104), MathHelper.Clamp(cu.HurtTimer / 0.3f, 0f, 1f));
                    tint *= cu.FadeAlpha;
                    SpriteEffects fx = o.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    Vector2 feet = IsoMath.TileToWorld(cu.Position);
                    var origin = new Vector2(o.Texture.Width / 2f, o.Texture.Height - Core.Graphics.UnitArt.FootOffset);
                    if (cu.IsDown)
                    {
                        // Yiqilish: tezlashib qulaydi, yerga tushib biroz sakraydi, keyin xira yotadi
                        float lie = MathHelper.Clamp(cu.DownTime / 0.45f, 0f, 1f);
                        Color down = Color.Lerp(tint, new Color(150, 140, 135) * cu.FadeAlpha, lie);
                        float ang = cu.FallAngle * (o.Flipped ? -1f : 1f);
                        Vector2 at = new Vector2(MathF.Round(feet.X), MathF.Round(feet.Y)) + cu.HitDirWorld * 4f * lie;
                        sb.Draw(o.Texture, at, null, down, ang, origin, 1f, fx, 0f);
                        if (cu.FlashTimer > 0f && _assets.FlashOf(o.Texture) is Texture2D wf)
                            sb.Draw(wf, at, null, Color.White * (cu.FlashTimer / Unit.FlashTime) * cu.FadeAlpha, ang, origin, 1f, fx, 0f);
                    }
                    else
                    {
                        // Zarba (kesish / chopish / sanchish / aylanma) va chayqalish: oyoq atrofida og'ish va siljish
                        cu.GetAttackPose(out Vector2 off, out float rot, out float sy, out bool spin);
                        cu.GetHitPose(out Vector2 hoff, out float hrot);
                        if (spin) fx = fx == SpriteEffects.None ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                        Vector2 at = new Vector2(MathF.Round(feet.X), MathF.Round(feet.Y)) + off + hoff;
                        sb.Draw(o.Texture, at, null, tint, rot + hrot, origin, new Vector2(1f, sy), fx, 0f);
                        // Zarba tekkan lahza: oq chaqnash
                        if (cu.FlashTimer > 0f && _assets.FlashOf(o.Texture) is Texture2D wf)
                            sb.Draw(wf, at, null, Color.White * (cu.FlashTimer / Unit.FlashTime) * cu.FadeAlpha, rot + hrot, origin, new Vector2(1f, sy), fx, 0f);
                    }
                    if (cu is CitizenUnit citizen && !cu.IsDown)
                    {
                        DrawCitizenRole(sb, o, citizen);
                        if (!string.IsNullOrEmpty(citizen.SpeechText)) DrawCitizenSpeech(sb, o, citizen);
                    }
                    continue;
                }

                Texture2D tex = TextureFilter != null ? TextureFilter(o, o.Texture) ?? o.Texture : o.Texture;
                sb.Draw(tex, o.DrawPosition, null, tint, 0f, Vector2.Zero, 1f,
                    o.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);

                if (o is Farm farm && farm.BuildProgress >= 1f) DrawFarmStatus(sb, o, farm);

                // Bino daraja nishoni (evolyutsiya belgisi) — BuildingEvolution
                if (o is Building b2 && b2.BuildProgress >= 1f)
                {
                    int pips = System.Math.Min(4, b2.Level - 1);
                    if (pips > 0)
                    {
                        float cx = o.DrawPosition.X + o.Texture.Width / 2f - pips * 6f / 2f;
                        float py = o.DrawPosition.Y + 3f;
                        for (int i = 0; i < pips; i++)
                        {
                            var rct = new Rectangle((int)(cx + i * 6), (int)py, 5, 5);
                            sb.Draw(_assets.Pixel, new Rectangle(rct.X - 1, rct.Y - 1, 7, 7), new Color(20, 20, 24, 220));
                            sb.Draw(_assets.Pixel, rct, new Color(216, 167, 83));
                        }
                    }

                    // Qiyshiq (variant) tint — uy darajasiga mos tablitsa overlay (placeholder)
                    if (b2.Type == BuildingType.House && b2.Level >= 2 && b2.SpriteTexture != null)
                    {
                        int variantIndex = System.Math.Min(3, b2.Level - 1);
                        Color variantTint = variantIndex switch
                        {
                            1 => new Color(255, 180, 100, 60),
                            2 => new Color(100, 180, 255, 60),
                            3 => new Color(255, 120, 255, 50),
                            _ => Color.White
                        };
                        var rect = new Rectangle((int)o.DrawPosition.X,
                                                 (int)o.DrawPosition.Y, o.Texture.Width, o.Texture.Height);
                        sb.Draw(_assets.Pixel, rect, variantTint);
                    }
                }
            }
            LastDrawnCount = _buffer.Count;
        }

        private void DrawCitizenRole(SpriteBatch sb, IWorldObject unit, CitizenUnit citizen)
        {
            Vector2 size = _font.Measure(citizen.RoleLabel, 1, 0);
            int width = (int)size.X + 6;
            int height = (int)size.Y + 4;
            int x = (int)MathF.Round(unit.DrawPosition.X + unit.Texture.Width / 2f - width / 2f);
            int y = (int)MathF.Round(unit.DrawPosition.Y - height - 2);
            sb.Draw(_assets.Pixel, new Rectangle(x, y, width, height), new Color(12, 35, 22, 220));
            _font.Draw(sb, citizen.RoleLabel, new Vector2(x + 3, y + 2), new Color(130, 245, 145), 1, 0);
        }

        private void DrawCitizenSpeech(SpriteBatch sb, IWorldObject unit, CitizenUnit citizen)
        {
            Vector2 size = _font.Measure(citizen.SpeechText, 1, 0);
            int width = (int)size.X + 8;
            int height = (int)size.Y + 6;
            int x = (int)MathF.Round(unit.DrawPosition.X + unit.Texture.Width / 2f - width / 2f);
            int y = (int)MathF.Round(unit.DrawPosition.Y - height - _font.Measure(citizen.RoleLabel, 1, 0).Y - 6);
            var box = new Rectangle(x, y, width, height);
            sb.Draw(_assets.Pixel, box, new Color(246, 230, 184));
            sb.Draw(_assets.Pixel, new Rectangle(x, y, width, 1), new Color(83, 54, 35));
            sb.Draw(_assets.Pixel, new Rectangle(x, y + height - 1, width, 1), new Color(83, 54, 35));
            _font.DrawCentered(sb, citizen.SpeechText, new Vector2(box.Center.X, box.Center.Y), new Color(60, 42, 30), 1, 0);
        }

        private void DrawFarmStatus(SpriteBatch sb, IWorldObject building, Farm farm)
        {
            string text = farm.CropStage == FarmCropStage.Empty
                ? "DALA: BO'SH"
                : farm.CropStage == FarmCropStage.Mature ? "HOSIL TAYYOR" : "EKIN O'SMOQDA";
            Vector2 size = _font.Measure(text, 1, 0);
            int width = (int)size.X + 8;
            int x = (int)MathF.Round(building.DrawPosition.X + building.Texture.Width / 2f - width / 2f);
            int y = (int)MathF.Round(building.DrawPosition.Y - size.Y - 6);
            var box = new Rectangle(x, y, width, (int)size.Y + 4);
            sb.Draw(_assets.Pixel, box, new Color(18, 30, 21, 220));
            _font.DrawCentered(sb, text, new Vector2(box.Center.X, box.Center.Y), new Color(165, 238, 139), 1, 0);
        }

        /// <summary>World nuqtasi ostidagi eng oldingi obyekt (Unit, Building yoki Decoration).</summary>
        public object PickAt(Vector2 world, MapManager map, BuildingManager buildings, UnitManager units)
        {
            Unit unit = units.GetAt(world);
            if (unit != null) return unit;

            IWorldObject best = null;
            void Test(IWorldObject o)
            {
                Vector2 p = o.DrawPosition;
                int lx = (int)MathF.Floor(world.X - p.X), ly = (int)MathF.Floor(world.Y - p.Y);
                if (lx < 0 || ly < 0 || lx >= o.Texture.Width || ly >= o.Texture.Height) return;
                if (o.Flipped) lx = o.Texture.Width - 1 - lx;
                if (!_assets.IsOpaque(o.Texture, lx, ly)) return;
                if (best == null || Compare(o, best) > 0) best = o;
            }
            foreach (var b in buildings.Buildings) Test(b);
            foreach (var d in map.Decorations) Test(d);
            return best;
        }
    }
}
