namespace AndijonSaltanatlar.Core
{
    /// <summary>
    /// O'yin sozlamalari (ish vaqtida o'zgartiriladigan). Hozircha oddiy, keyin audio,
    /// til, grafika sifati shu yerga qo'shiladi va JSON'ga saqlanadi.
    /// </summary>
    public sealed class GameSettings
    {
        public bool ShowGrid { get; set; } = false;
        public bool EdgeScroll { get; set; } = false;
        public float MusicVolume { get; set; } = 0.6f;
        public float SfxVolume { get; set; } = 0.8f;

        public static GameSettings Default => new GameSettings();
    }
}
