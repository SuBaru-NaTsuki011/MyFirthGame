using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Battles;
using AndijonSaltanatlar.CivilizationsAI;
using AndijonSaltanatlar.Core;
using AndijonSaltanatlar.Diplomacy;
using AndijonSaltanatlar.Kingdoms;
using AndijonSaltanatlar.Regions;
using AndijonSaltanatlar.Strategy;
using AndijonSaltanatlar.Trade;

namespace AndijonSaltanatlar.World
{
    /// <summary>
    /// Strategik dunyo orkestri: saltanatlar, hududlar, diplomatiya, savdo va AI qarorlarini bog'laydi.
    /// Shahar simulyatsiyasidan mustaqil (server-authoritative tizimga ko'chirishga mos). Har o'yin
    /// kunida bir marta StrategicTick chaqiriladi — AI qaror qabul qiladi, savdo va daromad hisoblanadi.
    /// </summary>
    public sealed class WorldSimulation
    {
        public const int GiftCost = 100;
        public const int AnnexCost = 250;

        private readonly Random _rng = new Random(GameConfig.WorldSeed + 505);
        private readonly AIKingdomController _ai = new AIKingdomController(GameConfig.WorldSeed + 707);

        public RegionManager Regions { get; } = new RegionManager();
        public KingdomManager Kingdoms { get; } = new KingdomManager();
        public DiplomacyManager Diplomacy { get; } = new DiplomacyManager();
        public InternationalTradeSystem Trade { get; } = new InternationalTradeSystem();
        public BattleManager Battles { get; } = new BattleManager();
        public List<string> Events { get; } = new List<string>();

        private int _playerCityPopulation = 40;
        private int _playerCityDefense = 0;

        public WorldSimulation()
        {
            Kingdoms.Initialize(Regions);
            Diplomacy.Set(1, 2, 10);
            Diplomacy.Set(1, 3, -5);
            Diplomacy.Set(2, 3, -15);
            AddEvent("Saltanat xaritasi ochildi. Asaka, Andijon va Shahrixon hukmdorlari sahnada.");
        }

        public Kingdom PlayerKingdom => Kingdoms.Player;

        // ------------------------------------------------------------------
        // Voqealar
        // ------------------------------------------------------------------
        public void AddEvent(string text)
        {
            if (Events.Count > 0 && Events[Events.Count - 1] == text) return; // takrorni oldini olish
            Events.Add(text);
            if (Events.Count > 40) Events.RemoveAt(0);
        }

        // ------------------------------------------------------------------
        // Kunlik strategik tick
        // ------------------------------------------------------------------
        public void SyncPlayer(int coins, int population, int militaryStrength, int cityDefense)
        {
            PlayerKingdom.Treasury = coins;
            PlayerKingdom.MilitaryStrength = militaryStrength;
            _playerCityPopulation = population;
            _playerCityDefense = cityDefense;
            Region asaka = Regions.Get(PlayerKingdom.CapitalRegionId);
            if (asaka != null) { asaka.Population = population; asaka.Food = population; asaka.Security = Math.Min(100, cityDefense / 2); }
        }

        /// <summary>Bir o'yin kunini simulyatsiya qiladi. O'yinchining qo'shimcha hudud daromadini qaytaradi.</summary>
        public int StrategicTick(int day)
        {
            // AI strategik qarorlarni har 2 kunda bir marta baholaydi (render siklini sekinlashtirmaydi)
            if (day % 2 == 0)
                foreach (Kingdom k in Kingdoms.AIKingdoms()) _ai.Run(k, this, day);

            Trade.Tick(Kingdoms);

            int playerDelta = MaybeAIAttackPlayer(day);
            foreach (Region r in Regions.Regions)
            {
                if (r.OwnerKingdomId == 0 || r.IsActiveCity) continue;
                Kingdom k = Kingdoms.Get(r.OwnerKingdomId);
                if (k == null) continue;
                int inc = r.DailyIncome / 5 + 1;
                if (k.Id == Kingdoms.PlayerKingdomId) playerDelta += inc;
                else k.Treasury += inc;
            }

            Kingdoms.RecomputeStats(Regions);
            PublishCityProgressionEvents(day);
            return playerDelta;
        }

