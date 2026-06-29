using BlackFace.Libraries.Modules.UIModule;
using Ezg.Feature.Shared;
using Ezg.Feature.Social.Account;
using UnityEngine.UI;
using Ezg.Feature.Shared.GameData;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.System.GameCheat
{
    internal class GameCheatSyncDataController : FeatureBaseController
    {
        public InputField AccountIdInput;

        public void OnAccept()
        {
            var accountId = AccountIdInput.text.Trim();
            if (string.IsNullOrEmpty(accountId))
            {
                GameSystems.ShowSimpleMessage("Account ID is empty");
                return;
            }

            PlayerDataManager.ClearCachedModules();
            PlayerDataSyncManager.Instance.GetPlayerDataAdmin(accountId);
        }
    }
}