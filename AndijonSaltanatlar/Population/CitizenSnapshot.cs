namespace AndijonSaltanatlar.Population
{
    /// <summary>
    /// Save uchun bitta fuqaro. PROMPT 17: Id / Name / HouseholdId / ish joyi koordinatasi qo'shildi —
    /// yuklanganda fuqaro "boshqa odamga aylanib qolmaydi". Eski save'larda bu maydonlar 0 / -1 bo'ladi.
    /// </summary>
    public sealed class CitizenSnapshot
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int HouseholdId { get; set; }
        /// <summary>Ish joyi binosining tile koordinatasi (bino Id'si emas — Id qayta yuklashda o'zgarishi mumkin).</summary>
        public int WorkX { get; set; } = -1;
        public int WorkY { get; set; } = -1;

        public int Profession { get; set; }
        public int Age { get; set; }
        public float Happiness { get; set; }
        public float Health { get; set; }
        public float Education { get; set; }
        public float Hunger { get; set; }
        public float Energy { get; set; }
        public float Money { get; set; }
        public bool Employed { get; set; }
    }

    /// <summary>Save uchun bitta oila (uy koordinatasi va jamg'armasi bilan).</summary>
    public sealed class HouseholdSnapshot
    {
        public int Id { get; set; }
        public string FamilyName { get; set; }
        public int HomeX { get; set; } = -1;
        public int HomeY { get; set; } = -1;
        public float Money { get; set; }
        public float FoodReserve { get; set; }
        public float FedRatio { get; set; } = 1f;
        public float Comfort { get; set; }
        public float LastIncome { get; set; }
        public float LastExpense { get; set; }
    }
}
