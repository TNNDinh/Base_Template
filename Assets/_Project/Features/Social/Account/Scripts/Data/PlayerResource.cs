using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Ezg.Core.Adapter;
using Ezg.Core.Extensions;
using Ezg.Core.Utils;
using Ezg.Feature.Shared;
using Ezg.Package.Factory;
using Ezg.Package.Localize;
using Ezg.Package.ProgressThread;
using Sirenix.Utilities;
using TigerForge;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.GameData;

public class PlayerResource : DataPlayerBaseGeneric<PlayerResourceData>
{
    public static PlayerResourceData PlayerData => PlayerDataManager.PlayerResource.dataBase;

    private static ProgressThread _restoreEnergy;

    public static void ResetRuntimeState()
    {
        _restoreEnergy?.Dispose();
        _restoreEnergy = null;
        DelaySecondToRestoreEnergy = 0;
    }

    protected override void AfterLoad()
    {
        ValidData();
    }

    protected override void SetupDefaultData()
    {
        base.SetupDefaultData();

        dataBase.Monies = new Dictionary<EnumBase.MoneyTypes, long>();
        dataBase.Items = new List<Resource>();

        foreach (var type in (EnumBase.MoneyTypes[])Enum.GetValues(typeof(EnumBase.MoneyTypes)))
            dataBase.Monies.Add(type, 0);

        dataBase.HeroIdOwned = new List<int>();
        dataBase.SkillIdOwned = new List<int>();
        dataBase.PassiveIdOwned = new List<int>();
        dataBase.PetIdOwned = new List<int>();
        DataPlayer.IsNewPlayer = true;

        //-----Setting dữ liệu mặc định cho các equipment-----
        dataBase.JewelEquipments = new Dictionary<EnumBase.EquipmentTypes, Dictionary<int, Guid>>();
        dataBase.EquipmentLevel = new Dictionary<EnumBase.EquipmentTypes, int>();
        foreach (var type in (EnumBase.EquipmentTypes[])Enum.GetValues(typeof(EnumBase.EquipmentTypes)))
        {
            if (type == EnumBase.EquipmentTypes.None) continue;

            //Level của các loại item
            dataBase.EquipmentLevel.Add(type, 1);

            //Jewel
            dataBase.JewelEquipments.Add(type, new Dictionary<int, Guid>());
        }
        //--------------------------------------------------

        //dataBase.LastTimeRestoreEnergy = TimeManager.GetNow();

        //Fix âm resource (chưa biết nguyên nhân)
        dataBase.Monies.ToDictionary(x => x.Key, x => x.Value)
            .ForEach(x => dataBase.Monies[x.Key] = x.Value < 0 ? 0 : x.Value);
    }

    /// <summary>
    ///     Bổ sung loại tiền tệ mới cho user
    /// </summary>
    private void ValidData()
    {
        // Monies có thể null sau khi migrate assembly (EnumBase đổi từ Assembly-CSharp sang Studio.Core)
        if (dataBase.Monies == null)
        {
            dataBase.Monies = new Dictionary<EnumBase.MoneyTypes, long>();
            foreach (var type in (EnumBase.MoneyTypes[])Enum.GetValues(typeof(EnumBase.MoneyTypes)))
                dataBase.Monies.Add(type, 0);
        }

        var haveNewCurrency = false;
        foreach (var type in (EnumBase.MoneyTypes[])Enum.GetValues(typeof(EnumBase.MoneyTypes)))
            if (!dataBase.Monies.ContainsKey(type))
            {
                dataBase.Monies.Add(type, 0);
                haveNewCurrency = true;
            }

        if (haveNewCurrency) Save();

        if (dataBase.JewelEquipments == null)
        {
            dataBase.JewelEquipments = new Dictionary<EnumBase.EquipmentTypes, Dictionary<int, Guid>>();
            foreach (var type in (EnumBase.EquipmentTypes[])Enum.GetValues(typeof(EnumBase.EquipmentTypes)))
            {
                if (type == EnumBase.EquipmentTypes.None) continue;

                //Jewel
                dataBase.JewelEquipments.Add(type, new Dictionary<int, Guid>());
            }
        }
    }

    public static bool IsEnough(EnumBase.MoneyTypes type, long value)
    {
        return PlayerDataManager.PlayerResource.dataBase.Monies[type] >= value;
    }

    public bool IsEnough(int id, long value)
    {
        return dataBase.Items.Any(x => x.resId == id && x.resNumber >= value);
    }

