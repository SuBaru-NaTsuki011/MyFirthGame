using AndijonSaltanatlar.CityPlanning;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.Blueprints
{
    /// <summary>
    /// Blueprint preview'ini xarita ustiga chizadi — PROMPT 15, 9-bo'lim ranglari:
    /// yashil — qurish mumkin, sariq — resurs kerak, qizil — to'siq, ko'k — rejalashtirilgan yo'l,
    /// oltin — tanlangan element. Faqat ko'rinish; hech qanday holat o'zgarmaydi.
    /// </summary>
    public sealed class BlueprintRenderer
    {
        private static readonly Color Green = new Color(80, 225, 125);
        private static readonly Color Yellow = new Color(245, 205, 70);
        private static readonly Color Red = new Color(235, 70, 60);
        private static readonly Color Blue = new Color(70, 150, 245);
        private static readonly Color Gold = new Color(255, 215, 90);

        private readonly MapManager _map;

        public BlueprintRenderer(MapManager map) => _map = map;

        public void Draw(SpriteBatch sb, CityPlan plan, int selectedItemId)
        {
            if (plan == null || sb == null) return;

            foreach (BlueprintRoad road in plan.Roads)
            {
                Color c = StatusColor(road.Status, Blue);
                _map.DrawTileOverlay(sb, road.Tile.X, road.Tile.Y, c * 0.42f, false);
                _map.DrawTileOverlay(sb, road.Tile.X, road.Tile.Y, c * 0.95f, true);
            }

            foreach (BlueprintItem item in plan.Buildings)
            {
                bool selected = item.Id == selectedItemId;
                Color c = selected ? Gold : StatusColor(item.Status, Green);
                for (int y = item.Tile.Y; y < item.Tile.Y + item.Size; y++)
                    for (int x = item.Tile.X; x < item.Tile.X + item.Size; x++)
                    {
                        if (!_map.InBounds(x, y)) continue;
                        _map.DrawTileOverlay(sb, x, y, c * (selected ? 0.50f : 0.34f), false);
                        _map.DrawTileOverlay(sb, x, y, c * (selected ? 1f : 0.78f), true);
                    }

                if (selected)
                {
                    int cx = item.Tile.X + item.Size / 2;
                    int cy = item.Tile.Y + item.Size / 2;
                    _map.DrawMarker(sb, IsoMath.TileToWorld(new Vector2(cx + 0.5f, cy + 0.5f)), Gold, 0.85f);
                }
            }
        }

        private static Color StatusColor(BlueprintStatus status, Color normal) => status switch
        {
            BlueprintStatus.NeedsResource => Yellow,
            BlueprintStatus.Blocked => Red,
            _ => normal
        };
    }
}
