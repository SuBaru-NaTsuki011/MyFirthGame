namespace AndijonSaltanatlar.Advisor
{
    public enum AdvisorCategory { Muhim, Tavsiya, Ogohlantirish, Tarix, Tutorial }

    /// <summary>Donishmand Vazirning bitta xabari — toifa, matn va bog'langan harakat.</summary>
    public sealed class AdvisorMessage
    {
        public AdvisorCategory Category { get; set; }
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
        public string ActionLabel { get; set; } = "";
        /// <summary>0=yo'q, 1=Uy, 2=Bog', 3=Bozor, 4=Yerni tanlash, 5=Soliqni kamaytirish.</summary>
        public int ActionId { get; set; }
        public int Priority { get; set; }

        public static string CategoryName(AdvisorCategory c)
        {
            switch (c)
            {
                case AdvisorCategory.Muhim: return "MUHIM";
                case AdvisorCategory.Ogohlantirish: return "OGOHLANTIRISH";
                case AdvisorCategory.Tarix: return "TARIX";
                case AdvisorCategory.Tutorial: return "O'RGATISH";
                default: return "TAVSIYA";
            }
        }
    }
}
