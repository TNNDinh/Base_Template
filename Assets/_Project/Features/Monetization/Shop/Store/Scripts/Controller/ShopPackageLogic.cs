using Ezg.Core.UI;
using Ezg.Core.Utils;
using TigerForge;
using UnityEngine;
using Ezg.Feature.Shared.Systems;

public class ShopPackageLogic : MonoBehaviour
{
    [SerializeField] private EnumBase.MoneyTypes moneyType;
    [SerializeField] private ShopItemRawPack prefab;
    [SerializeField] private Transform container;
    [SerializeField] private Transform holderTimeDiscount;
    [SerializeField] private UI_CooldownTimeView _cooldownTimeView;

    private void Start()
    {
        UpdateView();
    }


    private void OnEnable()
    {
        EventManager.StartListening(EventName.EndDiscountGemRaw, UpdateViewDiscount);
    }

    private void OnDisable()
    {
        EventManager.StopListening(EventName.EndDiscountGemRaw, UpdateViewDiscount);
    }

    private void UpdateView()
    {
        foreach (var packModel in ShopService.GetAllPack(moneyType))
        {
            var package = Instantiate(prefab, container);

            package.gameObject.SetActive(true);
            package.SetData(packModel, moneyType);
        }

        UpdateViewDiscount();
    }

    private void UpdateViewDiscount()
    {
        // removed: DiscountGemRawService
        holderTimeDiscount.gameObject.SetActive(false); // gameplay removed
    }
}