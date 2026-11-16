using System.Collections.Generic;
using AndijonSaltanatlar.Core;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Units
{
    /// <summary>
    /// Shahar fuqarosi: belgilangan yo'nalish bo'ylab (A* yo'li bilan) yuradi.
    /// Qurolsiz, tinch aholi — shaharni "jonli" ko'rsatadi. PatrolUnit xatti-harakatini meros oladi.
    ///
    /// PROMPT 17: ko'rinadigan figura PopulationManager'dagi HAQIQIY fuqaroga bog'lanadi (CitizenId).
    /// Ism, kasb, oila va ehtiyojlar o'sha fuqarodan olinadi — bu yerda nusxasi saqlanmaydi.
    /// </summary>
    public sealed class CitizenUnit : PatrolUnit
    {
        private static readonly string[] Names = { "Fuqaro", "Savdogar", "Hunarmand", "Dehqon", "Xaridor" };
        private static readonly string[] Descs =
        {
            "Shahar aholisi. Kundalik yumushlari bilan band.",
            "Ipak va matolar savdogari. Bozor sari ketmoqda.",
            "Kulol-hunarmand. Ustaxonasiga qaytmoqda.",
            "Dehqon. Dalalardan shaharga kelmoqda.",
            "Bozor xaridori. Narx-navo bilan qiziqadi."
        };

        public int Kind { get; }
        /// <summary>Bog'langan haqiqiy fuqaro (PopulationManager). 0 = bog'lanmagan (faqat bezak).</summary>
        public int CitizenId { get; private set; }
        public bool IsBound => CitizenId > 0;

        public CitizenUnit(IEnumerable<Vector2> route, int kind, float speed = 0.8f) : base(route, speed)
        {
            int k = ((kind % 5) + 5) % 5;
            Kind = k;
            Name = Names[k];
            Description = Descs[k];
            SpriteKey = SpriteKeys.UnitCitizen + k;
            SetHitPoints(30);
            PauseTime = 0.8f + (float)Rng.NextDouble() * 1.6f;
        }

        /// <summary>Figurani haqiqiy fuqaroga bog'laydi va ko'rsatiladigan ism/tavsifni yangilaydi.</summary>
        public void BindCitizen(int citizenId, string displayName, string description)
        {
            CitizenId = citizenId;
            if (!string.IsNullOrEmpty(displayName)) Name = displayName;
            if (!string.IsNullOrEmpty(description)) Description = description;
        }
    }
}
