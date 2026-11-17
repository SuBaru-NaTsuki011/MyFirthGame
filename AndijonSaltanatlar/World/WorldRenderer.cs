using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Core;
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

        public int LastDrawnCount { get; private set; }

        public WorldRenderer(AssetManager assets)
        {
            _assets = assets;
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

        public void Draw(SpriteBatch sb, Vector4 view, MapManager map, BuildingManager buildings, UnitManager units)
        {
            _buffer.Clear();
            foreach (var b in buildings.Buildings) if (Visible(b, view)) _buffer.Add(b);
            foreach (var d in map.Decorations) if (Visible(d, view)) _buffer.Add(d);
            foreach (var u in units.Units) if (Visible(u, view)) _buffer.Add(u);
            _buffer.Sort(Compare);

            foreach (var o in _buffer)
            {
                Color tint = Color.White;
                Building bb = o as Building;
                if (bb != null && bb.BuildProgress < 1f)
                    tint = new Color(150, 180, 255) * (0.45f + 0.55f * bb.BuildProgress); // qurilayotgan bino

                if (o is Unit cu)
                {
                    if (cu.HurtTimer > 0f) tint = new Color(255, 120, 110); // zarba yegan — qizarish
                    SpriteEffects fx = o.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    if (cu.IsDown)
                    {
                        // Yiqilgan jangchi: yerga yotgan holatda (90° burilgan), xira
                        Vector2 feet = IsoMath.TileToWorld(cu.Position);
                        var origin = new Vector2(o.Texture.Width / 2f, o.Texture.Height - Core.Graphics.UnitArt.FootOffset);
                        sb.Draw(o.Texture, new Vector2(MathF.Round(feet.X), MathF.Round(feet.Y)), null, new Color(150, 140, 135),
                            o.Flipped ? -MathHelper.PiOver2 : MathHelper.PiOver2, origin, 1f, fx, 0f);
                    }
                    else
                    {
                        // Zarba holati (kesish / chopish / sanchish / aylanma): oyoq atrofida og'ish va siljish
                        cu.GetAttackPose(out Vector2 off, out float rot, out float sy, out bool spin);
                        if (spin) fx = fx == SpriteEffects.None ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                        Vector2 feet = IsoMath.TileToWorld(cu.Position);
                        var origin = new Vector2(o.Texture.Width / 2f, o.Texture.Height - Core.Graphics.UnitArt.FootOffset);
                        sb.Draw(o.Texture, new Vector2(MathF.Round(feet.X), MathF.Round(feet.Y)) + off, null, tint, rot, origin,
                            new Vector2(1f, sy), fx, 0f);
                    }
                    continue;
                }

                sb.Draw(o.Texture, o.DrawPosition, null, tint, 0f, Vector2.Zero, 1f,
                    o.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);

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
