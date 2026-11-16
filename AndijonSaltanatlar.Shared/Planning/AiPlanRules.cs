using System;
using System.Collections.Generic;

namespace AndijonSaltanatlar.Shared.Planning
{
    /// <summary>
    /// Server-authoritative AI reja qoidalari. Client yuborgan narx/o'lchamga ishonilmaydi:
    /// narx, bino o'lchami va texnologiya talabi serverdagi jadvaldan olinadi.
    /// </summary>
    public static class AiPlanRules
    {
        public const int RoadCost = 10;
        public const int MaxSteps = 120;

        private static readonly Dictionary<BuildingKind, int> Costs = new()
        {
            { BuildingKind.House, 50 }, { BuildingKind.Garden, 90 }, { BuildingKind.Market, 200 },
            { BuildingKind.Caravanserai, 500 }, { BuildingKind.Barracks, 300 }, { BuildingKind.Madrasa, 600 },
            { BuildingKind.Mosque, 700 }, { BuildingKind.Tower, 120 }, { BuildingKind.Gate, 80 },
            { BuildingKind.Wall, 15 }, { BuildingKind.MainHall, 1500 },
        };

        private static readonly Dictionary<BuildingKind, int> Sizes = new()
        {
            { BuildingKind.House, 2 }, { BuildingKind.Market, 4 }, { BuildingKind.Garden, 3 },
            { BuildingKind.Caravanserai, 4 }, { BuildingKind.Barracks, 3 }, { BuildingKind.Madrasa, 4 },
            { BuildingKind.Mosque, 4 }, { BuildingKind.Tower, 2 }, { BuildingKind.Gate, 4 },
            { BuildingKind.Wall, 1 }, { BuildingKind.MainHall, 4 },
        };

        private static readonly Dictionary<BuildingKind, string> RequiredTech = new()
        {
            { BuildingKind.Caravanserai, "karvon-savdosi" },
            { BuildingKind.Madrasa, "me'morchilik" },
            { BuildingKind.Mosque, "gumbaz-qurilishi" },
            { BuildingKind.MainHall, "shaharsozlik" },
        };

        public static int CostOf(BuildingKind kind) => Costs.TryGetValue(kind, out int c) ? c : -1;
        public static int SizeOf(BuildingKind kind) => Sizes.TryGetValue(kind, out int s) ? s : 1;
        public static string TechOf(BuildingKind kind) => RequiredTech.TryGetValue(kind, out string t) ? t : "";

        public static long TileKey(int x, int y) => ((long)x << 32) ^ (uint)y;

