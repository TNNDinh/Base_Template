using System.Collections;
using Ezg.Core.Extensions;
using Ezg.Core.Utils;
using Ezg.Package.Localize;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;

public class ResourcePackController : MonoBehaviour
{
    [SerializeField] private Text textTotalCoinBonus;
    [SerializeField] private Text textTimeReshuffle;

    private void Start()
    {
        UpdateView();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void UpdateView()
    {
        var percent = $"{ShopService.GetTotalPercentBonusCoin() * 100}%";
        var colorPercent = $"{Utils.ChangeColorArticle(ColorUtils.Color3, percent)}";
        textTotalCoinBonus.text = string.Format(GameSystems.Localize("total_coin_bonus", LocalizeCategory.Shop),
            colorPercent);

        StartCoroutine(CountTimeNextDay());
    }

    private IEnumerator CountTimeNextDay()
    {
        while (true)
        {
            var timeNextDay = TimeManager.GetNextDayTime() - TimeManager.GetNow();
            textTimeReshuffle.text = GameSystems.Localize("scroll_reshuffle_in", LocalizeCategory.Shop) + " " +
                                     TimeManager.GetRemainingTimeToString(timeNextDay);
            yield return new WaitForSecondsRealtime(1f);
        }
    }
}