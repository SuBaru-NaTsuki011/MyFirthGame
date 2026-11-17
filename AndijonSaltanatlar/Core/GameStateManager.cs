using System;
using System.Collections.Generic;
using AndijonSaltanatlar.Advisor;
using AndijonSaltanatlar.AdvancedEconomy;
using AndijonSaltanatlar.Buildings;
using AndijonSaltanatlar.Campaigns;
using AndijonSaltanatlar.Civilization;
using AndijonSaltanatlar.Defense;
using AndijonSaltanatlar.Economy;
using AndijonSaltanatlar.Environment;
using AndijonSaltanatlar.Government;
using AndijonSaltanatlar.Kingdoms;
using AndijonSaltanatlar.LandExpansion;
using AndijonSaltanatlar.Military;
using AndijonSaltanatlar.Population;
using AndijonSaltanatlar.Progression;
using AndijonSaltanatlar.Story;
using AndijonSaltanatlar.Technology;
using AndijonSaltanatlar.World;
using Microsoft.Xna.Framework;

namespace AndijonSaltanatlar.Core;

public sealed class GameStateManager
{
	private readonly ResourceManager _resources = new ResourceManager(500, 200);

	private readonly EconomyManager _economy;

	public GameMode Mode { get; private set; } = GameMode.Playing;

	public bool Paused { get; set; }

	public bool ShowHelp { get; set; }

	public int Speed { get; private set; } = 1;

	public GameClock Clock { get; } = new GameClock();

	public ResourceManager Resources => _resources;

	public EconomyManager Economy => _economy;

	public GovernmentPolicy Policy { get; } = new GovernmentPolicy();

	public PopulationManager Pop { get; } = new PopulationManager();

	public PetitionManager Petitions { get; } = new PetitionManager();

	public WorldSimulation World { get; } = new WorldSimulation();

	public ArmyManager Military { get; } = new ArmyManager();

	public CivilizationManager Civ { get; } = new CivilizationManager();

	public CityLandExpansionManager Land { get; } = new CityLandExpansionManager();

	/// <summary>Yer ochildi — bulutga darhol saqlash kerak (Game1 tekshiradi va tushiradi).</summary>
	public bool LandChanged { get; set; }

	public AdvisorManager Advisor { get; } = new AdvisorManager();

	public WeatherManager Weather { get; } = new WeatherManager();

	/// <summary>Shahar hayoti (mahallalar, murojaatlar, voqealar) — PROMPT 17/18 yagona egasi.</summary>
	public CityLifeManager Life { get; } = new CityLifeManager();

	public Season Season => SeasonManager.Of(Clock.Month);

	public StoryModeManager Story { get; private set; }

	public bool IsStoryMode { get; set; }

	public CampaignState PendingCampaignState { get; set; }

	public int Coins
	{
		get
		{
			return _resources.Get(ResourceType.Coin);
		}
		set
		{
			_resources.Set(ResourceType.Coin, value);
		}
	}

	public int Food
	{
		get
		{
			return _resources.Get(ResourceType.Food);
		}
		set
		{
			_resources.Set(ResourceType.Food, value);
		}
	}

	public int Population { get; private set; }

	public int Day => Clock.Day;

	public BuildingType SelectedBuildType { get; private set; }

	public object Selected { get; private set; }

	public string Message { get; private set; }

	public float MessageTimer { get; private set; }

	public bool MessageIsError { get; private set; }

	public GameStateManager()
	{
		_economy = new EconomyManager(_resources);
	}

	public void EnterBuildMode(BuildingType type)
	{
		Mode = GameMode.BuildMode;
		SelectedBuildType = type;
		Selected = null;
	}

	public void CancelBuild()
	{
		Mode = GameMode.Playing;
	}

	public void Select(object obj)
	{
		Selected = obj;
	}

	public void ShowMessage(string text, bool isError = false)
	{
		Message = text;
		MessageIsError = isError;
		MessageTimer = 2.5f;
	}

	public bool CanAfford(int cost)
	{
		return _resources.CanAfford(ResourceType.Coin, cost);
	}

	public void CycleSpeed()
	{
		Speed = ((Speed >= 4) ? 1 : (Speed * 2));
	}

	public void SetSpeed(int s)
	{
		Speed = ((s != 2 && s != 4) ? 1 : s);
	}

	public void ResolvePetition(int choice)
	{
		Petitions.Resolve(choice, Pop, _resources, Policy);
	}

