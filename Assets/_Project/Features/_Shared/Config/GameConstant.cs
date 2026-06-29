using UnityEngine;

namespace Ezg.Feature.Shared.Config
{
public struct GameConstant
{
    public const int NULL_NUMBER = -1;
    public const string FolderProject = "_Project";
    public const string PathResources = FolderProject + "/Resources/";

    public static string DeviceId => SystemInfo.deviceUniqueIdentifier;

    // PlayerPrefs

    public const string LanguageSetting = "LanguageSetting";

    // CSV
    public const string InstallFromGoogle = "com.android.vending";
    public const string InstallFromApple = "com.android.vending";

    public const string LinkStoreFree =
        "https://play.google.com/store/apps/details?id=com.shadow.survival.rogue.action.shooter.game";

    public const string LinkStorePremium =
        "https://play.google.com/store/apps/details?id=com.shadow.survival.rogue.action.shooter.game.premium";

    public const string LinkStoreIos = "https://apps.apple.com/app/id6449216387";
    public const string LinkFacebook = "https://www.facebook.com/shadowsurvival.gl";
    public const string GoogleProvider = "google.com";
    public const string AppleProvider = "apple.com";
    public const string PackNameAndroidFree = "com.shadow.survival.rogue.action.shooter.game";
    public const string PackNameAndroidPremium = "com.shadow.survival.rogue.action.shooter.game.premium";

    public const string KEY_INVERT_CONTROL = "KEY_INVERT_CONTROL";
    public const string KEY_SHOW_TEXT_DAMAGE = "KEY_SHOW_TEXT_DAMAGE";

    public const float CANVAS_WIDTH = 1920;
    public const float CANVAS_HEIGHT = 1080;
    public const float BOUND_MIN_X = -4;
    public const float BOUND_MAX_X = 4;
    public const float BOUND_MIN_Y = 6;
    public const float BOUND_MAX_Y = 22;
    public const int DEFAULT_ROOM = 0;
    public const int DEFAULT_WAVE = 0;
    public const int DEFAULT_NUMBER_ROOM = 5;
    public const int DEFAULT_COUNT_RANDOM = 1;
    public const float DISTANCE_KILL = 20f;
    public const int NUMBER_CARD_SELECTION = 3;
    public const int SECOND_ONE_DAY = 86400;
    public const int MAX_AMOUNT_CURRENCY = 8;
    public static readonly Vector3 NULL_VECTOR3 = new(-1, -1, -1);


    public const int UNIT_ID = 10000;


    #region BuildUpGoal

    public const string AnimationNameBuild = "animation";
    public const string AnimationNameIdle = "idle";

    #endregion

    /// <summary>
    ///     item id bao gồm item type, rarity, id chính của item đó, format sẽ là: [itemType] * 10000 + [rarity] * 1000 + [id]
    /// </summary>
    public const int ITEM_ID = 10000;

    public const int ITEM_RARITY = 1000;
    public const int MAP_ID = 10000;


    #region Tag Tutorial

    public const string ButtonPlay = "button_play";
    public const string ButtonClaimOrder = "button_claim_order";
    public const string ButtonDoneGem = "button_done_gem";
    public const string ButtonCook = "button_cook";
    public const string ButtonConfirmNewEquipment = "button_confirm_new_equipment";
    public const string ButtonCloseOrderMania = "button_close_order_mania";
    public const string ButtonClickOrderMania = "button_click_order_mania";
    public const string ButtonCloseBattlePass = "button_close_battle_pass";
    public const string ButtonClaimTutBattlePass = "button_claim_tut_battle_pass";
    public const string ButtonLevelToLevelPass = "button_level_to_level_pass";
    public const string ButtonBuildUpGoalInTask = "button_build_up_goal_in_task";
    public const string ButtonBuildInGamePlay = "button_build_in_game_play";
    public const string ItemTemplate = "ItemTemplate";
    public const string CardBonus = "CardBonus";
    public const string CardsContainer = "cards_container";
    public const string ButtonPurchaseCurrency = "button_purchase_curency";
    public const string ButtonInfoScreenGameplay = "button_info_screen_gameplay";
    public const string ButtonShop = "button_shop";
    public const string BgHighLightLevelPass = "bg_highlight_level_pass";
    public const string ButtonLevelPassPackage = "button_level_pass_package";
    public const string ItemIngredient = "item_ingredient";

    public const string ButtonDoneBubble = "button_done_bubble";
    public const string ButtonChefsBook = "button_chefs_book";
    public const string ButtonUnlockChest = "button_unlock_chest";

    public const string ButtonInventory = "button_inventory";

    public const string ButtonDoneCharging = "button_done_charging";

    public const string ObjScrollGameplay = "obj_scroll_gameplay";

    public const string BackGroundObjScrollGameplay = "background_scroll_gameplay";
    public const string ButtonBuildUpGoalHome = "button_build_up_goal_home";

    #endregion

    #region Layer

    public const string LayerUI = "UI";
    public const string LayerUIHideByCheat = "ui_hide_by_cheat";

    #endregion
}
}