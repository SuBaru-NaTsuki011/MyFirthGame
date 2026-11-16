using System;
using System.Collections.Generic;
using System.Linq;
using AndijonSaltanatlar.Server.Data;
using AndijonSaltanatlar.Shared;
using AndijonSaltanatlar.Shared.Commands;
using AndijonSaltanatlar.Shared.Planning;

namespace AndijonSaltanatlar.Server.Services
{
    /// <summary>
    /// Server-authoritative bino qurish. Narx SERVERда belgilanadi — client yuborgan narxga ishonilmaydi.
    /// Resurs, egalik va tile bandligini tekshiradi.
    /// </summary>
    public class BuildingService
    {
        public CommandResult PlaceBuilding(GameDbContext db, Kingdom kingdom, GameCommand cmd)
        {
            if (!cmd.Payload.TryGetValue("x", out string sx) || !cmd.Payload.TryGetValue("y", out string sy) ||
                !cmd.Payload.TryGetValue("kind", out string sk))
                return CommandResult.Fail("Buyruq ma'lumotlari to'liq emas.");
            if (!int.TryParse(sx, out int x) || !int.TryParse(sy, out int y) || !Enum.TryParse(sk, true, out BuildingKind kind))
                return CommandResult.Fail("Buyruq ma'lumotlari noto'g'ri.");

            int cost = AiPlanRules.CostOf(kind);
            int size = AiPlanRules.SizeOf(kind);
            if (cost < 0) return CommandResult.Fail("Bino turi noma'lum.");
            if (x < 0 || y < 0 || x + size > 64 || y + size > 64)
                return CommandResult.Fail("Bino xarita tashqarisida.");

            string tech = AiPlanRules.TechOf(kind);
            if (!string.IsNullOrEmpty(tech) &&
                !db.ResearchProgress.Any(r => r.KingdomId == kingdom.Id && r.TechId == tech && r.Done))
                return CommandResult.Fail("Texnologiya hali ochilmagan: " + tech + ".");

            KingdomResource res = db.KingdomResources.FirstOrDefault(r => r.KingdomId == kingdom.Id);
            if (res == null) return CommandResult.Fail("Saltanat resurslari topilmadi.");
            if (res.Coin < cost) return CommandResult.Fail("Tanga yetarli emas (kerak: " + cost + ").");

            CityEntity city = db.Cities.FirstOrDefault(ct => ct.KingdomId == kingdom.Id);
            if (city == null) return CommandResult.Fail("Shahar topilmadi.");

            foreach (BuildingEntity existing in db.Buildings.Where(b => b.CityId == city.Id).ToList())
            {
                int es = AiPlanRules.SizeOf((BuildingKind)existing.Kind);
                if (x < existing.X + es && x + size > existing.X &&
                    y < existing.Y + es && y + size > existing.Y)
                    return CommandResult.Fail("Bu joy boshqa bino bilan ustma-ust tushadi.");
            }

            res.Coin -= cost;
            db.Buildings.Add(new BuildingEntity { KingdomId = kingdom.Id, CityId = city.Id, Kind = (int)kind, X = x, Y = y, Level = 1, BuildProgress = 0f });
            kingdom.StateVersion++;
            return CommandResult.Ok(kingdom.StateVersion, kind + " qurilishi boshlandi.");
        }
    }
}