        /// <summary>AI qarorlar natijasida yuz bergan shahar rivojlanishi haqida vizual voqea yozib chiqaradi (Prompt 8, 21).</summary>
        private void PublishCityProgressionEvents(int day)
        {
            if (day % 3 != 0) return; // har 3 kunda bir xabar
            foreach (Kingdom k in Kingdoms.AIKingdoms())
            {
                Region cap = Regions.Get(k.CapitalRegionId);
                if (cap == null) continue;
                if (!_lastReportedLevel.TryGetValue(k.Id, out int reported)) reported = 0;
                if (cap.Development >= reported + 10)
                {
                    _lastReportedLevel[k.Id] = cap.Development;
                    AddEvent(k.Name + " — " + cap.Name + " shahri yangi darajaga erishdi (rivojanish: " + cap.Development + ").");
                }
            }
        }

        private readonly Dictionary<int, int> _lastReportedLevel = new Dictionary<int, int>();

        /// <summary>AI saltanat harbiy quvvatini oshiradi (xazina evaziga).</summary>
        public bool Militarize(Kingdom k)
        {
            if (k.Treasury < 80) return false;
            k.Treasury -= 80;
            k.MilitaryStrength = Math.Min(400, k.MilitaryStrength + 12);
            return true;
        }

        /// <summary>Munosabati keskin dushman bo'lgan kuchli AI o'yinchiga hujum qilishi mumkin.</summary>
        private int MaybeAIAttackPlayer(int day)
        {
            foreach (Kingdom k in Kingdoms.AIKingdoms())
            {
                int rel = Diplomacy.Get(Kingdoms.PlayerKingdomId, k.Id);
                if (rel > -40 || k.AI.RiskTolerance < 0.6f) continue;
                if (k.MilitaryStrength < PlayerKingdom.MilitaryStrength) continue;
                if (_rng.NextDouble() > 0.25) continue;

                Region cap = Regions.Get(PlayerKingdom.CapitalRegionId);
                BattleResult r = BattleSimulationService.Simulate(k.Name, PlayerKingdom.Name, cap.Name,
                    k.MilitaryStrength, PlayerKingdom.MilitaryStrength, _playerCityDefense, day, _rng);
                int penalty = 0;
                if (r.Outcome == BattleOutcome.Galaba)
                {
                    penalty = -60; r.CoinChange = penalty;
                    r.TerritoryResult = "Asaka omon qoldi, ammo xazina talofat ko'rdi.";
                    r.DiplomaticResult = "Munosabatlar yanada keskinlashdi.";
                    Diplomacy.Add(Kingdoms.PlayerKingdomId, k.Id, -5);
                    AddEvent(k.Name + " Asakaga hujum qildi va xazinaga zarar yetkazdi!");
                }
                else
                {
                    r.TerritoryResult = "Asaka mudofaasi hujumni qaytardi.";
                    r.DiplomaticResult = "Shahar omon qoldi.";
                    Diplomacy.Add(Kingdoms.PlayerKingdomId, k.Id, -3);
                    AddEvent("Asaka mudofaasi " + k.Name + " hujumini qaytardi!");
                }
                r.Summary = BattleResult.OutcomeName(r.Outcome);
                Battles.Record(r);
                return penalty;
            }
            return 0;
        }

        public int PlayerExtraPopulation()
        {
            int extra = 0;
            foreach (Region r in Regions.OwnedBy(Kingdoms.PlayerKingdomId))
                if (!r.IsActiveCity) extra += r.Population;
            return extra;
        }

        // ------------------------------------------------------------------
        // AI mutatsiyalari (AIKingdomController chaqiradi)
        // ------------------------------------------------------------------
        public bool HasNeutralRegion()
        {
            foreach (Region r in Regions.Regions) if (r.OwnerKingdomId == 0) return true;
            return false;
        }

        public string DevelopRegion(Kingdom k)
        {
            if (k.Treasury < 20) return null;
            Region target = null;
            foreach (Region r in Regions.OwnedBy(k.Id))
                if (target == null || r.Development < target.Development) target = r;
            if (target == null) return null;
            target.Development = Math.Min(100, target.Development + 3);
            k.Treasury -= 20;
            return target.Name;
        }

        public bool Stabilize(Kingdom k)
        {
            if (k.Treasury < 15) return false;
            k.Stability = Math.Min(100, k.Stability + 4);
            k.Treasury -= 15;
            return true;
        }

        public string AIExpand(Kingdom k, int day)
        {
            Region cap = Regions.Get(k.CapitalRegionId);
            Region best = null; float bestD = float.MaxValue;
            foreach (Region r in Regions.Regions)
            {
                if (r.OwnerKingdomId != 0) continue;
                float dx = r.X - cap.X, dy = r.Y - cap.Y;
                float d = dx * dx + dy * dy;
                if (d < bestD) { bestD = d; best = r; }
            }
            if (best == null || k.Treasury < 220) return null;
            best.OwnerKingdomId = k.Id;
            k.Treasury -= 200;
            Kingdoms.RecomputeStats(Regions);
            return best.Name;
        }

