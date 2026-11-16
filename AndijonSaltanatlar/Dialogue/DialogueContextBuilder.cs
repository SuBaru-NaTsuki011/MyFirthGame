using System;
using System.Collections.Generic;
using System.Globalization;
using AndijonSaltanatlar.AdvancedEconomy;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Narrative;
using AndijonSaltanatlar.NPC;
using AndijonSaltanatlar.Population;

namespace AndijonSaltanatlar.Dialogue
{
    /// <summary>
    /// Dialog konteksti: shartlar uchun sonli qiymatlar va matn shablonlari uchun o'rinbosarlar.
    /// Suhbat OCHILGANDA bir marta yig'iladi (har kadrda emas) va faqat haqiqiy GameState'dan olinadi.
    /// </summary>
    public sealed class DialogueContext
    {
        public Dictionary<string, float> Values { get; } = new Dictionary<string, float>();
        public Dictionary<string, string> Text { get; } = new Dictionary<string, string>();

        public float Get(string key) => Values.TryGetValue(key, out float v) ? v : 0f;
        public void Set(string key, float value) => Values[key] = value;
        public void Flag(string key, bool on = true) => Values[key] = on ? 1f : 0f;

        /// <summary>Matndagi {kalit} o'rinbosarlarini haqiqiy qiymatlar bilan almashtiradi.</summary>
        public string Resolve(string template)
        {
            if (string.IsNullOrEmpty(template) || template.IndexOf('{') < 0) return template ?? "";
            var sb = new System.Text.StringBuilder(template.Length + 32);
            int i = 0;
            while (i < template.Length)
            {
                char ch = template[i];
                int close = ch == '{' ? template.IndexOf('}', i + 1) : -1;
                if (close > i)
                {
                    string key = template.Substring(i + 1, close - i - 1);
                    if (Text.TryGetValue(key, out string val)) sb.Append(val);
                    else if (Values.TryGetValue(key, out float num)) sb.Append(Num(num));
                    else sb.Append('?'); // noma'lum o'rinbosar — raqam to'qib chiqarilmaydi
                    i = close + 1;
                }
                else
                {
                    sb.Append(ch);
                    i++;
                }
            }
            return sb.ToString();
        }