        public static AiPlanValidationResult Validate(AiPlanValidationRequest request, AiPlanValidationContext context)
        {
            var result = new AiPlanValidationResult();
            if (request == null) { result.ErrorsUz.Add("Reja yuborilmadi."); return result; }
            if (context == null) { result.ErrorsUz.Add("Server konteksti yo'q."); return result; }

            if (!context.KingdomOwnedByPlayer)
                result.ErrorsUz.Add("Saltanat sizga tegishli emas.");
            if (!context.CityOwnedByPlayer)
                result.ErrorsUz.Add("Shahar sizga tegishli emas.");
            if (!context.LandOwnedByPlayer)
                result.ErrorsUz.Add("Yer sizga tegishli emas yoki qulflangan.");

            if (request.Steps == null || request.Steps.Count == 0)
                result.ErrorsUz.Add("Reja bo'sh.");
            else if (request.Steps.Count > MaxSteps)
                result.ErrorsUz.Add("Reja juda katta: " + request.Steps.Count + " qadam (maksimum " + MaxSteps + ").");

            var occupied = new HashSet<long>();
            foreach (AiExistingBuildingDto b in context.ExistingBuildings ?? new List<AiExistingBuildingDto>())
            {
                int size = Math.Max(1, b.Size);
                for (int y = b.Y; y < b.Y + size; y++)
                    for (int x = b.X; x < b.X + size; x++)
                        occupied.Add(TileKey(x, y));
            }

            var plannedRoads = new HashSet<long>();
            int computedCost = 0;
            int stepIndex = 0;
            foreach (AiPlanStepDto step in request.Steps ?? new List<AiPlanStepDto>())
            {
                stepIndex++;
                if (step == null) { result.ErrorsUz.Add("Qadam " + stepIndex + ": bo'sh."); continue; }

                bool isRoad = string.Equals(step.Kind, "Road", StringComparison.OrdinalIgnoreCase) || step.Kind == "1";
                if (isRoad)
                {
                    long key = TileKey(step.X, step.Y);
                    if (!InBounds(context, step.X, step.Y, 1))
                    {
                        result.ErrorsUz.Add("Qadam " + stepIndex + ": yo'l xarita tashqarisida.");
                        continue;
                    }
                    if (occupied.Contains(key))
                    {
                        result.ErrorsUz.Add("Qadam " + stepIndex + ": yo'l bino bilan to'qnashadi.");
                        continue;
                    }
                    if ((context.BlockedRoadTiles != null && context.BlockedRoadTiles.Contains(key)) || !plannedRoads.Add(key))
                    {
                        result.ErrorsUz.Add("Qadam " + stepIndex + ": yo'l katagi mos emas yoki takror.");
                        continue;
                    }
                    computedCost += RoadCost;
                    result.ValidRoadSteps++;
                    continue;
                }

                if (!Enum.TryParse(step.Building, true, out BuildingKind kind) || CostOf(kind) < 0)
                {
                    result.ErrorsUz.Add("Qadam " + stepIndex + ": bino turi noma'lum.");
                    continue;
                }

                int size = SizeOf(kind);
                if (!InBounds(context, step.X, step.Y, size))
                {
                    result.ErrorsUz.Add("Qadam " + stepIndex + ": " + kind + " xarita tashqarisida.");
                    continue;
                }

                string tech = TechOf(kind);
                if (!string.IsNullOrEmpty(tech) &&
                    !(context.CompletedTechs != null && context.CompletedTechs.Contains(tech)))
                {
                    result.ErrorsUz.Add("Qadam " + stepIndex + ": " + kind + " texnologiyasi ochilmagan (" + tech + ").");
                    continue;
                }

                bool overlap = false, blocked = false;
                for (int y = step.Y; y < step.Y + size && !overlap && !blocked; y++)
                    for (int x = step.X; x < step.X + size; x++)
                    {
                        long key = TileKey(x, y);
                        if (occupied.Contains(key)) { overlap = true; break; }
                        if (context.BlockedBuildingTiles != null && context.BlockedBuildingTiles.Contains(key))
                        { blocked = true; break; }
                    }
                if (overlap)
                {
                    result.ErrorsUz.Add("Qadam " + stepIndex + ": " + kind + " boshqa bino bilan ustma-ust tushadi.");
                    continue;
                }
                if (blocked)
                {
                    result.ErrorsUz.Add("Qadam " + stepIndex + ": " + kind + " uchun yer ochilmagan yoki mos emas.");
                    continue;
                }

                for (int y = step.Y; y < step.Y + size; y++)
                    for (int x = step.X; x < step.X + size; x++)
                        occupied.Add(TileKey(x, y));

                computedCost += CostOf(kind);
                result.ValidBuildingSteps++;
            }

            result.ComputedCost = computedCost;
            if (request.ApprovedBudget <= 0)
                result.ErrorsUz.Add("Tasdiqlangan budjet ko'rsatilmagan.");
            else if (request.ApprovedBudget < computedCost)
                result.ErrorsUz.Add("Tasdiqlangan budjet reja narxidan kam: " + request.ApprovedBudget + " < " + computedCost + ".");

            result.Affordable = context.Coin >= computedCost;
            if (!result.Affordable && computedCost > 0)
                result.WarningsUz.Add("Xazina hozircha yetmaydi: " + context.Coin + "/" + computedCost +
                                      " tanga. Reja qabul qilinadi, qurilish pauzaga tushadi.");

            if (result.ValidBuildingSteps == 0 && result.ValidRoadSteps == 0 && result.ErrorsUz.Count == 0)
                result.ErrorsUz.Add("Rejada bajariladigan qadam yo'q.");

            result.Accepted = result.ErrorsUz.Count == 0;
            return result;
        }

        private static bool InBounds(AiPlanValidationContext ctx, int x, int y, int size)
            => x >= 0 && y >= 0 && x + size <= ctx.MapWidth && y + size <= ctx.MapHeight;
    }
}
