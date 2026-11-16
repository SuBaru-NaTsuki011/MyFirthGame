using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Core.Graphics;
using AndijonSaltanatlar.Units;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.World
{
    /// <summary>
    /// Boshlang'ich shaharni quradi — Andijon / Asaka ruhidagi devor bilan o'ralgan shahriston.
    ///
    /// Reja (64x64 xarita, shahar markazi 32,32):
    ///   - qal'a devori: x,y = 14..49 halqasi, 4 ta darvoza (har biri qo'sh ravoq + 2 minora), 4 burchak minora
    ///   - ikki bosh ko'cha (2 tile keng) markaziy maydonda kesishadi; mahallalarni tor ko'chalar ajratadi
    ///   - markaziy maydon (26..37) o'rtasida hovuz; shimolida Saroy
    ///   - janubi-sharqda bozor maydoni: savdo toqi + rastalar
    ///   - madrasa, jome masjidi, karvonsaroy, qo'shinxona, bog'lar, 19 ta uy
    ///   - shahar ichidan ariq o'tadi, tashqarida soy, dalalar va teraklar
    /// Hamma narsa aniq koordinatalar bilan (deterministik) joylanadi, shuning uchun shahar har doim bir xil va tartibli.
    /// </summary>
    public static class CityGenerator
    {
        private const int W0 = 14, W1 = 49; // devor halqasi

        public static void Generate(MapManager map, BuildingManager buildings, UnitManager units, int seed)
        {
            var rng = new Random(seed);
            PaintTerrain(map);
            PlaceFortifications(buildings);
            PlaceBuildings(buildings);
            PlaceUnits(map, units);
            PlaceDecorations(map);
            ScatterNature(map, rng);
        }

        private static bool InsideWalls(int x, int y) => x > W0 && x < W1 && y > W0 && y < W1;

        // ------------------------------------------------------------------
        // 1. Yer: tuproq, yashil hududlar, suv, ko'chalar, maydonlar
        // ------------------------------------------------------------------
        private static float Noise(float x, float y)
        {
            int xi = (int)MathF.Floor(x), yi = (int)MathF.Floor(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            float a = Canvas.Hash(xi, yi), b = Canvas.Hash(xi + 1, yi), c = Canvas.Hash(xi, yi + 1), d = Canvas.Hash(xi + 1, yi + 1);
            return MathHelper.Lerp(MathHelper.Lerp(a, b, fx), MathHelper.Lerp(c, d, fx), fy);
        }

        private static void PaintTerrain(MapManager map)
        {
            // Shahar tashqarisi: dala va o'tloqlar
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                {
                    bool nearCity = x >= 10 && x <= 53 && y >= 10 && y <= 53;
                    float n = Noise(x / 6f + 3.7f, y / 6f + 9.2f) * 0.7f + Noise(x / 2.5f, y / 2.5f) * 0.3f;
                    map.SetTile(x, y, !nearCity && n > 0.44f ? TileType.Grass : TileType.Ground);
                }

            // Shahar ichidagi yashil hududlar
            map.FillRect(43, 15, 48, 20, TileType.Grass);  // shimoli-sharqiy bog' mahallasi
            map.FillRect(33, 43, 41, 48, TileType.Grass);  // janubiy bog'
            map.FillRect(33, 38, 37, 41, TileType.Grass);  // choyxona
            map.FillRect(15, 47, 20, 48, TileType.Grass);
            map.FillRect(15, 27, 25, 27, TileType.Grass);  // ariq bo'ylari
            map.FillRect(15, 29, 25, 29, TileType.Grass);
            map.FillRect(38, 27, 48, 27, TileType.Grass);
            map.FillRect(38, 29, 48, 29, TileType.Grass);

            // Soy (janubda, egri-bugri)
            Vector2[] river = { new Vector2(-1, 53), new Vector2(8, 56), new Vector2(20, 57.5f), new Vector2(31, 56.5f),
                                new Vector2(44, 58.5f), new Vector2(55, 56.5f), new Vector2(64, 58) };
            for (int i = 0; i + 1 < river.Length; i++)
            {
                Vector2 a = river[i], b = river[i + 1];
                int steps = (int)(Vector2.Distance(a, b) * 3);
                for (int s = 0; s <= steps; s++)
                {
                    Vector2 p = Vector2.Lerp(a, b, s / (float)steps);
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                            if (new Vector2(dx, dy).Length() <= 1.2f)
                                map.SetTile((int)MathF.Round(p.X) + dx, (int)MathF.Round(p.Y) + dy, TileType.Water);
                }
            }
            // Ariqlar: shimoldan keladi, shahar ichidan o'tib soyga quyiladi
            map.FillRect(8, 0, 8, 56, TileType.Water);
            map.FillRect(55, 0, 55, 57, TileType.Water);
            map.FillRect(8, 28, 25, 28, TileType.Water);
            map.FillRect(38, 28, 55, 28, TileType.Water);

            // --- Ko'chalar (suvdan keyin: suv ustidan o'tgan joy ko'prik bo'ladi) ---
            map.FillRect(0, 31, 63, 32, TileType.Road);   // sharq-g'arb bosh ko'chasi
            map.FillRect(31, 0, 32, 63, TileType.Road);   // shimol-janub bosh ko'chasi
            // Saroy atrofidagi halqa
            map.FillRect(27, 18, 36, 19, TileType.Road);
            map.FillRect(27, 18, 28, 25, TileType.Road);
            map.FillRect(35, 18, 36, 25, TileType.Road);
            // Mahalla ko'chalari
            map.FillRect(21, 15, 21, 48, TileType.Road);
            map.FillRect(42, 15, 42, 30, TileType.Road);
            map.FillRect(42, 42, 42, 48, TileType.Road);
            map.FillRect(15, 21, 26, 21, TileType.Road);
            map.FillRect(37, 21, 48, 21, TileType.Road);
            map.FillRect(15, 42, 48, 42, TileType.Road);
            // Qal'a atrofidagi aylanma yo'l
            map.FillRect(11, 11, 52, 11, TileType.Road);
            map.FillRect(11, 52, 52, 52, TileType.Road);
            map.FillRect(11, 11, 11, 52, TileType.Road);
            map.FillRect(52, 11, 52, 52, TileType.Road);

            // --- Maydonlar ---
            map.FillRect(26, 26, 37, 37, TileType.Plaza); // markaziy maydon
            map.FillRect(38, 33, 47, 41, TileType.Plaza); // bozor maydoni
            map.FillRect(29, 20, 34, 25, TileType.Plaza); // saroy ostidagi to'shama
        }

        // ------------------------------------------------------------------
        // 2. Qal'a devori, darvozalar, minoralar
        // ------------------------------------------------------------------
        private static void PlaceFortifications(BuildingManager bm)
        {
            void Tower(int x, int y) => bm.Add(new Tower(new Point(x, y)), false);
            void Gate(int x, int y, bool alongY) => bm.Add(new Gate(new Point(x, y), alongY), false);

            // Burchak minoralari
            Tower(W0 - 1, W0 - 1); Tower(W1, W0 - 1); Tower(W0 - 1, W1); Tower(W1, W1);
            // Darvozalar: qo'sh ravoq + ikki yonida minora
            Gate(31, W0, false); Gate(32, W0, false); Tower(29, W0 - 1); Tower(33, W0 - 1); // shimoliy
            Gate(31, W1, false); Gate(32, W1, false); Tower(29, W1); Tower(33, W1);         // janubiy (bosh darvoza)
            Gate(W0, 31, true); Gate(W0, 32, true); Tower(W0 - 1, 29); Tower(W0 - 1, 33);   // g'arbiy
            Gate(W1, 31, true); Gate(W1, 32, true); Tower(W1, 29); Tower(W1, 33);           // sharqiy

            // Devor bo'laklari (band bo'lmagan joylarga)
            for (int i = W0; i <= W1; i++)
            {
                bm.Add(new Wall(new Point(i, W0)), false);
                bm.Add(new Wall(new Point(i, W1)), false);
                bm.Add(new Wall(new Point(W0, i)), false);
                bm.Add(new Wall(new Point(W1, i)), false);
            }
        }

        // ------------------------------------------------------------------
        // 3. Binolar
        // ------------------------------------------------------------------
        private static void PlaceBuildings(BuildingManager bm)
        {
            int houseIndex = 0;
            void House(int x, int y)
            {
                var h = new House(new Point(x, y), houseIndex);
                h.Flipped = houseIndex % 3 == 2;
                houseIndex++;
                bm.Add(h, false);
            }

            // Markaz
            bm.Add(new MainHall(new Point(29, 20)), false);

            // Katta sharqona binolar
            bm.Add(new Madrasa(new Point(16, 23)), false);
            bm.Add(new Mosque(new Point(44, 23)), false);
            bm.Add(new Caravanserai(new Point(16, 35)), false);
            bm.Add(new Market(new Point(43, 35)), false);
            bm.Add(new Barracks(new Point(16, 16)), false);
            bm.Add(new Garden(new Point(44, 16)), false);
            bm.Add(new Garden(new Point(34, 44)), false);

            // Mahallalar (uylar orasida doim kamida 1 tile ochiq joy)
            House(22, 16); House(25, 16); House(24, 19);        // shimoli-g'arb
            House(38, 16); House(40, 19);                       // shimoli-sharq
            House(23, 23); House(23, 26);                       // madrasa yonida
            House(39, 23); House(38, 26);                       // masjid yonida
            House(23, 34); House(23, 38); House(27, 39);        // karvonsaroy mahallasi
            House(16, 44); House(19, 44);                       // janubi-g'arb
            House(23, 44); House(27, 45);
            House(39, 44);
            House(44, 44); House(47, 46);                       // janubi-sharq

            // Devor tashqarisidagi rabot (shahar atrofi) uylari
            House(26, 53); House(36, 53); House(54, 35); House(56, 24); House(5, 34); House(35, 6); House(24, 7);
        }

        // ------------------------------------------------------------------
        // 4. Jangchilar
        // ------------------------------------------------------------------
        private static void PlaceUnits(MapManager map, UnitManager um)
        {
            Vector2 V(float x, float y) => new Vector2(x, y);
            void Guard(Unit u)
            {
                um.Add(u);
                map.Reserve((int)u.Position.X, (int)u.Position.Y);
            }

            // Bosh (janubiy) darvoza oldida ikki nayzali soqchi
            Guard(new SpearmanUnit(V(30.5f, 51.5f), Direction.South));
            Guard(new SpearmanUnit(V(33.5f, 51.5f), Direction.South));
            // Saroy peshtoqi oldida ikki qalqonli jangchi
            Guard(new GuardUnit(V(30.5f, 26.5f), Direction.South));
            Guard(new GuardUnit(V(33.5f, 26.5f), Direction.South));
            // Minoralar va darvozalar yonida
            Guard(new SpearmanUnit(V(15.5f, 15.5f), Direction.East));   // shimoli-g'arbiy minora
            Guard(new SpearmanUnit(V(48.5f, 48.5f), Direction.West));   // janubi-sharqiy minora
            Guard(new GuardUnit(V(47.5f, 30.5f), Direction.West));      // sharqiy darvoza ichida
            Guard(new GuardUnit(V(15.5f, 33.5f), Direction.East));      // g'arbiy darvoza ichida
            Guard(new SpearmanUnit(V(30.5f, 12.5f), Direction.North));  // shimoliy darvoza tashqarisida
            Guard(new GuardUnit(V(17.5f, 19.5f), Direction.South));     // qo'shinxona eshigi

            // Markaziy maydonni aylanib yuradigan qilichli soqchi
            um.Add(new SwordsmanUnit(new[] { V(27.5f, 27.5f), V(36.5f, 27.5f), V(36.5f, 36.5f), V(27.5f, 36.5f) }));
            // Bozor rastalari orasida
            um.Add(new SwordsmanUnit(new[] { V(40.5f, 33.5f), V(40.5f, 41.5f), V(42.5f, 41.5f), V(42.5f, 33.5f) }, 0.9f) { PauseTime = 2.5f });
            // Bosh ko'chalarda patrul
            um.Add(new PatrolUnit(new[] { V(15.6f, 32f), V(25.4f, 32f) }));
            um.Add(new PatrolUnit(new[] { V(32f, 47.6f), V(32f, 38.5f) }, 1.0f));
            um.Add(new PatrolUnit(new[] { V(32f, 15.6f), V(32f, 18.5f), V(28f, 19f), V(28f, 25.5f), V(28f, 19f), V(32f, 18.5f) }, 1.0f));
            // Qal'a atrofini aylanadigan tashqi patrul
            um.Add(new PatrolUnit(new[] { V(11.5f, 11.5f), V(52.5f, 11.5f), V(52.5f, 52.5f), V(11.5f, 52.5f) }, 1.3f) { PauseTime = 3f });
        }

        // ------------------------------------------------------------------
        // 5. Qo'lda qo'yilgan bezaklar: maydon, bozor, choyxona, qorovul muhiti
        // ------------------------------------------------------------------
        private static void PlaceDecorations(MapManager map)
        {
            void D(DecorationType t, int x, int y, int v = 0) => map.AddDecoration(t, x, y, v);

            // Markaziy maydon: hovuz, chinorlar, bayroqlar
            D(DecorationType.Hovuz, 31, 31);
            D(DecorationType.TreeChinor, 29, 29); D(DecorationType.TreeChinor, 34, 29);
            D(DecorationType.TreeChinor, 29, 34); D(DecorationType.TreeChinor, 34, 34);
            D(DecorationType.Banner, 26, 26); D(DecorationType.Banner, 37, 26);
            D(DecorationType.Banner, 26, 37); D(DecorationType.Banner, 37, 37);
            D(DecorationType.Brazier, 29, 26); D(DecorationType.Brazier, 34, 26);
            D(DecorationType.Sori, 28, 35); D(DecorationType.Sori, 35, 35);
            D(DecorationType.Flowers, 30, 30); D(DecorationType.Flowers, 33, 30);
            D(DecorationType.Flowers, 30, 33); D(DecorationType.Flowers, 33, 33);

            // Bozor: rastalar qatori, yuklar
            int v = 0;
            foreach (int y in new[] { 34, 36, 38, 40 })
            {
                D(DecorationType.Stall, 39, y, v++);
                D(DecorationType.Stall, 41, y, v++);
            }
            foreach (int x in new[] { 43, 45, 47 }) D(DecorationType.Stall, x, 40, v++);
            D(DecorationType.Stall, 44, 33, v++); D(DecorationType.Stall, 46, 33, v++);
            D(DecorationType.Crates, 47, 35); D(DecorationType.Crates, 47, 38); D(DecorationType.Crates, 38, 41);
            D(DecorationType.Well, 38, 33);

            // Choyxona (maydonning janubi-sharqida)
            D(DecorationType.Sori, 34, 39); D(DecorationType.Sori, 36, 39); D(DecorationType.Sori, 34, 41);
            D(DecorationType.Tandir, 36, 41);
            D(DecorationType.TreeChinor, 35, 40); D(DecorationType.TreeApricot, 33, 40); D(DecorationType.TreeChinor, 37, 38);

            // Qo'shinxona mashq maydoni
            D(DecorationType.Dummy, 20, 16); D(DecorationType.Dummy, 20, 18); D(DecorationType.Dummy, 19, 20);
            D(DecorationType.WeaponRack, 16, 20); D(DecorationType.Banner, 15, 19); D(DecorationType.Crates, 19, 15);

            // Darvoza va minoralar atrofida qorovul muhiti
            D(DecorationType.Brazier, 28, 51); D(DecorationType.Brazier, 35, 51);   // janubiy darvoza
            D(DecorationType.Banner, 29, 51); D(DecorationType.Banner, 34, 51);
            D(DecorationType.Brazier, 28, 12); D(DecorationType.Brazier, 35, 12);   // shimoliy
            D(DecorationType.Brazier, 12, 30); D(DecorationType.Brazier, 12, 33);   // g'arbiy
            D(DecorationType.Brazier, 51, 30); D(DecorationType.Brazier, 51, 33);   // sharqiy
            D(DecorationType.Brazier, 30, 48); D(DecorationType.Brazier, 33, 48);   // darvoza ichkarisi
            D(DecorationType.WeaponRack, 47, 47); D(DecorationType.Brazier, 16, 15);
            D(DecorationType.Crates, 15, 34); D(DecorationType.WeaponRack, 48, 33);

            // Mahalla tafsilotlari: quduq, tandir, so'ri
            D(DecorationType.Well, 26, 47); D(DecorationType.Well, 18, 40); D(DecorationType.Well, 40, 17);
            D(DecorationType.Tandir, 41, 16); D(DecorationType.Tandir, 25, 36); D(DecorationType.Tandir, 18, 47);
            D(DecorationType.Tandir, 46, 44); D(DecorationType.Sori, 44, 20); D(DecorationType.Sori, 47, 19);
            D(DecorationType.Crates, 20, 37); D(DecorationType.Crates, 15, 39);
            D(DecorationType.Well, 28, 54); D(DecorationType.Tandir, 38, 55);

            // Saroy yonlarida archalar
            foreach (int y in new[] { 20, 22, 24 })
            {
                D(DecorationType.TreeCypress, 26, y);
                D(DecorationType.TreeCypress, 37, y);
            }
        }

        // ------------------------------------------------------------------
        // 6. Tabiat: xiyobon teraklari, ariq bo'yi, dalalar
        // ------------------------------------------------------------------
        private static void ScatterNature(MapManager map, Random rng)
        {
            bool Free(int x, int y) => map.InBounds(x, y) && !map.IsReserved(x, y) && map.CanPlaceDecoration(new Point(x, y), 1)
                                       && map.GetTile(x, y) != TileType.Plaza;
            bool NearWater(int x, int y)
            {
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        if (map.InBounds(x + dx, y + dy) && map.GetTile(x + dx, y + dy) == TileType.Water) return true;
                return false;
            }
            bool NextToBuilding(int x, int y)
            {
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        if (map.GetOccupant(x + dx, y + dy) is Building) return true;
                return false;
            }

            // Bosh ko'chalar bo'ylab teraklar xiyoboni
            for (int i = 0; i < map.Width; i += 2)
            {
                bool inner = i > W0 + 1 && i < W1 - 1;
                var kind = inner ? DecorationType.TreePoplar : (i % 4 == 0 ? DecorationType.TreePoplar : DecorationType.TreeChinor);
                foreach (var (x, y) in new[] { (30, i), (33, i), (i, 30), (i, 33) })
                    if (Free(x, y) && !NextToBuilding(x, y)) map.AddDecoration(kind, x, y);
            }

            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                {
                    if (!Free(x, y)) continue;
                    TileType t = map.GetTile(x, y);
                    double r = rng.NextDouble();
                    bool inside = InsideWalls(x, y);
                    bool nearWall = x >= W0 - 3 && x <= W1 + 3 && y >= W0 - 3 && y <= W1 + 3 && !inside;

                    if (NearWater(x, y))
                    {
                        // Ariq va soy bo'yida teraklar, tollar
                        if (r < 0.34) map.AddDecoration(DecorationType.TreePoplar, x, y);
                        else if (r < 0.46) map.AddDecoration(DecorationType.Bush, x, y);
                        else if (r < 0.52) map.AddDecoration(DecorationType.Flowers, x, y);
                    }
                    else if (inside)
                    {
                        if (NextToBuilding(x, y))
                        {
                            if (r < 0.05) map.AddDecoration(DecorationType.Bush, x, y);
                            continue;
                        }
                        if (t == TileType.Grass)
                        {
                            if (r < 0.26) map.AddDecoration(r < 0.13 ? DecorationType.TreeApricot : DecorationType.TreeChinor, x, y);
                            else if (r < 0.38) map.AddDecoration(DecorationType.Flowers, x, y);
                            else if (r < 0.46) map.AddDecoration(DecorationType.Bush, x, y);
                        }
                        else
                        {
                            if (r < 0.07) map.AddDecoration(r < 0.035 ? DecorationType.TreeApricot : DecorationType.TreeChinor, x, y);
                            else if (r < 0.11) map.AddDecoration(DecorationType.Bush, x, y);
                        }
                    }
                    else if (nearWall)
                    {
                        // Devor oldi ochiq qoladi (mudofaa uchun), faqat siyrak butalar
                        if (r < 0.03) map.AddDecoration(DecorationType.Bush, x, y);
                    }
                    else if (t == TileType.Grass)
                    {
                        if (r < 0.13) map.AddDecoration(r < 0.05 ? DecorationType.TreeApricot : (r < 0.09 ? DecorationType.TreeChinor : DecorationType.TreePoplar), x, y);
                        else if (r < 0.20) map.AddDecoration(DecorationType.Bush, x, y);
                        else if (r < 0.25) map.AddDecoration(DecorationType.Flowers, x, y);
                    }
                    else
                    {
                        if (r < 0.02) map.AddDecoration(DecorationType.TreeChinor, x, y);
                        else if (r < 0.05) map.AddDecoration(DecorationType.Bush, x, y);
                    }
                }
        }
    }
}
