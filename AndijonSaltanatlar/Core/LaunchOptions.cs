using System.Globalization;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar;

public sealed class LaunchOptions
{
	public string ScreenshotPath;

	public string ExportSpritesDir;

	public Vector2? CenterTile;

	public float? Zoom;

	public string BuildType;

	public Point? HoverTile;

	public Point? SelectTile;

	public float? Time;

	public string Tab;

	public int? SimDays;

	public int? PopTax;

	public int? Food;

	public bool Map;

	public int? AttackRegion;

	public bool Civ;

	public int? Techs;

	public string ResearchId;

	public bool Menu;

	public bool Advisor;

	public bool Governor;

	public string Plan;

	public string WeatherName;

	public bool ShowLevels;

	public bool Story;

	public bool QuestJournal;

	public string SmokeLog;

	// --- PROMPT 19A: online sinovlari uchun ---
	public bool Online;
	public string OnlineServer;
	public string OnlineUser;
	public string OnlinePass;
	public bool OnlineRegister;
	public string OnlineTab;
	/// <summary>Skrinshotni shuncha kadrdan keyin olish (server javobini kutish uchun).</summary>
	public int? ScreenshotFrames;

	/// <summary>Oyna o'lchami (masalan 854x480 — telefon ekranini sinash uchun).</summary>
	public Point? WindowSize;

	public static LaunchOptions Parse(string[] args)
	{
		LaunchOptions launchOptions = new LaunchOptions();
		for (int i = 0; i < args.Length; i++)
		{
			string text = ((i + 1 < args.Length) ? args[i + 1] : null);
			switch (args[i])
			{
			case "--screenshot":
				launchOptions.ScreenshotPath = text;
				i++;
				break;
			case "--export-sprites":
				launchOptions.ExportSpritesDir = text;
				i++;
				break;
			case "--zoom":
				launchOptions.Zoom = float.Parse(text, CultureInfo.InvariantCulture);
				i++;
				break;
			case "--time":
				launchOptions.Time = float.Parse(text, CultureInfo.InvariantCulture);
				i++;
				break;
			case "--tab":
				launchOptions.Tab = text;
				i++;
				break;
			case "--simdays":
				launchOptions.SimDays = int.Parse(text, CultureInfo.InvariantCulture);
				i++;
				break;
			case "--poptax":
				launchOptions.PopTax = int.Parse(text, CultureInfo.InvariantCulture);
				i++;
				break;
			case "--food":
				launchOptions.Food = int.Parse(text, CultureInfo.InvariantCulture);
				i++;
				break;
			case "--map":
				launchOptions.Map = true;
				break;
			case "--attack":
				launchOptions.AttackRegion = int.Parse(text, CultureInfo.InvariantCulture);
				i++;
				break;
			case "--civ":
				launchOptions.Civ = true;
				break;
			case "--techs":
				launchOptions.Techs = int.Parse(text, CultureInfo.InvariantCulture);
				i++;
				break;
			case "--research":
				launchOptions.ResearchId = text;
				i++;
				break;
			case "--menu":
				launchOptions.Menu = true;
				break;
			case "--advisor":
				launchOptions.Advisor = true;
				break;
			case "--governor":
				launchOptions.Governor = true;
				break;
			case "--plan":
				launchOptions.Plan = text;
				i++;
				break;
			case "--weather":
				launchOptions.WeatherName = text;
				i++;
				break;
			case "--showlevels":
				launchOptions.ShowLevels = true;
				break;
			case "--story":
				launchOptions.Story = true;
				break;
			case "--quest":
				launchOptions.QuestJournal = true;
				break;
			case "--smoke":
				launchOptions.SmokeLog = text ?? "smoke_result.txt";
				if (text != null)
				{
					i++;
				}
				break;
			case "--online":
				launchOptions.Online = true;
				break;
			case "--online-server":
				launchOptions.OnlineServer = text;
				launchOptions.Online = true;
				i++;
				break;
			case "--online-user":
				launchOptions.OnlineUser = text;
				i++;
				break;
			case "--online-pass":
				launchOptions.OnlinePass = text;
				i++;
				break;
			case "--online-register":
				launchOptions.OnlineRegister = true;
				break;
			case "--online-tab":
				launchOptions.OnlineTab = text;
				i++;
				break;
			case "--size":
			{
				string[] wh = (text ?? "1280x720").Split('x');
				launchOptions.WindowSize = new Point(int.Parse(wh[0], CultureInfo.InvariantCulture), int.Parse(wh[1], CultureInfo.InvariantCulture));
				i++;
				break;
			}
			case "--frames":
				launchOptions.ScreenshotFrames = int.Parse(text, CultureInfo.InvariantCulture);
				i++;
				break;
			case "--build":
				launchOptions.BuildType = text;
				i++;
				break;
			case "--center":
			{
				Vector2 value = ParsePair(text);
				launchOptions.CenterTile = value;
				i++;
				break;
			}
			case "--hover":
			{
				Vector2 vector2 = ParsePair(text);
				launchOptions.HoverTile = new Point((int)vector2.X, (int)vector2.Y);
				i++;
				break;
			}
			case "--select":
			{
				Vector2 vector = ParsePair(text);
				launchOptions.SelectTile = new Point((int)vector.X, (int)vector.Y);
				i++;
				break;
			}
			}
		}
		return launchOptions;
	}

	private static Vector2 ParsePair(string s)
	{
		string[] array = (s ?? "0,0").Split(',');
		return new Vector2(float.Parse(array[0], CultureInfo.InvariantCulture), float.Parse(array[1], CultureInfo.InvariantCulture));
	}
}
