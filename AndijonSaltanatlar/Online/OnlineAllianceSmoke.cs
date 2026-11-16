using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using AndijonSaltanatlar.Shared.DTOs;

namespace AndijonSaltanatlar.Online
{
    /// <summary>
    /// PROMPT 20 — uchta vertikal zanjir o'yin clientining AYNAN o'sha kodi (OnlineClient: REST buyruqlar + SignalR) orqali,
    /// ikki client bilan (birinchisi — Windows o'yinchisi, ikkinchisi — Android o'yinchisi o'rnida):
    ///  1) ittifoq taklifi → SignalR xabarnomasi → qabul qilish → ikkala clientda ittifoq → qayta ulanish;
    ///  2) qo'shma loyiha: yog'och + tanga hissalari → yakunlanish;
    ///  3) hudud kelishuvi → qabul → ikkala clientda xarita yangilanadi.
    /// Ishga tushirish: AndijonSaltanatlar.exe --alliance-smoke natija.txt --online-server http://localhost:5080
    /// </summary>
    public static class OnlineAllianceSmoke
    {
        public static string Run(string server)
        {
            var sb = new StringBuilder();
            int pass = 0, fail = 0;
            void Check(string name, bool ok, string detail = "")
            {
                if (ok) pass++; else fail++;
                sb.AppendLine((ok ? "PASS" : "FAIL") + " | " + name + (detail.Length > 0 ? "  [" + detail + "]" : ""));
            }
            string tag = DateTime.Now.ToString("HHmmss");
            int world = 300 + (int.Parse(tag) % 90000); // toza dunyo: mustaqil hududlar bo'sh
            sb.AppendLine("ALLIANCE SMOKE - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - " + server + " - dunyo " + world);

            var win = new OnlineClient();
            var and = new OnlineClient();
            win.Settings.WorldId = world;
            and.Settings.WorldId = world;

            bool Wait(OnlineClient c, Func<bool> cond, int seconds)
            {
                DateTime end = DateTime.UtcNow.AddSeconds(seconds);
                while (DateTime.UtcNow < end)
                {
                    c.Pump();
                    if (!c.Busy && cond()) return true;
                    Thread.Sleep(100);
                }
                c.Pump();
                return cond();
            }
            void Idle(OnlineClient c) => Wait(c, () => true, 20);
            bool Refresh(OnlineClient c, Func<bool> cond, int seconds)
            {
                DateTime end = DateTime.UtcNow.AddSeconds(seconds);
                while (DateTime.UtcNow < end)
                {
                    c.QueueRefresh();
                    if (Wait(c, cond, 2)) return true;
                }
                return cond();
            }

            try
            {
                // ---------------- 1-ZANJIR: ittifoq ----------------
                win.Login(server, "win_a" + tag, "Parol123!", register: true);
                and.Login(server, "and_a" + tag, "Parol123!", register: true);
                Check("Windows client dunyoga kirdi", Wait(win, () => win.LoggedIn && win.HubConnected, 25), win.Message);
                Check("Android client xuddi shu dunyoga kirdi", Wait(and, () => and.LoggedIn && and.HubConnected, 25) && and.State.WorldId == win.State.WorldId, "dunyo " + and.State?.WorldId);
                int andKingdom = and.State.KingdomId;
                bool seen = Refresh(win, () => win.Kingdom != null && win.Kingdom.Relations.Any(r => r.KingdomId == andKingdom && !r.IsAi), 15);
                Check("Windows o'yinchisi Android saltanatini ko'radi", seen, and.State.KingdomName);

                win.CreateAlliance("Vodiy ittifoqi " + tag, 1); Idle(win);
                bool created = Refresh(win, () => win.Alliance != null, 10);
                Check("Ittifoq yaratildi (server saqladi)", created, win.Message);

                and.Notification = null;
                win.InviteAlliance(win.Alliance?.Id ?? 0, andKingdom); Idle(win);
                Check("Ittifoq taklifi yuborildi", !win.MessageIsError, win.Message);
                bool notified = Wait(and, () => and.Notification != null && and.Notification.Contains("taklif"), 10);
                Check("Android SignalR orqali taklif xabarnomasini oldi", notified, and.Notification ?? "");
                bool hasInv = Refresh(and, () => and.Invitations.Count > 0, 10);
                Check("Android takliflar ro'yxatida taklif bor", hasInv, hasInv ? and.Invitations[0].AllianceName : "");

                win.Notification = null;
                and.RespondInvitation(and.Invitations.FirstOrDefault()?.Id ?? 0, accept: true); Idle(and);
                Check("Android taklifni qabul qildi", !and.MessageIsError, and.Message);
                Check("Windows SignalR orqali 'qo'shildi' xabarini oldi", Wait(win, () => win.Notification != null && win.Notification.Contains("qo'shildi"), 10), win.Notification ?? "");
                bool bothSee = Refresh(win, () => win.Alliance?.Members.Count == 2, 10) && Refresh(and, () => and.Alliance?.Members.Count == 2, 10);
                Check("Ikkala clientda ittifoq 2 a'zo bilan ko'rinadi", bothSee, (win.Alliance?.Name ?? "") + " / " + (and.Alliance?.Name ?? ""));

                // Qayta ulanish: Android chiqib, qayta kiradi — ittifoq serverda saqlangan.
                and.Logout();
                Thread.Sleep(1500);
                and.Login(server, "and_a" + tag, "Parol123!", register: false);
                bool back = Wait(and, () => and.LoggedIn, 20) && Refresh(and, () => and.Alliance != null && and.Alliance.Members.Count == 2, 15);
                Check("Qayta ulanishdan keyin ittifoq saqlangan", back, and.Alliance?.Name ?? "yo'q");

                // ---------------- 2-ZANJIR: qo'shma loyiha ----------------
                int aid = win.Alliance.Id;
                win.CreateAllianceProject(aid, 0); Idle(win);
                bool projSeen = Refresh(and, () => and.Alliance?.Projects?.Any(p => p.Status == 0) == true, 15);
                Check("Android yangi qo'shma loyihani ko'radi (Savdo yo'li)", projSeen, win.Message);
                AllianceProjectDto proj = and.Alliance.Projects.First(p => p.Status == 0);
                int woodBefore = win.State.Inventory.TryGetValue("Wood", out int w0) ? w0 : 0;
                win.ContributeAlliance(aid, proj.Id, "Wood", proj.ReqWood); Idle(win);
                Check("Windows yog'och ajratdi", !win.MessageIsError && win.State.Inventory["Wood"] == woodBefore - proj.ReqWood, win.Message + " (yog'och " + woodBefore + " -> " + win.State.Inventory["Wood"] + ")");
                int coinBefore = and.State.Coin;
                and.ContributeAlliance(aid, proj.Id, "Coin", proj.ReqCoin); Idle(and);
                Check("Android tanga ajratdi", !and.MessageIsError && and.State.Coin == coinBefore - proj.ReqCoin, and.Message + " (tanga " + coinBefore + " -> " + and.State.Coin + ")");
                bool done = Refresh(win, () => win.Alliance?.Projects?.Any(p => p.Id == proj.Id && p.Status == 1) == true, 15)
                            && Refresh(and, () => and.Alliance?.Projects?.Any(p => p.Id == proj.Id && p.Status == 1) == true, 15);
                Check("Loyiha yakunlandi — ikkala client natijani ko'radi", done);

                // ---------------- 3-ZANJIR: hudud kelishuvi ----------------
                RegionDto free = win.Kingdom.Regions.Where(r => r.OwnerKingdomId == 0 && r.CanAnnex).OrderBy(r => r.AnnexCost).FirstOrDefault();
                win.Annex(free?.Id ?? 0); Idle(win);
                bool owns = Refresh(win, () => win.Kingdom.Regions.Any(r => r.Id == free.Id && r.IsMine), 10);
                Check("Windows ikkinchi hududga ega bo'ldi (test uchun)", owns, free?.Name ?? "bo'sh hudud yo'q");
                int regionId = free.Id;

                and.Notification = null;
                win.ProposeAgreement(2, andKingdom, regionId, 100); Idle(win);
                Check("Hudud bo'yicha kelishuv taklifi yuborildi", !win.MessageIsError, win.Message);
                Check("Android kelishuv xabarnomasini oldi", Wait(and, () => and.Notification != null && and.Notification.Contains("kelishuv"), 10), and.Notification ?? "");
                bool agSeen = Refresh(and, () => and.Agreements.Any(a => a.Type == 2 && a.Status == 0 && a.ToKingdomId == andKingdom), 10);
                Check("Android kelishuvni ko'radi", agSeen);
                int agId = and.Agreements.First(a => a.Type == 2 && a.Status == 0).Id;
                int winTerritorySerial = win.TerritorySerial;
                and.RespondAgreement(agId, accept: true); Idle(and);
                Check("Android kelishuvni qabul qildi (server shartlarni tekshirdi)", !and.MessageIsError, and.Message);
                Check("Windows SignalR orqali hudud transferi xabarini oldi", Wait(win, () => win.TerritorySerial > winTerritorySerial, 10), win.Notification ?? "");
                bool mapAnd = Refresh(and, () => and.Kingdom.Regions.Any(r => r.Id == regionId && r.IsMine), 10);
                bool mapWin = Refresh(win, () => win.Kingdom.Regions.Any(r => r.Id == regionId && !r.IsMine && r.OwnerKingdomId == andKingdom), 10);
                Check("Ikkala client xaritasida hudud egasi yangilandi", mapAnd && mapWin, free.Name + " -> " + and.State.KingdomName);
                Check("Shahar va aholi Android saltanatiga o'tdi", Refresh(and, () => and.Kingdom.CityCount == 2, 10), "shaharlar " + and.Kingdom.CityCount + ", aholi " + and.Kingdom.Population);
                bool news = Refresh(win, () => win.News.Any(n => n.Type == "agreement"), 10);
                Check("Dunyo yangiliklarida kelishuv bor", news, news ? win.News.First(n => n.Type == "agreement").TextUz : "");
            }
            catch (Exception ex)
            {
                Check("Kutilmagan xatosiz", false, ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                win.Dispose();
                and.Dispose();
            }
            sb.AppendLine("NATIJA: " + pass + " PASS / " + fail + " FAIL");
            return sb.ToString();
        }

        public static void RunToFile(string server, string path)
        {
            string text = Run(server);
            try { File.WriteAllText(path, text); } catch { }
            Console.WriteLine(text);
        }
    }
}
