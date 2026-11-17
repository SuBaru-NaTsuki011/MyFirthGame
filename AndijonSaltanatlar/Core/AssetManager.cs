using System;
using System.Collections.Generic;
using System.IO;
using AndijonSaltanatlar.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.Core
{
    /// <summary>Barcha sprite kalitlari. Fayl nomi = kalit + ".png".</summary>
    public static class SpriteKeys
    {
        public const string Tiles = "tiles";

        public const string House = "building_house";       // + _0.._3
        public const int HouseVariants = 4;
        public const string Tower = "building_tower";
        public const string Wall = "building_wall";
        public const string Gate = "building_gate";
        public const string Market = "building_market";
        public const string MainHall = "building_mainhall";
        public const string Barracks = "building_barracks";
        public const string Garden = "building_garden";
        public const string Madrasa = "building_madrasa";
        public const string Mosque = "building_mosque";
        public const string Caravanserai = "building_caravanserai";

        public const string TreePoplar = "decor_tree_poplar";
        public const string TreeChinor = "decor_tree_chinor";
        public const string TreeApricot = "decor_tree_apricot";
        public const string TreeCypress = "decor_tree_cypress";
        public const string Bush = "decor_bush";
        public const string Flowers = "decor_flowers";
        public const string Stall = "decor_stall";           // + _0.._3
        public const int StallVariants = 4;
        public const string Hovuz = "decor_hovuz";
        public const string Well = "decor_well";
        public const string Brazier = "decor_brazier";
        public const string Banner = "decor_banner";
        public const string Crates = "decor_crates";
        public const string Dummy = "decor_dummy";
        public const string WeaponRack = "decor_weaponrack";
        public const string Tandir = "decor_tandir";
        public const string Sori = "decor_sori";

        public const string UnitSwordsman = "unit_swordsman"; // + _0.._2 (kadrlar)
        public const string UnitSpearman = "unit_spearman";
        public const string UnitShield = "unit_shield";
        public const string UnitPatrol = "unit_patrol";
        public const string UnitRaider = "unit_raider";           // bosqinchi
        public const string UnitRaiderChief = "unit_raider_chief"; // bosqinchilar sardori
        public const string UnitShah = "unit_shah";                // shoh (tojli)
        public const string UnitCitizen = "unit_citizen"; // + <0..4> + _0.._2 (fuqaro turi + kadr)
        public const int CitizenKinds = 5;
        public const int UnitFrames = 3;

        public const string IconCoin = "icon_coin";
        public const string IconFood = "icon_food";
        public const string IconPopulation = "icon_population";
    }

    /// <summary>
    /// Sprite'larni boshqaradi. Asosiy g'oya — "almashtiriladigan placeholder":
    ///   1) Content/Sprites/&lt;kalit&gt;.png bor bo'lsa — o'sha haqiqiy rasm yuklanadi;
    ///   2) bo'lmasa — kod bilan chizilgan placeholder ishlatiladi.
    /// Shunday qilib rassom tayyorlagan PNG'ni papkaga tashlash kifoya, kodga tegilmaydi.
    /// (Sprite langari: rasmning pastki-o'rta nuqtasi = obyekt izining janubiy burchagi.)
    /// </summary>
    public sealed class AssetManager
    {
        private readonly GraphicsDevice _gd;
        private readonly string _spriteDir;
        private readonly string _exportDir;
        private readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
        private readonly Dictionary<Texture2D, byte[]> _alpha = new Dictionary<Texture2D, byte[]>();

        /// <summary>1x1 oq piksel — UI to'rtburchaklari uchun.</summary>
        public Texture2D Pixel { get; private set; }
        /// <summary>GraphicsDevice — placeholder Animatsiya tizimida ToTexture chaqiruvi uchun.</summary>
        public GraphicsDevice GraphicsDevice => _gd;
        public string ContentDir { get; }

        public AssetManager(GraphicsDevice gd, string contentDir, string exportDir = null)
        {
            _gd = gd;
            ContentDir = contentDir;
            _spriteDir = Path.Combine(contentDir, "Sprites");
            _exportDir = exportDir;
            if (_exportDir != null) Directory.CreateDirectory(_exportDir);
        }

        public void LoadAll()
        {
            Pixel = new Texture2D(_gd, 1, 1);
            Pixel.SetData(new[] { Color.White });

            Register(SpriteKeys.Tiles, TileArt.BuildAtlas);

            for (int i = 0; i < SpriteKeys.HouseVariants; i++)
            {
                int v = i;
                Register(SpriteKeys.House + "_" + i, () => BuildingArt.House(v));
            }
            Register(SpriteKeys.Tower, BuildingArt.Tower);
            Register(SpriteKeys.Wall, BuildingArt.Wall);
            Register(SpriteKeys.Gate, BuildingArt.Gate);
            Register(SpriteKeys.Market, BuildingArt.Market);
            Register(SpriteKeys.MainHall, BuildingArt.MainHall);
            Register(SpriteKeys.Barracks, BuildingArt.Barracks);
            Register(SpriteKeys.Garden, BuildingArt.Garden);
            Register(SpriteKeys.Madrasa, BuildingArt.Madrasa);
            Register(SpriteKeys.Mosque, BuildingArt.Mosque);
            Register(SpriteKeys.Caravanserai, BuildingArt.Caravanserai);

            Register(SpriteKeys.TreePoplar, () => DecorArt.Tree(0));
            Register(SpriteKeys.TreeChinor, () => DecorArt.Tree(1));
            Register(SpriteKeys.TreeApricot, () => DecorArt.Tree(2));
            Register(SpriteKeys.TreeCypress, () => DecorArt.Tree(3));
            Register(SpriteKeys.Bush, DecorArt.Bush);
            Register(SpriteKeys.Flowers, DecorArt.Flowers);
            for (int i = 0; i < SpriteKeys.StallVariants; i++)
            {
                int v = i;
                Register(SpriteKeys.Stall + "_" + i, () => DecorArt.Stall(v));
            }
            Register(SpriteKeys.Hovuz, DecorArt.Hovuz);
            Register(SpriteKeys.Well, DecorArt.Well);
            Register(SpriteKeys.Brazier, DecorArt.Brazier);
            Register(SpriteKeys.Banner, DecorArt.Banner);
            Register(SpriteKeys.Crates, DecorArt.Crates);
            Register(SpriteKeys.Dummy, DecorArt.Dummy);
            Register(SpriteKeys.WeaponRack, DecorArt.WeaponRack);
            Register(SpriteKeys.Tandir, DecorArt.Tandir);
            Register(SpriteKeys.Sori, DecorArt.Sori);

            RegisterUnit(SpriteKeys.UnitSwordsman, UnitArt.Swordsman);
            RegisterUnit(SpriteKeys.UnitSpearman, UnitArt.Spearman);
            RegisterUnit(SpriteKeys.UnitShield, UnitArt.Shield);
            RegisterUnit(SpriteKeys.UnitPatrol, UnitArt.Patrol);
            RegisterUnit(SpriteKeys.UnitRaider, UnitArt.Raider);
            RegisterUnit(SpriteKeys.UnitRaiderChief, UnitArt.RaiderChief);
            RegisterUnit(SpriteKeys.UnitShah, UnitArt.Shah);

            // Fuqarolar (har turi 3 kadr)
            for (int k = 0; k < SpriteKeys.CitizenKinds; k++)
                for (int f = 0; f < SpriteKeys.UnitFrames; f++)
                {
                    int kk = k, ff = f;
                    Register(SpriteKeys.UnitCitizen + kk + "_" + ff, () => CitizenArt.Frame(kk, ff));
                }

            Register(SpriteKeys.IconCoin, DecorArt.IconCoin);
            Register(SpriteKeys.IconFood, DecorArt.IconFood);
            Register(SpriteKeys.IconPopulation, DecorArt.IconPopulation);
        }

        private void RegisterUnit(string key, int kind)
        {
            for (int f = 0; f < SpriteKeys.UnitFrames; f++)
            {
                int frame = f;
                Register(key + "_" + f, () => UnitArt.Frame(kind, frame));
            }
        }

        private void Register(string key, Func<Canvas> placeholder)
        {
            string path = Path.Combine(_spriteDir, key + ".png");
            Canvas canvas = null;
            if (File.Exists(path))
            {
                try { canvas = Canvas.FromPng(_gd, path); }
                catch (Exception) { canvas = null; } // buzuq fayl — placeholderga qaytamiz
            }
            if (canvas == null)
            {
                canvas = placeholder();
                if (_exportDir != null) canvas.SavePng(_gd, Path.Combine(_exportDir, key + ".png"));
            }

            Texture2D tex = canvas.ToTexture(_gd);
            tex.Name = key;
            _textures[key] = tex;

            var mask = new byte[canvas.Pixels.Length];
            for (int i = 0; i < mask.Length; i++) mask[i] = canvas.Pixels[i].A;
            _alpha[tex] = mask;
        }

        public Texture2D Get(string key)
        {
            if (_textures.TryGetValue(key, out var tex)) return tex;
            throw new KeyNotFoundException("Sprite topilmadi: " + key);
        }

        /// <summary>Sprite'ning berilgan pikseli shaffof emasmi (aniq tanlash uchun).</summary>
        public bool IsOpaque(Texture2D tex, int x, int y)
        {
            if (tex == null || (uint)x >= (uint)tex.Width || (uint)y >= (uint)tex.Height) return false;
            return _alpha.TryGetValue(tex, out var mask) && mask[y * tex.Width + x] > 40;
        }
    }
}
