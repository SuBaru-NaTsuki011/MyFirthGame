using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.RoadNetwork;

namespace AndijonSaltanatlar.Infrastructure
{
    /// <summary>
    /// Ko'prik qurish. Ko'prik — suv ustidagi yo'l katagi: yo'l grafiga qo'shiladi, transportlar
    /// undan foydalanadi, marshrutlar shu yo'l orqali quriladi. Faqat suv ustiga quriladi.
    /// </summary>
    public sealed class BridgeManager
    {
        private readonly RoadNetworkManager _network;

        public BridgeManager(RoadNetworkManager network) => _network = network;

        public int BuildCost => RoadTypeInfo.BuildCost(RoadType.StoneBridge);

        public bool CanBuildAt(int x, int y, out string reason)
        {
            RoadBuildStatus status = _network.Validate(x, y, RoadType.StoneBridge, out reason);
            return status == RoadBuildStatus.Ok;
        }

        public bool TryBuild(int x, int y, out string message)
            => _network.TryBuild(x, y, RoadType.StoneBridge, out message);

        /// <summary>Ikki qirg'oq orasida ko'prik kerakmi (yo'l kesilgan joy).</summary>
        public bool NeedsBridgeAt(int x, int y)
        {
            if (_network.IsRoad(x, y)) return false;
            return _network.Validate(x, y, RoadType.Dirt, out _) == RoadBuildStatus.NeedsBridge;
        }
    }
}
