using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AndijonSaltanatlar.Quests;
using AndijonSaltanatlar.Narrative;
using AndijonSaltanatlar.Campaigns;
using AndijonSaltanatlar.Achievements;

namespace AndijonSaltanatlar.Content
{
    /// <summary>To'liq story kontenti to'plami (JSON serializatsiya uchun).</summary>
    public class StoryContentBundle
    {
        public CampaignDefinition Campaign { get; set; }
        public List<QuestDefinition> Quests { get; set; } = new();
        public List<DialogueDefinition> Dialogues { get; set; } = new();
        public List<AchievementDefinition> Achievements { get; set; } = new();
    }

    /// <summary>
    /// Data-driven kontent yuklovchi — PROMPT 13, 5-bo'lim. Avval JSON fayldan o'qishga urinadi
    /// (Content/story.json); fayl yo'q yoki buzuq bo'lsa, kod ichidagi kanonik StoryContent'ga qaytadi.
    /// Shunday qilib kontent JSON orqali boshqariladi, lekin o'yin hech qachon ishdan chiqmaydi.
    /// </summary>
    public static class StoryContentLoader
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>Kod ichidagi kanonik to'plam.</summary>
        public static StoryContentBundle DefaultBundle() => new()
        {
            Campaign = StoryContent.BuildCampaign(),
            Quests = StoryContent.BuildQuests(),
            Dialogues = StoryContent.BuildDialogues(),
            Achievements = StoryContent.BuildAchievements()
        };

        /// <summary>JSON fayldan yuklaydi; muvaffaqiyatsiz bo'lsa kanonik to'plamni qaytaradi.</summary>
        public static StoryContentBundle Load(string contentRoot)
        {
            try
            {
                string path = Path.Combine(contentRoot ?? "", "story.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var bundle = JsonSerializer.Deserialize<StoryContentBundle>(json, Options);
                    if (bundle != null && bundle.Quests != null && bundle.Quests.Count > 0)
                        return bundle;
                }
            }
            catch
            {
                // Buzuq JSON — jim ravishda kanonik to'plamga qaytamiz (o'yin buzilmasin).
            }
            return DefaultBundle();
        }

        public static StoryContentBundle FromJson(string json) =>
            JsonSerializer.Deserialize<StoryContentBundle>(json, Options);

        public static string ToJson(StoryContentBundle bundle) =>
            JsonSerializer.Serialize(bundle, Options);

        /// <summary>Kanonik kontentni JSON fayl sifatida eksport qiladi (data-driven tahrirlash uchun).</summary>
        public static void ExportDefault(string contentRoot)
        {
            Directory.CreateDirectory(contentRoot);
            File.WriteAllText(Path.Combine(contentRoot, "story.json"), ToJson(DefaultBundle()));
        }
    }
}
