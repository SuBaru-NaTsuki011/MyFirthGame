using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AndijonSaltanatlar.Server.Data;

namespace AndijonSaltanatlar.Server.Services
{
    /// <summary>
    /// Dunyoning boshlang'ich siyosiy xaritasi (PROMPT 19B). Faqat bir marta — dunyoda hudud bo'lmasa — yaratiladi.
    /// Xarita koordinatasi (0..100) RegionEntity.Latitude (X) va Longitude (Y) maydonlarida saqlanadi.
    /// Aholi SHAHARLARDA: hudud aholisi = uning shaharlari aholisi yig'indisi.
    /// </summary>
    public static class WorldSeeder
    {
        private sealed record SeedCity(string Region, float X, float Y, string City, int Population, int Housing, int Jobs, int Development);

        private static readonly SeedCity[] AndijonKhanate =
        {
            new("Andijon", 60, 40, "Andijon", 520, 600, 380, 55),
            new("Shahrixon", 45, 62, "Shahrixon", 340, 380, 240, 40),
            new("Marhamat", 74, 64, "Marhamat", 180, 220, 120, 30),
        };

        private static readonly SeedCity[] FerganaBeylik =
        {
            new("Marg'ilon", 22, 58, "Marg'ilon", 410, 460, 300, 50),
            new("Quva", 32, 40, "Quva", 260, 300, 170, 35),
        };

        private static readonly SeedCity[] Neutral =
        {
            new("Paxtaobod", 66, 20, "Paxtaobod", 120, 150, 70, 20),
            new("Bo'z", 30, 22, "Bo'z", 90, 120, 50, 18),
        };

        /// <summary>O'yinchilar boshlanadigan hududlar (navbat bilan beriladi).</summary>
        private static readonly (string Name, float X, float Y)[] PlayerStarts =
        {
            ("Asaka", 55, 52), ("Xo'jaobod", 84, 50), ("Qo'rg'ontepa", 86, 30), ("Baliqchi", 44, 14), ("Izboskan", 52, 26), ("Oltinko'l", 40, 46)
        };

        private static readonly (int R, int G, int B)[] PlayerColors =
        {
            (50, 107, 150), (60, 150, 110), (200, 140, 50), (140, 80, 160), (40, 150, 170), (170, 70, 90)
        };

        public static (int, int, int) PlayerColor(int index) => PlayerColors[Math.Abs(index) % PlayerColors.Length];

        /// <summary>
        /// Dunyoni to'ldirish va o'yinchi hududini berish ketma-ket bajariladi: ikki o'yinchi bir vaqtda yangi dunyoga
        /// kirsa ham AI saltanatlar ikki marta yaratilmaydi va ikkalasiga bitta boshlang'ich hudud berilmaydi.
        /// (Bitta server jarayoni uchun. Bir nechta server nusxasida — DB darajasidagi qulf kerak.)
        /// </summary>
        private static readonly System.Threading.SemaphoreSlim Gate = new System.Threading.SemaphoreSlim(1, 1);

        public static async Task SeedAsync(GameDbContext db, World w)
        {
            await Gate.WaitAsync();
            try { await SeedCoreAsync(db, w); }
            finally { Gate.Release(); }
        }

        private static async Task SeedCoreAsync(GameDbContext db, World w)
        {
            if (await db.Regions.AnyAsync(r => r.WorldId == w.Id)) return;
            Kingdom andijon = await AiKingdom(db, w.Id, "Andijon xonligi", "Umarbek", 190, 80, 60, "balanced");
            Kingdom fergana = await AiKingdom(db, w.Id, "Farg'ona begligi", "Sulaymonbek", 120, 90, 170, "trade");
            await AddCities(db, w.Id, andijon.Id, AndijonKhanate);
            await AddCities(db, w.Id, fergana.Id, FerganaBeylik);
            await AddCities(db, w.Id, 0, Neutral);
            // AI omborlari: bozorda haqiqiy mol bilan qatnashadi (cheksiz emas).
            db.Inventory.Add(new InventoryEntity { KingdomId = andijon.Id, Product = "Wheat", Quantity = 600 });
            db.Inventory.Add(new InventoryEntity { KingdomId = andijon.Id, Product = "Cotton", Quantity = 300 });
            db.Inventory.Add(new InventoryEntity { KingdomId = fergana.Id, Product = "Cotton", Quantity = 500 });
            db.Inventory.Add(new InventoryEntity { KingdomId = fergana.Id, Product = "Fruit", Quantity = 400 });
            await db.SaveChangesAsync();
        }

