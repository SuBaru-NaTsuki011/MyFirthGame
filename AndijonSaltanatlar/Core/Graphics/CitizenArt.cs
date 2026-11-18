namespace AndijonSaltanatlar.Core.Graphics
{
    /// <summary>
    /// Fuqaro turlari. Barcha fuqarolar rasmi — Bobur bobo 3D modellaridan (Content/Sprites/unit_citizen*.png).
    /// Eski piksel (kod bilan chizilgan) fuqarolar olib tashlangan; Frame faqat shaffof bo'sh kanvas qaytaradi.
    /// </summary>
    public static class CitizenArt
    {
        public const int Citizen = 0, Merchant = 1, Craftsman = 2, Farmer = 3, Buyer = 4, Shepherd = 5;
        public const int Kinds = 6;

        /// <summary>Bo'sh (shaffof) kadr — haqiqiy rasm PNG'dan yuklanadi.</summary>
        public static Canvas Frame(int kind, int frame) => new Canvas(48, 60);
    }
}
