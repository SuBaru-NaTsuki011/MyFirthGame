namespace AndijonSaltanatlar.AIGovernor
{
    /// <summary>AI boshqaruv ruxsat darajalari — PROMPT 15, 13-bo'lim.</summary>
    public enum AutonomyLevel
    {
        /// <summary>LEVEL 0 — faqat maslahat, reja ham tuzmaydi.</summary>
        Manual = 0,
        /// <summary>LEVEL 1 — reja (blueprint) tuzadi, bajarmaydi.</summary>
        Planning = 1,
        /// <summary>LEVEL 2 — faqat tasdiqlangan rejani budjet/hudud chegarasida bajaradi.</summary>
        ApprovedExecution = 2
    }

    /// <summary>
    /// AI vakolatlari — PROMPT 15. AI hukmdorning o'rniga mustaqil qaror qabul qilmaydi:
    /// har qanday xarajat/qurilish FOYDALANUVCHI TASDIG'I bilan va tasdiqlangan budjet doirasida bo'ladi.
    /// </summary>
    public class AdvisorPermissions
    {
        public AutonomyLevel Level { get; set; } = AutonomyLevel.ApprovedExecution;

        /// <summary>Tasdiqlangan umumiy xarajat chegarasi (tanga). Bundan oshsa qayta tasdiq so'raladi.</summary>
        public int ApprovedBudget { get; set; }

        /// <summary>AI tasdiqsiz yangi yer sotib ololmaydi — bu qat'iy qoida.</summary>
        public bool CanBuyLandWithoutApproval => false;

        /// <summary>Har qanday xarajat oldindan foydalanuvchi tasdig'ini talab qiladi.</summary>
        public bool RequiresApprovalForSpending => true;

        public bool CanExecute => Level == AutonomyLevel.ApprovedExecution;
        public bool CanPlan => Level >= AutonomyLevel.Planning;

        public string ModeNameUz => Level switch
        {
            AutonomyLevel.Manual => "Faqat maslahat",
            AutonomyLevel.Planning => "Shahar me'mori (reja)",
            _ => "Tasdiqlangan reja ijrochisi"
        };

        public string ModeHintUz => Level switch
        {
            AutonomyLevel.Manual => "AI faqat tahlil va tavsiya beradi; reja ham tuzmaydi.",
            AutonomyLevel.Planning => "AI reja tuzadi, lekin qurilishni bajarish uchun ijro rejimini yoqing.",
            _ => "AI faqat siz tasdiqlagan rejani, tasdiqlangan budjet doirasida bajaradi."
        };

        public void CycleLevel()
            => Level = Level == AutonomyLevel.Manual ? AutonomyLevel.Planning
                : Level == AutonomyLevel.Planning ? AutonomyLevel.ApprovedExecution
                : AutonomyLevel.Manual;
    }
}
