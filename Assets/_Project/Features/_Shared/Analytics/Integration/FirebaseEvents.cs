using Ezg.Package.Singleton;
using Ezg.Feature.Shared.Config;

public enum FirebaseEvent
{
    tutorial,
    button_click,
    purchase_click,
    buy_resource,
    earn_resource,
    spend_resource,
    ads_reward,
    order_complete,
    order_mania_start,
    order_mania_complete,

    remove_item,
    undo_item,

    ad_impression,

    merge_item,
    generator_tap,
    task_start,
    task_complete,
    energy_depleted,
    inventory_open,
    quit_game,
    use_booster,

    ad_loaded,
    ad_reward,
    ad_click
}

public class FirebaseEventConfig : SingletonNormal<FirebaseEventConfig>
{
    public string ad_format;

    // MaxAdsvertising
    public string ad_platform;

    public string ad_source;
    public string ad_unit_id;
    public string ad_unit_name;

    public int booster_id = GameConstant.NULL_NUMBER;

    // Button Click
    public string category;
    public string currency;
    public int destination_item_id = GameConstant.NULL_NUMBER;
    public string destination_item_type;
    public int duration = GameConstant.NULL_NUMBER;
    public int energy_cost = GameConstant.NULL_NUMBER;

    public long energy_current = GameConstant.NULL_NUMBER;
    public string error_code;
    public string error_type;
    public string event_id;

    public int generator_id = GameConstant.NULL_NUMBER;
    public string generator_level;
    public string generator_type;
    public string id;
    public int[] id_order_remain;

    public int inventory_size = GameConstant.NULL_NUMBER;
    public string item_archieve;

    public string item_buy;
    //==============================


    // Resource
    public string item_category;
    public int item_id = GameConstant.NULL_NUMBER;

    public string item_name;

    public string item_type;

    public string last_action;
    public int level = GameConstant.NULL_NUMBER;

    public string name;
    public int number_tap = GameConstant.NULL_NUMBER;

    public int order_id = GameConstant.NULL_NUMBER;
    public int order_mania_gold_id = GameConstant.NULL_NUMBER;
    public int order_mania_id = GameConstant.NULL_NUMBER;
    public int order_mania_sliver_id = GameConstant.NULL_NUMBER;
    public string order_mania_type;
    public string placement;
    public string price_usd;

    // IAP
    public string product_id;

    // Quest
    public string quest_id;
    public double remain_value = GameConstant.NULL_NUMBER;
    public long remaining_value = GameConstant.NULL_NUMBER;
    public int remove_item_count = GameConstant.NULL_NUMBER;
    public int result_item_id = GameConstant.NULL_NUMBER;
    public string result_item_type;
    public int scene_id = GameConstant.NULL_NUMBER;
    public string source;

    public string source_detail;

    //=========Game custom=========
    public int source_item_id = GameConstant.NULL_NUMBER;
    public string source_item_type;
    public string sourceId;

    public string step_category;

    // Tutorial
    public int step_id = GameConstant.NULL_NUMBER;
    public string step_name;

    public string target_type;

    public string task_id;
    public double total_bought_value = GameConstant.NULL_NUMBER;
    public double total_earn_value = GameConstant.NULL_NUMBER;
    public double total_spent_value = GameConstant.NULL_NUMBER;
    public int tut_id = GameConstant.NULL_NUMBER;
    public int type_generator_cooldown = GameConstant.NULL_NUMBER;
    public int type_tool_cooldown = GameConstant.NULL_NUMBER;
    public int undo_item_count = GameConstant.NULL_NUMBER;
    public int used_slots = GameConstant.NULL_NUMBER;
    public double value = GameConstant.NULL_NUMBER;
}