using System.Collections.Generic;

namespace AndijonSaltanatlar.Shared.Planning
{
    /// <summary>AI rejasidagi bitta qadam (bino yoki yo'l katagi).</summary>
    public sealed class AiPlanStepDto
    {
        /// <summary>"Building" yoki "Road".</summary>
        public string Kind { get; set; } = "Building";
        /// <summary>BuildingKind nomi (House, Market, ...). Yo'l uchun bo'sh.</summary>
        public string Building { get; set; } = "";
        public int X { get; set; }
        public int Y { get; set; }
        public int Size { get; set; } = 1;
    }

    /// <summary>Serverga yuboriladigan AI reja — faqat tekshirish uchun.</summary>
    public sealed class AiPlanValidationRequest
    {
        public string CommandId { get; set; } = "";
        public int KingdomId { get; set; }
        public int ApprovedBudget { get; set; }
        public List<AiPlanStepDto> Steps { get; set; } = new();
    }

    /// <summary>Serverdagi mavjud bino (bandlikni tekshirish uchun).</summary>
    public sealed class AiExistingBuildingDto
    {
        public string Kind { get; set; } = "";
        public int X { get; set; }
        public int Y { get; set; }
        public int Size { get; set; } = 1;
    }

    /// <summary>
    /// Server tomondagi tekshiruv konteksti. Egalik, resurs, texnologiya va bandlik
    /// ma'lumotlari DB'dan to'ldiriladi; sof qoidalar testda ham ishlatiladi.
    /// </summary>
    public sealed class AiPlanValidationContext
    {
        public bool KingdomOwnedByPlayer { get; set; } = true;
        public bool CityOwnedByPlayer { get; set; } = true;
        public bool LandOwnedByPlayer { get; set; } = true;
        public int Coin { get; set; }
        public int MapWidth { get; set; } = 64;
        public int MapHeight { get; set; } = 64;
        public HashSet<string> CompletedTechs { get; set; } = new();
        public List<AiExistingBuildingDto> ExistingBuildings { get; set; } = new();
        /// <summary>Qurilish mumkin bo'lmagan (suv, qulflangan, boshqa davlat) kataklar.</summary>
        public HashSet<long> BlockedBuildingTiles { get; set; } = new();
        public HashSet<long> BlockedRoadTiles { get; set; } = new();
        public HashSet<long> ExistingRoadTiles { get; set; } = new();
    }

    /// <summary>Server validatsiyasi natijasi.</summary>
    public sealed class AiPlanValidationResult
    {
        public bool Accepted { get; set; }
        public bool Affordable { get; set; }
        public int ComputedCost { get; set; }
        public int ValidBuildingSteps { get; set; }
        public int ValidRoadSteps { get; set; }
        public List<string> ErrorsUz { get; set; } = new();
        public List<string> WarningsUz { get; set; } = new();

        public string SummaryUz => Accepted
            ? $"Qabul qilindi: {ComputedCost} tanga, {ValidBuildingSteps} bino, {ValidRoadSteps} yo'l." +
              (Affordable ? "" : " Xazina hozircha yetmaydi — qurilish pauzaga tushadi.")
            : "Rad etildi: " + string.Join(" ", ErrorsUz);
    }
}
