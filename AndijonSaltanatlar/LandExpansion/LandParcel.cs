using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.LandExpansion
{
    public enum LandParcelState { Open, Locked, Unlockable, OtherKingdom, Protected }

    /// <summary>Halqadagi tomon: har halqada 4 ta uchastka bor.</summary>
    public enum LandSide { North, South, West, East }

    /// <summary>
    /// Shahar ichidagi yer uchastkasi (mahalla hududi). Dunyo xaritasidagi hudud (Region) emas —
    /// bu faqat shahar qurilish maydonini kengaytirish uchun. GridBounds — tile koordinatalarida.
    /// Uchastkalar halqalarga bo'lingan: 1-halqaning 4 tomoni ochilgach 2-halqaning 4 tomoni ochiladi va hokazo.
    /// </summary>
    public sealed class LandParcel
    {
        public int Id { get; set; }
        public string NameUz { get; set; } = "";
        public Rectangle GridBounds { get; set; }   // tile'larda (x,y,w,h)
        public LandParcelState State { get; set; } = LandParcelState.Locked;
        public int UnlockCost { get; set; }
        public int RequiredPopulation { get; set; }
        public bool IsProtected { get; set; }

        /// <summary>Halqa raqami (1 — shahar devori atrofidagi mahallalar, 2.. — tashqi yerlar).</summary>
        public int Ring { get; set; } = 1;
        public LandSide Side { get; set; }
        /// <summary>Yerning tavsiya etilgan vazifasi (chorvachilik, dehqonchilik, savdo...).</summary>
        public string PurposeUz { get; set; } = "";
        /// <summary>Oldingi halqa hali to'liq ochilmagan — bu uchastka hozircha ko'rinadi, lekin ochib bo'lmaydi.</summary>
        public bool WaitingForPreviousRing { get; set; }

        public static string StateName(LandParcelState s)
        {
            switch (s)
            {
                case LandParcelState.Open: return "Ochiq";
                case LandParcelState.Unlockable: return "Ochish mumkin";
                case LandParcelState.OtherKingdom: return "Boshqa davlat yeri";
                case LandParcelState.Protected: return "Muhofaza qilinadi";
                default: return "Qulflangan";
            }
        }

        /// <summary>Holat matni (halqa kutilayotganini ham hisobga oladi).</summary>
        public string StatusText()
        {
            if (State != LandParcelState.Open && WaitingForPreviousRing)
                return "Avval " + (Ring - 1) + "-halqani to'liq oching";
            return StateName(State);
        }
    }
}
