using System;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Environment;
using AndijonSaltanatlar.World;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Water
{
    /// <summary>
    /// Suv ta'minoti boshqaruvi: quduq qurish, kunlik yangilanish va panel uchun statistika.
    /// Barcha raqamlar haqiqiy manbalar va binolardan hisoblanadi.
    /// </summary>
    public sealed class WaterSupplyManager
    {
        public const int WellCost = 30;

        private readonly MapManager _map;
        private readonly GameStateManager _state;
        private readonly BuildingManager _buildings;

        public WaterNetworkManager Network { get; }
        public IrrigationSystem Irrigation { get; }

        public WaterSupplyManager(MapManager map, GameStateManager state, BuildingManager buildings)
        {
            _map = map;
            _state = state;
            _buildings = buildings;
            Network = new WaterNetworkManager(map, buildings);
            Irrigation = new IrrigationSystem(Network);
        }

        // ==================================================================
        //  QUDUQ QURISH
        // ==================================================================

        public bool CanPlaceWell(int x, int y, out string reason)
        {
            reason = "";
            if (!_map.InBounds(x, y)) { reason = "Xarita chetida"; return false; }
            TileType t = _map.GetTile(x, y);
            if (t != TileType.Ground && t != TileType.Grass)
            {
                reason = "Quduq faqat bo'sh yerga quriladi";
                return false;
            }
            if (_map.GetOccupant(x, y) != null) { reason = "Katak band"; return false; }
            if (_state != null && !_state.Land.IsBuildable(x, y)) { reason = "Yer qulflangan"; return false; }
            if (_state != null && !_state.CanAfford(WellCost)) { reason = "Tanga yetarli emas (" + WellCost + ")"; return false; }
            return true;
        }

        /// <summary>Quduq qurish: tanga bir marta ayiriladi, suv tarmog'i qayta hisoblanadi.</summary>
        public bool TryPlaceWell(int x, int y, out string message)
        {
            if (!CanPlaceWell(x, y, out message)) return false;
            Decoration d = _map.AddDecoration(DecorationType.Well, x, y);
            if (d == null) { message = "Quduq qo'yilmadi"; return false; }
            if (_state != null) _state.Coins -= WellCost;
            Network.Rebuild();
            message = "Quduq qurildi (-" + WellCost + " tanga). Mahalla suv ta'minoti yaxshilandi.";
            return true;
        }

        public bool RemoveWell(Decoration d)
        {
            if (d == null || d.Type != DecorationType.Well) return false;
            _map.RemoveDecoration(d);
            Network.Rebuild();
            return true;
        }

        // ==================================================================
        //  STATISTIKA (panel uchun)
        // ==================================================================

        public int SourceCount => Network.Sources.Count;
        public float TotalSupply => Network.TotalSupply;
        public float TotalDemand => Network.TotalDemand;
        public int ServedBuildings => Network.ServedBuildings;
        public int ConsumingBuildings => Network.ConsumingBuildings;
        public float AverageCoverage => Network.AverageCoverage;
        public bool HasShortage => Network.HasShortage;

        public float SupplyCoveragePercent
            => TotalDemand <= 0.01f ? 100f : Math.Clamp(TotalSupply / TotalDemand * 100f, 0f, 100f);

        public int ServedPopulation => Network.ServedPopulation(_state?.Pop);

        public int IrrigatedGardens => Irrigation.IrrigatedGardens(_buildings);
        public int TotalGardens => Irrigation.TotalGardens(_buildings);

        public string WorstDistrictIssue()
        {
            int unserved = Network.UnservedBuildings;
            return unserved > 0 ? unserved + " bino suvsiz qolmoqda" : "Barcha binolar suv bilan ta'minlangan";
        }

        public void DayTick(Season season)
        {
            Network.SetSeason(season);
        }

        public void Rebuild() => Network.Rebuild();
    }
}