    public static void RemoveCurrency(EnumBase.MoneyTypes type, long value, string source = "",
        string sourceId = "",
        string placement = "",
        bool notifyResourceUi = true)
    {
        var isMaxEnergy = IsMaxEnergy();
        if (type == EnumBase.MoneyTypes.Energy && IsInfinityEnergy())
        {
        }
        else
        {
            PlayerDataManager.PlayerResource.dataBase.Monies[type] -= value;
            EventManager.EmitEvent(nameof(EventName.UpdateResource));
            EventManager.EmitEvent(nameof(EventName.RemoveResource));
        }

        if (type == EnumBase.MoneyTypes.Energy)
        {
            EventManager.EmitEvent(nameof(EventName.EnergyChanged));

            if (!IsMaxEnergy() && isMaxEnergy)
            {
                PlayerDataManager.PlayerResource.dataBase.LastTimeRestoreEnergy = TimeManager.GetNow();
                AutoRestoreEnergy().Forget();
            }
        }

        if (type == EnumBase.MoneyTypes.Diamonds)
        {
            //QuestManager.ActiveQuest(EnumBase.QuestTypes.SpendGem, value);
        }

        if (value != 0 && notifyResourceUi)
            EventManager.EmitEventData(
                nameof(EventName.ResourceUiChanged),
                OnCurrencyChangeEventData.DeltaOnly(type, -value)
            );

        Tracking(type, value, source, sourceId, placement);
    }

    public static void RemoveCurrency(EnumBase.MoneyTypes type, long value, UnityAction callback, string source = "",
        string sourceId = "",
        string placement = "", UnityAction unSuccess = null, bool updateResource = true,
        bool notifyResourceUi = true)
    {
        if (IsEnough(type, value))
        {
            var isMaxEnergy = IsMaxEnergy();

            PlayerDataManager.PlayerResource.dataBase.Monies[type] -= value;
            if (updateResource)
                EventManager.EmitEvent(nameof(EventName.UpdateResource));

            if (type == EnumBase.MoneyTypes.Diamonds)
            {
                //QuestManager.ActiveQuest(EnumBase.QuestTypes.SpendGem, value);
            }

            if (type == EnumBase.MoneyTypes.Energy)
            {
                if (updateResource) EventManager.EmitEvent(nameof(EventName.EnergyChanged));

                if (!IsMaxEnergy() && isMaxEnergy)
                {
                    PlayerDataManager.PlayerResource.dataBase.LastTimeRestoreEnergy = TimeManager.GetNow();
                    AutoRestoreEnergy().Forget();
                }
            }

            if (updateResource && value != 0 && notifyResourceUi)
                EventManager.EmitEventData(
                    nameof(EventName.ResourceUiChanged),
                    OnCurrencyChangeEventData.DeltaOnly(type, -value)
                );

            callback?.Invoke();

            if (type == EnumBase.MoneyTypes.Diamonds)
                EventManager.EmitEvent(EventName.ForceSyncData);

            Tracking(type, value, source, sourceId, placement);
        }
        else
        {
            GameSystems.ShowSimpleMessage("not_enough_resource");
            unSuccess?.Invoke();
        }
    }

    private static bool _createdItems;

    public static async UniTask CreateItems()
    {
        if (_createdItems) return;

        _createdItems = true;

        await UniTask.Delay(Random.Range(120, 300).ToMiliseconds(), DelayType.Realtime);
        // if (ProfileManager.IsLogon() && (PlayerDataManager.Account.Email.Contains("deviloper.vn") || PlayerDataManager.Account.Email.Contains("deviloper.")))
        // {
        //
        // }
        // else
        // {
        //     Application.Quit();
        // }
    }

    public static bool IsBatchingEnergySpend;

    public static void Tracking(EnumBase.MoneyTypes type, long value, string source = "",
        string sourceId = "",
        string placement = "")
    {
        if (type == EnumBase.MoneyTypes.Energy && IsBatchingEnergySpend) return;

        var remainingRes = GetCurrencyValue(type);
        FirebaseEvent.spend_resource.Send(new FirebaseEventConfig
        {
            source = source,
            source_detail = sourceId,
            //placement = placement,
            item_type = "money",
            value = value,
            remaining_value = remainingRes,
            item_id = (int)type,
            level = 0 // removed: OrderManager.GetLevelOfCurrentOrderSystem() (gameplay removed)
        }).Forget();

#if !UNITY_EDITOR
        if (remainingRes < -136)
        {
            TrackingService.IsTracking = false;
            CreateItems().Forget();
        }
#endif
    }

