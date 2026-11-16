using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AndijonSaltanatlar.Server.Hubs
{
    /// <summary>
    /// Real-vaqt yangilanishlar (SignalR). Butun xarita emas, faqat o'zgarishlar yuboriladi.
    /// Client dunyoga qo'shiladi (JoinWorld) va shu dunyo guruhiga tegishli xabarlarni oladi.
    ///
    /// Server → client xabarlari:
    ///   "MarketChanged"  (worldId)        — bozorda taklif qo'shildi/sotildi/bekor qilindi;
    ///   "StateChanged"   (kingdomId, ver) — shu foydalanuvchi saltanati holati o'zgardi (masalan guruchi sotildi);
    ///   "PlayersChanged" (worldId)        — dunyoga kimdir kirdi/chiqdi.
    /// Client hech qachon hub orqali holat o'zgartirmaydi — faqat REST buyruqlar (server tekshiradi).
    /// </summary>
    [Authorize]
    public class GameHub : Hub
    {
        private static readonly ConcurrentDictionary<string, (int world, string name)> Connections = new();

        public static string WorldGroup(int worldId) => "world-" + worldId;

        public static List<string> OnlinePlayers(int worldId)
            => Connections.Values.Where(v => v.world == worldId).Select(v => v.name).Distinct().OrderBy(n => n).ToList();

        public async Task JoinWorld(int worldId)
        {
            string name = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "?";
            Connections[Context.ConnectionId] = (worldId, name);
            await Groups.AddToGroupAsync(Context.ConnectionId, WorldGroup(worldId));
            await Clients.Group(WorldGroup(worldId)).SendAsync("PlayersChanged", worldId);
        }

        public async Task LeaveWorld(int worldId)
        {
            Connections.TryRemove(Context.ConnectionId, out _);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, WorldGroup(worldId));
            await Clients.Group(WorldGroup(worldId)).SendAsync("PlayersChanged", worldId);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (Connections.TryRemove(Context.ConnectionId, out var info))
                await Clients.Group(WorldGroup(info.world)).SendAsync("PlayersChanged", info.world);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
