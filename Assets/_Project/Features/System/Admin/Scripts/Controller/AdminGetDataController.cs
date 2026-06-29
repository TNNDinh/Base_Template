using BlackFace.Libraries.Modules.UIModule;
using Cysharp.Threading.Tasks;
using Ezg.Feature.Shared;
using Ezg.Feature.Social.Account;
using Ezg.Package.Factory;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.GameData;

namespace Ezg.Feature.System.Admin
{
    public class AdminGetDataController : FeatureBaseController
    {
        [SerializeField] [TabGroup("Cấu hình riêng")]
        private InputField _accountId;

        public void GetDataFromUserId()
        {
            PlayerDataSyncManager.Instance.GetPlayerDataAdmin(_accountId.text);
            //AdminToolManager.CheckUserEmail(_accountId.text, successAction: () =>
            //{
            //    //FirebaseLoginManager.OnGoogleLogout(null);
            //    PlayerDataSyncManager.Instance.GetPlayerDataAdmin(AdminToolManager.UserIdGetted);
            //});
        }

        public void PushData()
        {
            DataPlayer.SaveAllData();
            PlayerDataSyncManager.Instance
                .PushPlayerDataAdmin(_accountId.text ?? PlayerDataManager.Account.dataBase.accountId).Forget();
        }
    }
}