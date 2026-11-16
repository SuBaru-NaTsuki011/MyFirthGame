using System.Collections.Generic;

namespace AndijonSaltanatlar.Shared.DTOs
{
    public sealed class AllianceMemberDto
    {
        public int KingdomId { get; set; }
        public string KingdomName { get; set; } = "";
        public int Role { get; set; } // 0 founder, 1 envoy, 2 member
        public string RoleNameUz { get; set; } = "";
    }

    public sealed class AllianceDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Emblem { get; set; }
        public int FounderKingdomId { get; set; }
        public int TreasuryCoin { get; set; }
        public int TreasuryWood { get; set; }
        public int TreasuryStone { get; set; }
        public List<AllianceMemberDto> Members { get; set; } = new();
        public int MyRole { get; set; } = -1; // -1 = not a member
        public List<AllianceProjectDto> Projects { get; set; } = new();
    }

    public sealed class AllianceInvitationDto
    {
        public int Id { get; set; }
        public int AllianceId { get; set; }
        public string AllianceName { get; set; } = "";
        public int FromKingdomId { get; set; }
        public string FromKingdomName { get; set; } = "";
        public int ToKingdomId { get; set; }
        public string ToKingdomName { get; set; } = "";
        public int Status { get; set; } // 0 pending,1 accepted,2 rejected,3 cancelled
        public string StatusUz { get; set; } = "";
        public string CreatedAt { get; set; } = "";
    }

    public sealed class AllianceProjectDto
    {
        public int Id { get; set; }
        public int AllianceId { get; set; }
        public int Type { get; set; } // 0 trade route, 1 caravanserai, 2 culture center
        public string TypeNameUz { get; set; } = "";
        public int ReqCoin { get; set; }
        public int ReqWood { get; set; }
        public int ReqStone { get; set; }
        public int GotCoin { get; set; }
        public int GotWood { get; set; }
        public int GotStone { get; set; }
        public int Status { get; set; } // 0 collecting, 1 completed
        public string StatusUz { get; set; } = "";
        public int CreatedByKingdomId { get; set; }
        public string CreatedByName { get; set; } = "";
        public float Progress { get; set; }
    }

    public sealed class DiplomaticAgreementDto
    {
        public int Id { get; set; }
        public int Type { get; set; } // 0 trade, 1 friendship, 2 territory transfer, 3 trade route access
        public string TypeNameUz { get; set; } = "";
        public int FromKingdomId { get; set; }
        public string FromKingdomName { get; set; } = "";
        public int ToKingdomId { get; set; }
        public string ToKingdomName { get; set; } = "";
        public int RegionId { get; set; }
        public string RegionName { get; set; } = "";
        public int Price { get; set; }
        public int Status { get; set; } // 0 proposed,1 active,2 rejected,3 cancelled,4 expired,5 completed
        public string StatusUz { get; set; } = "";
        public string CreatedAt { get; set; } = "";
    }

    public sealed class WorldNewsDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = "";
        public string TextUz { get; set; } = "";
        public int KingdomAId { get; set; }
        public string KingdomAName { get; set; } = "";
        public int KingdomBId { get; set; }
        public string KingdomBName { get; set; } = "";
        public int RegionId { get; set; }
        public string RegionName { get; set; } = "";
        public int GameDay { get; set; }
        public string AtUtc { get; set; } = "";
    }

    // ---- Requests ----
    public sealed class CreateAllianceRequest
    {
        public string CommandId { get; set; } = "";
        public string Name { get; set; } = "";
        public int Emblem { get; set; }
    }

    public sealed class InviteToAllianceRequest
    {
        public string CommandId { get; set; } = "";
        public int AllianceId { get; set; }
        public int ToKingdomId { get; set; }
    }

    public sealed class RespondInvitationRequest
    {
        public string CommandId { get; set; } = "";
        public int InvitationId { get; set; }
        public bool Accept { get; set; }
    }

    public sealed class ContributeAllianceRequest
    {
        public string CommandId { get; set; } = "";
        public int AllianceId { get; set; }
        public int ProjectId { get; set; } // 0 = treasury
        public string Resource { get; set; } = "Coin";
        public int Amount { get; set; }
    }

    public sealed class CreateAllianceProjectRequest
    {
        public string CommandId { get; set; } = "";
        public int AllianceId { get; set; }
        public int Type { get; set; }
    }

    public sealed class ProposeAgreementRequest
    {
        public string CommandId { get; set; } = "";
        public int Type { get; set; }
        public int ToKingdomId { get; set; }
        public int RegionId { get; set; }
        public int Price { get; set; }
    }

    public sealed class RespondAgreementRequest
    {
        public string CommandId { get; set; } = "";
        public int AgreementId { get; set; }
        public bool Accept { get; set; }
    }

    public sealed class LeaveAllianceRequest
    {
        public string CommandId { get; set; } = "";
        public int AllianceId { get; set; }
    }

    /// <summary>PROMPT 20: asoschi a'zoga rol beradi (0 Asoschi, 1 Vakil, 2 A'zo).</summary>
    public sealed class SetAllianceRoleRequest
    {
        public string CommandId { get; set; } = "";
        public int KingdomId { get; set; }
        public int Role { get; set; } = 1;
    }}