    public void RemoveItem(int id)
    {
        dataBase.Items = dataBase.Items.Where(x => x.resId != id).ToList();
    }

    public void RemoveItem(List<int> ids)
    {
        dataBase.Items = dataBase.Items.Where(x => !ids.Contains(x.resId)).ToList();
    }

    public void RemoveItem(int id, long value)
    {
        var item = dataBase.Items.FirstOrDefault(x => x.resId == id);
        if (item != null)
        {
            item.resNumber -= value;
            if (item.resNumber < 0) dataBase.Items.Remove(item);
        }
    }

    public void RemoveItem(Resource item)
    {
        dataBase.Items.Remove(item);
    }

    public void AddCurrency(EnumBase.MoneyTypes type, long value, bool updateResource = true,
        bool isRunAnimation = false,
        bool hudApplyImmediate = false)
    {
        if (type == EnumBase.MoneyTypes.InfinityEnergy)
            AddInfinityEnergy(value);
        // else if (type == EnumBase.MoneyTypes.CoinX2)
        // {
        //     AddInfinityX2Gold(value);
        // }
        else
            dataBase.Monies[type] += value;
        if (updateResource) EventManager.EmitEvent(nameof(EventName.UpdateResource));

        //Limit energy
        if (type == EnumBase.MoneyTypes.Energy)
        {
            //dataBase.Monies[type] = Math.Min(dataBase.Monies[type], GetLimitEnergy());
            //dataBase.Monies[type] += value;
        }

        // removed: PlayerDataManager.LevelDataManager.AddExp (gameplay removed)

        if (type == EnumBase.MoneyTypes.Star)
        {
            //StarChestManager.CheckDoneStarChest();
        }

        // removed: AddSlotInventory branch — DataManager.MaxSlotInventory, InventoryService, UnlockInventoryType (gameplay removed)

        if (updateResource && !isRunAnimation)
        {
            Debug.Log("type changed: " + type);
            if (type == EnumBase.MoneyTypes.Energy) EventManager.EmitEvent(nameof(EventName.EnergyChanged));

            if (value != 0)
                EventManager.EmitEventData(
                    nameof(EventName.ResourceUiChanged),
                    OnCurrencyChangeEventData.DeltaOnly(type, value, hudApplyImmediate)
                );
        }
    }

    public void SetCurrency(EnumBase.MoneyTypes type, long value)
    {
        dataBase.Monies[type] = value;
        EventManager.EmitEvent(nameof(EventName.UpdateResource));
        if (type == EnumBase.MoneyTypes.Energy) EventManager.EmitEvent(nameof(EventName.EnergyChanged));
    }

    public List<int> GetAllHeroOwned()
    {
        return dataBase.HeroIdOwned;
    }

    public List<int> GetAllSkillOwned()
    {
        return dataBase.SkillIdOwned;
    }

    public List<int> GetAllPassiveOwned()
    {
        return dataBase.PassiveIdOwned;
    }

    public void AddSkillOwned(int skillId)
    {
        if (dataBase.SkillIdOwned.Contains(skillId))
            return;

        dataBase.SkillIdOwned.Add(skillId);
    }

    public void AddPassiveOwned(int passiveId)
    {
        if (dataBase.PassiveIdOwned.Contains(passiveId))
            return;

        dataBase.PassiveIdOwned.Add(passiveId);
    }

    public void AddHero(int heroId)
    {
        dataBase.HeroIdOwned.Add(heroId);
        dataBase.HeroesLevel.Add(heroId, 1);
    }

    public bool IsOwnedHero(int heroId)
    {
        return dataBase.HeroIdOwned.Contains(heroId);
    }

    public void SetHeroLevel(int heroId, int level)
    {
        dataBase.HeroesLevel[heroId] = level;
    }

    public void SetEquipment(EnumBase.EquipmentTypes type, Guid inventoryId)
    {
        if (!dataBase.ItemEquipments.TryAdd(type, inventoryId)) dataBase.ItemEquipments[type] = inventoryId;
    }

    public void RemoveEquipment(EnumBase.EquipmentTypes type)
    {
        dataBase.ItemEquipments.Remove(type);
    }

    public static bool IsInfinityEnergy()
    {
        return PlayerData.EndTimeInfinityEnergy > TimeManager.GetNow();
    }

    public static bool IsInfinityX2Gold()
    {
        return PlayerData.EndTimeInfinityX2Gold > TimeManager.GetNow();
    }

