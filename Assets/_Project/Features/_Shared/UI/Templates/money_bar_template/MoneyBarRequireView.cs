using Ezg.Core.Extensions;
using Ezg.Core.Utils;
using Ezg.Package.Localize.Localization;
using TigerForge;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;

public enum RequireViewType
{
    None,
    Mul,
    Add,
    Div,
    Sub
}

public enum RoundingType
{
    Rounding = 0,
    NotRounding = 1
}

public class MoneyBarRequireView : MonoBehaviour
{
    public Image icon;
    public Text value;

    public bool isShowWithCurrent;
    public bool showIconWhenFree;
    public RequireViewType type = RequireViewType.Mul;

    private Resource _resource;

    public virtual void OnEnable()
    {
        if (isShowWithCurrent)
            EventManager.StartListening(EventName.UpdateResource, UpdateView);
    }

    public virtual void OnDisable()
    {
        if (isShowWithCurrent)
            EventManager.StopListening(EventName.UpdateResource, UpdateView);
    }

    private void UpdateView()
    {
        if (_resource != null) SetRequirementData();
    }

    public async void SetData(Resource resource)
    {
        if (resource == null)
        {
            gameObject.SetActive(false);
            return;
        }

        icon.gameObject.SetActive(false);
        icon.sprite = PlayerResource.GetResImage(resource);
        icon.gameObject.SetActive(resource.resNumber >= 0);

        _resource = resource;

        if (!isShowWithCurrent)
        {
            // value.text = resource.resNumber.ValidResource((EnumBase.MoneyTypes)_resource.resId, true);
            if (!showIconWhenFree) icon.gameObject.SetActive(resource.resNumber > 0);

            value.text = resource.resNumber > 0 ? resource.resNumber.ToString() : GameSystems.Localize("free");
        }
        else
        {
            SetRequirementData();
        }


        gameObject.SetActive(true);
    }

    public async void SetData(Resource resource, bool isMax)
    {
        if (resource == null)
        {
            gameObject.SetActive(false);
            return;
        }

        if (isMax)
        {
            value.text = Localization.Current.Get("common", "max");
        }
        else
        {
            if (type == RequireViewType.None)
                value.text = Utils.ChangeColorArticle(ColorUtils.Color5, resource.resNumber.MoneyConvert());
            else
                value.text = Utils.GetOperator(type) + resource.resNumber.MoneyConvert();
        }


        icon.gameObject.SetActive(false);
        icon.sprite = PlayerResource.GetCurrencyImage(resource.resId);
        icon.gameObject.SetActive(true);


        gameObject.SetActive(true);
    }

    public void SetRequirementData()
    {
        var currentOwned = PlayerResource.GetMoneyQuantity((EnumBase.MoneyTypes)_resource.resId);
        var require = _resource.resNumber;

        var color = currentOwned - require >= 0 ? ColorUtils.Color1 : ColorUtils.Color5;

        value.text = //ColorUtils.GetStringWithColor(color,
            $"{Utils.ChangeColorArticle(color, require.MoneyConvert())}";
    }
}