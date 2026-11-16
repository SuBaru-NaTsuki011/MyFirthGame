namespace AndijonSaltanatlar.Government
{
    public enum PetitionType
    {
        FoodShortage,     // oziq-ovqat yetishmasligi
        HousingShortage,  // uy-joy tanqisligi
        Unemployment,     // ishsizlik
        HighTaxes,        // yuqori soliqlar
        HealthProblem,    // sog'liq
        SecurityProblem   // xavfsizlik
    }

    /// <summary>Fuqarolardan hukmdorga kelgan bitta murojaat va uning tanlovlari.</summary>
    public sealed class Petition
    {
        public PetitionType Type { get; }
        public string Title { get; }
        public string Body { get; }
        public string[] Choices { get; }

        public Petition(PetitionType type, string title, string body, string[] choices)
        {
            Type = type;
            Title = title;
            Body = body;
            Choices = choices;
        }
    }
}