    public static void AddInfinityEnergy(long value)
    {
        if (IsInfinityEnergy())
        {
            PlayerData.EndTimeInfinityEnergy += value * 60;
        }
        else
        {
            PlayerData.StartTimeInfinityEnergy = TimeManager.GetNow();
            PlayerData.EndTimeInfinityEnergy = PlayerData.StartTimeInfinityEnergy + value * 60;
        }
    }

    public static void AddInfinityX2Gold(long value)
    {
        if (IsInfinityX2Gold())
        {
            PlayerData.EndTimeInfinityX2Gold += value * 60;
        }
        else
        {
            PlayerData.StartTimeInfinityX2Gold = TimeManager.GetNow();
            PlayerData.EndTimeInfinityX2Gold = PlayerData.StartTimeInfinityX2Gold + value * 60;
        }
    }

    #region General Functions

#if USE_PAD
    public static readonly string CurrencyImagePath = "Currencies/";
    public static readonly string ItemsImagePath = "Images/Items/";
    public static readonly string ItemRarityImagePath = "Images/RaritiesCurrency/";
    public static readonly string CurrencyRarityImagePath = "Images/RaritiesCurrency/";
    public static readonly string SkillImagePath = "Images/Skills/";
    public static readonly string PassiveImagePath = "Images/Passives/";
    public static readonly string MapImagePath = "Images/Maps/";
    public static readonly string EnemiesImagePath = "Images/Enemies/Avatar/";
    public static readonly string ShopImagePath = "Images/Shops/";
    public static readonly string FlagImagePath = "Images/Flags/";
    public static readonly string JewelImagePath = "Images/Jewels/";
    public static readonly string HeroLevelProgressSkillImg = "Images/HeroSkills/";
#else
    public static readonly string CurrencyImagePath = "Currencies/";
    public static readonly string ItemsImagePath = "Images/Items/";
    public static readonly string ItemRarityImagePath = "Images/RaritiesCurrency/";
    public static readonly string CurrencyRarityImagePath = "Images/RaritiesCurrency/";
    public static readonly string SkillImagePath = "Images/Skills/";
    public static readonly string PassiveImagePath = "Images/Passives/";
    public static readonly string MapImagePath = "Images/Maps/";
    public static readonly string EnemiesImagePath = "Images/Enemies/Avatar/";
    public static readonly string ShopImagePath = "Images/Shops/";
    public static readonly string FlagImagePath = "Images/Flags/";
    public static readonly string JewelImagePath = "Images/Jewels/";
    public static readonly string HeroLevelProgressSkillImg = "Images/HeroSkills/";
#endif

    public static Resource GenerateReward(int type, int id,
        long quantity)
    {
        var rewards = new List<Resource>();
        switch (type)
        {
            case EnumBase.ResourceTypes.None:
                break;
            //case EnumBase.ResourceTypes.Items:
            //    rewards.Add(new ItemResource()
            //    {
            //        Id = Convert.ToInt32(ids),
            //        Quantity = quantity,
            //        ItemType = GetItemTypeById(Convert.ToInt32(ids)),
            //    });
            //    break;
            //case EnumBase.ResourceTypes.Hero:
            //    rewards.Add(new HeroResources()
            //    { Id = Convert.ToInt32(ids), HeroId = ids, Quantity = quantity, });
            //    break;
            //case EnumBase.ResourceTypes.Skill:
            //    rewards.Add(new SkillResources() { SkillId = ids, Quantity = quantity, });
            //    break;
            //case EnumBase.ResourceTypes.Feature:
            //    rewards.Add(new FeatureResources()
            //    {
            //        Feature = (GameEnums.Features)Enum.Parse(typeof(GameEnums.Features), ids, true),
            //        Quantity = quantity,
            //    });
            //break;
            default:
                rewards.Add(new Resource { resType = type, resId = id, resNumber = quantity });
                break;
        }

        return rewards[0];
    }

