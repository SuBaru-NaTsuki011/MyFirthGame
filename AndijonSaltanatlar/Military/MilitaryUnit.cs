namespace AndijonSaltanatlar.Military
{
    public enum AnimationState { Idle, Walking, Running, AttackAnimation, DefenseAnimation, Training, Victory, Retreat }

    /// <summary>Bitta harbiy birlik (simulyatsiya modeli). Daraja oshgani sari statistikasi yaxshilanadi.</summary>
    public sealed class MilitaryUnit
    {
        public int Id { get; set; }
        public string NameUz { get; set; } = "";
        public UnitType UnitType { get; set; }
        public int Level { get; set; } = 1;

        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int AttackPower { get; set; }
        public int Defense { get; set; }
        public int MovementSpeed { get; set; }

        public int TrainingCost { get; set; }
        public int UpkeepCost { get; set; }
        public int Experience { get; set; }

        public AnimationState AnimationState { get; set; } = AnimationState.Idle;
        public int OwnerKingdomId { get; set; }
        public int CurrentRegionId { get; set; }
        public int ArmyId { get; set; }

        /// <summary>Jangdagi hissasi (hujum + himoya), darajaga qarab oshadi.</summary>
        public int Strength => (int)((AttackPower + Defense) * (1f + 0.15f * (Level - 1)));

        public static MilitaryUnit Create(int id, UnitType type, int ownerKingdomId, int regionId)
        {
            UnitDef d = UnitCatalog.Get(type);
            return new MilitaryUnit
            {
                Id = id, UnitType = type, NameUz = d.NameUz, Level = 1,
                Health = d.Health, MaxHealth = d.Health,
                AttackPower = d.Attack, Defense = d.Defense, MovementSpeed = d.Speed,
                TrainingCost = d.TrainingCost, UpkeepCost = d.Upkeep,
                OwnerKingdomId = ownerKingdomId, CurrentRegionId = regionId
            };
        }
    }
}
