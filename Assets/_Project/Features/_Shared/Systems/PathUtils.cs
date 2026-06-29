using Ezg.Core.Adapter;

namespace Ezg.Feature.Shared.Systems
{
    public partial class PathUtils
    {
        public const string PathDataLevel = "DataLevels/{0}/";
        public const string PathPrefabLevels = "Prefabs/Levels/{0}/";

        public const string PathPrefabVFX = "Prefabs/VFX/";
        public const string PathPrefabBooster = "Prefabs/Booster/";

        public const string PathPrefabIconFeature = "Images/IconFeature/";

        public static readonly string UserAvatar = "user_ava/";
        public static readonly string UserFrame = "user_frame/";

        public static readonly string StatImgPath = "Images/StatsIcon/";
    }

    public partial class PathUtils
    {
        // Format strings — giữ nguyên string vì cần String.Format("{0}")
        public static readonly string StatusItem = "Prefabs/StatusItem/{0}";
        public static readonly string RoleItem = "Prefabs/RoleItem/{0}";

        public static readonly AssetRef VfxItemDeduct =
            AssetRefs.Gameplay.Prefabs(string.Format(RoleItem, "DeductEnergy/vfx_idle_item_deduct_energy"));

        public static readonly AssetRef VfxLight =
            AssetRefs.Gameplay.Prefabs(string.Format(RoleItem, "CoolDownChest/VFX_Light"));

        public static readonly AssetRef VfxDuplicateCameraBoost =
            AssetRefs.Gameplay.Booster("Prefabs/dupcamera_boost/vfx_dupcamera_boost");

        public static readonly AssetRef VfxSpeedBoostExplosion =
            AssetRefs.Gameplay.Booster("Prefabs/speed_boost/vfx_speedboost_explosion");

        public static readonly AssetRef
            VfxSpeedBoost = AssetRefs.Gameplay.Booster("Prefabs/speed_boost/vfx_speedboost");

        public static readonly AssetRef VfxSandglassBoostExplosion =
            AssetRefs.Gameplay.Booster("Prefabs/sandglass_boost/vfx_sandglass_boost_explosion");

        public static readonly AssetRef VfxSandglassBoost =
            AssetRefs.Gameplay.Booster("Prefabs/sandglass_boost/vfx_sandglass_boost");

        public static readonly AssetRef VfxUnlimitSymbol =
            AssetRefs.Gameplay.Booster("Prefabs/unlimit_energy/vfx_unlimit_symbol");

        public static readonly AssetRef VfxItemMergeHold = AssetRefs.Shared("Prefabs/fx_common/vfx_item_merge_hold");
        public static readonly AssetRef VfxImpactMerge = AssetRefs.Shared("Prefabs/fx_common/vfx_impact_merge_1");
        public static readonly AssetRef VfxImpactMerge2 = AssetRefs.Shared("Prefabs/fx_common/vfx_impact_merge_2");
        public static readonly AssetRef VfxImpactMerge4 = AssetRefs.Shared("Prefabs/fx_common/vfx_impact_merge_4");
        public static readonly AssetRef VfxOrderComplete = AssetRefs.Shared("Prefabs/fx_common/vfx_order_complete_1");
        public static readonly AssetRef VfxUsingItem = AssetRefs.Shared("Prefabs/fx_common/vfx_using_item_1");
        public static readonly AssetRef VfxDoneItem = AssetRefs.Shared("Prefabs/fx_common/vfx_done_item_1");
        public static readonly AssetRef VfxBreakBox = AssetRefs.Shared("Prefabs/fx_common/vfx_break_box_1");
        public static readonly AssetRef VfxBreakBox2 = AssetRefs.Shared("Prefabs/fx_common/vfx_break_box_2");

        public static readonly AssetRef VfxCompleteCooldown =
            AssetRefs.Shared("Prefabs/fx_common/vfx_generator_complete_cooldown");

        public static readonly AssetRef CellPrefab = AssetRefs.Gameplay.Prefabs("Prefabs/Gameplay/grid_cell");
        public static readonly AssetRef CurrencyPrefab = AssetRefs.Gameplay.Prefabs("Prefabs/Gameplay/Currency");

        public static readonly AssetRef IconFocusOverlay =
            AssetRefs.Gameplay.Prefabs("Prefabs/Gameplay/icon_focus_overlay");

        public static readonly AssetRef RoleItemBubbleImage =
            AssetRefs.Gameplay.Prefabs(string.Format(RoleItem, "Bubble/BubbleImage"));

