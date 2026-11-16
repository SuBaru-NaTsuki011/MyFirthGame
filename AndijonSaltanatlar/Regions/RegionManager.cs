using System.Collections.Generic;
using AndijonSaltanatlar.Geography;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Regions
{
    /// <summary>
    /// Barcha hududlarni saqlaydi. Xaritadagi joylashuv (X,Y) haqiqiy geografik koordinatalardan
    /// (GeographyDatabase + CoordinateConverter) hisoblanadi — Andijon viloyati shaharlari.
    /// Siyosiy chegaralar muqobil tarix; geografiya esa ochiq ma'lumotlarga asoslangan (taxminiy).
    /// </summary>
    public sealed class RegionManager
    {
        public List<Region> Regions { get; } = new List<Region>();

        public RegionManager()
        {
            // Id, Nom, rivojlanish, aholi — koordinata GeographyDatabase'dan olinadi
            Add(1, "Asaka", 55, 42);
            Add(2, "Andijon", 60, 70);
            Add(3, "Shahrixon", 45, 50);
            Add(4, "Xo'jaobod", 35, 38);
            Add(5, "Marhamat", 32, 34);
            Add(6, "Qo'rg'ontepa", 30, 30);
            Add(7, "Paxtaobod", 34, 36);
            Add(8, "Izboskan", 33, 35);
            Add(9, "Baliqchi", 31, 33);
            Add(10, "Bo'ston", 30, 30);
        }

        private void Add(int id, string name, int dev, int pop)
        {
            var r = new Region
            {
                Id = id, Name = name,
                Development = dev, Population = pop,
                Food = pop, Production = dev, Trade = dev / 4,
                Security = 40, Happiness = 60, Governance = 50
            };

            GeographicPoint g = GeographyDatabase.Get(name);
            if (g != null)
            {
                r.Latitude = g.Latitude; r.Longitude = g.Longitude;
                r.Source = g.Source; r.GeographicId = g.GeographicId;
                Vector2 n = CoordinateConverter.ToNormalized(g.Latitude, g.Longitude);
                r.X = MathHelper.Clamp(0.08f + n.X * 0.84f, 0.04f, 0.96f);
                r.Y = MathHelper.Clamp(0.08f + n.Y * 0.84f, 0.04f, 0.96f);
            }
            else { r.X = 0.5f; r.Y = 0.5f; }

            Regions.Add(r);
        }

        public Region Get(int id)
        {
            foreach (Region r in Regions) if (r.Id == id) return r;
            return null;
        }

        public IEnumerable<Region> OwnedBy(int kingdomId)
        {
            foreach (Region r in Regions) if (r.OwnerKingdomId == kingdomId) yield return r;
        }
    }
}
