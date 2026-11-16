using System.Collections.Generic;

namespace AndijonSaltanatlar.Shared.Commands
{
    /// <summary>
    /// Client serverga yuboradigan buyruq. Server uni tekshiradi va bajaradi (server-authoritative).
    /// Narx/resurs kabi muhim qiymatlar PAYLOADда bo'lsa ham server o'z ma'lumotidan hisoblaydi —
    /// clientga ishonilmaydi. CommandId idempotentlik uchun (takror bajarilmaydi).
    /// </summary>
    public sealed class GameCommand
    {
        public string CommandId { get; set; } = "";
        public AndijonSaltanatlar.Shared.CommandType Type { get; set; }
        public int KingdomId { get; set; }
        public int WorldId { get; set; }
        public long ExpectedStateVersion { get; set; }
        public Dictionary<string, string> Payload { get; set; } = new();
    }

    /// <summary>Buyruq natijasi.</summary>
    public sealed class CommandResult
    {
        public bool Success { get; set; }
        public string MessageUz { get; set; } = "";
        public long StateVersion { get; set; }

        public static CommandResult Ok(long version, string msg = "") => new() { Success = true, StateVersion = version, MessageUz = msg };
        public static CommandResult Fail(string msg) => new() { Success = false, MessageUz = msg };
    }
}
