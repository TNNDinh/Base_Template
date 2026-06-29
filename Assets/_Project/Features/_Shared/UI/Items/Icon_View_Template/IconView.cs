using System.Linq;
using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using Ezg.Core.Extensions;
using Ezg.Core.Utils;
using TigerForge;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;
using Ezg.Feature.Shared.Config;

public class IconView : MonoBehaviour
{
    public Image rarityFrame;
    public Image icon;
    public Image iconFull;
    public Text value;
    public bool isAlwaysShowNumber;
    public bool canShowIconInfo;

    public Button info;
    public GameObject iconInfo;

    public GameObject claimLabel;
    public GameObject bonusLabel;

    [SerializeField] private GameObject vfxHolder;

    [SerializeField] private bool isShowFreeWhenZero;

    public Resource resource;
    private bool _isVfxHolderActive;

    private Button buttonIcon;

    public Resource Resource
    {
        get => resource;
        set => resource = value;
    }

    private void Awake()
    {
        if (info != null) info.onClick.AddListener(ShowInfo);

        buttonIcon = GetComponent<Button>();
        buttonIcon?.onClick.AddListener(OnClick);
    }

    private void OnEnable()
    {
        if (_isVfxHolderActive && vfxHolder != null)
            vfxHolder.SetActive(true);
    }

    private void OnDisable()
    {
        DisableClaimLabel();
        DisableBonusLabel();
    }

    public void SetVfxHolderActive(bool active)
    {
        _isVfxHolderActive = active;
        if (vfxHolder != null)
            vfxHolder.SetActive(active);
    }

    public void EnableBonusLabel()
    {
        if (bonusLabel != null)
            bonusLabel.SetActive(true);
    }

    public void DisableBonusLabel()
    {
        if (bonusLabel != null) bonusLabel.SetActive(false);
    }

    public void EnableClaimLabel()
    {
        if (claimLabel != null) claimLabel.SetActive(true);
    }

    public void DisableClaimLabel()
    {
        if (claimLabel != null)
            claimLabel.SetActive(false);
    }

    private void ShowInfo()
    {
        // removed: ScreenItemInfoData / ScreenItemInfoController (gameplay removed)
    }

    public virtual async void SetData(Resource resource, int rarity = -1, bool isHavePlus = false)
    {
        Resource = resource;

        var valueText = Resource.resNumber.MoneyConvert() +
                        (resource.resId is (int)EnumBase.MoneyTypes
                            .InfinityEnergy /*or (int)EnumBase.MoneyTypes.CoinX2*/
                            ? " mins"
                            : "");


        value.text = valueText;
        iconFull.gameObject.SetActive(false);
        icon.gameObject.SetActive(false);
        icon.sprite = iconFull.sprite = PlayerResource.GetResImage(resource);
        iconFull.gameObject.SetActive(false);
        icon.gameObject.SetActive(true);

        if (rarity > -1)
        {
            rarityFrame.gameObject.SetActive(false);
            // TODO: Sửa file Utils format number
            //rarityFrame.sprite = await ResLoader.LoadAsync<Sprite>(string.Format(PathResourceUtils.iconRarity, rarity));
            rarityFrame.gameObject.SetActive(true);
        }

        if (resource.resType == EnumBase.ResourceTypes.Money)
        {
            //rarityFrame.sprite = PlayerResource.GetCurrencyBackground(DataManager.RarityBackground.GetRarity(resource.resId, resource.resNumber));
        }

        if (resource.resType == EnumBase.ResourceTypes.Item)
        {
            isHavePlus = true;
            // removed: GameplayService.GetItemImageAsync (gameplay removed)
        }

        if (!isAlwaysShowNumber && resource.resNumber == 1)
        {
            //value.text = "";
        }


        if (isHavePlus) value.text = "x" + valueText;

        if (resource.resNumber == 0 && isShowFreeWhenZero) value.text = GameSystems.Localize("free");

        if (canShowIconInfo)
        {
            var isItem = resource.resType == EnumBase.ResourceTypes.Item;
            iconInfo.gameObject.SetActive(isItem);
            if (buttonIcon != null) buttonIcon.enabled = isItem;
        }
        else
        {
            if (buttonIcon != null) buttonIcon.enabled = false;
        }
    }


    private void OnClick()
    {
        var isItem = resource.resType == EnumBase.ResourceTypes.Item;
        if (isItem) ShowInfo();
        // gameObject.ShowTooltip(GameSystems.Localize(resource.resType.ToString().ToLower() + "_" +
        //                                             resource.resId.ToString().ToLower(), LocalizeCategory.Common));
    }
}