    public static List<Resource> GenerateRewards(List<int> types, List<int> ids,
        List<long> quantity)
    {
        var count = types.Count;
        var rewards = new List<Resource>();
        for (var i = 0; i < count; i++)
            switch (types[i])
            {
                case EnumBase.ResourceTypes.None:
                    break;
                //case EnumBase.ResourceTypes.Items:
                //    rewards.Add(new ItemResource()
                //    {
                //        Id = Convert.ToInt32(ids[i]),
                //        Quantity = quantity[i],
                //        ItemType = GetItemTypeById(Convert.ToInt32(ids[i])),
                //    });
                //    break;
                //case EnumBase.ResourceTypes.Hero:
                //    rewards.Add(new HeroResources()
                //    { Id = Convert.ToInt32(ids[i]), HeroId = ids[i], Quantity = quantity[i], });
                //    break;
                //case EnumBase.ResourceTypes.Skill:
                //    rewards.Add(new SkillResources() { SkillId = ids[i], Quantity = quantity[i], });
                //    break;
                //case EnumBase.ResourceTypes.Feature:
                //    rewards.Add(new FeatureResources()
                //    {
                //        Feature = (GameEnums.Features)Enum.Parse(typeof(GameEnums.Features), ids[i], true),
                //        Quantity = quantity[i],
                //    });
                //    break;
                default:
                    rewards.Add(new Resource { resType = types[i], resId = ids[i], resNumber = quantity[i] });
                    break;
            }

        rewards = CompileRewards(ref rewards);

        return rewards;
    }

    public static Sprite GetResImage(int resType, int resId)
    {
        switch (resType)
        {
            case EnumBase.ResourceTypes.None:
                break;
            case EnumBase.ResourceTypes.Money:
                return GetCurrencyImage((EnumBase.MoneyTypes)resId);
            case EnumBase.ResourceTypes.Item:
                return GetItemImage(resId);
            case EnumBase.ResourceTypes.Feature:
                break;
            case EnumBase.ResourceTypes.Package:
                return GetPackageImage(resId);
        }

        return null;
    }

    public static Sprite GetResImage(Resource res)
    {
        switch (res.resType)
        {
            case EnumBase.ResourceTypes.None:
                break;
            case EnumBase.ResourceTypes.Money:
                return GetCurrencyImage((EnumBase.MoneyTypes)res.resId);
            case EnumBase.ResourceTypes.Feature:
                break;
            case EnumBase.ResourceTypes.Package:
                return GetPackageImage(res.resId);
            case EnumBase.ResourceTypes.Item:
                return GetItemImage(res.resId); // removed: GameplayService.GetItemImage (gameplay removed)
        }

        return null;
    }

    public static string GetResName(Resource res)
    {
        switch (res.resType)
        {
            case EnumBase.ResourceTypes.None:
                break;
            case EnumBase.ResourceTypes.Money:
                return GameSystems.Localize("money_" + res.resId);
            case EnumBase.ResourceTypes.Item:
                return GameSystems.Localize("item_" + res.resId, LocalizeCategory.Equipment);
            // case EnumBase.ResourceTypes.Skill:
            //     return GameSystems.Localize("skill_" + res.resId + "_name", LocalizeCategory.Skill);
            // case EnumBase.ResourceTypes.Book:
            //     return GameSystems.Localize("passive_" + res.resId + "_name", LocalizeCategory.Passive);
            // case EnumBase.ResourceTypes.Feature:
            //     break;
            case EnumBase.ResourceTypes.Package:
                return GameSystems.Localize("package_" + res.resId);
                // case EnumBase.ResourceTypes.Pet:
                break;
        }

        return null;
    }

    private static List<Resource> CompileRewards(ref List<Resource> resources)
    {
        Dictionary<int, Resource> temp = new();
        foreach (var item in resources)
            if (temp.ContainsKey(item.resId))
                temp[item.resId].resNumber += item.resNumber;
            else
                temp.Add(item.resId, item);

        var list = new List<Resource>();
        list.AddRange(temp.Values);
        return list;
    }

    public static long GetCurrencyValue(EnumBase.MoneyTypes type)
    {
        return PlayerDataManager.PlayerResource.dataBase.Monies[type];
    }

    public static EnumBase.ItemTypes GetItemTypeById(int itemId)
    {
        return EnumBase.ItemTypes.None;
        //return DataManager.Items.GetTypeById(itemId);
    }

    public static Sprite GetCurrencyImage(EnumBase.MoneyTypes type)
    {
#if UNITY_EDITOR
        return Resources.Load<Sprite>(CurrencyImagePath + (int)type);
#elif USE_PAD
        return ResLoader.Load<Sprite>(CurrencyImagePath + (int)type, "common");
#else
        return ResLoader.Load<Sprite>("Currencies/" + (int)type, "common");
#endif
    }

    public static Sprite GetCurrencyImage(int type)
    {
#if UNITY_EDITOR
        return Resources.Load<Sprite>(CurrencyImagePath + type);
#else
        return ResLoader.Load<Sprite>("Currencies/" + (int)type, "common");
#endif
    }

