using System.Collections.Generic;
using AndijonSaltanatlar.AdvancedEconomy;
using AndijonSaltanatlar.Population;

namespace AndijonSaltanatlar.NPC
{
    /// <summary>
    /// Fuqaro bilan muloqot uchun yig'ma profil (Citizen Info Card va dialog oynasi shundan o'qiydi).
    /// Bu alohida ma'lumot nusxasi EMAS: har safar PopulationManager, mahalla va oila iqtisodiyotidagi
    /// HAQIQIY manbalardan qayta yig'iladi (NPCInteractionManager.BuildProfile).
    /// </summary>
    public sealed class CitizenInteractionProfile
    {
        public int CitizenId { get; set; }
        public string DisplayName { get; set; } = "";
        public int Age { get; set; }

        public int HouseholdId { get; set; }
        public string FamilyName { get; set; } = "";
        public int FamilySize { get; set; }
        public int WorkingMembers { get; set; }
        public float HouseholdSavings { get; set; }
        public float HouseholdIncome { get; set; }
        public float HouseholdExpense { get; set; }
        public float FedRatio { get; set; } = 1f;

        public int DistrictId { get; set; }
        public string DistrictName { get; set; } = "";

        /// <summary>Kasb kaliti (dialog SpeakerType bilan mos): "savdogar", "bogbon", ..., "none".</summary>
        public string OccupationId { get; set; } = "none";
        public string OccupationName { get; set; } = "";
        public CitizenProfession Profession { get; set; }
        public bool IsEmployed { get; set; }

        public string HomeName { get; set; } = "";
        public int HomeBuildingId { get; set; }
        public string WorkplaceName { get; set; } = "";
        public int WorkplaceBuildingId { get; set; }

        public float Happiness { get; set; }
        public float Health { get; set; }
        public float Education { get; set; }
        public float Hunger { get; set; }
        public float TrustInGovernment { get; set; } = 50f;
        public string TrustLabel { get; set; } = "";
        public string PersonalityTrait { get; set; } = "";

        /// <summary>Hozirgi faoliyati (haqiqiy holatdan).</summary>
        public string CurrentActivity { get; set; } = "";

        /// <summary>Faol muammolar (ConcernKeys), muhimlik bo'yicha saralangan.</summary>
        public List<string> ActiveConcernIds { get; set; } = new();
        public string TopConcernId => ActiveConcernIds.Count > 0 ? ActiveConcernIds[0] : "";
        /// <summary>Hukmdorga murojaati (eng muhim muammo yorlig'i yoki "Murojaat yo'q").</summary>
        public string PetitionUz => ActiveConcernIds.Count > 0 ? ConcernKeys.LabelUz(ActiveConcernIds[0]) : "Murojaat yo'q";

        public string CurrentDialogueId { get; set; }

        /// <summary>Savdogar bo'lsa — rastasi va moli (aks holda null).</summary>
        public MerchantInfo Merchant { get; set; }

        public static string OccupationKey(CitizenProfession p)
        {
            switch (p)
            {
                case CitizenProfession.Savdogar: return "savdogar";
                case CitizenProfession.Bogbon: return "bogbon";
                case CitizenProfession.Dehqon: return "dehqon";
                case CitizenProfession.Tashuvchi: return "tashuvchi";
                case CitizenProfession.Hunarmand: return "hunarmand";
                case CitizenProfession.Soqchi: return "soqchi";
                case CitizenProfession.Amaldor: return "amaldor";
                case CitizenProfession.Muallim: return "muallim";
                case CitizenProfession.Tabib: return "tabib";
                case CitizenProfession.Quruvchi: return "quruvchi";
                default: return "none";
            }
        }
    }
}
