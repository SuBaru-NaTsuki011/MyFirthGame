using AndijonSaltanatlar.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.LandExpansion
{
    /// <summary>
    /// Qulflangan yer uchastkalarini shahar xaritasida xira qoplama va rangli kontur bilan chizadi.
    /// Ochish mumkin bo'lganlar oltin, qulflanganlar kulrang konturda. Ko'rinmaydigan tile'lar o'tkazib yuboriladi.
    /// </summary>
    public static class LandExpansionRenderer
    {
        private static readonly Color DarkLocked = new Color(8, 10, 20, 150);
        private static readonly Color DarkReady = new Color(20, 16, 8, 120);
        private static readonly Color BorderLocked = new Color(120, 120, 140, 160);
        private static readonly Color BorderReady = new Color(220, 175, 80, 200);

        public static void Draw(SpriteBatch sb, MapManager map, CityLandExpansionManager land, Vector4 view)
        {
            foreach (LandParcel p in land.Parcels)
            {
                if (p.State == LandParcelState.Open) continue;
                bool ready = p.State == LandParcelState.Unlockable;
                Color fill = ready ? DarkReady : DarkLocked;
                Color border = ready ? BorderReady : BorderLocked;
                Rectangle b = p.GridBounds;

                for (int y = b.Y; y < b.Y + b.Height; y++)
                    for (int x = b.X; x < b.X + b.Width; x++)
                    {
                        if (!map.InBounds(x, y)) continue;
                        float wx = (x - y) * GameConfigHalfW, wy = (x + y) * GameConfigHalfH;
                        if (wx + 40 < view.X || wx - 40 > view.Z || wy + 40 < view.Y || wy - 20 > view.W) continue;
                        map.DrawTileOverlay(sb, x, y, fill, false);
                        bool edge = x == b.X || x == b.X + b.Width - 1 || y == b.Y || y == b.Y + b.Height - 1;
                        if (edge) map.DrawTileOverlay(sb, x, y, border, true);
                    }
            }
        }

        private const int GameConfigHalfW = 32;
        private const int GameConfigHalfH = 16;
    }
}
