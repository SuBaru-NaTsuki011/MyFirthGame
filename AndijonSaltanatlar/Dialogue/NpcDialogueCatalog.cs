using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AndijonSaltanatlar.AdvancedEconomy;
using AndijonSaltanatlar.Narrative;

namespace AndijonSaltanatlar.Dialogue
{
    /// <summary>
    /// Fuqaro dialoglari katalogi (data-driven). Content/npc_dialogues.json mavjud bo'lsa o'sha fayldan
    /// yuklanadi; yo'q yoki buzuq bo'lsa shu yerdagi kanonik to'plam ishlatiladi (o'yin hech qachon to'xtamaydi).
    /// Faylni qayta yaratish:  AndijonSaltanatlar.exe --export-npc-dialogues &lt;Content papkasi&gt;
    ///
    /// Matnlardagi {kalit} o'rinbosarlar DialogueContext'dan — ya'ni haqiqiy o'yin holatidan to'ldiriladi.
    /// Tarixiy ma'lumotlar IsHistoricalFact + SourceNote bilan belgilangan; qolgan hammasi badiiy NPC so'zi.
    /// </summary>
    public static class NpcDialogueCatalog
    {
        public const string FileName = "npc_dialogues.json";

        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public static List<DialogueDefinition> Load(string contentRoot)
        {
            try
            {
                string path = Path.Combine(contentRoot ?? "", FileName);
                if (File.Exists(path))
                {
                    var list = JsonSerializer.Deserialize<List<DialogueDefinition>>(File.ReadAllText(path), Options);
                    if (list != null && list.Count > 0 && Validate(list).Count == 0) return list;
                }
            }
            catch
            {
                // buzuq JSON — kanonik katalogga qaytamiz
            }
            return BuildDefault();
        }

        public static string ToJson(List<DialogueDefinition> defs) => JsonSerializer.Serialize(defs, Options);

        public static void ExportDefault(string contentRoot)
        {
            Directory.CreateDirectory(contentRoot);
            File.WriteAllText(Path.Combine(contentRoot, FileName), ToJson(BuildDefault()));
        }

        /// <summary>Katalog xatolarini qaytaradi (takroriy id, yo'q zanjir, manbasiz tarixiy da'vo).</summary>
        public static List<string> Validate(List<DialogueDefinition> defs)
        {
            var errors = new List<string>();
            var ids = new HashSet<string>();
            foreach (DialogueDefinition d in defs)
            {
                if (string.IsNullOrEmpty(d.DialogueId)) { errors.Add("Bo'sh DialogueId"); continue; }
                if (!ids.Add(d.DialogueId)) errors.Add("Takroriy id: " + d.DialogueId);
                if (string.IsNullOrWhiteSpace(d.TextUz)) errors.Add("Bo'sh matn: " + d.DialogueId);
                if (d.IsHistoricalFact && string.IsNullOrWhiteSpace(d.SourceNote)) errors.Add("Manbasiz tarixiy da'vo: " + d.DialogueId);
            }
            foreach (DialogueDefinition d in defs)
            {
                if (!string.IsNullOrEmpty(d.NextDialogueId) && !ids.Contains(d.NextDialogueId)) errors.Add("Yo'q zanjir: " + d.DialogueId + " -> " + d.NextDialogueId);
                if (d.Choices == null) continue;
                foreach (StoryChoice c in d.Choices)
                    if (!string.IsNullOrEmpty(c.NextDialogueId) && !ids.Contains(c.NextDialogueId)) errors.Add("Yo'q zanjir: " + d.DialogueId + " -> " + c.NextDialogueId);
            }
            return errors;
        }

        // ------------------------------------------------------------------
        // Qurish yordamchilari
        // ------------------------------------------------------------------
        private static DialogueCondition C(string key, string op = ">=", float value = 1f) => new DialogueCondition(key, op, value);
        private static StoryChoice Ch(string text, string action, string next = "") => new StoryChoice { TextUz = text, ActionId = action, NextDialogueId = next };

        private static DialogueDefinition D(string id, DialogueKind kind, int priority, string text, string speaker = "any", int cooldown = 0)
            => new DialogueDefinition
            {
                DialogueId = id, Kind = kind, Priority = priority, TextUz = text, SpeakerType = speaker,
                CooldownDays = cooldown, Speaker = Speaker.Fuqaro
            };

