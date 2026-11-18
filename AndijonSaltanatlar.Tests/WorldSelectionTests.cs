using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using AndijonSaltanatlar.Server.Authentication;
using AndijonSaltanatlar.Server.Data;
using AndijonSaltanatlar.Server.Services;
using AndijonSaltanatlar.Shared.DTOs;

namespace AndijonSaltanatlar.Tests
{
    /// <summary>PROMPT 26: DUNYONI TANLASH — dunyo ro'yxati, a'zolik va qaytgan o'yinchining saltanati.</summary>
    public class WorldSelectionTests : IDisposable
    {
        private readonly SqliteConnection _conn;
        private readonly OnlineGameService _svc;

        public WorldSelectionTests()
        {
            _conn = new SqliteConnection("Data Source=:memory:");
            _conn.Open();
            using GameDbContext db = NewDb();
            db.Database.EnsureCreated();
            _svc = new OnlineGameService(NewDb(), new SystemClock());
        }

        public void Dispose()
        {
            _svc.Db.Dispose();
            _conn.Dispose();
        }

        private GameDbContext NewDb() => new GameDbContext(new DbContextOptionsBuilder<GameDbContext>().UseSqlite(_conn).Options);

        private AppUser NewPlayer(string sub)
        {
            using GameDbContext db = NewDb();
            return GoogleLoginService.FindOrCreateUser(db, new GoogleIdentity
            {
                Sub = sub, Email = sub + "@gmail.com", Name = "Ali", EmailVerified = true
            });
        }

        [Fact]
        public async Task Worlds_ListsPlayersAndOwnKingdom_SameAccountSameKingdom()
        {
            AppUser a = NewPlayer("world-a");
            AppUser b = NewPlayer("world-b");
            Kingdom ka = await _svc.JoinAsync(a.Id, new JoinWorldRequest { WorldId = 1, ChooseStart = true });
            Kingdom kb = await _svc.JoinAsync(b.Id, new JoinWorldRequest { WorldId = 1, ChooseStart = true });

            WorldsResponse wa = await _svc.WorldsAsync(a.Id);
            Assert.NotNull(wa.MyKingdomId);
            Assert.Equal(ka.Id, wa.MyKingdomId);
            Assert.Equal(1, wa.MyWorldId);
            Assert.Single(wa.Worlds);
            Assert.True(wa.Worlds[0].IsMine);
            Assert.True(wa.Worlds[0].PlayerCount >= 2);
            Assert.True(wa.Worlds[0].FreeRegionCount > 0);

            // Boshqa qurilmada qayta kirish (yoki qayta so'rov) yangi saltanat yaratmaydi.
            Kingdom again = await _svc.JoinAsync(a.Id, new JoinWorldRequest { WorldId = 1, ChooseStart = true });
            Assert.Equal(ka.Id, again.Id);
            Assert.NotEqual(ka.Id, kb.Id);
            Assert.Equal(2, _svc.Db.Kingdoms.Count(k => !k.IsAi && k.OwnerUserId != null));
        }

        [Fact]
        public async Task NewPlayer_ChoosesWorldThenFreeRegion_AndCannotTakeItTwice()
        {
            AppUser a = NewPlayer("start-a");
            Kingdom ka = await _svc.JoinAsync(a.Id, new JoinWorldRequest { WorldId = 2, ChooseStart = true });
            Assert.Equal(2, ka.WorldId);
            Assert.True((await _svc.StateAsync(ka)).NeedsHome);

            StartRegionsDto starts = await _svc.StartRegionsAsync(ka);
            StartRegionDto free = starts.Regions.First(r => r.Free);
            Assert.Null(await _svc.ChooseStartAsync(ka, new ChooseStartRequest
            {
                RegionId = free.RegionId, KingdomName = "Asaka sultonligi"
            }));
            Assert.False((await _svc.StateAsync(ka)).NeedsHome);

            // Boshqa o'yinchi egallangan hududni ololmaydi.
            AppUser b = NewPlayer("start-b");
            Kingdom kb = await _svc.JoinAsync(b.Id, new JoinWorldRequest { WorldId = 2, ChooseStart = true });
            Assert.NotNull(await _svc.ChooseStartAsync(kb, new ChooseStartRequest { RegionId = free.RegionId }));

            WorldsResponse wb = await _svc.WorldsAsync(b.Id);
            Assert.Equal(2, wb.MyWorldId);
            Assert.Contains(wb.Worlds, w => w.Id == 2 && w.PlayerCount >= 2);
        }
    }
}
