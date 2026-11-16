using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Water
{
    /// <summary>
    /// Suv qamrovi to'ri. Manbalar atrofiga aylana bo'ylab "suv bosimi" yoziladi; natija
    /// 0..1 oralig'ida — 1 = to'liq ta'minlangan, 0 = suv yo'q. To'r manbalar o'zgarganda
    /// (quduq qurildi, ariq o'zgardi, mavsum almashdi) qayta hisoblanadi, har kadrda emas.
    /// </summary>
    public sealed class WaterCoverageCalculator
    {
        private readonly int _width;
        private readonly int _height;
        private float[] _grid;

        public int Version { get; private set; }
        public int Width => _width;
        public int Height => _height;

        public WaterCoverageCalculator(int width, int height)
        {
            _width = width;
            _height = height;
            _grid = new float[width * height];
        }

        public void Rebuild(IReadOnlyList<WaterSource> sources)
        {
            Array.Clear(_grid, 0, _grid.Length);
            if (sources != null)
                foreach (WaterSource s in sources)
                    Paint(s);
            Version++;
        }

        private void Paint(WaterSource s)
        {
            int r = Math.Max(1, s.ServiceRadius);
            float strength = Math.Clamp(s.EffectiveCapacity / Math.Max(1f, r * 0.9f), 0.25f, 1f);
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    int x = s.Position.X + dx, y = s.Position.Y + dy;
                    if ((uint)x >= (uint)_width || (uint)y >= (uint)_height) continue;
                    float d = MathF.Sqrt(dx * dx + dy * dy);
                    if (d > r) continue;
                    float falloff = 1f - (d / r) * 0.75f;
                    float value = Math.Clamp(strength * falloff, 0f, 1f);
                    int i = y * _width + x;
                    if (value > _grid[i]) _grid[i] = value;
                }
        }

        public float At(int x, int y)
        {
            if ((uint)x >= (uint)_width || (uint)y >= (uint)_height) return 0f;
            return _grid[y * _width + x];
        }

        public float At(Point p) => At(p.X, p.Y);

        /// <summary>Bino izining eng yaxshi qamrovi (bino markazi + burchaklari).</summary>
        public float ForBuilding(Point pos, int size)
        {
            float best = At(pos.X + size / 2, pos.Y + size / 2);
            best = Math.Max(best, At(pos.X, pos.Y));
            best = Math.Max(best, At(pos.X + size - 1, pos.Y));
            best = Math.Max(best, At(pos.X, pos.Y + size - 1));
            best = Math.Max(best, At(pos.X + size - 1, pos.Y + size - 1));
            return best;
        }

        public void Clear()
        {
            Array.Clear(_grid, 0, _grid.Length);
            Version++;
        }
    }
}
