using System;
using AndijonSaltanatlar.AdvancedEconomy;
using AndijonSaltanatlar.Advisor;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.CitizenQuests;
using AndijonSaltanatlar.CityPlanning;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.NPC;

namespace AndijonSaltanatlar.Dialogue
{
    public sealed class DialogueActionResult
    {
        public bool Close;
        public string NextDialogueId = "";
        public string MessageUz = "";
    }

    /// <summary>
    /// Dialog tanlovining o'yin holatiga ta'sirini bajaradi. Muhim qoidalar:
    ///  - QURILISH hech qachon to'g'ridan-to'g'ri bajarilmaydi: "plan:*" faqat reja SO'ROVINI navbatga qo'yadi,
    ///    xarajat va qurilishni o'yinchi Donishmand Vazir panelida tasdiqlaydi;
    ///  - "build:*" faqat qurilish rejimini ochadi (joyni o'yinchi tanlaydi, tangani o'zi sarflaydi);
    ///  - xotira, ishonch, murojaat va vazifa — shu yerda, strukturali buyruqlar orqali.
    /// Noma'lum harakat hech narsa qilmaydi (xavfsiz).
    ///
    /// Harakatlar: close | ack | talk | dismiss:&lt;muammo&gt; | forward:&lt;muammo|top&gt; | plan:&lt;DistrictType|improve&gt; |
    ///             build:&lt;BuildingType&gt; | lower_tax | show_district | show_household | show_prices |
    ///             focus_market | open_reports | show_dashboard
    /// </summary>
    public sealed class DialogueActionExecutor
    {
        public DialogueActionResult Execute(string actionId, NpcDialogueSession s, CityLifeManager life,
                                            GameStateManager state, BuildingManager buildings)
        {
            var r = new DialogueActionResult();
            if (string.IsNullOrEmpty(actionId)) return r;
            string name = actionId, arg = "";
            int colon = actionId.IndexOf(':');
            if (colon > 0) { name = actionId.Substring(0, colon); arg = actionId.Substring(colon + 1); }

            switch (name)
            {
                case "close":
                case "talk":
                    break;

                case "ack":
                    s.Memory.AdjustTrust(1f);
                    break;

                case "dismiss":
                {
                    string concern = ResolveConcern(arg, s);
                    if (concern.Length > 0) s.Memory.Remember("dismissed." + concern, s.Day);
                    s.Memory.AdjustTrust(-5f);
                    break;
                }

                case "forward":
                {
                    string concern = ResolveConcern(arg, s);
                    if (concern.Length == 0) { r.MessageUz = "Yuboriladigan murojaat yo'q"; break; }
                    Forward(concern, s, life, state);
                    r.MessageUz = "Murojaat Donishmand Vazirga yuborildi";
                    life.UiRequests.Enqueue(new UiRequest(UiRequestKind.ShowReports));
                    break;
                }

                case "plan":
                {
                    // Faqat so'rov: reja preview sifatida tuziladi, tasdiqlash — Vazir panelida.
                    string concern = s.ConcernKey;
                    if (!string.IsNullOrEmpty(concern))
                    {
                        s.Memory.Remember("promised." + concern, s.Day);
                        Forward(concern, s, life, state);
                        CitizenReport rep = life.Reports.Find(concern, s.Profile.DistrictId);
                        if (rep != null && rep.Status == CitizenReportStatus.Open) rep.Status = CitizenReportStatus.PlanProposed;
                    }
                    s.Memory.AdjustTrust(6f);
                    int planArg = arg == "improve" ? -1 : (Enum.TryParse(arg, true, out DistrictType t) ? (int)t : (int)DistrictType.Mixed);
                    life.UiRequests.Enqueue(new UiRequest(UiRequestKind.GeneratePlan, planArg));
                    r.MessageUz = "Reja so'raldi - tasdiqlash sizning qo'lingizda";
                    break;
                }

                case "build":
                    if (Enum.TryParse(arg, true, out BuildingType bt))
                    {
                        if (!string.IsNullOrEmpty(s.ConcernKey)) s.Memory.Remember("promised." + s.ConcernKey, s.Day);
                        s.Memory.AdjustTrust(4f);
                        life.UiRequests.Enqueue(new UiRequest(UiRequestKind.EnterBuild, (int)bt));
                        r.Close = true;
                    }
                    break;

                case "lower_tax":
                {
                    int before = state.Policy.PopulationTax;
                    state.Policy.AdjustPopulation(-5);
                    if (state.Policy.PopulationTax < before)
                    {
                        s.Memory.AdjustTrust(8f);
                        if (!string.IsNullOrEmpty(s.ConcernKey)) s.Memory.Remember("promised." + s.ConcernKey, s.Day);
                        r.MessageUz = "Aholi solig'i " + state.Policy.PopulationTax + " foizga tushirildi";
                    }
                    else r.MessageUz = "Soliq allaqachon eng past darajada";
                    break;
                }

                case "show_district":
                    life.UiRequests.Enqueue(new UiRequest(UiRequestKind.ShowDistrict, s.Profile.DistrictId));
                    r.Close = true;
                    break;

                case "show_household":
                    life.UiRequests.Enqueue(new UiRequest(UiRequestKind.ShowHousehold, s.Profile.HouseholdId));
                    r.Close = true;
                    break;

                case "show_prices":
                    life.UiRequests.Enqueue(new UiRequest(UiRequestKind.ShowMarket, MarketBuildingFor(s, life)));
                    r.Close = true;
                    break;

                case "focus_market":
                    life.UiRequests.Enqueue(new UiRequest(UiRequestKind.FocusMarket, MarketBuildingFor(s, life)));
                    r.Close = true;
                    break;

                case "open_reports":
                    life.UiRequests.Enqueue(new UiRequest(UiRequestKind.ShowReports));
                    r.Close = true;
                    break;

                case "show_dashboard":
                    life.UiRequests.Enqueue(new UiRequest(UiRequestKind.ShowDashboard));
                    r.Close = true;
                    break;
            }
            return r;
        }

