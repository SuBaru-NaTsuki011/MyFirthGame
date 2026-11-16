using System;
using AndijonSaltanatlar.Content;

namespace AndijonSaltanatlar;

public static class Program
{
	[STAThread]
	private static void Main(string[] args)
	{
		for (int i = 0; i < args.Length - 1; i++)
		{
			if (args[i] == "--export-story")
			{
				StoryContentLoader.ExportDefault(args[i + 1]);
				Console.WriteLine("Story kontenti eksport qilindi: " + args[i + 1]);
				return;
			}
		}
		// PROMPT 19A: online client zanjiri (oynasiz) — --online-smoke natija.txt --online-server http://IP:5080
		for (int j = 0; j < args.Length - 1; j++)
		{
			if (args[j] == "--alliance-smoke")
			{
				string server = "http://localhost:5080";
				for (int k = 0; k < args.Length - 1; k++) if (args[k] == "--online-server") server = args[k + 1];
				Online.OnlineAllianceSmoke.RunToFile(server, args[j + 1]);
				return;
			}
			if (args[j] == "--online-smoke")
			{
				string server = "http://localhost:5080";
				for (int k = 0; k < args.Length - 1; k++) if (args[k] == "--online-server") server = args[k + 1];
				Online.OnlineClientSmoke.RunToFile(server, args[j + 1]);
				return;
			}
		}
		using Game1 game = new Game1(LaunchOptions.Parse(args));
		game.Run();
	}
}
