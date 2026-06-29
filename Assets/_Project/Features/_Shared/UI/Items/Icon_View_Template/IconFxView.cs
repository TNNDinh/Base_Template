using System.Collections.Generic;
using Ezg.Core.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class IconFxView : IconView, IExecute
{
    [SerializeField] private List<GameObject> fxAppears;
    [SerializeField] private string prefixFx = "item_rarity_";
    [SerializeField] private Image _rarityImage;

    [SerializeField] [TabGroup("Cấu hình tính năng")]
    private Image _levelBackground;

    [SerializeField] [TabGroup("Cấu hình tính năng")]
    private Text _levelText;

    private EnumBase.ItemRarities _rarity;

    protected void OnEnable()
    {
        foreach (var fx in fxAppears) fx.SetActive(false);
    }

    public void Execute()
    {
        if (resource == null) return;
        ShowFx(_rarity);
    }

    public override async void SetData(Resource resource, int rarity = -1, bool isHavePlus = false)
    {
        //_rarity = ((ItemResource)resource).Rarity;

        base.SetData(resource, rarity);

        //_rarityImage.sprite = PlayerResource.GetItemBackground(((ItemResource)resource).Rarity);

        ShowLevel();
    }

    private void ShowLevel()
    {
        //_levelBackground.gameObject.SetActive(Resource is ItemJewelResource or ItemEquipmentResource);
        //if (_levelBackground.gameObject.activeSelf)
        //{
        //    _levelBackground.color =
        //        DataManager.GeneralAssets.ColorRarites[((ItemResource)Resource).Rarity];
        //    _levelText.text = Resource is ItemJewelResource ? ((ItemJewelResource)Resource).Level.ToString() : PlayerDataManager.PlayerResource.dataBase.EquipmentLevel[((ItemEquipmentResource)Resource).equipmentType].ToString();
        //}
    }

    private void ShowFx(EnumBase.ItemRarities rarity)
    {
        foreach (var fx in fxAppears)
        {
            var nameRarity = $"{prefixFx}{(int)rarity}";
            fx.SetActive(false);
            if (fx.name.Equals(nameRarity)) fx.SetActive(true);
        }
    }
}