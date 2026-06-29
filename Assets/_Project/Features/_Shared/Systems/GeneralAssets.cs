using System;
using Easygoing.Packages.Dictionary;
using Ezg.Core.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using Ezg.Feature.Shared.Systems;

[Serializable]
[CreateAssetMenu(fileName = "ResourcesAssetLoader", menuName = "Game custom/General Assets")]
public class GeneralAssets : ScriptableObject
{
    [SerializeField] [TabGroup("Cấu hình")]
    public GameObject TooltipElement;

    [SerializeField] [TabGroup("Cấu hình")]
    public GameObject TooltipImageButtonElement;

    [SerializeField] [TabGroup("Cấu hình")]
    public GameObject TooltipIngredientElement;

    [SerializeField] [TabGroup("Cấu hình")]
    public GameObject BattleShockWaveFx;

    [SerializeField] [TabGroup("Cấu hình")]
    public GameObject BattlePowerUpShieldFx;

    [SerializeField] [TabGroup("Cấu hình")]
    public GameObject BattlePowerIceFx;

    [SerializeField] [TabGroup("Cấu hình")]
    public GameObject BattlePowerMagnet;

    [SerializeField] [TabGroup("Cấu hình")]
    public GameObject BattlePowerRageFx;

    [SerializeField] [TabGroup("Cấu hình")]
    public GameObject BattlePowerUpGoldRushFx;

    [SerializeField] [TabGroup("Cấu hình")]
    public GameObject BattleExplodeWhenEnemyDie;

    [SerializeField] [TabGroup("Cấu hình")]
    public GameObject BattleRageModeEndWave;

    [SerializeField] [TabGroup("Cấu hình")]
    public GameObject BattleStunFx;

    [SerializeField] [TabGroup("Cấu hình")]
    public GameObject CharacterDirectionArrow;

    [SerializeField] [TabGroup("Cấu hình")]
    public Color32 CurrencyEnough;

    [SerializeField] [TabGroup("Cấu hình")]
    public Color32 CurrencyNotEnough;

    [SerializeField] [TabGroup("Cấu hình")]
    public GameObject BattleFreezeEffect;

    [SerializeField] [TabGroup("Cấu hình")]
    public GameObject BattleInstantKill;


    // [SerializeField]
    // [TabGroup("Cấu hình")]
    // public SupabaseSettings SupabaseSettings;

    [SerializeField] [TabGroup("Cấu hình")]
    public GameObject[] GameObjectManualLoad;

    [SerializeField] public SerializableDictionary<EnumBase.ItemRarities, Color32> ColorRarites;
}