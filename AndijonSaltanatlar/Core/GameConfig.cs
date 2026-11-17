namespace AndijonSaltanatlar;

public static class GameConfig
{
	public const string Title = "ANDIJON: SALTANATLAR";

	public const string CityName = "ASAKA";

	public const string RulerName = "Hokim Zahiriddin";

	public const int TileWidth = 64;

	public const int TileHeight = 32;

	public const int HalfTileW = 32;

	public const int HalfTileH = 16;

	/// <summary>Asl shahar bloki (64x64) — CityGenerator koordinatalari shu blok ichida yoziladi.</summary>
	public const int CityBlockSize = 64;

	/// <summary>Yer kengaytirish halqasining eni (tile). 1-halqa — shahar blokining o'zidagi 4 mahalla.</summary>
	public const int RingWidth = 10;

	/// <summary>Jami yer halqalari soni (1-halqa + tashqi halqalar). Har halqada 4 tomon (shimol, janub, g'arb, sharq).</summary>
	public const int MaxRings = 5;

	/// <summary>Shahar bloki xarita markazida turishi uchun siljish: tashqi halqalarga joy.</summary>
	public const int CityOffset = (MaxRings - 1) * RingWidth;

	/// <summary>To'liq xarita o'lchami: shahar bloki + har tomonda tashqi halqalar.</summary>
	public const int MapSize = CityBlockSize + 2 * CityOffset;

	/// <summary>Shahar markazi (tile).</summary>
	public const int CityCenter = CityOffset + 32;

	public const int WindowWidth = 1280;

	public const int WindowHeight = 720;

	public const float MinZoom = 0.4f;

	public const float MaxZoom = 2.5f;

	public const float StartZoom = 0.8f;

	public const int StartCoins = 500;

	public const int StartFood = 200;

	public const int WorldSeed = 1483;
}
