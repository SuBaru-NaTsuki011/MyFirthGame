namespace AndijonSaltanatlar.Military
{
    /// <summary>
    /// Jangchi tajribasi va daraja oshishini boshqaradi (konfiguratsiya qiymatlari shu yerda).
    /// Daraja: 1-Yangi, 2-Tajribali, 3-Mohir, 4-Elita, 5-Saltanat faxri.
    /// </summary>
    public static class TrainingSystem
    {
        public const int MaxLevel = 5;

        public static int XpForNext(int level) => level * 100;

        public static bool AddExperience(MilitaryUnit u, int xp)
        {
            bool leveled = false;
            u.Experience += xp;
            while (u.Level < MaxLevel && u.Experience >= XpForNext(u.Level))
            {
                u.Experience -= XpForNext(u.Level);
                u.Level++;
                u.MaxHealth += 12;
                u.Health = u.MaxHealth;
                u.AttackPower += 2;
                u.Defense += 2;
                leveled = true;
            }
            return leveled;
        }
    }
}
