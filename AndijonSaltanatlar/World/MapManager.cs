using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.World
{
    /// <summary>Yer turlari.</summary>
    public enum TileType : byte
    {
        Ground = 0, // oddiy yer
        Road = 1,   // ko'cha
        Plaza = 2,  // markaziy maydon / bozor maydoni
        Grass = 3,  // yashil hudud
        Water = 4   // suv / ariq
    }

    /// <summary>
    /// Izometrik tile-xarita: yer turlari, tile'larni kim egallagani (bino yoki bezak),
    /// bezaklar ro'yxati va yerni chizish.
    /// </summary>
    public sealed class MapManager
    {
        private readonly TileType[,] _tiles;
        private readonly byte[,] _variant;
        private readonly object[,] _occupant;
        private readonly bool[,] _reserved;
        private readonly Texture2D _atlas;
        private readonly AssetManager _assets;
        private float _waterTimer;
        private int _nextDecorId = 1;

        public int Width { get; }
        public int Height { get; }
        public List<Decoration> Decorations { get; } = new List<Decoration>();

        public MapManager(AssetManager assets, int width, int height)
        {
            _assets = assets;
            _atlas = assets.Get(SpriteKeys.Tiles);
            Width = width;
            Height = height;
            _tiles = new TileType[width, height];
            _variant = new byte[width, height];
            _occupant = new object[width, height];
            _reserved = new bool[width, height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    SetTile(x, y, TileType.Ground);
        }

        // ---------- Tile ma'lumotlari ----------

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public TileType GetTile(int x, int y) => _tiles[x, y];

        public void SetTile(int x, int y, TileType type)
        {
            if (!InBounds(x, y)) return;
            _tiles[x, y] = type;
            int h = (int)(Canvas.Hash(x * 7 + 3, y * 13 + 5) * 1000);
            if (type == TileType.Plaza)
                _variant[x, y] = (byte)((x % 3 == 0 && y % 3 == 0) ? 3 : h % 3); // muntazam koshin naqsh
            else
                _variant[x, y] = (byte)(h % TileArt.Columns);
        }

        public void FillRect(int x0, int y0, int x1, int y1, TileType type)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    SetTile(x, y, type);
        }

        /// <summary>Tile'ni egallab turgan obyekt (Building yoki Decoration) yoki null.</summary>
        public object GetOccupant(int x, int y) => InBounds(x, y) ? _occupant[x, y] : null;

        public void SetOccupant(Point pos, int size, object occupant)
        {
            for (int y = pos.Y; y < pos.Y + size; y++)
                for (int x = pos.X; x < pos.X + size; x++)
                    if (InBounds(x, y)) _occupant[x, y] = occupant;
        }

        /// <summary>Generator uchun: bu tile'ga tasodifiy bezak tushmasin (soqchi turadigan joy).</summary>
        public void Reserve(int x, int y) { if (InBounds(x, y)) _reserved[x, y] = true; }
        public bool IsReserved(int x, int y) => InBounds(x, y) && _reserved[x, y];

        /// <summary>Unit shu tile ustidan yura oladimi (keyingi bosqichda yo'l topish uchun).</summary>
        public bool IsWalkable(int x, int y)
        {
            if (!InBounds(x, y) || _tiles[x, y] == TileType.Water) return false;
            object o = _occupant[x, y];
            if (o == null) return true;
            if (o is Building b) return b.IsWalkable;
            if (o is Decoration d) return !d.BlocksMovement;
            return false;
        }

        /// <summary>Bu yer turida qurish mumkinmi (ko'cha va suv ustiga qurilmaydi).</summary>
        public bool IsBuildableTerrain(int x, int y)
        {
            if (!InBounds(x, y)) return false;
            TileType t = _tiles[x, y];
            return t == TileType.Ground || t == TileType.Grass || t == TileType.Plaza;
        }

        // ---------- Bezaklar ----------

        public bool CanPlaceDecoration(Point pos, int size)
        {
            for (int y = pos.Y; y < pos.Y + size; y++)
                for (int x = pos.X; x < pos.X + size; x++)
                {
                    if (!InBounds(x, y) || _occupant[x, y] != null) return false;
                    TileType t = _tiles[x, y];
                    if (t == TileType.Water || t == TileType.Road) return false;
                }
            return true;
        }

        public Decoration AddDecoration(DecorationType type, int x, int y, int variant = 0)
        {
            var d = new Decoration(type, new Point(x, y), variant);
            if (!CanPlaceDecoration(d.Position, d.Size)) return null;
            d.Id = _nextDecorId++;
            d.Texture = _assets.Get(d.SpriteKey);
            d.Flipped = d.Removable && Canvas.Hash(x * 3 + 1, y * 5 + 2) > 0.5f;
            d.DrawPosition = WorldObjectExtensions.AnchorBottomCenter(d.Texture, d.Position, d.Size);
            Decorations.Add(d);
            SetOccupant(d.Position, d.Size, d);
            return d;
        }

        public void RemoveDecoration(Decoration d)
        {
            if (!Decorations.Remove(d)) return;
            SetOccupant(d.Position, d.Size, null);
        }

        // ---------- Chizish ----------

        public void Update(float dt)
        {
            _waterTimer += dt;
        }

        /// <summary>Fasl atlasi (SeasonVisuals). null — asl atlas. Tuzilishi asl atlas bilan bir xil.</summary>
        public Texture2D AtlasOverride { get; set; }

        /// <summary>Faqat ekranda ko'rinadigan tile'larni chizadi (bitta atlas — bitta draw-call).</summary>
        public void Draw(SpriteBatch sb, Vector4 view)
        {
            Texture2D atlas = AtlasOverride ?? _atlas;
            int waterRow = 4 + ((int)(_waterTimer * 1.6f) & 1);
            const int hw = GameConfig.HalfTileW, hh = GameConfig.HalfTileH;

            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    float wx = (x - y) * hw, wy = (x + y) * hh;
                    if (wx + 34 < view.X || wx - 34 > view.Z || wy + 34 < view.Y || wy - 2 > view.W) continue;

                    TileType t = _tiles[x, y];
                    int row = t == TileType.Water ? waterRow : (int)t;
                    var pos = new Vector2(wx + TileArt.DrawOffsetX, wy + TileArt.DrawOffsetY);
                    sb.Draw(atlas, pos, TileArt.Cell(row, _variant[x, y]), Color.White);

                    if (t == TileType.Water)
                    {
                        // Qirg'oqlar: qo'shni tile suv bo'lmasa, o'sha tomonga tuproq chet chiziladi.
                        if (IsLand(x, y - 1)) sb.Draw(atlas, pos, TileArt.Cell(TileArt.RowBanks, 0), Color.White);
                        if (IsLand(x - 1, y)) sb.Draw(atlas, pos, TileArt.Cell(TileArt.RowBanks, 1), Color.White);
                        if (IsLand(x + 1, y)) sb.Draw(atlas, pos, TileArt.Cell(TileArt.RowBanks, 2), Color.White);
                        if (IsLand(x, y + 1)) sb.Draw(atlas, pos, TileArt.Cell(TileArt.RowBanks, 3), Color.White);
                    }
                }
        }

        private bool IsLand(int x, int y) => InBounds(x, y) && _tiles[x, y] != TileType.Water;

        /// <summary>Tile ustiga rangli romb (highlight, ghost izi). outline=true — faqat kontur.</summary>
        public void DrawTileOverlay(SpriteBatch sb, int x, int y, Color color, bool outline)
        {
            Vector2 w = IsoMath.TileToWorld(x, y);
            sb.Draw(_atlas, new Vector2(w.X + TileArt.DrawOffsetX, w.Y + TileArt.DrawOffsetY),
                TileArt.Cell(TileArt.RowOverlay, outline ? 1 : 0), color);
        }

        /// <summary>Unit oyog'i ostidagi kichik tanlov halqasi.</summary>
        public void DrawMarker(SpriteBatch sb, Vector2 world, Color color, float scale = 0.5f)
        {
            sb.Draw(_atlas, world, TileArt.Cell(TileArt.RowOverlay, 1), color, 0f, new Vector2(33, 17), scale, SpriteEffects.None, 0f);
        }
    }
}
