using System;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.CityPlanning;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Blueprints
{
    /// <summary>
    /// Joriy blueprint (preview) holati va tahrir amallari. Reja faqat shu yerda o'zgaradi;
    /// GameState'ga tegmaydi va tasdiqlanmaguncha hech narsa qurilmaydi.
    /// </summary>
    public sealed class BlueprintManager
    {
        public const int RoadTileCost = CityPlanningManager.RoadTileCost;

        private readonly ICityPlanningGrid _grid;
        private readonly Func<int> _treasury;
        private readonly Func<BuildingType, bool> _isUnlocked;

        public CityPlan Plan { get; private set; }
        public int SelectedItemId { get; private set; }
        public string LastMessageUz { get; private set; } = "";

        public BlueprintManager(ICityPlanningGrid grid, Func<int> treasury,
            Func<BuildingType, bool> isUnlocked)
        {
            _grid = grid; _treasury = treasury; _isUnlocked = isUnlocked;
        }

        public bool HasPlan => Plan != null;

        public BlueprintItem SelectedItem
            => Plan?.Buildings.Find(b => b.Id == SelectedItemId);

        public void SetPlan(CityPlan plan)
        {
            Plan = plan;
            SelectedItemId = 0;
            LastMessageUz = plan == null ? "" : "Yangi blueprint tayyor.";
        }

        /// <summary>Save'dan blueprint va tanlangan elementni tiklaydi (qayta generatsiya qilmaydi).</summary>
        public void Restore(CityPlan plan, int selectedItemId)
        {
            Plan = plan;
            SelectedItemId = plan != null && plan.Buildings.Exists(b => b.Id == selectedItemId)
                ? selectedItemId : 0;
            LastMessageUz = plan == null ? "" : "Saqlangan blueprint yuklandi.";
        }

        public void Clear()
        {
            Plan = null;
            SelectedItemId = 0;
            LastMessageUz = "";
        }

        public bool SelectItem(int id)
        {
            if (Plan == null || !Plan.Buildings.Exists(b => b.Id == id)) return false;
            SelectedItemId = id;
            LastMessageUz = "Element tanlandi.";
            return true;
        }

        public bool SelectAt(Point tile)
        {
            if (Plan == null) return false;
            BlueprintItem item = Plan.ItemAt(tile.X, tile.Y);
            if (item == null) return false;
            SelectedItemId = item.Id;
            LastMessageUz = Describe(item);
            return true;
        }

        /// <summary>Bino ma'lumotini bir qatorli xabar shaklida qaytaradi (map bosilganda).</summary>
        public static string Describe(BlueprintItem item)
        {
            if (item == null) return "";
            string status = item.Status == BlueprintStatus.Blocked ? "QURILMAYDI" :
                            item.Status == BlueprintStatus.NeedsResource ? "RESURS KERAK" : "QURILADI";
            string text = $"{item.NameUz}: {item.Cost} tanga, {item.Size}x{item.Size}, {item.BuildTimeDays} kun. " +
                          $"{status}. Foyda: {item.BenefitUz}. Talab: {item.RequirementUz}.";
            if (!string.IsNullOrEmpty(item.StatusReasonUz)) text += " Sabab: " + item.StatusReasonUz;
            return text;
        }

        public bool RemoveSelected()
        {
            if (Plan == null || SelectedItemId == 0) return false;
            bool ok = BlueprintEditor.RemoveBuilding(Plan, SelectedItemId, _grid, _isUnlocked,
                _treasury(), RoadTileCost);
            if (ok)
            {
                SelectedItemId = 0;
                LastMessageUz = "Element rejadan olib tashlandi va narx qayta hisoblandi.";
            }
            else LastMessageUz = "Elementni olib tashlab bo'lmadi.";
            return ok;
        }

        public bool AddBuilding(BuildingType kind, Point origin)
        {
            if (Plan == null) return false;
            bool ok = BlueprintEditor.AddBuilding(Plan, kind, origin, _grid, _isUnlocked,
                _treasury(), RoadTileCost);
            if (ok)
            {
                BlueprintItem added = Plan.Buildings.Find(b => b.Tile == origin && b.Kind == kind);
                SelectedItemId = added?.Id ?? SelectedItemId;
                LastMessageUz = added != null ? "Rejaga qo'shildi: " + added.NameUz + "." : "Bino qo'shildi.";
            }
            else LastMessageUz = "Bu joyga bino qo'shib bo'lmaydi (band, yopiq yoki yo'lga ulanmagan).";
            return ok;
        }

        public bool MoveSelected(Point newOrigin)
        {
            if (Plan == null || SelectedItemId == 0) return false;
            bool ok = BlueprintEditor.MoveBuilding(Plan, SelectedItemId, newOrigin, _grid, _isUnlocked,
                _treasury(), RoadTileCost);
            LastMessageUz = ok
                ? "Bino joyi o'zgartirildi; reja qayta tekshirildi."
                : "Bu joyga ko'chirib bo'lmaydi — reja o'zgartirilmadi.";
            return ok;
        }

        public bool ToggleRoad(Point tile)
        {
            if (Plan == null) return false;
            bool ok = BlueprintEditor.ToggleRoad(Plan, tile, _grid, _isUnlocked, _treasury(), RoadTileCost);
            LastMessageUz = ok
                ? "Yo'l rejasi o'zgartirildi va qayta tekshirildi."
                : "Bu katakda yo'l o'zgarishi rejani buzadi — qaytarildi.";
            return ok;
        }

        public void Recalculate()
        {
            if (Plan == null) return;
            BlueprintEditor.Revalidate(Plan, _grid, _isUnlocked, _treasury(), RoadTileCost);
            LastMessageUz = Plan.IsValid ? "Reja yaroqli." : "Reja hozircha to'liq yaroqli emas.";
        }
    }
}