	public void InitPopulation(BuildingManager buildings)
	{
		Pop.Initialize(buildings);
		Military.Initialize(World.Kingdoms.PlayerKingdomId, 1);
		List<int> list = new List<int>();
		foreach (Kingdom item in World.Kingdoms.AIKingdoms())
		{
			list.Add(item.Id);
		}
		Civ.Initialize(World.Kingdoms.PlayerKingdomId, list);
		Land.Initialize();
		Land.RefreshStates(Pop.Count);
		Population = Pop.Count;
		Advisor.Analyze(this);
		Story = new StoryModeManager();
		Story.Initialize(this, PendingCampaignState, IsStoryMode);
		PendingCampaignState = null;
	}

	public bool IsLandBuildable(Point pos, int size)
	{
		for (int i = pos.Y; i < pos.Y + size; i++)
		{
			for (int j = pos.X; j < pos.X + size; j++)
			{
				if (!Land.IsBuildable(j, i))
				{
					return false;
				}
			}
		}
		return true;
	}

	public void UnlockSelectedParcel()
	{
		if (Selected is LandParcel { State: not LandParcelState.Open } landParcel)
		{
			if (!Land.CanUnlockRing(landParcel))
			{
				ShowMessage("Avval " + (landParcel.Ring - 1) + "-halqaning 4 tomonini oching", isError: true);
				return;
			}
			if (Population < landParcel.RequiredPopulation)
			{
				ShowMessage("Aholi yetarli emas (kerak: " + landParcel.RequiredPopulation + ")", isError: true);
				return;
			}
			if (!_resources.CanAfford(ResourceType.Coin, landParcel.UnlockCost))
			{
				ShowMessage("Tanga yetarli emas (" + landParcel.UnlockCost + ")", isError: true);
				return;
			}
			Coins -= landParcel.UnlockCost;
			Land.Unlock(landParcel);
			Land.RefreshStates(Population);
			if (Land.LastCompletedRing > 0 && Land.LastCompletedRing < Land.RingCount)
				ShowMessage(Land.LastCompletedRing + "-HALQA TO'LIQ OCHILDI! " + (Land.LastCompletedRing + 1) + "-halqada 4 ta yangi yer paydo bo'ldi");
			else
				ShowMessage("YANGI HUDUD OCHILDI: " + landParcel.NameUz);
			LandChanged = true;
			Story?.NotifyLandUnlocked();
			Advisor.Analyze(this);
		}
	}

	public void DoAdvisorAction(AdvisorMessage m)
	{
		if (m != null)
		{
			switch (m.ActionId)
			{
			case 1:
				EnterBuildMode(BuildingType.House);
				break;
			case 2:
				EnterBuildMode(BuildingType.Garden);
				break;
			case 3:
				EnterBuildMode(BuildingType.Market);
				break;
			case 4:
				ShowMessage("Xaritada yopiq (oltin) hududni tanlab, 'YERNI OCHISH' bosing");
				break;
			case 5:
				Policy.AdjustPopulation(-5);
				ShowMessage("Aholi solig'i kamaytirildi");
				break;
			}
		}
	}

	public bool StartResearch(string techId)
	{
		if (Civ.Player.StartResearch(techId))
		{
			ShowMessage("Tadqiqot boshlandi: " + (TechnologyCatalog.Get(techId)?.NameUz ?? techId));
			return true;
		}
		ShowMessage("Bu texnologiyani hozir o'rganib bo'lmaydi", isError: true);
		return false;
	}

	public bool IsBuildingUnlocked(BuildingType type)
	{
		return Civ.Player.IsBuildingUnlocked(BuildingCatalog.RequiredTech(type));
	}

	public void UpgradeSelected()
	{
		if (Selected is Building building && BuildingUpgradeSystem.CanUpgrade(building))
		{
			int num = BuildingUpgradeSystem.UpgradeCost(building);
			if (!_resources.CanAfford(ResourceType.Coin, num))
			{
				ShowMessage("Daraja uchun tanga yetarli emas", isError: true);
				return;
			}
			Coins -= num;
			BuildingUpgradeSystem.Apply(building);
			ShowMessage(building.Name + " darajasi oshdi (" + building.Level + ")");
			Story?.NotifyBuildingUpgraded(building);
		}
	}

	public bool RecruitUnit(UnitType type)
	{
		UnitDef unitDef = UnitCatalog.Get(type);
		if (!_resources.CanAfford(ResourceType.Coin, unitDef.TrainingCost))
		{
			ShowMessage("Tanga yetarli emas", isError: true);
			return false;
		}
		Coins -= unitDef.TrainingCost;
		Military.Enqueue(type);
		ShowMessage(unitDef.NameUz + " tayyorlashga berildi");
		return true;
	}

