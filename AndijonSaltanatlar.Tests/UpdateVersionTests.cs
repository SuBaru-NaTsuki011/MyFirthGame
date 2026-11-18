using AndijonSaltanatlar.Shared;
using Xunit;

namespace AndijonSaltanatlar.Tests
{
    /// <summary>AppVersion — yangilash tekshiruvi uchun versiyalarni solishtirish mantiqi.</summary>
    public class UpdateVersionTests
    {
        [Theory]
        [InlineData("0.21.0", "0.20.0", true)]   // keyingi minor — yangi
        [InlineData("1.0.0", "0.20.0", true)]    // major — yangi
        [InlineData("0.20.1", "0.20.0", true)]   // patch — yangi
        [InlineData("0.20.0", "0.20.0", false)]  // bir xil — yangi emas
        [InlineData("0.19.0", "0.20.0", false)]  // eski — yangi emas
        public void IsNewer_SolishtirishTogri(string latest, string current, bool expected)
        {
            Assert.Equal(expected, AppVersion.IsNewer(latest, current));
        }

        [Theory]
        [InlineData("v0.21.0", "0.20.0", true)]  // "v" prefiksi (GitHub tag) tushuniladi
        [InlineData("V1.2.3", "1.2.2", true)]
        [InlineData("0.20.0-beta", "0.20.0", false)] // pre-release qo'shimchasi e'tiborsiz
        public void IsNewer_TagFormatlari(string latest, string current, bool expected)
        {
            Assert.Equal(expected, AppVersion.IsNewer(latest, current));
        }

        [Fact]
        public void Manbalar_TogriShakllanadi()
        {
            Assert.Contains("/releases/latest/download/" + AppVersion.ApkAssetName, AppVersion.LatestApkUrl);
            Assert.Contains("/releases/latest", AppVersion.LatestReleaseApiUrl);
            Assert.StartsWith("https://", AppVersion.LatestApkUrl);
        }
    }
}
