using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace AndijonSaltanatlar.Server.Authentication
{
    /// <summary>
    /// VIP hisoblar (appsettings.json → "Vip:Accounts" yoki Vip__Accounts__0 muhit o'zgaruvchilari).
    /// Ro'yxatga Google emailning to'liq manzili yoki faqat @ oldidagi qismi yoziladi.
    /// VIP belgisi bazada saqlanadi va barcha qurilmalarda bir xil hisobga beriladi.
    /// </summary>
    public sealed class VipOptions
    {
        public List<string> Accounts { get; set; } = new List<string>();

        public static VipOptions From(IConfiguration cfg)
        {
            var o = new VipOptions();
            cfg.GetSection("Vip").Bind(o);
            return o;
        }

        public bool IsVip(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            string full = email.Trim().ToLowerInvariant();
            string local = full.Split('@')[0];
            foreach (string raw in Accounts)
            {
                string a = (raw ?? "").Trim().ToLowerInvariant();
                if (a.Length == 0) continue;
                if (a.Contains("@") ? full == a : local == a) return true;
            }
            return false;
        }
    }
}