        /// <summary>Muammo dialogi: birinchi marta aytilganda ko'rsatiladi (keyin "kutyapmiz" dialogi chiqadi).</summary>
        private static DialogueDefinition Problem(string concern, string text, string planAction, string planText, params StoryChoice[] extra)
        {
            DialogueDefinition d = D("p_" + concern, DialogueKind.Problem, ConcernKeys.Weight(concern) + 100, text);
            d.When.Add(C("concern.top." + concern));
            d.When.Add(C("memory.reported." + concern, "==", 0));
            d.Consequences["report." + concern] = 1;
            if (!string.IsNullOrEmpty(planAction)) d.Choices.Add(Ch(planText, planAction, "r_plan"));
            foreach (StoryChoice c in extra) d.Choices.Add(c);
            d.Choices.Add(Ch("Masalani Donishmand Vazirga yetkaz.", "forward:" + concern, "r_forward"));
            d.Choices.Add(Ch("Hozircha boshqa ishlarimiz bor.", "dismiss:" + concern, "r_dismiss"));
            return d;
        }

        private static DialogueDefinition Gratitude(string concern, string text, bool employedOnly = false)
        {
            DialogueDefinition d = D("g_" + concern, DialogueKind.Gratitude, 300, text);
            d.When.Add(C("resolved." + concern));
            if (employedOnly) d.When.Add(C("citizen.employed"));
            d.Consequences["thank." + concern] = 1;
            d.Consequences["trust"] = 10;
            d.Choices.Add(Ch("Xalq roziligi - bizning burchimiz.", "close"));
            return d;
        }

        private static DialogueDefinition History(string id, string text, string source)
        {
            DialogueDefinition d = D(id, DialogueKind.History, 30, text, "any", 14);
            d.IsHistoricalFact = true;
            d.SourceNote = source;
            d.When.Add(C("concern.any", "==", 0));
            d.Choices.Add(Ch("Qiziq ma'lumot, rahmat.", "ack"));
            return d;
        }

