namespace AndijonSaltanatlar.Diplomacy
{
    /// <summary>Munosabat darajasi bosqichlari (chegaralar konfiguratsiya qilinadi).</summary>
    public enum RelationBand { Dushman, Sovuq, Betaraf, Doston, Ittifoqchi }

    public static class RelationBands
    {
        // Chegaralar (o'zgartirilishi mumkin)
        public static int HostileMax = -60;
        public static int ColdMax = -20;
        public static int NeutralMax = 19;
        public static int FriendlyMax = 59;

        public static RelationBand Of(int value)
        {
            if (value <= HostileMax) return RelationBand.Dushman;
            if (value <= ColdMax) return RelationBand.Sovuq;
            if (value <= NeutralMax) return RelationBand.Betaraf;
            if (value <= FriendlyMax) return RelationBand.Doston;
            return RelationBand.Ittifoqchi;
        }

        public static string Name(RelationBand b)
        {
            switch (b)
            {
                case RelationBand.Dushman: return "Dushman";
                case RelationBand.Sovuq: return "Sovuq";
                case RelationBand.Doston: return "Do'stona";
                case RelationBand.Ittifoqchi: return "Ittifoqchi";
                default: return "Betaraf";
            }
        }
    }
}