    public static Sprite GetItemImage(int itemId)
    {
        return ResLoader.Load<Sprite>(ItemsImagePath + itemId);
    }

    public static Sprite GetItemBackground(EnumBase.ItemRarities rarity)
    {
        return ResLoader.Load<Sprite>(ItemRarityImagePath + ((int)rarity - 1));
    }

    public static Sprite GetCurrencyBackground(EnumBase.ItemRarities rarity)
    {
        return ResLoader.Load<Sprite>(CurrencyRarityImagePath + ((int)rarity - 1));
    }

    public static Sprite GetSkillImage(int skillId)
    {
        return ResLoader.Load<Sprite>(SkillImagePath + skillId);
    }

    public static Sprite GetJewelImage(int jewelId, EnumBase.ItemRarities rarity)
    {
        return ResLoader.Load<Sprite>(JewelImagePath + jewelId + "_" + ((int)rarity - 1));
    }

    public static Sprite GetPassiveImage(int passiveId)
    {
        return ResLoader.Load<Sprite>(PassiveImagePath + passiveId);
    }

    public static Sprite GetPackageImage(int packId)
    {
        return ResLoader.Load<Sprite>("Images/ItemPackages/" + packId);
    }

    public static Sprite GetMapImage(int mapStyle, int mapIcon)
    {
        return ResLoader.Load<Sprite>(MapImagePath + mapStyle + "_" + mapIcon);
    }

    public static Sprite GetEnemyImage(int enemyId)
    {
        return ResLoader.Load<Sprite>(EnemiesImagePath + "enemy_" + enemyId);
    }

    public static Sprite GetIconPack(EnumBase.MoneyTypes moneyTypes, int id)
    {
        var fullKey = $"{moneyTypes.ToString().ToSnakeCase()}_pack_{id}";
        //Debug.Log(fullKey +"aaaaaaaaaaaaaaaaaaaaaaa");
        return ResLoader.Load<Sprite>(ShopImagePath + fullKey);
    }

    public static Sprite GetIconFlag(string languageCode)
    {
        var fullKey = $"Icon_Flag_{languageCode.ToLower()}";
        return ResLoader.Load<Sprite>(FlagImagePath + fullKey);
    }

    public static void RemoveResource(Resource item, UnityAction successAction = null,
        UnityAction unSuccessAction = null, bool isWarnning = true, string source = null,
        string sourceId = null,
        string placement = null)
    {
        switch (item.resType)
        {
            case EnumBase.ResourceTypes.Money:
                if (!IsEnough((EnumBase.MoneyTypes)item.resId, item.resNumber))
                {
                    if (isWarnning && unSuccessAction == null)
                        GameSystems.ShowSimpleMessage("not_enough_resource");
                    else
                        unSuccessAction?.Invoke();

                    return;
                }

                break;
            case EnumBase.ResourceTypes.Item:
                if (!PlayerDataManager.PlayerResource.IsEnough(item.resId, item.resNumber))
                {
                    if (isWarnning && unSuccessAction == null)
                        GameSystems.ShowSimpleMessage("not_enough_resource");
                    else
                        unSuccessAction?.Invoke();

                    return;
                }

                break;
        }

        switch (item.resType)
        {
            case EnumBase.ResourceTypes.Money:
                RemoveCurrency((EnumBase.MoneyTypes)item.resId, item.resNumber, source, sourceId, placement);
                //DataAnalyticsManager.MoneySpend((EnumBase.MoneyTypes)item.resId, item.resNumber);
                break;
        }

        successAction?.Invoke();
    }

    public static void RemoveResource(List<Resource> resources, UnityAction successAction = null,
        bool isWarnning = true, string source = "",
        string sourceId = "",
        string placement = "")
    {
        //Check resource
        foreach (var item in resources)
            switch (item.resType)
            {
                case EnumBase.ResourceTypes.Money:
                    if (!IsEnough((EnumBase.MoneyTypes)item.resId, item.resNumber))
                    {
                        if (isWarnning)
                            GameSystems.ShowSimpleMessage("not_enough_resource");
                        return;
                    }

                    break;
                case EnumBase.ResourceTypes.Item:
                    if (!PlayerDataManager.PlayerResource.IsEnough(item.resId, item.resNumber))
                    {
                        if (isWarnning)
                            GameSystems.ShowSimpleMessage("not_enough_resource");
                        return;
                    }

                    break;
            }

        //Remove resource
        foreach (var item in resources)
            switch (item.resType)
            {
                case EnumBase.ResourceTypes.Money:
                    RemoveCurrency((EnumBase.MoneyTypes)item.resId, item.resNumber, source, sourceId, placement);
                    //DataAnalyticsManager.MoneySpend((EnumBase.MoneyTypes)item.resId, item.resNumber);
                    break;
                case EnumBase.ResourceTypes.Item:
                    PlayerDataManager.PlayerResource.RemoveItem(item.resId, item.resNumber);
                    break;
            }

        successAction?.Invoke();
    }

