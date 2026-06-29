using UnityEngine;

namespace Ezg.Feature.Meta.HomeScene
{
    internal class CampaignChangeModeButton : MonoBehaviour
    {
        private void Start()
        {
            //gameObject.SetActive(PlayerDataManager.Campaign.Level > DataManager.BattleConfig.GetData().unlockHardStage);
        }

        public void OnChangeMode()
        {
            //PlayerDataManager.BattleData.dataBase.CampaignModeSelected++;
            //if ((int)PlayerDataManager.BattleData.dataBase.CampaignModeSelected >= Enum.GetValues(typeof(EnumBase.CampaignModes)).Length)
            //{
            //    PlayerDataManager.BattleData.dataBase.CampaignModeSelected = 0;
            //}
            //PlayerDataManager.BattleData.Save();
            //EventManager.EmitEvent(nameof(EventName.BattleCampaignChangeMode));
        }
    }
}