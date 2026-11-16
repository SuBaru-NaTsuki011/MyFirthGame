using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Advisor;
using AndijonSaltanatlar.AIGovernor;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.CityPlanning;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Core.Graphics;
using AndijonSaltanatlar.Forecasting;
using AndijonSaltanatlar.LandExpansion;
using AndijonSaltanatlar.SmartConstruction;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AndijonSaltanatlar.UI
{
    /// <summary>Blueprint tahriri uchun xarita rejimi.</summary>
    public enum GovernorMapMode { None, MoveBuilding, AddBuilding, ToggleRoad }

    /// <summary>
    /// Donishmand Vazir paneli — PROMPT 15, 19-bo'lim. Beshta bo'lim: shahar tahlili, tavsiyalar,
    /// yangi reja (blueprint), qurilish navbati va rivojlanish hisoboti. Barcha xarajat/qurilish
    /// amallari aniq tugma bosish (foydalanuvchi tasdig'i) bilan boshlanadi.
    /// </summary>
    public sealed class AIGovernorPanel
    {
        private static readonly Color PanelBg = new Color(28, 24, 19, 246);
        private static readonly Color PanelBg2 = new Color(48, 40, 31, 248);
        private static readonly Color Border = new Color(150, 116, 66);
        private static readonly Color Gold = new Color(235, 200, 120);
        private static readonly Color Cream = new Color(234, 224, 202);
        private static readonly Color Muted = new Color(176, 160, 134);
        private static readonly Color Green = new Color(120, 214, 132);
        private static readonly Color Yellow = new Color(245, 205, 90);
        private static readonly Color Red = new Color(236, 112, 96);
        private static readonly Color Blue = new Color(110, 170, 245);

        private enum PanelTab { Analysis, Advice, Plan, Queue, Report }

        private sealed class UiButton
        {
            public Rectangle Rect;
            public string Label = "";
            public Action OnClick;
            public bool Primary;
            public bool Left;
            public bool Enabled = true;
            public Color TextColor = Cream;
        }

        private readonly AssetManager _assets;
        private readonly PixelFont _font;
        private readonly GameStateManager _state;
        private readonly AIGovernorManager _governor;
        private readonly Texture2D _px;

        public bool IsOpen { get; private set; }
        public GovernorMapMode PickMode { get; private set; } = GovernorMapMode.None;
        public Action OpenStateTab;
        /// <summary>Tanlangan yer uchastkasiga kamerani siljitish — Game1 ulaydi.</summary>
        public Action<LandParcel> FocusLandRequested;

        private PanelTab _tab = PanelTab.Analysis;
        private Rectangle _panel, _closeBtn, _modeBtn;
        private readonly Rectangle[] _tabRects = new Rectangle[5];
        private readonly List<UiButton> _buttons = new List<UiButton>();
        private int _selectedPlanItem;
        private BuildingType _pendingAddKind = BuildingType.House;

        public AIGovernorPanel(AssetManager assets, PixelFont font, GameStateManager state, AIGovernorManager governor)
        {
            _assets = assets; _font = font; _state = state; _governor = governor; _px = assets.Pixel;
        }

        public void Open()
        {
            IsOpen = true;
            _state.CancelBuild();
            _governor.Analyze();
            if (_governor.Blueprint != null) _governor.RevalidateBlueprint();
            if (_governor.Blueprint != null && _selectedPlanItem == 0)
            {
                _selectedPlanItem = _governor.Blueprint.Buildings.Count > 0 ? _governor.Blueprint.Buildings[0].Id : 0;
                if (_selectedPlanItem > 0) _governor.SelectBlueprintItem(_selectedPlanItem);
            }
        }

        public void Close() { IsOpen = false; }
        public void Toggle() { if (IsOpen) Close(); else Open(); }
        public void ShowPlan() { Open(); _tab = PanelTab.Plan; }
        public void ShowQueue() { Open(); _tab = PanelTab.Queue; }
        public void CancelMapMode() { PickMode = GovernorMapMode.None; }

        /// <summary>Xaritadagi bosishni blueprint tahririga uzatadi. Game1 chaqiradi.</summary>
        public void HandleMapClick(Point tile)
        {
            if (PickMode == GovernorMapMode.MoveBuilding)
            {
                BlueprintItem item = _governor.Blueprints.SelectedItem;
                if (item == null)
                {
                    PickMode = GovernorMapMode.None;
                    _state.ShowMessage("Avval reja ro'yxatidan element tanlang.", true);
                    return;
                }
                int off = (item.Size - 1) / 2;
                var origin = new Point(tile.X - off, tile.Y - off);
                if (_governor.MoveSelectedBlueprintItem(origin))
                {
                    PickMode = GovernorMapMode.None;
                    Open();
                    _tab = PanelTab.Plan;
                }
                return;
            }

            if (PickMode == GovernorMapMode.AddBuilding)
            {
                Building proto = BuildingFactory.Create(_pendingAddKind, Point.Zero, 0);
                int off = (proto.Size.X - 1) / 2;
                var origin = new Point(tile.X - off, tile.Y - off);
                if (_governor.AddBlueprintItem(_pendingAddKind, origin))
                {
                    PickMode = GovernorMapMode.None;
                    Open();
                    _tab = PanelTab.Plan;
                }
                return;
            }

            if (PickMode == GovernorMapMode.ToggleRoad)
            {
                _governor.ToggleBlueprintRoad(tile);
                return;
            }
        }

        // ------------------------------------------------------------------
        // Layout
        // ------------------------------------------------------------------
        private void Layout(int vw, int vh)
        {
            int pw = Math.Min(940, vw - 60);
            int ph = Math.Min(650, vh - 50);
            _panel = new Rectangle((vw - pw) / 2, (vh - ph) / 2, pw, ph);
            _closeBtn = new Rectangle(_panel.Right - 112, _panel.Y + 12, 96, 28);
            _modeBtn = new Rectangle(_panel.Right - 306, _panel.Y + 42, 288, 26);

            int tx = _panel.X + 18, ty = _panel.Y + 80;
            for (int i = 0; i < _tabRects.Length; i++)
                _tabRects[i] = new Rectangle(tx + i * 154, ty, 146, 28);

            _buttons.Clear();
            for (int i = 0; i < _tabRects.Length; i++)
            {
                PanelTab target = (PanelTab)i;
                _buttons.Add(new UiButton
                {
                    Rect = _tabRects[i],
                    Label = TabName(target),
                    OnClick = () => { _tab = target; }
                });
            }
            _buttons.Add(new UiButton { Rect = _closeBtn, Label = "YOPISH", OnClick = Close });
            _buttons.Add(new UiButton
            {
                Rect = _modeBtn, Label = "REJIMNI O'ZGARTIRISH", Primary = true,
                OnClick = () =>
                {
                    _governor.Permissions.CycleLevel();
                    _state.ShowMessage("Vazir rejimi: " + _governor.Permissions.ModeNameUz);
                }
            });

            int cy = _panel.Y + 118;
            switch (_tab)
            {
                case PanelTab.Analysis: LayoutAnalysis(cy); break;
                case PanelTab.Advice: LayoutAdvice(cy); break;
                case PanelTab.Plan: LayoutPlan(cy); break;
                case PanelTab.Queue: LayoutQueue(cy); break;
                case PanelTab.Report: LayoutReport(cy); break;
            }
        }

        private static string TabName(PanelTab t) => t switch
        {
            PanelTab.Analysis => "TAHLIL",
            PanelTab.Advice => "TAVSIYALAR",
            PanelTab.Plan => "YANGI REJA",
            PanelTab.Queue => "NAVBAT",
            _ => "HISOBOT"
        };

        private void LayoutAnalysis(int cy)
        {
            int bottom = _panel.Bottom - 62;
            _buttons.Add(new UiButton
            {
                Rect = new Rectangle(_panel.X + 20, bottom, 170, 28), Label = "QAYTA TAHLIL",
                OnClick = () => { _governor.Analyze(); _state.ShowMessage("Shahar tahlili yangilandi."); }
            });
            _buttons.Add(new UiButton
            {
                Rect = new Rectangle(_panel.X + 200, bottom, 150, 28), Label = "TAVSIYALAR",
                Primary = true, OnClick = () => { _tab = PanelTab.Advice; }
            });
            LandParcel land = FirstRecommendedParcel();
            _buttons.Add(new UiButton
            {
                Rect = new Rectangle(_panel.X + 360, bottom, 180, 28), Label = "YERNI KO'RSAT",
                Enabled = land != null,
                OnClick = () =>
                {
                    LandParcel parcel = FirstRecommendedParcel();
                    if (parcel == null) { _state.ShowMessage("Ochiladigan uchastka topilmadi.", true); return; }
                    FocusLand(parcel);
                }
            });
            _buttons.Add(new UiButton
            {
                Rect = new Rectangle(_panel.Right - 230, bottom, 210, 28), Label = "YANGI REJA",
                Primary = true, OnClick = () => { _tab = PanelTab.Plan; }
            });
        }

        private void LayoutAdvice(int cy)
        {
            List<Recommendation> recs = _governor.Recommendations ?? new List<Recommendation>();
            int shown = Math.Min(4, recs.Count);
            for (int i = 0; i < shown; i++)
            {
                Recommendation rec = recs[i];
                int cardY = cy + i * 88;
                _buttons.Add(new UiButton
                {
                    Rect = new Rectangle(_panel.X + 478, cardY + 8, 190, 26),
                    Label = rec.ActionLabelUz,
                    Primary = true,
                    OnClick = BuildRecommendationAction(rec)
                });
            }
            _buttons.Add(new UiButton
            {
                Rect = new Rectangle(_panel.X + 20, _panel.Bottom - 62, 190, 28), Label = "TAHLILGA QAYTISH",
                OnClick = () => { _tab = PanelTab.Analysis; }
            });
            _buttons.Add(new UiButton
            {
                Rect = new Rectangle(_panel.X + 222, _panel.Bottom - 62, 190, 28), Label = "REJA TUZISH",
                Primary = true, OnClick = () => { _tab = PanelTab.Plan; }
            });
        }

        private Action BuildRecommendationAction(Recommendation rec)
        {
            if (rec.Action == RecommendationAction.UnlockLand)
                return () =>
                {
                    LandParcel parcel = FirstRecommendedParcel();
                    if (parcel == null)
                    {
                        _state.ShowMessage("Ochiladigan uchastka topilmadi.", true);
                        return;
                    }
                    FocusLand(parcel);
                };
            if (rec.Action == RecommendationAction.ReviewTaxes)
                return () =>
                {
                    Close();
                    OpenStateTab?.Invoke();
                    _state.ShowMessage("DAVLAT bo'limida soliq va xarajatlarni ko'rib chiqing.");
                };
            return () =>
            {
                DistrictType type = rec.SuggestedDistrict ??
                    (rec.Action == RecommendationAction.BuildMarket ? DistrictType.Commercial :
                     rec.Action == RecommendationAction.BuildGarden ? DistrictType.Garden :
                     rec.Action == RecommendationAction.BuildMadrasa ? DistrictType.Cultural :
                     DistrictType.Residential);
                if (_governor.GeneratePlan(type) != null)
                {
                    _selectedPlanItem = FirstPlanItemId();
                    _tab = PanelTab.Plan;
                }
            };
        }

        private void LayoutPlan(int cy)
        {
            if (_governor.Blueprint == null)
            {
                DistrictType[] types =
                {
                    DistrictType.Residential, DistrictType.Commercial, DistrictType.Craft,
                    DistrictType.Cultural, DistrictType.Agricultural, DistrictType.Garden,
                    DistrictType.Mixed, DistrictType.Mixed
                };
                string[] labels =
                {
                    "TURAR JOY", "SAVDO", "HUNARMANDCHILIK", "MADANIYAT",
                    "DEHQONCHILIK", "BOG' VA DAM", "MUVOZANATLI", "MAVJUDNI YAXSHILASH"
                };
                for (int i = 0; i < types.Length; i++)
                {
                    int col = i % 2, row = i / 2;
                    DistrictType t = types[i];
                    bool improve = i == types.Length - 1;
                    _buttons.Add(new UiButton
                    {
                        Rect = new Rectangle(_panel.X + 40 + col * 230, cy + 44 + row * 54, 214, 40),
                        Label = labels[i], Primary = true,
                        OnClick = () =>
                        {
                            CityPlan made = improve ? _governor.GenerateImprovementPlan() : _governor.GeneratePlan(t);
                            if (made != null)
                            {
                                _selectedPlanItem = FirstPlanItemId();
                                _tab = PanelTab.Plan;
                            }
                        }
                    });
                }
                _buttons.Add(new UiButton
                {
                    Rect = new Rectangle(_panel.X + 20, _panel.Bottom - 62, 190, 28), Label = "TAVSIYALAR",
                    OnClick = () => { _tab = PanelTab.Advice; }
                });
                return;
            }

            BlueprintItem selected = _governor.Blueprints.SelectedItem;
            if (selected != null) _selectedPlanItem = selected.Id;
            int rowY = cy + 178;
            int maxRows = Math.Min(8, _governor.Blueprint.Buildings.Count);
            for (int i = 0; i < maxRows; i++)
            {
                BlueprintItem item = _governor.Blueprint.Buildings[i];
                int id = item.Id;
                _buttons.Add(new UiButton
                {
                    Rect = new Rectangle(_panel.X + 20, rowY + i * 23, 440, 20),
                    Label = (item.Status == BlueprintStatus.Blocked ? "X " :
                             item.Status == BlueprintStatus.NeedsResource ? "! " : "- ") +
                            item.NameUz + "  (" + item.Tile.X + "," + item.Tile.Y + ")  " +
                            item.Size + "x" + item.Size + "  " + item.Cost + " tanga",
                    Left = true,
                    TextColor = item.Id == _selectedPlanItem ? Gold :
                                item.Status == BlueprintStatus.Blocked ? Red :
                                item.Status == BlueprintStatus.NeedsResource ? Yellow : Green,
                    OnClick = () => { _selectedPlanItem = id; _governor.SelectBlueprintItem(id); }
                });
            }

            int addY = _panel.Bottom - 130;
            _buttons.Add(BuildAddButton(new Rectangle(_panel.X + 176, addY, 82, 24), "UY +", BuildingType.House));
            _buttons.Add(BuildAddButton(new Rectangle(_panel.X + 266, addY, 100, 24), "BOZOR +", BuildingType.Market));
            _buttons.Add(BuildAddButton(new Rectangle(_panel.X + 374, addY, 82, 24), "BOG' +", BuildingType.Garden));

            int by = _panel.Bottom - 98;
            _buttons.Add(new UiButton
            {
                Rect = new Rectangle(_panel.X + 20, by, 128, 28), Label = "KO'CHIRISH",
                Enabled = selected != null,
                OnClick = () =>
                {
                    if (_governor.Blueprints.SelectedItem == null) { _state.ShowMessage("Avval element tanlang.", true); return; }
                    PickMode = GovernorMapMode.MoveBuilding;
                    Close();
                    _state.ShowMessage("Xaritada yangi joyni bosing. Esc — tahrirni bekor qilish.");
                }
            });
            _buttons.Add(new UiButton
            {
                Rect = new Rectangle(_panel.X + 156, by, 156, 28), Label = "YO'L TAHRIRI",
                OnClick = () =>
                {
                    PickMode = GovernorMapMode.ToggleRoad;
                    Close();
                    _state.ShowMessage("Xaritada yo'l katagini bosing (qo'shish/olib tashlash). V — panelga qaytish.");
                }
            });
            _buttons.Add(new UiButton
            {
                Rect = new Rectangle(_panel.X + 320, by, 116, 28), Label = "O'CHIRISH",
                Enabled = selected != null,
                OnClick = () =>
                {
                    if (_governor.RemoveSelectedBlueprintItem()) _selectedPlanItem = FirstPlanItemId();
                }
            });
            _buttons.Add(new UiButton
            {
                Rect = new Rectangle(_panel.X + 444, by, 204, 28), Label = "QAYTA TEKSHIRISH",
                OnClick = () => { _governor.RevalidateBlueprint(); _state.ShowMessage("Blueprint qayta tekshirildi."); }
            });
            _buttons.Add(new UiButton
            {
                Rect = new Rectangle(_panel.X + 656, by, 158, 28), Label = "BEKOR QILISH",
                OnClick = () => { _governor.CancelBlueprint(); _selectedPlanItem = 0; }
            });

            int by2 = _panel.Bottom - 62;
            _buttons.Add(new UiButton
            {
                Rect = new Rectangle(_panel.X + 20, by2, 292, 28),
                Label = "TASDIQLASH (" + (_governor.Blueprint.TotalCost) + " tanga)",
                Primary = true,
                Enabled = _governor.Blueprint.IsValid && _governor.Blueprint.BuildableCount > 0,
                OnClick = () =>
                {
                    if (_governor.ApprovePlan())
                    {
                        _selectedPlanItem = 0;
                        _tab = PanelTab.Queue;
                    }
                }
            });
            _buttons.Add(new UiButton
            {
                Rect = new Rectangle(_panel.X + 324, by2, 268, 28), Label = "TAVSIYALARGA QAYTISH",
                OnClick = () => { _tab = PanelTab.Advice; }
            });
        }

        private void LayoutQueue(int cy)
        {
            ApprovedConstructionPlan plan = _governor.Queue?.Plan;
            if (plan == null) return;

            int by = _panel.Bottom - 62;
            if (plan.State == PlanExecState.InProgress)
                _buttons.Add(new UiButton { Rect = new Rectangle(_panel.X + 20, by, 150, 28), Label = "PAUZA", OnClick = _governor.PauseQueue });
            else if (plan.State == PlanExecState.Paused && plan.NeedsReapproval)
                _buttons.Add(new UiButton
                {
                    Rect = new Rectangle(_panel.X + 20, by, 300, 28),
                    Label = "QAYTA TASDIQ (" + _governor.Queue.RequiredBudget + " tanga)",
                    Primary = true, OnClick = _governor.ReapproveQueue
                });
            else if (plan.State == PlanExecState.Paused)
                _buttons.Add(new UiButton { Rect = new Rectangle(_panel.X + 20, by, 170, 28), Label = "DAVOM ETISH", Primary = true, OnClick = _governor.ResumeQueue });

            bool active = plan.State == PlanExecState.InProgress || plan.State == PlanExecState.Paused;
            _buttons.Add(new UiButton
            {
                Rect = new Rectangle(_panel.X + 330, by, 150, 28), Label = "BIR QADAM",
                Enabled = plan.State == PlanExecState.InProgress,
                OnClick = () => { _governor.StepQueueOnce(); }
            });
            _buttons.Add(new UiButton
            {
                Rect = new Rectangle(_panel.X + 486, by, 150, 28), Label = "BEKOR QILISH",
                Enabled = active,
                OnClick = () => { _governor.CancelQueue(); }
            });
            _buttons.Add(new UiButton
            {
                Rect = new Rectangle(_panel.Right - 260, by, 240, 28), Label = "YANGI REJA", Primary = true,
                Enabled = !active,
                OnClick = () => { _tab = PanelTab.Plan; }
            });
        }

        private void LayoutReport(int cy)
        {
            _buttons.Add(new UiButton
            {
                Rect = new Rectangle(_panel.X + 20, _panel.Bottom - 62, 200, 28), Label = "TAHLILNI YANGILASH",
                OnClick = () => { _governor.Analyze(); _state.ShowMessage("Hisobot yangilandi."); }
            });
            _buttons.Add(new UiButton
            {
                Rect = new Rectangle(_panel.X + 232, _panel.Bottom - 62, 190, 28), Label = "YANGI REJA",
                Primary = true, OnClick = () => { _tab = PanelTab.Plan; }
            });
        }

        private int FirstPlanItemId()
            => _governor.Blueprint != null && _governor.Blueprint.Buildings.Count > 0
                ? _governor.Blueprint.Buildings[0].Id : 0;

        /// <summary>Tahlildagi tavsiya etilgan uchastkani, bo'lmasa birinchi ochiladigan uchastkani topadi.</summary>
        private LandParcel FirstRecommendedParcel()
        {
            if (_governor.Report != null)
                foreach (int id in _governor.Report.RecommendedLandParcels)
                {
                    LandParcel parcel = _state.Land.Parcels.Find(p => p.Id == id);
                    if (parcel != null) return parcel;
                }
            return _state.Land.Parcels.Find(p => p.State == LandParcelState.Unlockable ||
                                                 p.State == LandParcelState.Locked);
        }

        private void FocusLand(LandParcel parcel)
        {
            _state.Select(parcel);
            Close();
            FocusLandRequested?.Invoke(parcel);
            string size = parcel.GridBounds.Width + "x" + parcel.GridBounds.Height + " katak";
            string need = parcel.RequiredPopulation > 0
                ? " Aholi talabi: " + parcel.RequiredPopulation + "."
                : "";
            _state.ShowMessage("Uchastka ko'rsatildi: " + parcel.NameUz + " — " + size +
                ", ochish narxi " + parcel.UnlockCost + " tanga; yangi mahalla uchun qurilish maydoni paydo bo'ladi." +
                need + " 'YERNI OCHISH' tugmasini o'zingiz bosing — AI yerni sotib olmaydi.");
        }

        private UiButton BuildAddButton(Rectangle rect, string label, BuildingType kind)
            => new UiButton
            {
                Rect = rect, Label = label,
                OnClick = () =>
                {
                    _pendingAddKind = kind;
                    PickMode = GovernorMapMode.AddBuilding;
                    Close();
                    _state.ShowMessage("Xaritada " + label.Replace(" +", "").ToLowerInvariant() +
                        " uchun joyni bosing. Esc — bekor qilish.");
                }
            };

        // ------------------------------------------------------------------
        // Update
        // ------------------------------------------------------------------
        public void Update(InputManager input, int vw, int vh)
        {
            if (!IsOpen) return;
            Layout(vw, vh);
            input.MouseOverUI = true;
            if (!input.LeftPressed) return;

            Point m = input.MousePosition;
            if (!_panel.Contains(m)) { Close(); return; }
            foreach (UiButton b in _buttons)
            {
                if (!b.Enabled || !b.Rect.Contains(m)) continue;
                b.OnClick?.Invoke();
                return;
            }
        }

        // ------------------------------------------------------------------
        // Draw
        // ------------------------------------------------------------------
        public void Draw(SpriteBatch sb, int vw, int vh)
        {
            Layout(vw, vh);
            Fill(sb, new Rectangle(0, 0, vw, vh), new Color(0, 0, 0, 165));
            Panel(sb, _panel, PanelBg, Gold, 2);

            _font.Draw(sb, "DONISHMAND VAZIR", new Vector2(_panel.X + 22, _panel.Y + 12), Gold, 3, 1);
            _font.Draw(sb, "Rejim: " + _governor.Permissions.ModeNameUz,
                new Vector2(_panel.X + 22, _panel.Y + 46), Cream, 2, 1);
            _font.DrawRight(sb, _governor.Permissions.CanExecute ? "IJRO FAOL" : "IJRO O'CHIQ",
                new Vector2(_modeBtn.X - 16, _panel.Y + 46),
                _governor.Permissions.CanExecute ? Green : Yellow, 2, 1);

            for (int i = 0; i < _tabRects.Length; i++)
            {
                bool active = (int)_tab == i;
                Panel(sb, _tabRects[i], active ? PanelBg2 : PanelBg, active ? Gold : Border, active ? 2 : 1);
                _font.DrawCentered(sb, TabName((PanelTab)i), _tabRects[i].Center.ToVector2(), active ? Gold : Cream, 2, 1);
            }

            switch (_tab)
            {
                case PanelTab.Analysis: DrawAnalysis(sb); break;
                case PanelTab.Advice: DrawAdvice(sb); break;
                case PanelTab.Plan: DrawPlan(sb); break;
                case PanelTab.Queue: DrawQueue(sb); break;
                case PanelTab.Report: DrawReport(sb); break;
            }

            foreach (UiButton b in _buttons) DrawButton(sb, b);
            _font.Draw(sb, "Barcha xarajat va qurilish faqat sizning tasdig'ingiz bilan.", 
                new Vector2(_panel.X + 22, _panel.Bottom - 26), Muted, 2, 1);
        }

        private void DrawAnalysis(SpriteBatch sb)
        {
            CityAnalysisReport r = _governor.Report;
            int x = _panel.X + 22, y = _panel.Y + 120, cx = _panel.X + 560;
            _font.Draw(sb, "SHAHAR TAHLILI", new Vector2(x, y), Gold, 2, 1); y += 26;
            if (r == null) { _font.Draw(sb, "Tahlil hali tayyor emas.", new Vector2(x, y), Muted, 2, 1); return; }

            y = DrawLine(sb, "Aholi: " + r.Population + " / sig'im " + r.HousingCapacity, x, y, Cream);
            y = DrawLine(sb, r.HousingShortage > 0 ? "Turar joy tanqisligi: " + r.HousingShortage
                                                   : "Turar joy zaxirasi: " + (-r.HousingShortage), x, y,
                          r.HousingShortage > 0 ? Red : Green);
            y = DrawLine(sb, "Xazina: " + r.Treasury + " tanga   Oziq: " + r.Food, x, y, Gold);
            y = DrawLine(sb, "Baxt: " + r.Happiness + "   Sog'liq: " + r.Health + "   Ta'lim: " + r.Education, x, y, Cream);
            y = DrawLine(sb, "Xavfsizlik: " + r.Security + "   Ishchilar: " + r.Workers + "   Ishsiz: " + r.Unemployed, x, y, Cream);
            y = DrawLine(sb, "Kunlik: +" + r.DailyIncome + " / -" + r.DailyExpense + "  (sof " + r.DailyNet + ")", x, y,
                          r.DailyNet >= 0 ? Green : Red);
            y = DrawLine(sb, "Binolar: " + r.TotalBuildings + "   Yo'llar: " + r.RoadTiles + " katak", x, y, Cream);
            y = DrawLine(sb, "Bo'sh quriladigan katak: " + r.BuildableTiles, x, y,
                          r.BuildableTiles >= 20 ? Green : Yellow);
            y = DrawLine(sb, "Yer: " + r.OpenParcels + " ochiq, " + r.LockedParcels + " yopiq uchastka", x, y, Cream);
            y = DrawLine(sb, "Shahar salomatligi: " + r.OverallCityHealthScore + "/100", x, y,
                          r.OverallCityHealthScore >= 60 ? Green : r.OverallCityHealthScore >= 40 ? Yellow : Red);
            y += 8;
            _font.Draw(sb, "MUAMMOLAR", new Vector2(x, y), Gold, 2, 1); y += 22;
            int shown = 0;
            foreach (string p in r.DetectedProblems)
            {
                if (shown++ >= 4) break;
                foreach (string line in Wrap("- " + p, 500, 2)) { _font.Draw(sb, line, new Vector2(x, y), Red, 2, 1); y += 16; }
            }
            if (shown == 0) _font.Draw(sb, "Jiddiy muammo topilmadi.", new Vector2(x, y), Green, 2, 1);

            int ry = _panel.Y + 120;
            _font.Draw(sb, "IMKONIYATLAR", new Vector2(cx, ry), Gold, 2, 1); ry += 26;
            int op = 0, opLines = 0;
            foreach (string line in r.DevelopmentOpportunities)
            {
                if (op++ >= 3 || opLines >= 5) break;
                foreach (string w in Wrap("- " + line, 350, 2))
                {
                    if (opLines++ >= 5) break;
                    _font.Draw(sb, w, new Vector2(cx, ry), Cream, 2, 1); ry += 16;
                }
            }
            if (op == 0) _font.Draw(sb, "Hozircha qo'shimcha imkoniyat yo'q.", new Vector2(cx, ry), Muted, 2, 1);

            ry += 16;
            _font.Draw(sb, "YER BO'YICHA MASLAHAT", new Vector2(cx, ry), Gold, 2, 1); ry += 24;
            int landLines = 0;
            foreach (string line in Wrap(_governor.LandAdviceUz(), 350, 2))
            {
                if (landLines++ >= 3) break;
                _font.Draw(sb, line, new Vector2(cx, ry), Cream, 2, 1); ry += 16;
            }

            ry += 16;
            _font.Draw(sb, "INFRASTRUKTURA BO'SHLIQLARI", new Vector2(cx, ry), Gold, 2, 1); ry += 24;
            int gaps = 0, gapLines = 0;
            foreach (string g in r.InfrastructureGaps)
            {
                if (gaps++ >= 3 || gapLines >= 4) break;
                foreach (string line in Wrap("- " + g, 350, 2))
                {
                    if (gapLines++ >= 4) break;
                    _font.Draw(sb, line, new Vector2(cx, ry), Yellow, 2, 1); ry += 16;
                }
            }
            if (gaps == 0) _font.Draw(sb, "Asosiy infratuzilma joyida.", new Vector2(cx, ry), Green, 2, 1);
        }

        private void DrawAdvice(SpriteBatch sb)
        {
            int x = _panel.X + 22, y = _panel.Y + 120;
            _font.Draw(sb, "TAVSIYALAR", new Vector2(x, y), Gold, 2, 1);
            _font.Draw(sb, "Utility AI ustuvorlik bo'yicha — xarajat qilishdan oldin tasdiq so'raladi.",
                new Vector2(x + 160, y), Muted, 2, 1);

            List<Recommendation> recs = _governor.Recommendations ?? new List<Recommendation>();
            int shown = Math.Min(4, recs.Count);
            if (shown == 0) _font.Draw(sb, "Tavsiya yo'q — tahlilni yangilang.", new Vector2(x, y + 40), Muted, 2, 1);

            for (int i = 0; i < shown; i++)
            {
                Recommendation rec = recs[i];
                int cardY = y + 34 + i * 88;
                var card = new Rectangle(_panel.X + 20, cardY, _panel.Width - 40, 82);
                Panel(sb, card, i == 0 ? new Color(58, 48, 32, 245) : PanelBg2, i == 0 ? Gold : Border, 1);
                _font.Draw(sb, (i + 1) + ". " + rec.TitleUz, new Vector2(card.X + 12, card.Y + 8),
                    i == 0 ? Gold : Cream, 2, 1);
                _font.DrawRight(sb, "Ball: " + rec.PriorityScore, new Vector2(card.Right - 12, card.Y + 8),
                    rec.PriorityScore >= 70 ? Red : Muted, 2, 1);
                int ly = card.Y + 30;
                int lines = 0;
                foreach (string line in Wrap(rec.ReasonUz, 430, 2))
                {
                    if (lines++ >= 3) break;
                    _font.Draw(sb, line, new Vector2(card.X + 12, ly), Cream, 2, 1); ly += 16;
                }
            }

            int ay = _panel.Bottom - 126;
            _font.Draw(sb, "DONISHMAND VAZIR (eski maslahatlar)", new Vector2(x, ay), Gold, 2, 1);
            int my = ay + 22, count = 0;
            foreach (AdvisorMessage m in _state.Advisor.Messages)
            {
                if (count++ >= 2) break;
                _font.Draw(sb, "- " + m.Title, new Vector2(x, my), Muted, 2, 1); my += 16;
            }
            if (count == 0) _font.Draw(sb, "Qo'shimcha eski maslahat yo'q.", new Vector2(x, my), Muted, 2, 1);
        }

        private void DrawPlan(SpriteBatch sb)
        {
            CityPlan p = _governor.Blueprint;
            int x = _panel.X + 22, y = _panel.Y + 120;

            if (p == null)
            {
                _font.Draw(sb, "YANGI MAHALLA REJASI", new Vector2(x, y), Gold, 3, 1);
                _font.Draw(sb, "Mahalla turini tanlang — AI haqiqiy xarita ustida bino va yo'llarni rejalashtiradi.",
                    new Vector2(x, y + 32), Cream, 2, 1);
                _font.Draw(sb, "Bu faqat preview: tasdiqlamaguningizcha GameState o'zgarmaydi.",
                    new Vector2(x, y + 54), Yellow, 2, 1);
                _font.Draw(sb, "Reja tanlangandan keyin har bir binoni ko'chirish, o'chirish va yo'lni tahrirlash mumkin.",
                    new Vector2(x, y + 76), Muted, 2, 1);
                _font.Draw(sb, "Taxminiy namuna: 3 uy + 1 bozor + 1 bog' + ko'cha.",
                    new Vector2(x, y + 98), Muted, 2, 1);
                return;
            }

            _font.Draw(sb, p.NameUz, new Vector2(x, y), Gold, 3, 1);
            _font.DrawRight(sb, p.IsValid ? "YAROQLI" : "YAROQSIZ",
                new Vector2(_panel.Right - 22, y + 6), p.IsValid ? Green : Red, 2, 1);

            int ry = y + 30;
            string counts1 = $"{p.HouseCount} uy / {p.MarketCount} savdo / {p.GardenCount} bog' / {p.ExpectedHousing} joy";
            string counts2 = $"{p.RoadCount} yo'l katagi / {p.BuildableCount} bino quriladi";
            _font.Draw(sb, counts1, new Vector2(x, ry), Cream, 2, 1); ry += 16;
            _font.Draw(sb, counts2, new Vector2(x, ry), Cream, 2, 1); ry += 16;
            ry += 6;
            List<string> rationale = Wrap(p.RationaleUz, 435, 2);
            int shownRationale = Math.Min(4, rationale.Count);
            for (int i = 0; i < shownRationale; i++)
            {
                string line = rationale[i];
                if (i == shownRationale - 1 && rationale.Count > shownRationale)
                    line = Ellipsize(line + "...", 435, 2);
                _font.Draw(sb, line, new Vector2(x, ry), Muted, 2, 1); ry += 16;
            }

            int listY = y + 176;
            _font.Draw(sb, "REJA ELEMENTLARI (TANLANG)", new Vector2(x, listY - 22), Gold, 2, 1);
            if (p.Buildings.Count == 0)
                _font.Draw(sb, "Rejada bino yo'q.", new Vector2(x, listY), Red, 2, 1);
            _font.Draw(sb, "BINO QO'SHISH:", new Vector2(x, _panel.Bottom - 125), Gold, 2, 1);

            int ex = _panel.X + 478, ey = y + 70;
            _font.Draw(sb, "XARAJAT SMETASI", new Vector2(ex, ey), Gold, 2, 1); ey += 24;
            PlanCostEstimate est = _governor.CurrentEstimate;
            if (est != null)
            {
                int costLines = 0;
                foreach (CostLine line in est.Lines)
                {
                    if (costLines++ >= 5) break;
                    _font.Draw(sb, line.Count + "x " + line.NameUz, new Vector2(ex, ey), Cream, 2, 1);
                    _font.DrawRight(sb, line.Total + " tanga", new Vector2(_panel.Right - 24, ey), Gold, 2, 1);
                    ey += 18;
                }
                if (est.Lines.Count > 5)
                {
                    _font.Draw(sb, "... yana " + (est.Lines.Count - 5) + " qator", new Vector2(ex, ey), Muted, 2, 1);
                    ey += 18;
                }
                ey += 4;
                _font.Draw(sb, "Jami: " + est.TotalCost + " tanga", new Vector2(ex, ey), Gold, 2, 1); ey += 18;
                _font.Draw(sb, "Xazina: " + est.Treasury + " tanga", new Vector2(ex, ey), Cream, 2, 1); ey += 18;
                _font.Draw(sb, est.MissingCoins > 0 ? "Yetishmaydi: " + est.MissingCoins + " tanga" : "Xazina yetarli",
                    new Vector2(ex, ey), est.MissingCoins > 0 ? Red : Green, 2, 1); ey += 18;
                _font.Draw(sb, "Taxminiy muddat: " + est.EstimatedDays + " kun", new Vector2(ex, ey), Muted, 2, 1);
                ey += 24;
            }

            DrawSelectedDetails(sb, ex, ey);
        }

        private void DrawQueue(SpriteBatch sb)
        {
            int x = _panel.X + 22, y = _panel.Y + 120;
            ApprovedConstructionPlan p = _governor.Queue?.Plan;
            _font.Draw(sb, "QURILISH NAVBATI", new Vector2(x, y), Gold, 3, 1);
            if (p == null)
            {
                _font.Draw(sb, "Faol qurilish navbati yo'q.", new Vector2(x, y + 42), Cream, 2, 1);
                _font.Draw(sb, "Yangi reja tuzing va tasdiqlang — shundan keyingina qurilish boshlanadi.",
                    new Vector2(x, y + 66), Muted, 2, 1);
                return;
            }

            _font.Draw(sb, p.PlanName, new Vector2(x, y + 34), Cream, 2, 1);
            _font.DrawRight(sb, _governor.Queue.StateNameUz, new Vector2(_panel.Right - 22, y + 34),
                p.State == PlanExecState.Completed ? Green :
                p.State == PlanExecState.Paused ? Yellow :
                p.State == PlanExecState.Cancelled ? Red : Gold, 2, 1);

            int total = Math.Max(1, p.Steps.Count);
            int done = p.DoneCount;
            var bar = new Rectangle(x, y + 62, _panel.Width - 44, 16);
            Fill(sb, bar, new Color(20, 18, 15));
            Fill(sb, new Rectangle(bar.X, bar.Y, (int)(bar.Width * done / (float)total), bar.Height),
                p.State == PlanExecState.Cancelled ? Red : Gold);
            Panel(sb, bar, Color.Transparent, Border, 1);
            _font.Draw(sb, _governor.Queue.ProgressUz, new Vector2(x, y + 84), Cream, 2, 1);
            _font.DrawRight(sb, "Qolgan budjet: " + Math.Max(0, p.ApprovedBudget - p.SpentSoFar) + " tanga",
                new Vector2(_panel.Right - 22, y + 84), Muted, 2, 1);

            int my = y + 108;
            foreach (string line in Wrap(string.IsNullOrEmpty(_governor.Queue.LastMessageUz)
                         ? "Navbat kutilmoqda." : _governor.Queue.LastMessageUz, _panel.Width - 44, 2))
            {
                _font.Draw(sb, line, new Vector2(x, my), p.NeedsReapproval ? Yellow : Cream, 2, 1);
                my += 16;
            }

            my += 8;
            _font.Draw(sb, "QADAMLAR", new Vector2(x, my), Gold, 2, 1); my += 22;
            int shown = 0;
            foreach (PlanStep step in p.Steps)
            {
                if (shown++ >= 12) break;
                string mark = step.Status == StepStatus.Done ? "+ " :
                              step.Status == StepStatus.Failed ? "X " :
                              step.Status == StepStatus.Skipped ? "- " : "O ";
                Color c = step.Status == StepStatus.Done ? Green :
                          step.Status == StepStatus.Failed ? Red :
                          step.Status == StepStatus.Skipped ? Muted : Cream;
                _font.Draw(sb, mark + step.LabelUz + (step.Kind == StepKind.Road ? "  yo'l" : "") +
                               "  " + step.Cost + " tanga", new Vector2(x, my), c, 2, 1);
                my += 17;
            }
            if (p.Steps.Count > 12)
                _font.Draw(sb, "... yana " + (p.Steps.Count - 12) + " qadam", new Vector2(x, my), Muted, 2, 1);
        }

        private void DrawSelectedDetails(SpriteBatch sb, int ex, int ey)
        {
            _font.Draw(sb, "TANLANGAN ELEMENT", new Vector2(ex, ey), Gold, 2, 1); ey += 22;
            BlueprintItem item = _governor.Blueprints?.SelectedItem;
            if (item == null)
            {
                _font.Draw(sb, "Ro'yxatdan yoki xaritadan bino tanlang.", new Vector2(ex, ey), Muted, 2, 1);
                return;
            }

            Color statusColor = item.Status == BlueprintStatus.Blocked ? Red :
                                item.Status == BlueprintStatus.NeedsResource ? Yellow : Green;
            _font.Draw(sb, item.NameUz + "  (" + item.Tile.X + "," + item.Tile.Y + ")", new Vector2(ex, ey), statusColor, 2, 1);
            ey += 18;
            _font.Draw(sb, item.Cost + " tanga  |  " + item.Size + "x" + item.Size + "  |  " +
                           item.BuildTimeDays + " kun", new Vector2(ex, ey), Cream, 2, 1);
            ey += 18;

            int lines = 0;
            foreach (string line in Wrap("Foyda: " + item.BenefitUz, 425, 2))
            {
                if (lines++ >= 2) break;
                _font.Draw(sb, line, new Vector2(ex, ey), Cream, 2, 1); ey += 16;
            }
            lines = 0;
            foreach (string line in Wrap("Talab: " + item.RequirementUz, 425, 2))
            {
                if (lines++ >= 2) break;
                _font.Draw(sb, line, new Vector2(ex, ey), Muted, 2, 1); ey += 16;
            }
            if (!string.IsNullOrEmpty(item.StatusReasonUz))
            {
                lines = 0;
                foreach (string line in Wrap(item.StatusReasonUz, 425, 2))
                {
                    if (lines++ >= 2) break;
                    _font.Draw(sb, line, new Vector2(ex, ey), statusColor, 2, 1); ey += 16;
                }
            }
        }

        private void DrawReport(SpriteBatch sb)
        {
            CityAnalysisReport r = _governor.Report;
            int x = _panel.X + 22, y = _panel.Y + 120;
            _font.Draw(sb, "RIVOJLANISH HISOBOTI", new Vector2(x, y), Gold, 3, 1);
            if (r == null) return;

            int ly = y + 42;
            ly = DrawWrapped(sb, "Shahar salomatligi: " + r.OverallCityHealthScore + "/100. " +
                "Aholi " + r.Population + ", uy-joy sig'imi " + r.HousingCapacity + ". " +
                (r.HousingShortage > 0 ? "Tanqislik " + r.HousingShortage + " kishi." : "Turar joy zaxirasi bor."),
                x, ly, _panel.Width - 44, Cream);
            ly += 6;
            ly = DrawWrapped(sb, "Xazina " + r.Treasury + " tanga, kunlik sof oqim " + r.DailyNet +
                ". Oziq-ovqat " + r.Food + " (ishlab chiqarish +" + r.FoodProduced +
                ", sarf -" + r.FoodConsumed + ").", x, ly, _panel.Width - 44, Cream);
            ly += 6;
            ly = DrawWrapped(sb, "Yer: " + r.OpenParcels + " ochiq, " + r.LockedParcels + " yopiq uchastka. " +
                "Bo'sh quriladigan kataklar " + r.BuildableTiles + ", yo'llar " + r.RoadTiles + " katak.",
                x, ly, _panel.Width - 44, Cream);
            ly += 10;

            _font.Draw(sb, "OXIRGI REJA", new Vector2(x, ly), Gold, 2, 1); ly += 24;
            ApprovedConstructionPlan q = _governor.Queue?.Plan;
            if (q == null)
                ly = DrawWrapped(sb, "Hali tasdiqlangan reja yo'q. Tavsiyalar bo'limidan yangi mahalla rejasini tuzing.",
                    x, ly, _panel.Width - 44, Muted);
            else
                ly = DrawWrapped(sb, q.PlanName + " — " + _governor.Queue.StateNameUz + ". " +
                    _governor.Queue.ProgressUz + " " + _governor.Queue.LastMessageUz,
                    x, ly, _panel.Width - 44, Cream);

            ly += 10;
            _font.Draw(sb, "KEYINGI TAVSIYA", new Vector2(x, ly), Gold, 2, 1); ly += 24;
            ly = DrawWrapped(sb, _governor.TopAdviceUz(), x, ly, _panel.Width - 44, Cream);
            ly += 10;

            _font.Draw(sb, "YER BO'YICHA", new Vector2(x, ly), Gold, 2, 1); ly += 24;
            ly = DrawWrapped(sb, _governor.LandAdviceUz(), x, ly, _panel.Width - 44, Muted);

            if (ly > _panel.Bottom - 190) return;
            ly += 8;
            _font.Draw(sb, "IQTISODIY PROGNOZ (kafolat emas)", new Vector2(x, ly), Gold, 2, 1); ly += 22;
            int scenarios = 0;
            foreach (ForecastScenario f in _governor.ForecastCurrent())
            {
                if (scenarios++ >= 3 || ly > _panel.Bottom - 90) break;
                _font.Draw(sb, f.NameUz + ":", new Vector2(x, ly), f.NameUz == "Salbiy" ? Red : Green, 2, 1);
                int lines = 0;
                foreach (string line in Wrap(f.SummaryUz, _panel.Width - 190, 2))
                {
                    if (lines++ >= 2) break;
                    _font.Draw(sb, line, new Vector2(x + 130, ly), Cream, 2, 1); ly += 16;
                }
                ly += 4;
            }

            if (ly > _panel.Bottom - 150) return;
            ly += 6;
            _font.Draw(sb, "TUGALLANGAN REJALAR", new Vector2(x, ly), Gold, 2, 1); ly += 22;
            if (_governor.History.Count == 0)
            {
                _font.Draw(sb, "Hali yakunlangan reja yo'q.", new Vector2(x, ly), Muted, 2, 1);
            }
            else
            {
                int shownHistory = 0;
                for (int i = _governor.History.Count - 1; i >= 0 && shownHistory < 3; i--, shownHistory++)
                {
                    if (ly > _panel.Bottom - 70) break;
                    ly = DrawWrapped(sb, "- " + _governor.History[i].SummaryUz, x, ly,
                        _panel.Width - 44, Muted);
                }
            }
        }

        // ------------------------------------------------------------------
        // Drawing helpers
        // ------------------------------------------------------------------
        private int DrawLine(SpriteBatch sb, string text, int x, int y, Color c)
        {
            _font.Draw(sb, text, new Vector2(x, y), c, 2, 1);
            return y + 18;
        }

        private int DrawWrapped(SpriteBatch sb, string text, int x, int y, int width, Color c)
        {
            foreach (string line in Wrap(text, width, 2))
            {
                _font.Draw(sb, line, new Vector2(x, y), c, 2, 1);
                y += 16;
            }
            return y;
        }

        private void DrawButton(SpriteBatch sb, UiButton b)
        {
            Color border = !b.Enabled ? new Color(80, 72, 60) : b.Primary ? Gold : Border;
            Color bg = b.Primary && b.Enabled ? new Color(60, 49, 30, 248) : PanelBg2;
            Panel(sb, b.Rect, bg, border, b.Primary ? 2 : 1);
            Color text = !b.Enabled ? Muted : b.Primary ? Gold : b.TextColor;
            if (b.Left)
                _font.Draw(sb, b.Label, new Vector2(b.Rect.X + 6, b.Rect.Y + (b.Rect.Height - 14) / 2f), text, 2, 1);
            else
                _font.DrawCentered(sb, b.Label, b.Rect.Center.ToVector2(), text, 2, 1);
        }

        private void Fill(SpriteBatch sb, Rectangle r, Color c) => sb.Draw(_px, r, c);

        private void Panel(SpriteBatch sb, Rectangle r, Color bg, Color border, int t)
        {
            if (bg.A > 0) Fill(sb, r, bg);
            if (border.A == 0) return;
            Fill(sb, new Rectangle(r.X, r.Y, r.Width, t), border);
            Fill(sb, new Rectangle(r.X, r.Bottom - t, r.Width, t), border);
            Fill(sb, new Rectangle(r.X, r.Y, t, r.Height), border);
            Fill(sb, new Rectangle(r.Right - t, r.Y, t, r.Height), border);
        }

        private string Ellipsize(string text, int maxPx, int scale)
        {
            if (string.IsNullOrEmpty(text)) return text;
            int maxChars = Math.Max(1, maxPx / ((PixelFont.GW + 1) * scale));
            if (text.Length <= maxChars) return text;
            int keep = Math.Max(0, maxChars - 3);
            return text.Substring(0, keep).TrimEnd() + "...";
        }

        private List<string> Wrap(string text, int maxPx, int scale)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(text)) return lines;
            string[] words = text.Split(' ');
            string cur = "";
            foreach (string word in words)
            {
                string test = cur.Length == 0 ? word : cur + " " + word;
                if (_font.Measure(test, scale, 1).X > maxPx && cur.Length > 0) { lines.Add(cur); cur = word; }
                else cur = test;
            }
            if (cur.Length > 0) lines.Add(cur);
            return lines;
        }
    }
}
