using BlackFace.Libraries.Modules.UIModule;
using Ezg.Feature.Social.Account;
using Sirenix.OdinInspector;
using TigerForge;
using UnityEngine;
using UnityEngine.UI;

public class ScreenConfirmAccountLocalController : FeatureBaseController
{
    [TabGroup("Cấu hình riêng")] [SerializeField]
    private Button _buttonConfirm;

    protected override void Start()
    {
        base.Start();
        _buttonConfirm.onClick.AddListener(ConfirmServerData);
    }

    private void ConfirmServerData()
    {
        //EventManager.EmitEvent(EventName.GetLocalData);
        //EventManager.EmitEvent(EventName.ScreenSaveFoundCLose);
        CloseMe(() =>
        {
            EventManager.EmitEvent(EventName.ScreenSaveFoundCLose);
            PlayerDataSyncManager.Instance.GetLocalData();
        });
    }
}