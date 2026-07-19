using Ezg.Core.Utils;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.Shared.Config
{
public class GameEnums
{
    public enum Features
    {
        none = 0,
        [BundleName("features__settings")] Settings = 1,
        [BundleName("features__reward_popup")] RewardPopup = 2,

        [BundleName("features__overview_canvas")]
        OverviewCanvas = 3,
        Battle = 4,
        BattleResultWin = 5,
        BattleResultLose = 6,
        TimeUp = 7,
        [BundleName("features__home_scene")] HomeScreen = 8,
        [BundleName("features__shop")] Shop = 9,
        RemoveAdsPack = 10,
        [BundleName("packages__piggy_bank")] PiggyBank = 11,
        [BundleName("packages__battle_pass")] BattlePass = 12,

        [BundleName("packages__battle_pass_package")]
        BattlePassPackage = 13,
        ChainPack = 14,
        LevelSelect = 15,
        BoosterFreeze = 16,
        BoosterMagnet = 17,
        BoosterCancelOrder = 18,
        BoosterVipOrder = 19,

        [BundleName("features__unlock_feature")]
        UnlockFeature = 20,
        [BundleName("features__daily_reward")] DailyReward = 21,
        LuckySpin = 22,
        [BundleName("features__rating")] Rating = 23,
        MapEditor = 24,

        //BattleRoyale = 25,
        LevelOverview = 26,
        [BundleName("tutorial__tutorials")] Tutorial = 27,

        [BundleName("packages__energy_trilogy")]
        GameCheat = 28,
        Revive = 29,
        BuyBooster = 30,

        [BundleName("features__avatar_select")]
        AvatarSelect = 31,
        LostStreak = 32,

        ArenaLoadout = 91, // Arena micro-RPG: chọn hero + vũ khí + mở khóa (screen_arena_loadout)
        ArenaStageSelect = 92, // Arena micro-RPG: chọn stage/map (screen_arena_stage_select)

        [BundleName("features__require_internet")]
        RequireInternet = 33,
        [BundleName("packages__piggy_bank")] PiggyBankIngame = 34,
        HeartRefill = 35,
        PreBattle = 36,

        [BundleName("features__popup_next_theme")]
        QuickBreak = 37,
        HeartRemove = 38,
        StreakInfo = 39,
        TutPopup = 40,

        [BundleName("packages__energy_trilogy")]
        Gameplay = 41,

        [BundleName("features__open_zone_build_up_goal")]
        OpenZoneBuildUpGoal = 42,

        [BundleName("features__popup_confirm")]
        PopupConfirm = 43,
        [BundleName("features__select_theme")] SelectTheme = 44,

        [BundleName("features__popup_next_theme")]
        PopupNextTheme = 45,
        [BundleName("features__buy_currency")] BuyCurrency = 46,

        [BundleName("features__not_enough_currency")]
        NotEnoughCurrency = 47,
        SoftTut = 48,
        [BundleName("features__item_info")] ItemInfo = 49,
        [BundleName("features__inventory")] Inventory = 50,
        [BundleName("features__order_mania")] OrderMania = 51,
        [BundleName("features__chefs_book")] ChefsBook = 52,

        [BundleName("features__new_equipment")]
        NewEquipment = 53,

        [BundleName("features__open_star_chest")]
        OpenStarChest = 54,

        [BundleName("features__select_language")]
        SelectLanguage = 55,
        [BundleName("features__choice_chest")] ChoiceChest = 56,
        [BundleName("features__change_name")] ChangeName = 57,

        [BundleName("features__confirm_sell_rare_item")]
        ConfirmSellRareItem = 58,
        Waiting = 59,
        ItemBubble = 60,

        [BundleName("features__order_mania_reward")]
        OrderManiaReward = 61,
        [BundleName("features__shop")] WeeklyPassInfo = 62,
        [BundleName("packages__energy_pack")] EnergyPack = 63,

        [BundleName("packages__video_bonuses")]
        VideoBonuses = 64,
        [BundleName("packages__piggy_bank")] PiggyBankConfirm = 65,
        [BundleName("features__save_found")] SaveFound = 66,
        [BundleName("features__account")] ConfirmAccount = 67,
        [BundleName("features__account")] ConfirmAccountServer = 68,

        [BundleName("packages__energy_trilogy")]
        EnergyTrilogy = 69,

        [BundleName("features__renovation_completed")]
        RenovationCompleted = 70,

        [BundleName(AssetBundleName.Features.ScreenMaskChangeScene)]
        MaskChangeScene = 71,

        [BundleName("features__video_intro")] WatchRateItemGen = 74,
        [BundleName("packages__starter_pack")] StarterPack = 75,

        [BundleName("packages__openning_pack")]
        OpenningPack = 76,

        [BundleName("packages__happiness_express")]
        HappinessExpress = 77,

        [BundleName("packages__discount_gem_raw")]
        DiscountGemRaw = 78,

        [BundleName("packages__infinity_pack")]
        InfinityPack = 79,
        [BundleName("features__video_intro")] VideoIntro = 80,
        LevelUp = 81,
        CheatGetData = 82,
        ConfirmExpandItem = 83,
        SpeedPackage = 84,
        [BundleName("features__admin")] Admin = 85,
        [BundleName("features__admin")] AdminGetData = 86,
        GiftCode = 87,
        StageSelect = 88,
        BattleResult = 89,
        TeamFormation = 90,

        [BundleName("features__home_scene")] CurrencyBar = 500,
        [BundleName("features__home_scene")] LevelAccount = 501,

        BugLogger = 999,
        [BundleName("tutorial__tutorials")] Tut1_1 = 1000,
        [BundleName("tutorial__tutorials")] Tut1_2 = 1001,
        [BundleName("tutorial__tutorials")] Tut1_3 = 1002,
        [BundleName("tutorial__tutorials")] Tut1_4 = 1003,
        [BundleName("tutorial__tutorials")] Tut1_5 = 1004,
        [BundleName("tutorial__tutorials")] Tut1_6 = 1005,
        [BundleName("tutorial__tutorials")] Tut2_1 = 1006,
        [BundleName("tutorial__tutorials")] Tut2_2 = 1007,
        [BundleName("tutorial__tutorials")] Tut2_3 = 1008,
        [BundleName("tutorial__tutorials")] Tut2_4 = 1009,
        [BundleName("tutorial__tutorials")] Tut3_1 = 1010,
        [BundleName("tutorial__tutorials")] Tut3_2 = 1011,
        [BundleName("tutorial__tutorials")] Tut3_3 = 1012,
        [BundleName("tutorial__tutorials")] Tut3_4 = 1013,
        [BundleName("tutorial__tutorials")] Tut3_5 = 1014,
        [BundleName("tutorial__tutorials")] Tut3_6 = 1015,
        [BundleName("tutorial__tutorials")] Tut4_1 = 1016,
        [BundleName("tutorial__tutorials")] Tut5_1 = 1017,
        [BundleName("tutorial__tutorials")] Tut5_2 = 1018,
        [BundleName("tutorial__tutorials")] Tut5_3 = 1019,
        [BundleName("tutorial__tutorials")] Tut5_4 = 1020,
        [BundleName("tutorial__tutorials")] Tut6_1 = 1021,
        [BundleName("tutorial__tutorials")] Tut7_1 = 1022,
        [BundleName("tutorial__tutorials")] Tut8_1 = 1023,
        [BundleName("tutorial__tutorials")] Tut9_1 = 1024,
        [BundleName("tutorial__tutorials")] Tut9_2 = 1025,
        [BundleName("tutorial__tutorials")] Tut9_3 = 1026,
        [BundleName("tutorial__tutorials")] Tut9_4 = 1027,
        [BundleName("tutorial__tutorials")] Tut9_5 = 1028,
        [BundleName("tutorial__tutorials")] Tut10_1 = 1029,
        [BundleName("tutorial__tutorials")] Tut10_2 = 1030,
        [BundleName("tutorial__tutorials")] Tut11_1 = 1031,
        [BundleName("tutorial__tutorials")] Tut11_2 = 1032,
        [BundleName("tutorial__tutorials")] Tut12_1 = 1033,
        [BundleName("tutorial__tutorials")] Tut12_2 = 1034,
        [BundleName("tutorial__tutorials")] Tut13_1 = 1035,
        [BundleName("tutorial__tutorials")] Tut14_1 = 1036,
        [BundleName("tutorial__tutorials")] TutHome38 = 1037,
        [BundleName("tutorial__tutorials")] Tut3_7 = 1038,
        [BundleName("tutorial__tutorials")] Tut7_2 = 1039,
        [BundleName("tutorial__tutorials")] Tut9_0 = 1040,
        [BundleName("tutorial__tutorials")] Tut7_3 = 1041,
        [BundleName("tutorial__tutorials")] Tut7_4 = 1042,
        [BundleName("tutorial__tutorials")] Tut15_1 = 1043,
        [BundleName("tutorial__tutorials")] Tut15_2 = 1044,
        [BundleName("tutorial__tutorials")] Tut16_1 = 1045,
        [BundleName("tutorial__tutorials")] Tut17_1 = 1046,
        [BundleName("tutorial__tutorials")] Tut18_1 = 1047,


        [BundleName("tutorial__tutorials")] TutGamePlayMergeD2021 = 2000,
        [BundleName("tutorial__tutorials")] TutGamePlayDoneOrderD2021 = 2001,
        [BundleName("tutorial__tutorials")] TutGamePlayMergeF1011 = 2002,
        [BundleName("tutorial__tutorials")] TutGamePlayDoneOrderF2021 = 2003,
        [BundleName("tutorial__tutorials")] TutBuyCurrencyEnergy = 2004,
        [BundleName("tutorial__tutorials")] TutExpandF1015 = 2005,
        [BundleName("tutorial__tutorials")] TutItemToolJ10033 = 2006,
        [BundleName("tutorial__tutorials")] TutItemToolJ10033_1011 = 2007,
        [BundleName("tutorial__tutorials")] TutCardBonusToolJ10033 = 2008,
        [BundleName("tutorial__tutorials")] TutMergeItemToolJ10033 = 2009,
        [BundleName("tutorial__tutorials")] TutItemToolG10013 = 2010,

        [BundleName("tutorial__tutorials")] TutUpgradeBuild = 3000,
        [BundleName("tutorial__tutorials")] TutClickInfoNewEquipment = 3001,
        [BundleName("tutorial__tutorials")] TutShopDailyGift = 3002,
        [BundleName("tutorial__tutorials")] TutShopGetDailyGift = 3003,
        [BundleName("tutorial__tutorials")] TutUnlockLevelPass = 3004,
        [BundleName("tutorial__tutorials")] TutBoosterPlus = 3005,
        [BundleName("tutorial__tutorials")] TutBubble = 3006,
        [BundleName("tutorial__tutorials")] TutChefsBook = 3007,
        [BundleName("tutorial__tutorials")] TutCharging = 3008,

        [BundleName("events__petal_plate_party")]
        PetalPlateParty = 5000,

        [BundleName("events__petal_plate_party")]
        PetalPlatePartyPreview = 5001,

        [BundleName("events__speed_feast_race")]
        SpeedFeastRace = 5002,

        [BundleName("events__speed_feast_race")]
        SpeedFeastRacePreview = 5003,

        [BundleName("features__require_internet")]
        RewardEventPopup = 5004,

        [BundleName("events__pizza_tower_race")]
        PizzaTowerRace = 5005,

        [BundleName("events__pizza_tower_race")]
        PizzaTowerRacePreview = 5006,

        [BundleName("packages__energy_trilogy")]
        FortuneMeetsCookie = 5007,

        [BundleName("packages__energy_trilogy")]
        FortuneMeetsCookiePreview = 5008,

        [BundleName("packages__battle_pass_package")]
        BattleRoyale = 5009,

        [BundleName("packages__battle_pass_package")]
        BattleRoyalePreview = 5010
    }

    public enum Scenes
    {
        HomeScene,
        BattleScene,
        SplashScene,
        Scene_test_01_QuickOutline
    }
}
}