        private static async Task<Kingdom> AiKingdom(GameDbContext db, int worldId, string name, string ruler, int r, int g, int b, string strategy)
        {
            var k = new Kingdom { WorldId = worldId, Name = name, RulerName = ruler, IsAi = true, ColorR = r, ColorG = g, ColorB = b, AiStrategy = strategy };
            db.Kingdoms.Add(k);
            await db.SaveChangesAsync();
            db.KingdomResources.Add(new KingdomResource { KingdomId = k.Id, Coin = 3000, Food = 800 });
            await db.SaveChangesAsync();
            return k;
        }

        private static async Task AddCities(GameDbContext db, int worldId, int ownerId, SeedCity[] seeds)
        {
            bool first = true;
            foreach (SeedCity s in seeds)
            {
                var region = new RegionEntity { WorldId = worldId, Name = s.Region, OwnerKingdomId = ownerId, Latitude = s.X, Longitude = s.Y, Development = s.Development, Stability = 60 };
                db.Regions.Add(region);
                await db.SaveChangesAsync();
                db.Cities.Add(NewCity(ownerId, region.Id, s.City, s.Population, s.Housing, s.Jobs, first && ownerId != 0));
                first = false;
            }
            await db.SaveChangesAsync();
        }

        private static CityEntity NewCity(int kingdomId, int regionId, string name, int population, int housing, int jobs, bool capital) => new CityEntity
        {
            KingdomId = kingdomId, RegionId = regionId, Name = name, Population = population,
            Households = (int)Math.Ceiling(population / 4.5), HousingCapacity = housing, Jobs = jobs, IsCapital = capital,
            Level = 1 + population / 200, Happiness = 65, Trust = 60,
            GovernorName = "Hokim " + name.Split(' ')[0]
        };

        /// <summary>O'yinchining o'z hududi va poytaxti (bo'lmasa yaratiladi).</summary>
        public static async Task EnsurePlayerHomeAsync(GameDbContext db, Kingdom k)
        {
            await Gate.WaitAsync();
            try { await EnsurePlayerHomeCoreAsync(db, k); }
            finally { Gate.Release(); }
        }

        private static async Task EnsurePlayerHomeCoreAsync(GameDbContext db, Kingdom k)
        {
            if (await db.Cities.AnyAsync(c => c.KingdomId == k.Id)) return;
            var taken = await db.Regions.Where(r => r.WorldId == k.WorldId).Select(r => r.Name).ToListAsync();
            var start = PlayerStarts.FirstOrDefault(p => !taken.Contains(p.Name));
            string name = start.Name ?? ("Yangi yurt " + k.Id);
            float x = start.Name != null ? start.X : 10 + (k.Id * 13 % 80);
            float y = start.Name != null ? start.Y : 10 + (k.Id * 29 % 80);
            var region = new RegionEntity { WorldId = k.WorldId, Name = name, OwnerKingdomId = k.Id, Latitude = x, Longitude = y, Development = 30, Stability = 70 };
            db.Regions.Add(region);
            await db.SaveChangesAsync();
            db.Cities.Add(NewCity(k.Id, region.Id, name, 42, 60, 30, true));
            await db.SaveChangesAsync();
        }

        public static async Task<CityEntity> CapitalOfAsync(GameDbContext db, int kingdomId)
            => await db.Cities.AsNoTracking().Where(c => c.KingdomId == kingdomId).OrderByDescending(c => c.IsCapital).ThenBy(c => c.Id).FirstOrDefaultAsync();

        public static async Task<double> DistanceAsync(GameDbContext db, CityEntity a, CityEntity b)
        {
            if (a == null || b == null) return 0;
            var ra = await db.Regions.AsNoTracking().FirstOrDefaultAsync(r => r.Id == a.RegionId);
            var rb = await db.Regions.AsNoTracking().FirstOrDefaultAsync(r => r.Id == b.RegionId);
            if (ra == null || rb == null) return 0;
            double dx = ra.Latitude - rb.Latitude, dy = ra.Longitude - rb.Longitude;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