        // ------------------------------------------------------------------
        // Kanonik katalog
        // ------------------------------------------------------------------
        public static List<DialogueDefinition> BuildDefault()
        {
            var list = new List<DialogueDefinition>();

            // ============ MUAMMO ============
            list.Add(Problem(ConcernKeys.Unemployed,
                "Hazratim, mahallamizda ish topish qiyinlashmoqda. Shaharda {unemployed} kishi ishsiz, bo'sh ish o'rni esa {freeJobs} ta. Yangi ustaxona yoki bozor ochilsa, ko'plab fuqarolar ish bilan ta'minlanardi.",
                "plan:Craft", "Yangi ish o'rinlari yaratish rejasini tuzamiz.",
                Ch("Mahallangizning iqtisodiy holatini ko'rsat.", "show_district")));

            list.Add(Problem(ConcernKeys.Poverty,
                "Hazratim, oilamizda {members} jon bor, ishlaydiganimiz esa {working} nafar. Bozorda non olishga pulimiz yetmay qoldi, jamg'armamiz {savings} tanga.",
                "plan:Craft", "Ish o'rinlarini ko'paytirish rejasini tuzamiz.",
                Ch("Oilangiz ahvolini ko'rsat.", "show_household")));

            list.Add(Problem(ConcernKeys.Housing,
                "Hazratim, uyimiz tor, mahallada bo'sh uy qolmadi. Yangi uylar qurilsa, yosh oilalar ham boshpanali bo'lardi.",
                "plan:Residential", "Yangi turar joy rejasini tuzamiz.",
                Ch("Mahallangizning holatini ko'rsat.", "show_district")));

            list.Add(Problem(ConcernKeys.FoodPrice,
                "Hazratim, bozorda oziq-ovqat narxi {foodPrice} tangaga chiqdi. Avvalgidek xarid qila olmayapmiz. Bog'lar ko'paysa, narx tushardi.",
                "plan:Agricultural", "Bog'larni ko'paytirish rejasini tuzamiz.",
                Ch("Bozor narxlarini ko'rsat.", "show_prices")));

            list.Add(Problem(ConcernKeys.HighTax,
                "Hazratim, aholi solig'i {tax} foizga yetdi. Topganimizning ko'pi soliqqa ketmoqda, ro'zg'or tebratish og'irlashdi.",
                "lower_tax", "Aholi solig'ini 5 foizga kamaytiramiz."));

            list.Add(Problem(ConcernKeys.NoRoad,
                "Hazratim, uyimizga olib boradigan ko'cha yo'q. Yomg'irda loydan o'tib bo'lmaydi. Mahallamizni bosh ko'chaga ulab bersangiz.",
                "plan:improve", "Yo'l rejasini tuzamiz.",
                Ch("Mahallangizning holatini ko'rsat.", "show_district")));

            list.Add(Problem(ConcernKeys.NoMarket,
                "Hazratim, mahallamizda savdo qiladigan joy yetishmaydi, eng yaqin rasta ham olisda. Yangi bozor ochilsa, xalq ham, xazina ham yutardi.",
                "plan:Commercial", "Savdo mahallasi rejasini tuzamiz.",
                Ch("Mahallangizning holatini ko'rsat.", "show_district")));

            list.Add(Problem(ConcernKeys.Security,
                "Hazratim, mahallamizda soqchi kam, kechalari notinch. Bir qo'riqlash minorasi qurilsa, ko'nglimiz tinch bo'lardi.",
                "build:Tower", "Minora qurish joyini tanlaymiz."));

            list.Add(Problem(ConcernKeys.NoSchool,
                "Hazratim, bolalarimiz savod chiqaradigan joy yo'q. Madrasa qurilsa, ilmli avlod yetishardi.",
                "plan:Cultural", "Madaniyat mahallasi rejasini tuzamiz."));

            // ---- Muammo aytilgan, hali hal bo'lmagan: takror emas, "kutyapmiz" ----
            DialogueDefinition waiting = D("f_waiting", DialogueKind.Problem, 160,
                "Hazratim, murojaatimiz Donishmand Vazirga yetkazilgan edi. Hali ham umid bilan kutyapmiz: {reportedLabel}.");
            waiting.When.Add(C("memory.any_forward"));
            waiting.Choices.Add(Ch("Vazir hisobotini ko'raylik.", "open_reports"));
            waiting.Choices.Add(Ch("Sabr qiling, ish ketyapti.", "close"));
            list.Add(waiting);

            DialogueDefinition again = D("f_again", DialogueKind.Problem, 150,
                "Hazratim, o'tgan safar aytgan gapim hali ham o'sha: {reportedLabel}. Bu masalaga e'tibor qaratsangiz.");
            again.When.Add(C("memory.any_report"));
            again.When.Add(C("memory.any_forward", "==", 0));
            again.Choices.Add(Ch("Masalani Donishmand Vazirga yetkaz.", "forward:top", "r_forward"));
            again.Choices.Add(Ch("Mahallangizning holatini ko'rsat.", "show_district"));
            again.Choices.Add(Ch("Eslab qolaman.", "close"));
            list.Add(again);

            // ============ MINNATDORCHILIK (faqat haqiqatan hal bo'lganda) ============
            list.Add(Gratitude(ConcernKeys.Unemployed,
                "Hazratim, mahallamizda yangi ish o'rinlari ochilganidan xursandmiz. Men ham endi {profession} bo'lib ishlayapman. Oilamiz sizni duo qilmoqda!", true));
            list.Add(Gratitude(ConcernKeys.Poverty,
                "Hazratim, ro'zg'orimiz o'nglandi: dasturxonimizda yana non bor. Murojaatimiz e'tiborsiz qolmadi, rahmat."));
            list.Add(Gratitude(ConcernKeys.Housing,
                "Hazratim, mahallamizda yangi uylar qad rostladi. Endi oilamiz keng yashayapti."));
            list.Add(Gratitude(ConcernKeys.FoodPrice,
                "Hazratim, bozorda narxlar joyiga tushdi: oziq-ovqat hozir {foodPrice} tanga. Xalq yengil nafas oldi."));
            list.Add(Gratitude(ConcernKeys.HighTax,
                "Hazratim, soliq yengillashgani ro'zg'orimizga baraka kiritdi. Hozir aholi solig'i {tax} foiz."));
            list.Add(Gratitude(ConcernKeys.NoRoad,
                "Hazratim, uyimizgacha ko'cha tushdi! Endi bozorga ham, ishga ham bemalol qatnayapmiz."));
            list.Add(Gratitude(ConcernKeys.NoMarket,
                "Hazratim, mahallamizga bozor yaqinlashdi. Xarid uchun endi olisga bormaymiz."));
            list.Add(Gratitude(ConcernKeys.Security,
                "Hazratim, mahallamizda soqchilar ko'paydi. Kechalari tinch uxlayapmiz."));
            list.Add(Gratitude(ConcernKeys.NoSchool,
                "Hazratim, madrasa ochildi. Bolalarimiz endi savod chiqaryapti."));

            // Muammo o'zida qolgan, ammo mahallada yengillashgan — halol javob.
            DialogueDefinition easing = D("g_easing_unemployed", DialogueKind.Reaction, 250,
                "Hazratim, mahallada ish o'rinlari ko'paydi, ko'pchilik ishga joylashdi. Men hali ish topmadim, ammo navbat menga ham yetar degan umiddaman.");
            easing.When.Add(C("easing." + ConcernKeys.Unemployed));
            easing.Consequences["ease." + ConcernKeys.Unemployed] = 1;
            easing.Consequences["trust"] = 4;
            easing.Choices.Add(Ch("Albatta yetadi, sabr qiling.", "close"));
            list.Add(easing);

            // ============ SHAHARDAGI O'ZGARISHGA MUNOSABAT ============
            DialogueDefinition e1 = D("e_market_built", DialogueKind.Reaction, 60, "Shaharda yangi bozor ochilibdi, hazratim! Bozor ko'paysa, savdo ham, ish ham ko'payadi.");
            e1.When.Add(C("event.market_built")); e1.Choices.Add(Ch("Xalq uchun qurildi.", "close")); list.Add(e1);

            DialogueDefinition e2 = D("e_tax_lowered", DialogueKind.Reaction, 60, "Soliq yengillashganini eshitdik, hazratim. Hozir aholi solig'i {tax} foiz ekan - ro'zg'orga yengillik bo'ldi.");
            e2.When.Add(C("event.tax_lowered")); e2.Choices.Add(Ch("Xalq farovonligi muhim.", "close")); list.Add(e2);

            DialogueDefinition e3 = D("e_tax_raised", DialogueKind.Reaction, 60, "Hazratim, soliq oshgani bizni tashvishga soldi. Hozir aholi solig'i {tax} foiz.");
            e3.When.Add(C("event.tax_raised")); e3.Choices.Add(Ch("Bu vaqtinchalik chora.", "close")); list.Add(e3);

            DialogueDefinition e4 = D("e_school_built", DialogueKind.Reaction, 60, "Madrasa qurilganidan butun mahalla xursand, hazratim. Ilm - eng katta boylik.");
            e4.When.Add(C("event.school_built")); e4.Choices.Add(Ch("Ilmli xalq - kuchli saltanat.", "close")); list.Add(e4);

            DialogueDefinition e5 = D("e_jobs_up", DialogueKind.Reaction, 58, "Shaharda ish o'rinlari ko'payganini eshitdik, hazratim. Hozir bo'sh ish o'rni {freeJobs} ta, ishsizlar {unemployed} nafar.");
            e5.When.Add(C("event.jobs_up")); e5.Choices.Add(Ch("Hali davom ettiramiz.", "close")); list.Add(e5);

            DialogueDefinition e6 = D("e_land_opened", DialogueKind.Reaction, 56, "Shaharga yangi hudud qo'shilibdi, hazratim. Yangi mahallada uy va bozor qurilsa, ko'p oilalar ko'chib boradi.");
            e6.When.Add(C("event.land_opened")); e6.Choices.Add(Ch("Reja shunday.", "close")); list.Add(e6);

            // ============ ISH BILAN BOG'LIQ ============
            DialogueDefinition w1 = D("w_savdogar", DialogueKind.Work, 45, "Rastamda {product} sotaman, hazratim. Hozir narxi {price} tanga, zaxirada {stock} birlik bor. {tradeStatus}.", "savdogar", 2);
            w1.When.Add(C("merchant.any"));
            w1.Choices.Add(Ch("Narxlarni ko'rsat.", "show_prices"));
            w1.Choices.Add(Ch("Bozorni ko'rsat.", "focus_market"));
            w1.Choices.Add(Ch("Savdoingiz baroridan kelsin.", "close"));
            list.Add(w1);

            DialogueDefinition w2 = D("w_bogbon", DialogueKind.Work, 45, "Bog'da ishlar qizg'in, hazratim. Kecha shahar bog'lari {foodProduced} birlik hosil berdi, omborda {foodStock} birlik zaxira bor. Suv va mehnat bo'lsa, yer saxiy.", "bogbon", 2);
            w2.Choices.Add(Ch("Hosilingiz mo'l bo'lsin.", "close")); list.Add(w2);

            DialogueDefinition w3 = D("w_tashuvchi", DialogueKind.Work, 45, "Karvonsaroyda yuk tushiryapmiz, hazratim. Omborda hozir {clothStock} birlik mato bor. Karvon yo'li tinch bo'lsa, mol uzilmaydi.", "tashuvchi", 2);
            w3.Choices.Add(Ch("Yo'lingiz bexatar bo'lsin.", "close")); list.Add(w3);

            DialogueDefinition w4 = D("w_soqchi", DialogueKind.Work, 45, "Navbatchilikdaman, hazratim. Shahar xavfsizligi darajasi {security}. Devor va minoralar mustahkam bo'lsa, xalq tinch.", "soqchi", 2);
            w4.Choices.Add(Ch("Hushyor bo'ling.", "close")); list.Add(w4);

            DialogueDefinition w5 = D("w_amaldor", DialogueKind.Work, 45, "Devonda hisob-kitob bilan bandmiz, hazratim. Xazinada {coins} tanga bor, kechagi tushum {income} tanga bo'ldi.", "amaldor", 2);
            w5.Choices.Add(Ch("Iqtisodiyot hisobotini och.", "show_dashboard"));
            w5.Choices.Add(Ch("Hisob aniq bo'lsin.", "close"));
            list.Add(w5);

            DialogueDefinition w6 = D("w_muallim", DialogueKind.Work, 45, "Madrasada shogirdlarga saboq beryapman, hazratim. Shahar savodxonligi hozir {education} darajada.", "muallim", 2);
            w6.Choices.Add(Ch("Ilm yo'lida charchamang.", "close")); list.Add(w6);

            // ============ TAKLIF (haqiqiy ehtiyojdan) ============
            DialogueDefinition s1 = D("s_garden", DialogueKind.Suggestion, 50, "Hazratim, ombordagi zaxira {foodStock} birlik - bu {population} kishilik shaharga ko'pga yetmaydi. Yana bir bog' barpo etsak bo'lardi.", "any", 6);
            s1.When.Add(C("city.foodDays", "<", 4));
            s1.When.Add(C("concern.any", "==", 0));
            s1.Choices.Add(Ch("Bog' qurish joyini tanlaymiz.", "build:Garden"));
            s1.Choices.Add(Ch("O'ylab ko'ramiz.", "close"));
            list.Add(s1);

            DialogueDefinition s2 = D("s_market", DialogueKind.Suggestion, 50, "Hazratim, bozorlarimiz tor kelyapti: bugun xaridorlarning bir qismi molsiz qaytdi. Yana bir bozor qurilsa, savdo kengayardi.", "any", 6);
            s2.When.Add(C("city.marketShortagePct", ">=", 10));
            s2.When.Add(C("concern.any", "==", 0));
            s2.Choices.Add(Ch("Bozor qurish joyini tanlaymiz.", "build:Market"));
            s2.Choices.Add(Ch("O'ylab ko'ramiz.", "close"));
            list.Add(s2);

            // ============ ODDIY SUHBAT ============
            DialogueDefinition c1 = D("c_city_good", DialogueKind.Casual, 22, "Shahrimiz obod, hazratim. Aholi {population} nafarga yetdi, ko'pchilik ahvolidan rozi.", "any", 1);
            c1.When.Add(C("city.happiness", ">=", 65)); c1.When.Add(C("concern.any", "==", 0));
            c1.Choices.Add(Ch("Oilangiz haqida gapirib bering.", "talk", "c_family"));
            c1.Choices.Add(Ch("Omon bo'ling.", "close"));
            list.Add(c1);

            DialogueDefinition c2 = D("c_city_mid", DialogueKind.Casual, 20, "Kun kechiryapmiz, hazratim. Shaharda yaxshi kunlar ham, tashvishlar ham bor. Aholi {population} nafar.", "any", 1);
            c2.When.Add(C("concern.any", "==", 0));
            c2.Choices.Add(Ch("Oilangiz haqida gapirib bering.", "talk", "c_family"));
            c2.Choices.Add(Ch("Omon bo'ling.", "close"));
            list.Add(c2);

            DialogueDefinition c3 = D("c_market_day", DialogueKind.Casual, 21, "Bugun bozorlarda {customers} xaridor bo'ldi, hazratim. Bozor gavjum bo'lsa, shahar tirik.", "any", 2);
            c3.When.Add(C("city.customers", ">=", 1)); c3.When.Add(C("concern.any", "==", 0));
            c3.Choices.Add(Ch("Bozor narxlarini ko'rsat.", "show_prices"));
            c3.Choices.Add(Ch("Omon bo'ling.", "close"));
            list.Add(c3);

            // Hech narsa mos kelmaganda ham xavfsiz javob bo'lishi uchun (shartsiz).
            DialogueDefinition c0 = D("c_fallback", DialogueKind.Casual, 1, "Assalomu alaykum, hazratim. {district}da yashaymiz, kunimiz o'tib turibdi.");
            c0.Choices.Add(Ch("Oilangiz haqida gapirib bering.", "talk", "c_family"));
            c0.Choices.Add(Ch("Omon bo'ling.", "close"));
            list.Add(c0);

            DialogueDefinition fam = D("c_family", DialogueKind.Casual, 0, "Oilamiz {members} kishidan iborat, {district}da yashaymiz. Ishlaydiganimiz {working} nafar, jamg'armamiz {savings} tanga atrofida.");
            fam.FollowUpOnly = true;
            fam.Choices.Add(Ch("Oilangiz ahvolini ko'rsat.", "show_household"));
            fam.Choices.Add(Ch("Xonadoningizga baraka.", "close"));
            list.Add(fam);

            // ============ TARIXIY MA'LUMOT (badiiy so'zdan alohida belgilanadi) ============
            list.Add(History("h_bobur",
                "Bilasizmi, hazratim, Zahiriddin Muhammad Bobur 1483-yilda Andijonda tug'ilgan. U 'Boburnoma' asarida Andijon haqida yozib qoldirgan.",
                "Zahiriddin Muhammad Bobur, 'Boburnoma'"));
            list.Add(History("h_silk",
                "Farg'ona vodiysi qadimdan ipakchilik va to'qimachilik bilan mashhur. Marg'ilon atlasi hozir ham dong taratgan.",
                "Umumma'lum ensiklopedik ma'lumot (tarixchi tekshiruvi kutilmoqda)"));
            list.Add(History("h_rishton",
                "Vodiydagi Rishton kulollari ko'k-feruza sirli sopol buyumlari bilan tanilgan.",
                "Umumma'lum ensiklopedik ma'lumot (tarixchi tekshiruvi kutilmoqda)"));
            list.Add(History("h_silkroad",
                "Buyuk ipak yo'lining tarmoqlari Farg'ona vodiysi orqali o'tgan, karvonlar shu yerlardagi shaharlarda qo'nib o'tgan.",
                "Umumma'lum ensiklopedik ma'lumot (tarixchi tekshiruvi kutilmoqda)"));

            // ============ ZANJIR JAVOBLARI (tanlovdan keyingi munosabat) ============
            DialogueDefinition rPlan = D("r_plan", DialogueKind.Reaction, 0, "Rahmat, hazratim! Reja tayyor bo'lib, siz tasdiqlasangiz, butun mahalla duo qiladi.");
            rPlan.FollowUpOnly = true; list.Add(rPlan);
            DialogueDefinition rFwd = D("r_forward", DialogueKind.Reaction, 0, "Minnatdormiz, hazratim. Donishmand Vazir chorasini topadi, deb umid qilamiz.");
            rFwd.FollowUpOnly = true; list.Add(rFwd);
            DialogueDefinition rDis = D("r_dismiss", DialogueKind.Reaction, 0, "Tushunarli, hazratim... Vaqtingiz bo'lganda bizni ham yodga olarsiz.");
            rDis.FollowUpOnly = true; list.Add(rDis);

            return list;
        }
    }
}