        public static string Num(float v)
        {
            float r = MathF.Round(v, 1);
            return Math.Abs(r - MathF.Round(r)) < 0.05f
                ? ((int)MathF.Round(r)).ToString(CultureInfo.InvariantCulture)
                : r.ToString("0.0", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>Dialog shartlarini kontekst bo'yicha tekshiradi.</summary>
    public static class DialogueConditionEvaluator
    {
        public static bool Check(DialogueCondition c, DialogueContext ctx)
        {
            if (c == null || string.IsNullOrEmpty(c.Key)) return true;
            float v = ctx.Get(c.Key);
            switch ((c.Op ?? ">=").Trim())
            {
                case ">=": return v >= c.Value;
                case "<=": return v <= c.Value;
                case ">": return v > c.Value;
                case "<": return v < c.Value;
                case "==": case "=": return Math.Abs(v - c.Value) < 0.0001f;
                case "!=": return Math.Abs(v - c.Value) >= 0.0001f;
                default: return false; // noma'lum operator — xavfsiz tomonga
            }
        }

        public static bool CheckAll(List<DialogueCondition> conditions, DialogueContext ctx)
        {
            if (conditions == null) return true;
            foreach (DialogueCondition c in conditions) if (!Check(c, ctx)) return false;
            return true;
        }
    }

    /// <summary>
    /// Fuqaro uchun dialog kontekstini haqiqiy o'yin holatidan yig'adi.
    ///
    /// Asosiy kalitlar:
    ///   citizen.*    unemployed, employed, happiness, trust, talks
    ///   prof.*       savdogar / bogbon / tashuvchi / soqchi / amaldor / muallim / none
    ///   concern.*    faol muammolar (concern.unemployed ...), concern.any, concern.top.&lt;k&gt;
    ///   memory.*     reported.&lt;k&gt;, forwarded.&lt;k&gt;, promised.&lt;k&gt;, dismissed.&lt;k&gt;, any_report
    ///   resolved.*   muammosini aytgan va u HAQIQATAN hal bo'lgan (hali minnatdorchilik bildirmagan)
    ///   easing.*     muammo o'zi uchun davom etyapti, lekin mahallada yengillashgan
    ///   event.*      oxirgi suhbatdan keyin shaharda sodir bo'lgan o'zgarishlar
    ///   city.* district.* household.* price.* merchant.*
    /// </summary>
    public static class DialogueContextBuilder
    {
        public static DialogueContext Build(Citizen c, CitizenInteractionProfile p, NPCMemory mem, CityLifeManager life,
                                            GameStateManager state, BuildingManager buildings)
        {
            var ctx = new DialogueContext();
            PopulationManager pop = state.Pop;
            AdvancedEconomyManager econ = life.Economy;
            DistrictStats district = life.Districts.Get(p.DistrictId);

            // ---- Fuqaro ----
            ctx.Flag("citizen.unemployed", !c.IsEmployed);
            ctx.Flag("citizen.employed", c.IsEmployed);
            ctx.Set("citizen.happiness", c.Happiness);
            ctx.Set("citizen.trust", mem.Trust);
            ctx.Set("citizen.talks", mem.TalkCount);
            ctx.Flag("prof." + p.OccupationId);
            ctx.Text["name"] = FirstName(c.Name);
            ctx.Text["fullName"] = c.Name;
            ctx.Text["family"] = p.FamilyName;
            ctx.Text["profession"] = p.OccupationName.ToLowerInvariant();
            ctx.Text["workplace"] = p.WorkplaceName;
            ctx.Text["district"] = p.DistrictName;

            // ---- Muammolar ----
            foreach (string k in p.ActiveConcernIds) ctx.Flag("concern." + k);
            ctx.Flag("concern.any", p.ActiveConcernIds.Count > 0);
            if (p.ActiveConcernIds.Count > 0) ctx.Flag("concern.top." + p.TopConcernId);
            ctx.Text["concernLabel"] = ConcernKeys.LabelUz(p.TopConcernId).ToLowerInvariant();

            // ---- Xotira va haqiqatan hal bo'lgan muammolar ----
            bool anyReport = false, anyForward = false;
            foreach (string k in ConcernKeys.All)
            {
                bool reported = mem.Has("reported." + k);
                bool active = p.ActiveConcernIds.Contains(k);
                bool districtNeeds = district != null && district.Needs.Contains(k);
                ctx.Flag("memory.reported." + k, reported);
                ctx.Flag("memory.forwarded." + k, mem.Has("forwarded." + k));
                ctx.Flag("memory.promised." + k, mem.Has("promised." + k));
                ctx.Flag("memory.dismissed." + k, mem.Has("dismissed." + k));
                // Minnatdorchilik FAQAT muammo shu fuqaro uchun haqiqatan yo'qolganda.
                ctx.Flag("resolved." + k, reported && !active && !mem.Has("thanked." + k));
                // Muammo o'zida qolgan, ammo mahalla miqyosida yengillashgan.
                ctx.Flag("easing." + k, reported && active && !districtNeeds && !mem.Has("eased." + k));
                if (reported && active)
                {
                    anyReport = true;
                    if (mem.Has("forwarded." + k)) anyForward = true;
                    ctx.Text["reportedLabel"] = ConcernKeys.LabelUz(k).ToLowerInvariant();
                }
            }
            ctx.Flag("memory.any_report", anyReport);
            ctx.Flag("memory.any_forward", anyForward);
            ctx.Flag("memory.any_dismissed", mem.HasPrefix("dismissed."));

            // ---- Oxirgi suhbatdan keyingi shahar voqealari ----
            if (mem.TalkCount > 0)
                foreach (string ev in CityLifeManager.CityEventKeys)
                    ctx.Flag("event." + ev, life.CityEvents.HappenedAfter(ev, mem.LastTalkDay));

            // ---- Shahar ----
            ctx.Set("city.population", pop.Count);
            ctx.Set("city.unemployed", pop.Unemployed);
            ctx.Set("city.freeJobs", pop.FreeJobs);
            ctx.Set("city.unemploymentPct", pop.Count > 0 ? pop.Unemployed * 100f / pop.Count : 0f);
            ctx.Set("city.happiness", pop.AvgHappiness);
            ctx.Set("city.education", pop.AvgEducation);
            ctx.Set("city.security", pop.AvgSecurity);
            ctx.Set("city.markets", buildings.Count(BuildingType.Market));
            ctx.Set("city.schools", buildings.Count(BuildingType.Madrasa));
            ctx.Set("city.gardens", buildings.Count(BuildingType.Garden));
            ctx.Set("city.foodStock", state.Food);
            ctx.Set("city.foodDays", pop.Count > 0 ? state.Food / (float)pop.Count : 99f);
            ctx.Set("city.tax", state.Policy.PopulationTax);
            ctx.Set("city.coins", state.Coins);
            ctx.Set("city.income", state.Economy.LastIncome);
            ctx.Set("city.foodProduced", state.Economy.LastFoodProduced);
            ctx.Set("city.marketShortagePct", econ.MarketShortageShare * 100f);
            int customers = 0;
            foreach (MarketState m in econ.Merchants.Markets) customers += m.CustomersToday;
            ctx.Set("city.customers", customers);
            // Qisqa nomlar (matn shablonlari uchun)
            ctx.Set("unemployed", pop.Unemployed);
            ctx.Set("freeJobs", pop.FreeJobs);
            ctx.Set("population", pop.Count);
            ctx.Set("tax", state.Policy.PopulationTax);
            ctx.Set("foodStock", state.Food);
            ctx.Set("foodProduced", state.Economy.LastFoodProduced);
            ctx.Set("coins", state.Coins);
            ctx.Set("income", state.Economy.LastIncome);
            ctx.Set("education", pop.AvgEducation);
            ctx.Set("security", pop.AvgSecurity);
            ctx.Set("customers", customers);

            // ---- Mahalla ----
            if (district != null)
            {
                ctx.Set("district.population", district.Population);
                ctx.Set("district.unemployed", district.Unemployed);
                ctx.Set("district.markets", district.Markets);
                ctx.Set("district.roadPct", district.RoadConnectionPercent);
                ctx.Set("district.devLevel", district.DevelopmentLevel);
                ctx.Set("districtUnemployed", district.Unemployed);
                foreach (string need in district.Needs) ctx.Flag("district.need." + need);
            }

            // ---- Oila ----
            ctx.Set("household.members", p.FamilySize);
            ctx.Set("household.working", p.WorkingMembers);
            ctx.Set("household.savings", p.HouseholdSavings);
            ctx.Set("household.fed", p.FedRatio);
            ctx.Set("household.income", p.HouseholdIncome);
            ctx.Set("members", p.FamilySize);
            ctx.Set("working", p.WorkingMembers);
            ctx.Set("savings", MathF.Round(p.HouseholdSavings));

            // ---- Narxlar ----
            foreach (SupplyDemandModel m in econ.Prices.All)
            {
                string k = m.Product.ToString().ToLowerInvariant();
                ctx.Set("price." + k, m.CurrentPrice);
                ctx.Set("price." + k + ".ratio", m.PriceRatio);
                ctx.Set("price." + k + ".trend", m.Trend);
            }
            ctx.Set("foodPrice", econ.Prices.Price(ProductType.Food));
            ctx.Set("clothStock", MathF.Round(econ.WarehouseStock(ProductType.Cloth, state.Resources)));

            // ---- Savdogar ----
            if (p.Merchant != null)
            {
                ctx.Flag("merchant.any");
                ctx.Set("merchant.stock", p.Merchant.Available);
                ctx.Set("merchant.price", p.Merchant.Price);
                ctx.Set("merchant.sold", p.Merchant.Market.UnitsSoldToday);
                ctx.Text["product"] = econ.Config.Get(p.Merchant.Product).NameUz.ToLowerInvariant();
                ctx.Text["market"] = p.Merchant.Market.Name;
                ctx.Text["tradeStatus"] = p.Merchant.StatusUz;
                ctx.Set("stock", MathF.Round(p.Merchant.Available));
                ctx.Set("price", p.Merchant.Price);
                ctx.Set("stall", p.Merchant.Stall);
            }
            return ctx;
        }

        private static string FirstName(string full)
        {
            if (string.IsNullOrEmpty(full)) return "";
            int i = full.IndexOf(' ');
            return i > 0 ? full.Substring(0, i) : full;
        }
    }
}
