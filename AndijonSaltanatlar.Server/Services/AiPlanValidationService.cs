using System.Collections.Generic;
using System.Linq;
using AndijonSaltanatlar.Server.Data;
using AndijonSaltanatlar.Shared;
using AndijonSaltanatlar.Shared.Planning;

namespace AndijonSaltanatlar.Server.Services
{
    /// <summary>
    /// AI rejasini serverda tekshiradi — egalik, resurs, texnologiya, bandlik va budjet.
    /// Client yuborgan narx/o'lchamga ishonilmaydi; qoidalar Shared.AiPlanRules'da.
    /// </summary>
    public class AiPlanValidationService
    {
        public AiPlanValidationResult Validate(GameDbContext db, Kingdom kingdom, AiPlanValidationRequest request)
        {
            var context = new AiPlanValidationContext
            {
                KingdomOwnedByPlayer = kingdom.OwnerUserId != null,
                CityOwnedByPlayer = true,
                LandOwnedByPlayer = true,
                MapWidth = 64,
                MapHeight = 64
            };

            KingdomResource res = db.KingdomResources.FirstOrDefault(r => r.KingdomId == kingdom.Id);
            context.Coin = res?.Coin ?? 0;

            CityEntity city = db.Cities.FirstOrDefault(c => c.KingdomId == kingdom.Id);
            if (city == null)
            {
                context.CityOwnedByPlayer = false;
            }
            else
            {
                foreach (BuildingEntity b in db.Buildings.Where(b => b.CityId == city.Id).ToList())
                {
                    var kind = (BuildingKind)b.Kind;
                    context.ExistingBuildings.Add(new AiExistingBuildingDto
                    {
                        Kind = kind.ToString(), X = b.X, Y = b.Y,
                        Size = AiPlanRules.SizeOf(kind)
                    });
                }
            }

            foreach (ResearchProgressEntity tech in db.ResearchProgress
                         .Where(r => r.KingdomId == kingdom.Id && r.Done).ToList())
                context.CompletedTechs.Add(tech.TechId);

            return AiPlanRules.Validate(request, context);
        }
    }
}
