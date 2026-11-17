using System;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.UI;
using AndijonSaltanatlar.Units;
using AndijonSaltanatlar.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.Buildings
{
    /// <summary>
    /// Qurilish rejimi: ghost preview, joylashuvni tekshirish va binoni qo'yish.
    ///   - chap tugma: qurish (devorni tugmani bosib turib "chizish" mumkin)
    ///   - o'ng tugma / Esc: bekor qilish
    /// </summary>
    public sealed class ConstructionSystem
    {
        private readonly MapManager _map;
        private readonly BuildingManager _buildings;
        private readonly UnitManager _units;
        private readonly GameStateManager _state;
        private readonly AssetManager _assets;
        private readonly Random _rng = new Random();

        private Building _ghost;          // ko'rsatilayotgan namuna
        private int _ghostVariant;
        private Point _lastPlaced = new Point(-1, -1);

        /// <summary>Ghost izining shimoliy tile'i.</summary>
        public Point Origin { get; private set; }
        public bool IsValid { get; private set; }
        public bool HasHover { get; private set; }

        public ConstructionSystem(MapManager map, BuildingManager buildings, UnitManager units, GameStateManager state, AssetManager assets)
        {
            _map = map;
            _buildings = buildings;
            _units = units;
            _state = state;
            _assets = assets;
        }

        public void Update(InputManager input, CameraManager camera)
        {
            if (_state.Mode != GameMode.BuildMode)
            {
                _ghost = null;
                HasHover = false;
                return;
            }

            if (_ghost == null || _ghost.Type != _state.SelectedBuildType)
                NewGhost();

            if (input.RightClicked)
            {
                _state.CancelBuild();
                return;
            }

            HasHover = !input.MouseOverUI;
            if (!HasHover) return;

            SetHover(IsoMath.WorldToTile(camera.ScreenToWorld(input.MouseVector)));

            bool click = input.LeftPressed;
            // Devorni sudrab ketma-ket qurish
            bool drag = _ghost.Type == BuildingType.Wall && input.LeftDown && Origin != _lastPlaced && IsValid;
            if (click || drag) TryPlace(click);
        }

        /// <summary>Kursor ostidagi tile bo'yicha ghost o'rnini hisoblaydi (bino kursorga markazlanadi).</summary>
        public void SetHover(Point tile)
        {
            if (_ghost == null) NewGhost();
            int size = _ghost.Size.X;
            Origin = new Point(tile.X - (size - 1) / 2, tile.Y - (size - 1) / 2);
            IsValid = _buildings.CanPlace(Origin, size) && !_units.AnyInArea(Origin, size) && _state.IsLandBuildable(Origin, size);
            HasHover = true;
        }

        private void NewGhost()
        {
            _ghostVariant = _rng.Next(SpriteKeys.HouseVariants);
            _ghost = BuildingFactory.Create(_state.SelectedBuildType, Point.Zero, _ghostVariant);
            _ghost.SpriteTexture = _assets.Get(_ghost.SpriteKey);
        }

        /// <summary>
        /// Qurilishni berilgan katakda sinab ko'radi va joylashtiradi (test/smoke uchun ommaviy).
        /// Click yo'li ham shu metoddan foydalanadi — xatti-harakat bir xil.
        /// </summary>
        public bool PlaceAt(Point tile, bool showErrors = true)
        {
            if (_ghost == null || _ghost.Type != _state.SelectedBuildType) NewGhost();
            SetHover(tile);
            return TryPlace(showErrors);
        }

        /// <summary>
        /// Binoni aniq shimoliy-g'arbiy origin bo'yicha joylashtiradi. AI navbati blueprint'dagi
        /// kataklarni aynan shu ma'noda saqlaydi; PlaceAt esa kursorni markazga moslaydi.
        /// </summary>
        public bool PlaceAtOrigin(Point origin, bool showErrors = true)
        {
            if (_ghost == null || _ghost.Type != _state.SelectedBuildType) NewGhost();
            int size = _ghost.Size.X;
            Origin = origin;
            IsValid = _buildings.CanPlace(Origin, size) && !_units.AnyInArea(Origin, size) &&
                      _state.IsLandBuildable(Origin, size);
            HasHover = true;
            return TryPlace(showErrors);
        }

        private bool TryPlace(bool showErrors)
        {
            if (!_state.IsBuildingUnlocked(_ghost.Type))
            {
                if (showErrors) _state.ShowMessage("Avval texnologiyani oching (TARAQQIYOT)", true);
                return false;
            }
            if (!_state.IsLandBuildable(Origin, _ghost.Size.X))
            {
                if (showErrors) _state.ShowMessage("Bu yer hali ochilmagan — avval hududni oching", true);
                return false;
            }
            if (!IsValid)
            {
                if (showErrors) _state.ShowMessage(Texts.CannotBuildHere, true);
                return false;
            }
            if (!_state.CanAfford(_ghost.Cost))
            {
                if (showErrors) _state.ShowMessage(Texts.NotEnoughCoins, true);
                return false;
            }

            Building b = BuildingFactory.Create(_ghost.Type, Origin, _ghostVariant);
            b.BuiltByPlayer = true;
            if (!_buildings.Add(b)) return false;
            b.BuildProgress = 0f; // bosqichli qurilish boshlanadi

            _state.Coins -= b.Cost;
            _lastPlaced = Origin;
            _state.ShowMessage(b.Name + " qurildi  (−" + b.Cost + " tanga)");
            if (b.Type == BuildingType.House) NewGhost(); // keyingi uy boshqacha ko'rinishda bo'lsin
            IsValid = false;
            return true;
        }

        /// <summary>Ghost: iz tile'lari (yashil/qizil) va yarim shaffof bino sprite'i.</summary>
        public void Draw(SpriteBatch sb)
        {
            if (_state.Mode != GameMode.BuildMode || _ghost == null || !HasHover) return;

            int size = _ghost.Size.X;
            Color fill = IsValid ? new Color(70, 220, 100) * 0.42f : new Color(235, 60, 50) * 0.48f;
            Color line = IsValid ? new Color(150, 255, 170) * 0.9f : new Color(255, 120, 110) * 0.9f;
            for (int y = Origin.Y; y < Origin.Y + size; y++)
                for (int x = Origin.X; x < Origin.X + size; x++)
                {
                    if (!_map.InBounds(x, y)) continue;
                    _map.DrawTileOverlay(sb, x, y, fill, false);
                    _map.DrawTileOverlay(sb, x, y, line, true);
                }

            Vector2 pos = WorldObjectExtensions.AnchorBottomCenter(_ghost.SpriteTexture, Origin, size);
            Color tint = IsValid ? new Color(190, 255, 200) * 0.72f : new Color(255, 130, 120) * 0.62f;
            sb.Draw(_ghost.SpriteTexture, pos, tint);
        }
    }
}
