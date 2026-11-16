using System;
using AndijonSaltanatlar.Kingdoms;
using AndijonSaltanatlar.World;

namespace AndijonSaltanatlar.CivilizationsAI
{
    /// <summary>
    /// Bitta AI saltanatni boshqaradi: haqiqiy strategik tikда vaziyatni baholab (DecisionPlanner orqali)
    /// bitta qaror qabul qiladi va uni WorldSimulation orqali bajaradi. Har bir qaror o'zbekcha
    /// voqea sifatida yozib boriladi (debug/voqealar tasmasi).
    /// Prompt 16 (AI Kingdom autonomy): AI davlatlarning o'z-o'zidan rivojlanmasligi muammosini
    /// bartaraf etish — treasury cheklovlari, AI-to-AI munosabatlar, hudud rivojlantirish darajasining
    /// ko'rsatkichlari, savdo takliflarining AI-to-AI hamkorligi.
    /// </summary>
    public sealed class AIKingdomController
    {
        private readonly DecisionPlanner _planner = new DecisionPlanner();
        private readonly Random _aiRng;

        // Universal (old-style) uzun qurilishi uchun qoldirildi — yangi seed-variant ham qo'llab-quvvatlanadi.
        public AIKingdomController()
        {
            _aiRng = new Random(GameConfig.WorldSeed + 707);
        }

        public AIKingdomController(int seed)
        {
            _aiRng = new Random(seed);
        }

        public void Run(Kingdom k, WorldSimulation w, int day)
        {
            AIDecision d = _planner.Plan(k, w, _aiRng);
            switch (d)
            {
                case AIDecision.DevelopEconomy:
                    string rn = w.DevelopRegion(k);
                    if (rn != null) w.AddEvent(k.RulerName + " " + rn + " hududida iqtisodni rivojlantirdi.");
                    break;

                case AIDecision.Stabilize:
                    if (w.Stabilize(k)) w.AddEvent(k.RulerName + " saltanat barqarorligini mustahkamladi.");
                    break;

                case AIDecision.Expand:
                    string reg = w.AIExpand(k, day);
                    if (reg != null)
                    {
                        w.AddEvent(k.Name + " " + reg + " hududini o'z tasarrufiga oldi.");
                        // Yangi hudud ochildiktan keyin strategik regen boshlanadi (prompt 4/6)
                        foreach (Kingdom otherAI in w.Kingdoms.AIKingdoms())
                        {
                            if (otherAI.Id == k.Id) continue;
                            if (w.Diplomacy.Get(otherAI.Id, k.Id) >= 30)
                            {
                                w.AddEvent(k.Name + " va " + otherAI.Name + " o'rtasida yangi savdo yo'li amalga oshirildi.");
                            }
                        }
                    }
                    break;

                case AIDecision.Militarize:
                    if (w.Militarize(k)) w.AddEvent(k.RulerName + " qo'shin kuchini oshirdi.");
                    break;

                case AIDecision.Diplomacy:
                    // AI-to-AI diplomatiya hamkorligi (Prompt 7)
                    int pid = w.Kingdoms.PlayerKingdomId;
                    foreach (var other in w.Kingdoms.AIKingdoms())
                    {
                        if (other.Id == k.Id) continue;
                        int rel = w.Diplomacy.Get(k.Id, other.Id);
                        if (rel < 50 && _aiRng.NextDouble() < 0.3f)
                        {
                            w.Diplomacy.Add(k.Id, other.Id, 3);
                            w.AddEvent(k.RulerName + " — " + other.Name + " bilan munosabatlarni yaxshiladi.");
                        }
                    }

                    if (k.AI.EconomicFocus > 0.6f && !w.Diplomacy.HasTradePact(k.Id, pid))
                    {
                        w.AIProposeTradeToPlayer(k);
                        w.AddEvent(k.RulerName + " Asaka Saltanatiga savdo taklif qildi.");
                    }
                    else
                    {
                        w.Diplomacy.Add(k.Id, pid, 4);
                        w.AddEvent(k.RulerName + " Asaka bilan aloqalarni yaxshiladi.");
                    }
                    break;
            }
        }
    }
}