        public void AIProposeTradeToPlayer(Kingdom k)
        {
            Trade.Propose(k.Id, Kingdoms.PlayerKingdomId, "Oziq-ovqat", 20, 30, 10);
        }

        // ------------------------------------------------------------------
        // O'yinchi harakatlari (StrategicView chaqiradi)
        // ------------------------------------------------------------------
        public string PlayerSendEnvoy(int target)
        {
            Diplomacy.Add(Kingdoms.PlayerKingdomId, target, 5);
            string msg = Kingdoms.Get(target).Name + "ga elchi yuborildi (+5 munosabat).";
            AddEvent(msg);
            return msg;
        }

        public string PlayerGift(int target)
        {
            Diplomacy.Add(Kingdoms.PlayerKingdomId, target, 12);
            string msg = Kingdoms.Get(target).Name + "ga sovg'a yuborildi (+12 munosabat).";
            AddEvent(msg);
            return msg;
        }

        public string PlayerProposeTrade(int target)
        {
            int rel = Diplomacy.Get(Kingdoms.PlayerKingdomId, target);
            if (rel >= 0)
            {
                Diplomacy.AddTreaty(TreatyType.TradePact, Kingdoms.PlayerKingdomId, target, 0);
                Trade.Propose(Kingdoms.PlayerKingdomId, target, "Mato", 15, 25, 12).Status = TradeStatus.Active;
                Diplomacy.Add(Kingdoms.PlayerKingdomId, target, 8);
                string msg = Kingdoms.Get(target).Name + " savdo taklifini qabul qildi.";
                AddEvent(msg);
                return msg;
            }
            Diplomacy.Add(Kingdoms.PlayerKingdomId, target, -2);
            string no = Kingdoms.Get(target).Name + " savdo taklifini rad etdi.";
            AddEvent(no);
            return no;
        }

        public string PlayerProposeAlliance(int target)
        {
            int rel = Diplomacy.Get(Kingdoms.PlayerKingdomId, target);
            if (rel >= RelationBands.FriendlyMax + 1)
            {
                Diplomacy.AddTreaty(TreatyType.Alliance, Kingdoms.PlayerKingdomId, target, 0);
                Diplomacy.Add(Kingdoms.PlayerKingdomId, target, 5);
                string msg = Kingdoms.Get(target).Name + " bilan ITTIFOQ tuzildi!";
                AddEvent(msg);
                return msg;
            }
            string no = Kingdoms.Get(target).Name + " ittifoq uchun munosabat yetarli emas (60+ kerak).";
            AddEvent(no);
            return no;
        }

        public bool IsNeutral(int regionId) => Regions.Get(regionId)?.OwnerKingdomId == 0;

        public string PlayerAnnexNeutral(int regionId)
        {
            Region r = Regions.Get(regionId);
            if (r == null || r.OwnerKingdomId != 0) return null;
            r.OwnerKingdomId = Kingdoms.PlayerKingdomId;
            Kingdoms.RecomputeStats(Regions);
            string msg = "Asaka Saltanati " + r.Name + " hududini tinch yo'l bilan qo'shib oldi.";
            AddEvent(msg);
            return msg;
        }

        /// <summary>O'yinchi hududga hujum qiladi — strategik jang simulyatsiyasi. Natijani qaytaradi.</summary>
        public BattleResult PlayerAttackRegion(int regionId)
        {
            Region r = Regions.Get(regionId);
            if (r == null || r.OwnerKingdomId == 0 || r.OwnerKingdomId == Kingdoms.PlayerKingdomId) return null;
            Kingdom defender = Kingdoms.Get(r.OwnerKingdomId);
            Kingdom attacker = PlayerKingdom;
            int defenseBonus = r.Security + r.Development / 2 + (r.IsCapital ? 20 : 0);

            BattleResult res = BattleSimulationService.Simulate(attacker.Name, defender.Name, r.Name,
                attacker.MilitaryStrength, defender.MilitaryStrength, defenseBonus, 0, _rng);

            if (res.Outcome == BattleOutcome.Galaba)
            {
                r.OwnerKingdomId = attacker.Id;
                Diplomacy.Add(attacker.Id, defender.Id, -30);
                Kingdoms.RecomputeStats(Regions);
                res.CoinChange = 80;
                res.TerritoryResult = r.Name + " Asaka Saltanatiga qo'shildi.";
                res.DiplomaticResult = defender.Name + " bilan munosabat keskinlashdi (-30).";
                AddEvent("Asaka qo'shini g'alaba qozondi — " + r.Name + " qo'shib olindi.");
            }
            else if (res.Outcome == BattleOutcome.Maglubiyat)
            {
                Diplomacy.Add(attacker.Id, defender.Id, -15);
                res.CoinChange = -40;
                res.TerritoryResult = "Hudud egallanmadi.";
                res.DiplomaticResult = defender.Name + " bilan munosabat yomonlashdi (-15).";
                AddEvent("Asaka qo'shini " + r.Name + " uchun jangda mag'lub bo'ldi.");
            }
            else
            {
                Diplomacy.Add(attacker.Id, defender.Id, -10);
                res.CoinChange = -15;
                res.TerritoryResult = "Hal qiluvchi natija bo'lmadi.";
                res.DiplomaticResult = "Munosabat biroz yomonlashdi.";
                AddEvent(r.Name + " uchun jang hal qiluvchi natijasiz tugadi.");
            }
            res.Summary = BattleResult.OutcomeName(res.Outcome);
            Battles.Record(res);
            return res;
        }

