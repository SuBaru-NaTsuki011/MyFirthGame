using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.LandExpansion
{
    public enum LandParcelState { Open, Locked, Unlockable, OtherKingdom, Protected }

    /// <summary>
    /// Shahar ichidagi yer uchastkasi (mahalla hududi). Dunyo xaritasidagi hudud (Region) emas —
    /// bu faqat shahar qurilish maydonini kengaytirish uchun. GridBounds — tile koordinatalarida.
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
    }
}
