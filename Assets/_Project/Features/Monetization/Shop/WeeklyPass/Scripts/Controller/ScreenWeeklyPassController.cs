using BlackFace.Libraries.Modules.UIModule;
using Ezg.Core.Extensions;
using Ezg.Package.Localize;
using Ezg.Package.Localize.Localization;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;

public class ScreenWeeklyPassController : FeatureBaseController
{
    [TabGroup("Cấu hình riêng")] [SerializeField]
    private Transform textDes;

    [TabGroup("Cấu hình riêng")] [SerializeField]
    private Transform holderDes;

    protected override void LoadData()
    {
        base.LoadData();
        SpawnDes();
    }

    private void SpawnDes()
    {
        for (var i = 0; i < 100; i++)
        {
            var localize =
                Localization.Current.Get(LocalizeCategory.Shop.ToString().ToSnakeCase(), "weekly_pass_des_" + i);
            if (string.IsNullOrEmpty(localize)) break;
            var objText = Instantiate(textDes, holderDes);
            objText.GetComponentInChildren<Text>().text = localize;
            objText.gameObject.SetActive(true);
        }
    }
}