using System;

namespace AndijonSaltanatlar.Server.Data
{
    // PROMPT 20 — ittifoqlar, qo'shma loyihalar, diplomatik kelishuvlar, dunyo yangiliklari.

    public static class AllianceRole { public const int Founder = 0, Envoy = 1, Member = 2; }
    public static class InviteStatus { public const int Pending = 0, Accepted = 1, Rejected = 2, Cancelled = 3; }
    public static class ProjectStatus { public const int Collecting = 0, Completed = 1; }
    public static class AgreementStatus { public const int Proposed = 0, Active = 1, Rejected = 2, Cancelled = 3, Expired = 4, Completed = 5; }

    /// <summary>Ittifoq. Xazina ixtiyoriy hissalardan to'ladi — a'zolar omboridan avtomatik olinmaydi.</summary>
    public class AllianceEntity
    {
        public int Id { get; set; }
        public int WorldId { get; set; }
        public string Name { get; set; } = "";
        public int Emblem { get; set; }
        public int FounderKingdomId { get; set; }
        public int TreasuryCoin { get; set; }
        public int TreasuryWood { get; set; }
        public int TreasuryStone { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>A'zolik. KingdomId UNIQUE — saltanat bir vaqtda faqat bitta ittifoqda.</summary>
    public class AllianceMemberEntity
    {
        public int Id { get; set; }
        public int AllianceId { get; set; }
        public int KingdomId { get; set; }
        public int Role { get; set; } = AllianceRole.Member;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }

    public class AllianceInvitationEntity
    {
        public int Id { get; set; }
        public int WorldId { get; set; }
        public int AllianceId { get; set; }
        public int FromKingdomId { get; set; }
        public int ToKingdomId { get; set; }
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }
    }

    /// <summary>Qo'shma loyiha: 0 Savdo yo'li, 1 Karvonsaroy, 2 Madaniyat markazi.</summary>
    public class AllianceProjectEntity
    {
        public int Id { get; set; }
        public int AllianceId { get; set; }
        public int Type { get; set; }
        public int ReqCoin { get; set; }
        public int ReqWood { get; set; }
        public int ReqStone { get; set; }
        public int GotCoin { get; set; }
        public int GotWood { get; set; }
        public int GotStone { get; set; }
        public int Status { get; set; }
        public int CreatedByKingdomId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }

    /// <summary>Har bir hissa — alohida yozuv, CommandId UNIQUE (ikki marta hisoblanmaydi). ProjectId 0 = xazinaga.</summary>
    public class AllianceContributionEntity
    {
        public int Id { get; set; }
        public int AllianceId { get; set; }
        public int ProjectId { get; set; }
        public int KingdomId { get; set; }
        public string Resource { get; set; } = "Coin";
        public int Amount { get; set; }
        public string CommandId { get; set; } = "";
        public DateTime At { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Diplomatik kelishuv: 0 Savdo, 1 Do'stlik, 2 Hududni topshirish, 3 Savdo yo'lidan foydalanish.
    /// Hudud kelishuvlari ham shu jadvalda (RegionId, Price) — alohida takroriy jadval yo'q.
    /// </summary>
    public class DiplomaticAgreementEntity
    {
        public int Id { get; set; }
        public int WorldId { get; set; }
        public int Type { get; set; }
        public int FromKingdomId { get; set; }
        public int ToKingdomId { get; set; }
        public int RegionId { get; set; }
        /// <summary>Hudud kelishuvida: hududni OLUVCHI tomon to'laydigan tanga.</summary>
        public int Price { get; set; }
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class WorldNewsEntity
    {
        public int Id { get; set; }
        public int WorldId { get; set; }
        /// <summary>alliance_created, alliance_joined, alliance_left, agreement, territory, project_done, project_new, trade ...</summary>
        public string Type { get; set; } = "";
        public int KingdomAId { get; set; }
        public int KingdomBId { get; set; }
        public int RegionId { get; set; }
        public string TextUz { get; set; } = "";
        public int GameDay { get; set; }
        public DateTime At { get; set; } = DateTime.UtcNow;
    }
}
