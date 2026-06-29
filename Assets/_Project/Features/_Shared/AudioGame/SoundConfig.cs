using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class SoundConfig : ScriptableObject
{
    [SerializeField] [TabGroup("Cấu hình")]
    public AudioClip[] MainMenuMusics;

    [SerializeField] [TabGroup("Cấu hình")]
    public AudioClip[] BattleMusics;

    [SerializeField] [TabGroup("Cấu hình")]
    public AudioClip[] BattleHardMusics;

    [SerializeField] [TabGroup("Cấu hình")]
    public AudioClip ButtonSelect;

    [SerializeField] [TabGroup("Cấu hình")]
    public AudioClip PurchaseSuccess;

    [SerializeField] [TabGroup("Gameplay")]
    public AudioClip TapGenerator;

    [SerializeField] [TabGroup("Gameplay")]
    public AudioClip[] MergeLevelItems;

    [SerializeField] [TabGroup("Gameplay")]
    public AudioClip[] GenerateItems;

    [SerializeField] [TabGroup("Gameplay")]
    public AudioClip CompleteManiaOrder;

    [SerializeField] [TabGroup("Gameplay")]
    public AudioClip OpenCompleteManiaOrder;

    [TabGroup("Gameplay")] public AudioClip SpawnBubble;

    [TabGroup("Gameplay")] public AudioClip InventoryOpen;

    [TabGroup("Gameplay")] public AudioClip OrderComplete;

    [TabGroup("Gameplay")] public AudioClip PutItemOutTool;

    [TabGroup("Gameplay")] [Title("Cooking Tools")]
    public AudioClip ChefCounterActive;

    [TabGroup("Gameplay")] public AudioClip GrillActive;

    [TabGroup("Gameplay")] public AudioClip JuicerActive;

    [TabGroup("Gameplay")] public AudioClip PanActive;

    [TabGroup("Gameplay")] public AudioClip CookingToolComplete;

    [TabGroup("Cấu hình")] public AudioClip OrderManiaOpen;

    [TabGroup("Cấu hình")] public AudioClip OpenShop;

    [TabGroup("Cấu hình")] public AudioClip PurchaseItem;

    [SerializeField] [TabGroup("Cấu hình")]
    public AudioClip GoldFlyStart;

    [SerializeField] [TabGroup("Cấu hình")]
    public AudioClip GoldFlyEnd;

    [SerializeField] [TabGroup("Cấu hình")]
    public AudioClip PiggyBankCollect;

    [SerializeField] [TabGroup("Cấu hình")]
    public AudioClip PiggyBankBreak;

    [SerializeField] [TabGroup("Cấu hình")]
    public AudioClip OpenPopup;

    [SerializeField] [TabGroup("Cấu hình")]
    public AudioClip ClosePopup;

    [FormerlySerializedAs("DoneOrder")] [SerializeField] [TabGroup("Cấu hình")]
    public AudioClip ClaimOrder;

    [SerializeField] [TabGroup("Build Up Goal")]
    public AudioClip levelComplete;

    [SerializeField] [TabGroup("Build Up Goal")]
    public SoundSceneConfig[] soundBuildUp;

    [SerializeField] [TabGroup("Cấu hình")]
    public AudioClip OpenNewItemScreen;
}

[Serializable]
public class SoundSceneConfig
{
    public ThemeType themeType;
    public AudioClip[] sounds;
}