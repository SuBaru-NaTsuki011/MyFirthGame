using Microsoft.EntityFrameworkCore;

namespace AndijonSaltanatlar.Server.Data
{
    /// <summary>PostgreSQL ma'lumotlar bazasi konteksti (Entity Framework Core).</summary>
    public class GameDbContext : DbContext
    {
        public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

        public DbSet<AppUser> Users => Set<AppUser>();
        public DbSet<World> Worlds => Set<World>();
        public DbSet<Kingdom> Kingdoms => Set<Kingdom>();
        public DbSet<KingdomResource> KingdomResources => Set<KingdomResource>();
        public DbSet<RegionEntity> Regions => Set<RegionEntity>();
        public DbSet<CityEntity> Cities => Set<CityEntity>();
        public DbSet<BuildingEntity> Buildings => Set<BuildingEntity>();
        public DbSet<LandParcelEntity> LandParcels => Set<LandParcelEntity>();
        public DbSet<ResearchProgressEntity> ResearchProgress => Set<ResearchProgressEntity>();
        public DbSet<DiplomaticRelationEntity> DiplomaticRelations => Set<DiplomaticRelationEntity>();
        public DbSet<TradeOfferEntity> TradeOffers => Set<TradeOfferEntity>();
        public DbSet<GameCommandEntity> GameCommands => Set<GameCommandEntity>();
        public DbSet<PlayerNotificationEntity> Notifications => Set<PlayerNotificationEntity>();
        public DbSet<RiceFieldEntity> RiceFields => Set<RiceFieldEntity>();
        public DbSet<MarketOfferEntity> MarketOffers => Set<MarketOfferEntity>();
        public DbSet<TradeTransactionEntity> TradeTransactions => Set<TradeTransactionEntity>();
        public DbSet<InventoryEntity> Inventory => Set<InventoryEntity>();
        public DbSet<CaravanEntity> Caravans => Set<CaravanEntity>();
        public DbSet<GovernorPlanEntity> GovernorPlans => Set<GovernorPlanEntity>();
        // PROMPT 20
        public DbSet<AllianceEntity> Alliances => Set<AllianceEntity>();
        public DbSet<AllianceMemberEntity> AllianceMembers => Set<AllianceMemberEntity>();
        public DbSet<AllianceInvitationEntity> AllianceInvitations => Set<AllianceInvitationEntity>();
        public DbSet<AllianceProjectEntity> AllianceProjects => Set<AllianceProjectEntity>();
        public DbSet<AllianceContributionEntity> AllianceContributions => Set<AllianceContributionEntity>();
        public DbSet<DiplomaticAgreementEntity> DiplomaticAgreements => Set<DiplomaticAgreementEntity>();
        public DbSet<WorldNewsEntity> WorldNews => Set<WorldNewsEntity>();
        // Google login + bulutdagi saqlash
        public DbSet<CloudSaveEntity> CloudSaves => Set<CloudSaveEntity>();
        // PROMPT 25: urush tizimi
        public DbSet<KingdomWarStateEntity> WarStates => Set<KingdomWarStateEntity>();
        public DbSet<ArmyEntity> Armies => Set<ArmyEntity>();
        public DbSet<ArmyUnitEntity> ArmyUnits => Set<ArmyUnitEntity>();
        public DbSet<CommanderEntity> Commanders => Set<CommanderEntity>();
        public DbSet<MilitaryFacilityEntity> MilitaryFacilities => Set<MilitaryFacilityEntity>();
        public DbSet<RecruitOrderEntity> RecruitOrders => Set<RecruitOrderEntity>();
        public DbSet<BattleEntity> Battles => Set<BattleEntity>();
        public DbSet<BattleDecisionEntity> BattleDecisions => Set<BattleDecisionEntity>();
        public DbSet<BattleRoundEntity> BattleRounds => Set<BattleRoundEntity>();
        public DbSet<BattleInvitationEntity> BattleInvitations => Set<BattleInvitationEntity>();
        public DbSet<KingdomConflictEntity> KingdomConflicts => Set<KingdomConflictEntity>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<AppUser>().HasIndex(u => u.Email).IsUnique();
            b.Entity<AppUser>().HasIndex(u => u.Username).IsUnique();
            b.Entity<AppUser>().HasIndex(u => u.GoogleSub).IsUnique();
            b.Entity<CloudSaveEntity>().HasIndex(c => c.UserId).IsUnique();
            // PROMPT 25: takror qarorlar, takror raund natijalari va ikki marta qo'shilgan guruhlar DB darajasida ham mumkin emas.
            b.Entity<KingdomWarStateEntity>().HasIndex(w => w.KingdomId).IsUnique();
            b.Entity<ArmyEntity>().HasIndex(a => a.KingdomId);
            b.Entity<ArmyUnitEntity>().HasIndex(u => new { u.ArmyId, u.Line, u.Level }).IsUnique();
            b.Entity<CommanderEntity>().HasIndex(c => c.KingdomId);
            b.Entity<MilitaryFacilityEntity>().HasIndex(f => new { f.KingdomId, f.Kind }).IsUnique();
            b.Entity<RecruitOrderEntity>().HasIndex(o => new { o.KingdomId, o.Delivered });
            b.Entity<BattleEntity>().HasIndex(x => new { x.WorldId, x.Status });
            b.Entity<BattleDecisionEntity>().HasIndex(d => new { d.BattleId, d.Round, d.KingdomId }).IsUnique();
            b.Entity<BattleRoundEntity>().HasIndex(r => new { r.BattleId, r.Round }).IsUnique();
            b.Entity<BattleInvitationEntity>().HasIndex(i => new { i.ToKingdomId, i.Status });
            b.Entity<KingdomConflictEntity>().HasIndex(c => new { c.WorldId, c.KingdomA, c.KingdomB }).IsUnique();
            b.Entity<GameCommandEntity>().HasKey(c => c.CommandId);
            // Bitta hududda bitta asosiy ega (neytrallardan tashqari) — indeks orqali nazorat
            b.Entity<RegionEntity>().HasIndex(r => new { r.WorldId, r.Name }).IsUnique();
            // PROMPT 19A: bir saltanatda bitta slotda bitta maydon; bitta taklif — bitta savdo.
            b.Entity<KingdomResource>().HasIndex(r => r.KingdomId).IsUnique();
            b.Entity<RiceFieldEntity>().HasIndex(f => new { f.KingdomId, f.Slot }).IsUnique();
            b.Entity<MarketOfferEntity>().HasIndex(o => new { o.WorldId, o.Status });
            b.Entity<TradeTransactionEntity>().HasIndex(t => t.OfferId).IsUnique();
            // PROMPT 19B: bitta mahsulot uchun bitta ombor qatori; bitta taklif — bitta karvon; diplomatik juftlik — bitta munosabat.
            b.Entity<InventoryEntity>().HasIndex(i => new { i.KingdomId, i.Product }).IsUnique();
            b.Entity<CaravanEntity>().HasIndex(c => c.OfferId).IsUnique();
            b.Entity<CaravanEntity>().HasIndex(c => new { c.Status, c.ArriveAt });
            b.Entity<CityEntity>().HasIndex(c => c.RegionId);
            b.Entity<DiplomaticRelationEntity>().HasIndex(r => new { r.WorldId, r.KingdomA, r.KingdomB }).IsUnique();
            b.Entity<GovernorPlanEntity>().HasIndex(p => new { p.CityId, p.Status });
            // PROMPT 20: saltanat faqat bitta ittifoqda; ittifoq nomi dunyoda yagona; hissa buyrug'i takrorlanmaydi.
            b.Entity<AllianceEntity>().HasIndex(a => new { a.WorldId, a.Name }).IsUnique();
            b.Entity<AllianceMemberEntity>().HasIndex(m => m.KingdomId).IsUnique();
            b.Entity<AllianceMemberEntity>().HasIndex(m => m.AllianceId);
            b.Entity<AllianceInvitationEntity>().HasIndex(i => new { i.ToKingdomId, i.Status });
            b.Entity<AllianceProjectEntity>().HasIndex(p => p.AllianceId);
            b.Entity<AllianceContributionEntity>().HasIndex(c => c.CommandId).IsUnique();
            b.Entity<DiplomaticAgreementEntity>().HasIndex(a => new { a.WorldId, a.Status });
            b.Entity<WorldNewsEntity>().HasIndex(n => new { n.WorldId, n.Id });
            base.OnModelCreating(b);
        }
    }
}
