using BlackFace.Libraries.Modules.UIModule;
using Ezg.Feature.Shared;
using Ezg.Feature.Social.Account;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.GameData;

public class ScreenConfirmAccountServerController : FeatureBaseController
{
    [TabGroup("Data Server")] [SerializeField]
    private Text textLevelServer;

    [TabGroup("Data Server")] [SerializeField]
    private Text textExpServer;

    [TabGroup("Data Local")] [SerializeField]
    private Text textLevelLocal;

    [TabGroup("Data Local")] [SerializeField]
    private Text textExpLocal;

    [TabGroup("Cấu hình riêng")] [SerializeField]
    private Button _buttonConfirm;

    protected override void Start()
    {
        base.Start();
        _buttonConfirm.onClick.AddListener(ConfirmServerData);
    }

    protected override void LoadData()
    {
        base.LoadData();

        UpdateUI();
    }

    private void UpdateUI()
    {
        // removed: OrderData / OrderDataManager / PlayerDataManager.OrderDataManager (gameplay removed)
    }

    private void ConfirmServerData()
    {
        PlayerDataSyncManager.Instance.GetServerData();
        //EventManager.EmitEvent(EventName.GetServerData);
    }
}