        public static readonly AssetRef RoleItemBubbleTextTime =
            AssetRefs.Gameplay.Prefabs(string.Format(RoleItem, "Bubble/TextTime"));

        public static readonly AssetRef RoleItemExpandImageTime =
            AssetRefs.Gameplay.Prefabs(string.Format(RoleItem, "Expand/ImageTime"));

        public static readonly AssetRef RoleItemCoolDownChestImageTime =
            AssetRefs.Gameplay.Prefabs(string.Format(RoleItem, "CoolDownChest/ImageTime"));

        public static readonly AssetRef RoleItemCoolDownChestTextTime =
            AssetRefs.Gameplay.Prefabs(string.Format(RoleItem, "CoolDownChest/TextTime"));

        public static readonly AssetRef RoleItemGeneratorTextBonus =
            AssetRefs.Gameplay.Prefabs(string.Format(RoleItem, "Generator/TextBonus"));

        public static readonly AssetRef RoleItemGeneratorImageTime =
            AssetRefs.Gameplay.Prefabs(string.Format(RoleItem, "Generator/ImageTime"));

        public static readonly AssetRef GeneratorVfxIdleItemAddBonus =
            AssetRefs.Gameplay.Prefabs(string.Format(RoleItem, "Generator/vfx_idle_item_add_bonus"));

        public static readonly AssetRef RoleItemImageStar =
            AssetRefs.Gameplay.Prefabs(string.Format(RoleItem, "ItemMergeTemplate/ImageStar"));

        public static readonly AssetRef RoleItemToolImageProgress =
            AssetRefs.Gameplay.Prefabs(string.Format(RoleItem, "Tool/ImageProgress"));

        public static readonly AssetRef RoleItemToolImageDone =
            AssetRefs.Gameplay.Prefabs(string.Format(RoleItem, "Tool/ImageDone"));

        public static readonly AssetRef RoleItemToolImageInfo =
            AssetRefs.Gameplay.Prefabs(string.Format(RoleItem, "Tool/Image!"));

        public static readonly AssetRef ImageToolInfoYellow =
            AssetRefs.Gameplay.Prefabs(string.Format(RoleItem, "Tool/Image!_yellow"));

        public static readonly AssetRef ImageToolSliderProgress =
            AssetRefs.Gameplay.Prefabs(string.Format(RoleItem, "Tool/slider_progress"));


        public static readonly AssetRef OrderFrameBackground =
            AssetRefs.Gameplay.Prefabs("OrderTemplateFrames/back_ground");

        public static readonly AssetRef OrderFrameBackgroundDone =
            AssetRefs.Gameplay.Prefabs("OrderTemplateFrames/back_ground_done");

        public static readonly AssetRef OrderFrameBackgroundReward =
            AssetRefs.Gameplay.Prefabs("OrderTemplateFrames/back_ground_reward");

        public static readonly AssetRef OrderFrameBackgroundRewardDone =
            AssetRefs.Gameplay.Prefabs("OrderTemplateFrames/back_ground_reward_done");

        public static AssetRef CookingToolSkeleton(string stringName)
        {
            return AssetRefs.Gameplay.Tools($"Spine/CookingTools/{stringName}/{stringName}_SkeletonData");
        }

        public static AssetRef NpcSkeleton(int idNpc)
        {
            return AssetRefs.Gameplay.Prefabs($"Spine/NPC/{idNpc}/NPC_{idNpc:D2}_SkeletonData");
        }
    }

    // Features
    public partial class PathUtils
    {
        public static readonly AssetRef OpenStarChestImage =
            AssetRefs.Features.OpenStarChest("Images/star_chest");

        public static AssetRef ThemeImage(string key)
        {
            return AssetRefs.Features.SelectTheme("ImagesTheme/" + key);
        }

        public static AssetRef MaskChangeSceneImage(string key)
        {
            return AssetRefs.Features.ScreenMaskChangeScene("MashChangeScene/" + key);
        }
    }

    // Events
    public partial class PathUtils
    {
        public static readonly AssetRef SpeedFeastRaceButtonGate =
            AssetRefs.Events.SpeedFeastRace("button_gate_speed_feast_race");

        public static AssetRef PetalPlatePartyBoxIcon(string iconBoxId)
        {
            return AssetRefs.Events.PetalPlateParty("Box/" + iconBoxId);
        }

        public static AssetRef PetalPlatePartyTaskItemIcon(string iconId)
        {
            return AssetRefs.Events.PetalPlateParty("TaskItem/" + iconId);
        }
    }
}