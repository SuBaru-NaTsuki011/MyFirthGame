using System.Collections.Generic;

namespace AndijonSaltanatlar.Military
{
    /// <summary>Qo'shin qo'mondoni. Bonuslar — o'yin mexanikasi uchun (tarixiy fakt emas).</summary>
    public sealed class Commander
    {
        public int Id { get; set; }
        public string NameUz { get; set; } = "";
        public int Level { get; set; } = 1;
        public int Experience { get; set; }
        public int LeadershipBonus { get; set; }
        public int TrainingBonus { get; set; }
        public int MoraleBonus { get; set; }
        public int AssignedArmyId { get; set; }
        public string Bio { get; set; } = "";
        public string Era { get; set; } = "";
    }

    /// <summary>
    /// Tarixiy qahramonlar ma'lumot katalogi. Sana va joy — ishonchli tarixiy ma'lumot;
    /// o'yin bonuslari esa faqat o'yin mexanikasi uchun (tarixiy dalil emas).
    /// MUHIM: Bobur (1483-1530) va Amir Temur (1336-1405) bir davrda yashamagan — alohida davrlar.
    /// </summary>
    public static class HeroCatalog
    {
        public static List<Commander> CreateHeroes()
        {
            return new List<Commander>
            {
                new Commander
                {
                    Id = 1, NameUz = "Zahiriddin Muhammad Bobur", Level = 3,
                    LeadershipBonus = 15, TrainingBonus = 10, MoraleBonus = 12,
                    Era = "1483-1530", Bio = "Andijonda tug'ilgan. Boburiylar saltanatiga asos solgan sarkarda va shoir."
                },
            };
        }
    }
}