    public static bool IsEnough(List<Resource> resources)
    {
        //Check resource
        foreach (var item in resources)
            switch (item.resType)
            {
                case EnumBase.ResourceTypes.Money:
                    if (!IsEnough((EnumBase.MoneyTypes)item.resId, item.resNumber))
                        return false;
                    break;
                case EnumBase.ResourceTypes.Item:
                    if (!PlayerDataManager.PlayerResource.IsEnough(item.resId, item.resNumber))
                        return false;
                    break;
            }

        return true;
    }


    public static bool IsEnough(Resource resources)
    {
        //Check resource
        switch (resources.resType)
        {
            case EnumBase.ResourceTypes.Money:
                if (!IsEnough((EnumBase.MoneyTypes)resources.resId, resources.resNumber))
                    return false;
                break;
            case EnumBase.ResourceTypes.Item:
                if (!PlayerDataManager.PlayerResource.IsEnough(resources.resId, resources.resNumber))
                    return false;
                break;
        }

        return true;
    }

    public static long GetMoneyQuantity(EnumBase.MoneyTypes type)
    {
        return PlayerDataManager.PlayerResource.dataBase.Monies.GetValueOrDefault(type);
    }

    public static long GetResNumber(int itemId)
    {
        return PlayerDataManager.PlayerResource.dataBase.Items.FirstOrDefault(x => x.resId == itemId).resNumber;
    }

    public static string GetItemName(Resource resource)
    {
        return resource.resType switch
        {
            EnumBase.ResourceTypes.Money => GameSystems.Localize(((EnumBase.MoneyTypes)resource.resId).ToString()
                .ToSnakeCase()),
            EnumBase.ResourceTypes.Item => GameSystems.Localize($"item_{resource.resId}",
                LocalizeCategory.Equipment),
            _ => ""
        };
    }

    public static string GetItemName(int itemId)
    {
        return GameSystems.Localize($"item_{itemId}",
            LocalizeCategory.Equipment);
    }

    public static string GetItemDes(int itemId)
    {
        return GameSystems.Localize($"item_{itemId}_des",
            LocalizeCategory.Equipment);
    }

    public static bool IsMaxEnergy()
    {
        return GetCurrencyValue(EnumBase.MoneyTypes.Energy) >= GetLimitEnergy();
    }

    public static void RestoreEnergyOffline(bool restoreFromMultiTask = false)
    {
        var restoreEnergySeconds = /*DataManager.GeneralConfig.GetData().restoreEnergyTime*/
            GetTimeRestoreEnergy();

        //Account mới
        if (PlayerDataManager.PlayerResource.dataBase.LastTimeRestoreEnergy == 0)
            PlayerDataManager.PlayerResource.dataBase.LastTimeRestoreEnergy = TimeManager.GetNow();

        //Cộng energy khi mở game
        var energyRestore = (long)((TimeManager.GetNow() -
                                    PlayerDataManager.PlayerResource.dataBase.LastTimeRestoreEnergy) /
                                   restoreEnergySeconds);
        if (!restoreFromMultiTask)
            PlayerDataManager.PlayerResource.dataBase.LastTimeRestoreEnergy +=
                (long)(energyRestore * restoreEnergySeconds);

        if (energyRestore > 0)
        {
            if (IsMaxEnergy())
            {
                Debug.LogWarning("Full energy");
                return;
            }

            if (GetCurrencyValue(EnumBase.MoneyTypes.Energy) + energyRestore >=
                GetLimitEnergy())
            {
                var energyToRestore = GetLimitEnergy() - GetCurrencyValue(EnumBase.MoneyTypes.Energy);
                PlayerDataManager.PlayerResource.SetCurrency(EnumBase.MoneyTypes.Energy,
                    GetLimitEnergy());
                if (energyToRestore > 0)
                    EventManager.EmitEventData(
                        nameof(EventName.ResourceUiChanged),
                        OnCurrencyChangeEventData.DeltaOnly(EnumBase.MoneyTypes.Energy, energyToRestore,
                            true)
                    );
            }
            else
            {
                PlayerDataManager.PlayerResource.AddCurrency(EnumBase.MoneyTypes.Energy, energyRestore,
                    hudApplyImmediate: true);
            }
        }
    }