        /// <summary>"top" yoki bo'sh bo'lsa — suhbatdagi / avval aytilgan muammoni topadi.</summary>
        private static string ResolveConcern(string arg, NpcDialogueSession s)
        {
            if (!string.IsNullOrEmpty(arg) && arg != "top") return arg;
            if (!string.IsNullOrEmpty(s.ConcernKey)) return s.ConcernKey;
            foreach (string k in s.Profile.ActiveConcernIds)
                if (s.Memory.Has("reported." + k)) return k;
            return s.Profile.TopConcernId ?? "";
        }

        /// <summary>Murojaatni Vazir hisobotiga qo'shadi va (mumkin bo'lsa) fuqaro vazifasini boshlaydi.</summary>
        private static void Forward(string concern, NpcDialogueSession s, CityLifeManager life, GameStateManager state)
        {
            DistrictStats d = life.Districts.Get(s.Profile.DistrictId);
            bool first = !s.Memory.Has("forwarded." + concern);
            life.Reports.Forward(s.Citizen.Id, concern, s.Profile.DistrictId, s.Day,
                d != null ? d.Population : 0, d != null ? d.CountOf(concern) : 1);
            s.Memory.Remember("reported." + concern, s.Day);
            s.Memory.Remember("forwarded." + concern, s.Day);
            if (first) s.Memory.AdjustTrust(4f);
            s.ConcernKey = concern;

            // Muammo mahallada haqiqatan mavjud bo'lsa — fuqaro vazifasi (mavjud QuestManager orqali).
            CitizenQuestRecord quest = life.Quests.TryStart(concern, d, s.Citizen.Id, s.Citizen.Name, s.Day);
            if (quest != null) s.Memory.Remember("quest." + concern, s.Day);
            state.Advisor.Analyze(state);
        }

        private static int MarketBuildingFor(NpcDialogueSession s, CityLifeManager life)
        {
            if (s.Profile.Merchant != null) return s.Profile.Merchant.Market.BuildingId;
            DistrictStats d = life.Districts.Get(s.Profile.DistrictId);
            MarketState m = life.Economy.Merchants.Nearest(d != null ? d.CenterX : GameConfig.CityCenter, d != null ? d.CenterY : GameConfig.CityCenter);
            return m != null ? m.BuildingId : 0;
        }
    }
}
