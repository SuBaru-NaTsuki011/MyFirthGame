using System;

namespace AndijonSaltanatlar.Server.Services
{
    /// <summary>Oddiy nomaqbul so'z filtri (o'yinchilararo xatlar uchun). Kengaytirilishi mumkin.</summary>
    public static class ProfanityFilter
    {
        // Qisqa ro'yxat — asosiy haqoratli so'zlar (uz/ru). Kerak bo'lsa serverdan sozlanadi.
        private static readonly string[] Bad =
        {
            "jalab", "qanjiq", "ahmoq", "tentak", "haromi", "padar", "go'ng", "eshak",
            "suka", "blyat", "blyad", "mudak", "pidr", "huy", "pizd", "yeban",
            "fuck", "shit", "bitch", "asshole",
        };

        public static bool Contains(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            string t = text.ToLowerInvariant();
            foreach (string w in Bad)
                if (t.Contains(w)) return true;
            return false;
        }
    }
}
