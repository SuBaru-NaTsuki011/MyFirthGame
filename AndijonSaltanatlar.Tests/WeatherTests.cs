using AndijonSaltanatlar.Environment;
using Xunit;

namespace AndijonSaltanatlar.Tests
{
    /// <summary>Promt 8: ob-havo — yangi turlar (jazirama, bo'ron) va hosil/kayfiyat/jang ta'siri.</summary>
    public class WeatherTests
    {
        [Fact]
        public void YangiTurlar_NomlariBor()
        {
            Assert.Equal("Jazirama", WeatherManager.NameOf(Weather.Hot));
            Assert.Equal("Bo'ron", WeatherManager.NameOf(Weather.Storm));
        }

        [Fact]
        public void Hosil_JaziramaVaBoron_Kamaytiradi()
        {
            Assert.True(WeatherManager.ProductionModifier(Weather.Clear) > 0f);
            Assert.True(WeatherManager.ProductionModifier(Weather.Hot) < 0f);
            Assert.True(WeatherManager.ProductionModifier(Weather.Storm) < WeatherManager.ProductionModifier(Weather.Hot)); // bo'ron eng yomoni
        }

        [Fact]
        public void Kayfiyat_YomonHavo_Pasaytiradi()
        {
            Assert.True(WeatherManager.MoodModifier(Weather.Clear) > 0f);
            Assert.True(WeatherManager.MoodModifier(Weather.Storm) < WeatherManager.MoodModifier(Weather.Snowy));
            Assert.True(WeatherManager.MoodModifier(Weather.Storm) < 0f);
        }

        [Fact]
        public void Jang_BoronEngKuchsiz()
        {
            Assert.Equal(1.0f, WeatherManager.BattleModifier(Weather.Clear), 3);
            Assert.True(WeatherManager.BattleModifier(Weather.Rainy) < 1.0f);
            Assert.True(WeatherManager.BattleModifier(Weather.Snowy) < WeatherManager.BattleModifier(Weather.Rainy));
            Assert.True(WeatherManager.BattleModifier(Weather.Storm) < WeatherManager.BattleModifier(Weather.Snowy));
        }

        [Fact]
        public void Update_OzgartiradiVaSetCurrent_Ishlaydi()
        {
            var w = new WeatherManager();
            w.Update(5, Season.Yoz);
            Weather afterUpdate = w.Current;          // bir qiymat o'rnatiladi
            w.SetCurrent(Weather.Storm);
            Assert.Equal(Weather.Storm, w.Current);
            Assert.True(System.Enum.IsDefined(typeof(Weather), afterUpdate));
        }
    }
}
