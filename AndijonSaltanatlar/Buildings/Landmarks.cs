using AndijonSaltanatlar.Core;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Buildings
{
    // Kattaroq sharqona binolar — boshlang'ich shaharning "diqqatga sazovor" joylari.

    /// <summary>Madrasa — ichki hovlili, peshtoqli ilm maskani.</summary>
    public sealed class Madrasa : Building
    {
        public Madrasa(Point position) : base(position)
        {
            Type = BuildingType.Madrasa;
            Name = "Madrasa";
            Description = "Ilm maskani. Peshtoqi koshin bilan bezatilgan, hujralarida tolibi ilmlar yashaydi.";
            Size = new Point(4, 4);
            Category = BuildingCategory.Culture;
            Cost = 600;
            PopulationCapacity = 10;
            SetHitPoints(1200);
            SpriteKey = SpriteKeys.Madrasa;
        }
    }

    /// <summary>Jome masjidi — ko'k gumbaz va baland minora.</summary>
    public sealed class Mosque : Building
    {
        public Mosque(Point position) : base(position)
        {
            Type = BuildingType.Mosque;
            Name = "Jome masjidi";
            Description = "Ko‘k gumbazli jome masjidi. Minorasi shaharning eng baland nuqtasi.";
            Size = new Point(4, 4);
            Category = BuildingCategory.Culture;
            Cost = 700;
            SetHitPoints(1300);
            SpriteKey = SpriteKeys.Mosque;
        }
    }

    /// <summary>Karvonsaroy — Buyuk ipak yo'li savdogarlari to'xtaydigan qo'rg'on.</summary>
    public sealed class Caravanserai : Building
    {
        public Caravanserai(Point position) : base(position)
        {
            Type = BuildingType.Caravanserai;
            Name = "Karvonsaroy";
            Description = "Ipak yo‘li karvonlari qo‘nadigan qo‘rg‘on. Savdoni jonlantiradi.";
            Size = new Point(4, 4);
            Category = BuildingCategory.Economy;
            Cost = 500;
            PopulationCapacity = 8;
            SetHitPoints(1100);
            SpriteKey = SpriteKeys.Caravanserai;
        }
    }
}