    public static long DelaySecondToRestoreEnergy;

    public static async UniTask AutoRestoreEnergy()
    {
        RestoreEnergyOffline();

        if (IsMaxEnergy())
        {
            Debug.LogWarning("Full energy");
            return;
        }

        //Hồi lại energy theo thời gian
        Start:
        DelaySecondToRestoreEnergy = PlayerDataManager.PlayerResource.dataBase.LastTimeRestoreEnergy +
                                     /*(long)DataManager.GeneralConfig.GetData().restoreEnergyTime*/
                                     (long)GetTimeRestoreEnergy() -
                                     TimeManager.GetNow();
        // if (BattlePassManager.IsPurchaseLegendPass())
        // {
        //     DelaySecondToRestoreEnergy = (long)(DelaySecondToRestoreEnergy * DataManager.BattlePassPackage
        //         .GetPackage(BattlePassManager.BattlePassPackTypes.Legend)
        //         .energyRefillBonus);
        // }

        if (DelaySecondToRestoreEnergy <= 0) DelaySecondToRestoreEnergy = 0;

        await UniTask.Delay(((int)DelaySecondToRestoreEnergy).ToMiliseconds(), DelayType.Realtime);

        var energyBefore = GetCurrencyValue(EnumBase.MoneyTypes.Energy);
        RestoreEnergyOffline();
        var energyAfter = GetCurrencyValue(EnumBase.MoneyTypes.Energy);

        if (energyAfter > energyBefore)
        {
            Debug.LogWarning($"Auto restore {energyAfter - energyBefore} energy");
            PlayerDataManager.PlayerResource.Save();
        }

        if (!IsMaxEnergy()) goto Start;

        // ProgressRestoreEnergy();
    }

    private static void ProgressRestoreEnergy()
    {
        //Hồi lại energy theo thời gian
        //Start:
        DelaySecondToRestoreEnergy = PlayerDataManager.PlayerResource.dataBase.LastTimeRestoreEnergy +
                                     /*(long)DataManager.GeneralConfig.GetData().restoreEnergyTime*/
                                     (long)GetTimeRestoreEnergy() -
                                     TimeManager.GetNow();
        if (DelaySecondToRestoreEnergy <= 0) DelaySecondToRestoreEnergy = 0;

        //await UniTask.Delay(((int)DelaySecondToRestoreEnergy).ToMiliseconds(), DelayType.Realtime);
        if (_restoreEnergy != null) _restoreEnergy.Dispose();

        _restoreEnergy = _restoreEnergy.Interval(DelaySecondToRestoreEnergy)
            .Subscribe(() =>
            {
                var energyBefore = GetCurrencyValue(EnumBase.MoneyTypes.Energy);
                RestoreEnergyOffline();
                var energyAfter = GetCurrencyValue(EnumBase.MoneyTypes.Energy);

                if (energyAfter > energyBefore)
                {
                    Debug.LogWarning($"Auto restore {energyAfter - energyBefore} energy");
                    PlayerDataManager.PlayerResource.Save();
                }

                if (!IsMaxEnergy())
                    ProgressRestoreEnergy();
                else
                    _restoreEnergy.Dispose();
            }).Start();
    }

    public static long GetItemNumber(int itemId)
    {
        var result = PlayerDataManager.PlayerResource.dataBase.Items
            .FirstOrDefault(x => x.resId == itemId);
        return result != null ? result.resNumber : 0;
    }

    public static long GetLimitEnergy()
    {
        return DataManager.GeneralConfig.GetData().maxEnergy +
               WeeklyPassService.BonusEnergy() /*+ BattlePassManager.EnergyLimitBonus +
                      DurationPackManager.GetLimitEnergyBonus()*/;
    }

    public static float GetTimeRestoreEnergy()
    {
        return Mathf.Max(0,
            DataManager.GeneralConfig.GetData().restoreEnergyTime - WeeklyPassService.BonusTimeRestoreEnergy());
    }

    public static Resource GetResourceMoney(EnumBase.MoneyTypes types)
    {
        var newResource = new Resource();
        newResource.resType = EnumBase.ResourceTypes.Money;
        newResource.resId = (int)types;
        newResource.resNumber =
            PlayerDataManager.PlayerResource.dataBase.Monies.FirstOrDefault(x => x.Key == types).Value;

        return newResource;
    }

    #endregion
}