using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.Core
{
    /// <summary>
    /// Izometrik koordinata almashtirishlari.
    /// Tile o'qlari: +X ekranda o'ng-pastga, +Y ekranda chap-pastga ketadi.
    /// Butun (x, y) uchun TileToWorld tile rombining YUQORI (shimoliy) burchagini qaytaradi.
    /// </summary>
    public static class IsoMath
    {
        public static Vector2 TileToWorld(float tx, float ty)
        {
            return new Vector2((tx - ty) * GameConfig.HalfTileW, (tx + ty) * GameConfig.HalfTileH);
        }

        public static Vector2 TileToWorld(Vector2 t) => TileToWorld(t.X, t.Y);

        /// <summary>World piksel -> uzluksiz tile koordinatasi.</summary>
        public static Vector2 WorldToTileF(Vector2 w)
        {
            float a = w.X / GameConfig.HalfTileW;
            float b = w.Y / GameConfig.HalfTileH;
            return new Vector2((a + b) * 0.5f, (b - a) * 0.5f);
        }

        public static Point WorldToTile(Vector2 w)
        {
            var t = WorldToTileF(w);
            return new Point((int)MathF.Floor(t.X), (int)MathF.Floor(t.Y));
        }
    }

    /// <summary>
    /// Dunyoda chuqurlik bo'yicha saralanib chiziladigan har qanday obyekt (bino, bezak, jangchi).
    ///
    /// Saralash qoidasi: Depth kichik bo'lgan obyekt oldin chiziladi.
    ///  - kvadrat izli (n x n) statik obyekt: Depth = x + y + n
    ///  - harakatlanuvchi unit:              Depth = pos.X + pos.Y
    /// Shu qoida kvadrat izlar uchun har doim to'g'ri tartib beradi, shuning uchun
    /// barcha binolar kvadrat (1x1, 2x2, 3x3 ...) qilib olingan.
    /// </summary>
    public interface IWorldObject
    {
        float Depth { get; }
        /// <summary>Teng Depth bo'lganda: 0 = statik, 1 = unit (unit ustida chiziladi).</summary>
        int SortLayer { get; }
        Texture2D Texture { get; }
        /// <summary>Sprite'ning chap-yuqori burchagi (world piksel).</summary>
        Vector2 DrawPosition { get; }
        bool Flipped { get; }
    }

    public static class WorldObjectExtensions
    {
        /// <summary>
        /// Sprite konvensiyasi: rasmning PASTKI-O'RTA nuqtasi obyekt izining janubiy (eng pastki)
        /// burchagiga to'g'ri keladi. Shu sabab istalgan o'lchamdagi PNG bilan almashtirish mumkin.
        /// </summary>
        public static Vector2 AnchorBottomCenter(Texture2D tex, Point tile, int size)
        {
            Vector2 south = IsoMath.TileToWorld(tile.X + size, tile.Y + size);
            return new Vector2(south.X - tex.Width / 2f, south.Y - tex.Height);
        }
    }
}
