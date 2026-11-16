using System.Collections.Generic;

namespace AndijonSaltanatlar.Shared.DTOs
{
    /// <summary>Boshqaruv rejimi (PROMPT 19B, 7-bo'lim).</summary>
    public enum GovernorMode { Manual = 0, Advisory = 1, ApprovedAutomation = 2 }

    public sealed class RegionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int OwnerKingdomId { get; set; }
        public string OwnerName { get; set; } = "";
        public int ColorR { get; set; }
        public int ColorG { get; set; }
        public int ColorB { get; set; }
        /// <summary>Xarita koordinatasi (0..100).</summary>
        public float MapX { get; set; }
        public float MapY { get; set; }
        public int Population { get; set; }
        public int Development { get; set; }
        public int Stability { get; set; }
        public List<string> Cities { get; set; } = new();
        public bool IsMine { get; set; }
        /// <summary>Joriy o'yinchi bilan egasi o'rtasidagi munosabat (-100..100).</summary>
        public int Relation { get; set; }
        public int AnnexCost { get; set; }
        public bool CanAnnex { get; set; }
        public string AnnexBlockReasonUz { get; set; } = "";
    }

    public sealed class CityDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int RegionId { get; set; }
        public string RegionName { get; set; } = "";
        public bool IsCapital { get; set; }
        public int Population { get; set; }
        public int Households { get; set; }
        public int HousingCapacity { get; set; }
        public int Jobs { get; set; }
        public int Level { get; set; }
        public int Happiness { get; set; }
        public int Trust { get; set; }
        public int DailyIncome { get; set; }
        public int DailyExpense { get; set; }
        public int Buildings { get; set; }
        public GovernorMode GovernorMode { get; set; }
        public string GovernorName { get; set; } = "";
        public int GovernorSpendLimit { get; set; }
        public List<string> Problems { get; set; } = new();
        public GovernorPlanDto PendingPlan { get; set; }
    }

    public sealed class GovernorPlanDto
    {
        public int Id { get; set; }
        public int CityId { get; set; }
        public string TitleUz { get; set; } = "";
        public string ReasonUz { get; set; } = "";
        public int Houses { get; set; }
        public int Markets { get; set; }
        public int Gardens { get; set; }
        public int Cost { get; set; }
        public string Status { get; set; } = "";
    }

    public sealed class KingdomOverviewDto
    {
        public int KingdomId { get; set; }
        public string Name { get; set; } = "";
        public string Ruler { get; set; } = "";
        public string Capital { get; set; } = "";
        public int Coin { get; set; }
        /// <summary>Saltanatga tegishli shaharlar aholisi yig'indisi (har bir fuqaro bir marta).</summary>
        public int Population { get; set; }
        public int CityCount { get; set; }
        public int RegionCount { get; set; }
        public int DevelopmentScore { get; set; }
        public List<CityDto> Cities { get; set; } = new();
        public List<RegionDto> Regions { get; set; } = new();
        public List<KingdomRelationDto> Relations { get; set; } = new();
        public List<string> AdvisorNotes { get; set; } = new();
        public Dictionary<string, int> Inventory { get; set; } = new();
    }

    public sealed class KingdomRelationDto
    {
        public int KingdomId { get; set; }
        public string Name { get; set; } = "";
        public bool IsAi { get; set; }
        public int Value { get; set; }
        public int Regions { get; set; }
    }

    public sealed class CaravanDto
    {
        public int Id { get; set; }
        public int OfferId { get; set; }
        public string Product { get; set; } = "";
        public int Quantity { get; set; }
        public string FromName { get; set; } = "";
        public string ToName { get; set; } = "";
        public bool Outgoing { get; set; }
        /// <summary>0 = yo'lda, 1 = yetib keldi.</summary>
        public int Status { get; set; }
        public float Progress { get; set; }
        public int SecondsLeft { get; set; }
    }

    public sealed class RegionActionRequest
    {
        public string CommandId { get; set; } = "";
        public int RegionId { get; set; }
    }

    public sealed class GovernorRequest
    {
        public string CommandId { get; set; } = "";
        public int CityId { get; set; }
        public GovernorMode Mode { get; set; }
        public int PlanId { get; set; }
    }

    public sealed class PlantCropRequest
    {
        public string CommandId { get; set; } = "";
        public int FieldId { get; set; }
        public string Crop { get; set; } = "Rice";
    }
}
