using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Environment;
using AndijonSaltanatlar.World;
using Xunit;

namespace AndijonSaltanatlar.Tests
{
    /// <summary>Promt 1: kun uzunligi migratsiyasi, fasl bo'yicha tong/shom, ish davomiyligi kun uzunligiga nisbati.</summary>
    public class TimeSeasonTests
    {
        // --- 1. Kun uzunligi: standart va migratsiya ---

        [Fact]
        public void YangiStandart_120Daqiqa()
        {
            Assert.Equal(120, new TimePrefs().DayMinutes);
        }

        [Fact]
        public void Presetlar_30_60_120_240()
        {
            Assert.Equal(new[] { 30, 60, 120, 240 }, TimePrefs.DayPresets);
        }

        [Theory]
        [InlineData(60, 0, 120)]  // eski standart (60) → 120
        [InlineData(60, 1, 60)]   // versiya 1 — allaqachon ko'chirilgan, tegilmaydi
        [InlineData(30, 0, 30)]   // o'yinchi 30 tanlagan — saqlanadi
        [InlineData(240, 0, 240)] // boshqa qiymat — saqlanadi
        public void DayMinutes_Migratsiya(int saved, int version, int expected)
        {
            Assert.Equal(expected, TimePrefs.MigratedDayMinutes(saved, version));
        }

        // --- 2. Fasl bo'yicha tong va shom ---

        [Theory]
        [InlineData(Season.Yoz, 5.0, 20.0)]   // yoz — kunduz uzun
        [InlineData(Season.Bahor, 6.0, 18.5)]
        [InlineData(Season.Kuz, 6.0, 18.5)]
        [InlineData(Season.Qish, 7.5, 17.0)]  // qish — kunduz qisqa
        public void FaslBoyicha_TongShom(Season s, double sunrise, double sunset)
        {
            Assert.Equal(sunrise, SeasonClock.SunriseHour(s), 3);
            Assert.Equal(sunset, SeasonClock.SunsetHour(s), 3);
            Assert.Equal((float)(sunrise / 24.0), SeasonClock.Sunrise(s), 4);
        }

        [Fact]
        public void TongShom_MatnFormati()
        {
            Assert.Equal("05:00", SeasonClock.SunriseText(Season.Yoz));
            Assert.Equal("20:00", SeasonClock.SunsetText(Season.Yoz));
            Assert.Equal("07:30", SeasonClock.SunriseText(Season.Qish));
        }

        [Fact]
        public void Qishda_KunduzYozdan_Qisqa()
        {
            float yoz = SeasonClock.SunsetHour(Season.Yoz) - SeasonClock.SunriseHour(Season.Yoz);
            float qish = SeasonClock.SunsetHour(Season.Qish) - SeasonClock.SunriseHour(Season.Qish);
            Assert.True(qish < yoz);
        }

        // --- Kun-tun aylanishi faslga bog'liq ---

        [Fact]
        public void Soat19da_QishYozdan_Qorongu()
        {
            var dn = new DayNightCycle();
            dn.Update(19f / 24f, Season.Yoz);
            float yozTun = dn.NightAmount;
            dn.Update(19f / 24f, Season.Qish);
            float qishTun = dn.NightAmount;
            Assert.True(qishTun > yozTun); // 19:00 da qish qorong'uroq (shom 17:00), yoz hali yorug' (shom 20:00)
        }

        [Fact]
        public void Tush_HarFaslda_Yorug()
        {
            var dn = new DayNightCycle();
            dn.Update(12f / 24f, Season.Qish);
            Assert.True(dn.NightAmount < 0.2f);
        }

        // --- 3. Ish davomiyligi kun uzunligiga bog'liq ---

        [Fact]
        public void UzunKun_BirSoniyada_KamroqOyinSoati()
        {
            float qisqaKun = GameClock.GameHours(1f, 3600f);  // 60 daqiqalik kun
            float uzunKun = GameClock.GameHours(1f, 7200f);   // 120 daqiqalik kun
            Assert.True(uzunKun < qisqaKun);
            Assert.Equal(qisqaKun / 2f, uzunKun, 5); // ikki barobar uzun kun — ikki barobar sekin
        }

        [Fact]
        public void Qurilish_120DaqKunda_EskiTezlikniSaqlaydi()
        {
            // Eski: BuildProgress += dt*0.4. Yangi: += GameHours(dt, 7200)*120.
            // 120 daqiqalik kun (7200 sek) uchun ikkalasi teng bo'lishi kerak.
            float dt = 1f;
            float eski = dt * 0.4f;
            float yangi = GameClock.GameHours(dt, 7200f) * 120f;
            Assert.Equal(eski, yangi, 4);
        }
    }
}