	public void TrainArmy()
	{
		if (!_resources.CanAfford(ResourceType.Coin, 20))
		{
			ShowMessage("Mashg'ulot uchun tanga yetarli emas", isError: true);
			return;
		}
		Coins -= 20;
		int num = Military.TrainAll(60);
		ShowMessage("Mashg'ulot o'tkazildi" + ((num > 0) ? (" (" + num + " daraja oshdi)") : ""));
	}

	public void Update(float dt, BuildingManager buildings)
	{
		if (MessageTimer > 0f)
		{
			MessageTimer -= dt;
		}
		Population = (Pop.Initialized ? Pop.Count : buildings.TotalPopulation);
		_resources.Set(ResourceType.Population, Population);
		float num = (Paused ? 0f : (dt * (float)Speed));
		if (num <= 0f)
		{
			return;
		}
		int day = Clock.Day;
		int month = Clock.Month;
		Clock.Update(num);
		foreach (Building building in buildings.Buildings)
		{
			if (building.BuildProgress < 1f)
			{
				building.BuildProgress = Math.Min(1f, building.BuildProgress + num * 0.4f);
				if (building.BuildProgress >= 1f)
				{
					ShowMessage(building.Name + " qurilishi yakunlandi");
					Story?.NotifyBuildingConstructed(building);
				}
			}
		}
		if (!Pop.Initialized || Clock.Day == day)
		{
			return;
		}
		Weather.Update(Clock.Day, Season);
		Pop.AssignJobs(buildings);
		bool hasFarmers = Pop.CountProfession(CitizenProfession.Dehqon) > 0;
		foreach (Building building in buildings.Buildings)
		{
			if (!(building is Farm farm)) continue;
			int harvest = farm.WorkAutonomously(hasFarmers);
			if (harvest <= 0) continue;
			if (buildings.Count(BuildingType.Market) > 0) Coins += farm.SellStoredProduce() * 2;
			else Food += harvest;
		}
		CivilizationModifiers civilizationModifiers = Civ.PlayerModifiers();
		float num2 = SeasonManager.FoodModifier(Season) + Weather.ProductionModifier();
		_economy.DailyTick(buildings, Pop, Policy, civilizationModifiers.Get(TechEffectType.FoodProductionBonus) + num2, civilizationModifiers.Get(TechEffectType.TradeIncomeBonus) + Weather.ProductionModifier() * 0.5f, civilizationModifiers.Get(TechEffectType.TaxEfficiencyBonus));
		Pop.DailyTick(buildings, _resources, Policy, civilizationModifiers.Get(TechEffectType.PopulationHealthBonus), civilizationModifiers.Get(TechEffectType.EducationBonus), civilizationModifiers.Get(TechEffectType.CityStabilityBonus));
		Life.DayTick(this, buildings);
		string text = Civ.DailyTick(buildings, Pop.AvgEducation);
		if (text != null)
		{
			ShowMessage("Texnologiya ochildi: " + (TechnologyCatalog.Get(text)?.NameUz ?? text));
			Story?.NotifyTechnologyUnlocked(text);
		}
		Petitions.Update(Clock.Day, Pop, _resources, Policy);
		if (Clock.Month != month)
		{
			Pop.MonthlyGrowth(buildings, _resources);
		}
		Population = Pop.Count;
		Military.MaxConcurrent = Math.Max(2, buildings.Count(BuildingType.Barracks) * 2);
		Military.DailyTick();
		int num3 = buildings.Count(BuildingType.Barracks) * 6;
		if (num3 > 0)
		{
			Military.TrainAll(num3);
		}
		Coins -= Military.TotalUpkeep;
		int cityDefense = CityDefenseManager.Compute(buildings, Military.TotalStrength);
		World.SyncPlayer(Coins, Population, Military.TotalStrength, cityDefense);
		Coins += World.StrategicTick(Clock.Day);
		foreach (Kingdom item in World.Kingdoms.AIKingdoms())
		{
			if (Civ.AITick(item.Id, item.EconomicStrength))
			{
				item.TechnologyLevel = 1 + Civ.AITechCount(item.Id);
				World.AddEvent(item.RulerName + " yangi ilmiy yutuqqa erishdi.");
			}
		}
		Land.RefreshStates(Population);
		Advisor.Analyze(this);
		Story?.OnDayTick();
	}
}
