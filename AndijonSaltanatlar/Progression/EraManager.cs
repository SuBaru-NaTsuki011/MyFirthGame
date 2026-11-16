namespace AndijonSaltanatlar.Progression
{
    /// <summary>
    /// Tarixiy davrni ochilgan texnologiyalar soniga qarab belgilaydi. Chiziqli daraja emas —
    /// badiiy uslub Temuriylar/Bobur davridan ilhomlangan; keyingi davrlar kelajakda.
    /// </summary>
    public static class EraManager
    {
        public static string EraName(int researchedCount)
        {
            if (researchedCount < 3) return "Ilk o'rta asrlar";
            if (researchedCount < 7) return "Rivojlangan o'rta asrlar";
            if (researchedCount < 12) return "Temuriylar davri";
            return "Bobur davri (XVI asr)";
        }
    }
}