        // Kutilayotgan AI savdo takliflari (o'yinchiga)
        public List<TradeAgreement> PendingOffersForPlayer()
        {
            var list = new List<TradeAgreement>();
            foreach (TradeAgreement a in Trade.Agreements)
                if (a.Status == TradeStatus.Pending &&
                    (a.BuyerKingdomId == Kingdoms.PlayerKingdomId || a.SellerKingdomId == Kingdoms.PlayerKingdomId))
                    list.Add(a);
            return list;
        }

        public void AcceptOffer(TradeAgreement a)
        {
            a.Status = TradeStatus.Active;
            int other = a.SellerKingdomId == Kingdoms.PlayerKingdomId ? a.BuyerKingdomId : a.SellerKingdomId;
            Diplomacy.Add(Kingdoms.PlayerKingdomId, other, 6);
            AddEvent(Kingdoms.Get(other).Name + " bilan savdo shartnomasi tuzildi.");
        }

        public void RejectOffer(TradeAgreement a)
        {
            a.Status = TradeStatus.Rejected;
            int other = a.SellerKingdomId == Kingdoms.PlayerKingdomId ? a.BuyerKingdomId : a.SellerKingdomId;
            Diplomacy.Add(Kingdoms.PlayerKingdomId, other, -3);
            AddEvent(Kingdoms.Get(other).Name + "ning savdo taklifi rad etildi.");
        }

        // ------------------------------------------------------------------
        // Save / Load
        // ------------------------------------------------------------------
        public WorldSnapshot Snapshot()
        {
            var s = new WorldSnapshot { PlayerKingdomName = PlayerKingdom.Name };
            foreach (Region r in Regions.Regions)
                s.RegionOwner.Add(new[] { r.Id, r.OwnerKingdomId, r.Development, r.Population });
            foreach (Kingdom k in Kingdoms.Kingdoms)
                s.KingdomState.Add(new[] { k.Id, k.Treasury, k.Stability, k.Reputation });
            int[][] pairs = { new[] { 1, 2 }, new[] { 1, 3 }, new[] { 2, 3 } };
            foreach (int[] p in pairs) s.Relations.Add(new[] { p[0], p[1], Diplomacy.Get(p[0], p[1]) });
            return s;
        }

        public void Restore(WorldSnapshot s)
        {
            if (s == null) return;
            if (!string.IsNullOrEmpty(s.PlayerKingdomName)) PlayerKingdom.Name = s.PlayerKingdomName;
            foreach (int[] a in s.RegionOwner)
            {
                Region r = Regions.Get(a[0]);
                if (r != null) { r.OwnerKingdomId = a[1]; r.Development = a[2]; r.Population = a[3]; }
            }
            foreach (int[] a in s.KingdomState)
            {
                Kingdom k = Kingdoms.Get(a[0]);
                if (k != null) { k.Treasury = a[1]; k.Stability = a[2]; k.Reputation = a[3]; }
            }
            foreach (int[] a in s.Relations) Diplomacy.Set(a[0], a[1], a[2]);
            Kingdoms.RecomputeStats(Regions);
        }
    }

    /// <summary>Strategik dunyoning JSON'ga saqlanadigan holati.</summary>
    public sealed class WorldSnapshot
    {
        public string PlayerKingdomName { get; set; } = "";
        public List<int[]> RegionOwner { get; set; } = new List<int[]>();   // [id, owner, dev, pop]
        public List<int[]> KingdomState { get; set; } = new List<int[]>();  // [id, treasury, stability, reputation]
        public List<int[]> Relations { get; set; } = new List<int[]>();     // [a, b, value]
    }
}
