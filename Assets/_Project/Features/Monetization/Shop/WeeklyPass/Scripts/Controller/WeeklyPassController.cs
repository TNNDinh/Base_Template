using TigerForge;
using UnityEngine;
using Ezg.Feature.Shared.GameData;

public class WeeklyPassController : MonoBehaviour
{
    public bool isAds;
    [SerializeField] private WeeklyPassItem silverWeeklyPassItem;
    [SerializeField] private WeeklyPassItem goldenWeeklyPassItem;
    [SerializeField] private Transform container;


    private void Start()
    {
        UpdateView();

        EventManager.StartListening(EventName.ShopUpdated, UpdateView);
    }

    private void UpdateView()
    {
        var dataSilverWeeklyPass = DataManager.SilverWeeklyPass.dataGroup;

        if (dataSilverWeeklyPass != null) silverWeeklyPassItem.SetData(dataSilverWeeklyPass, WeeklyPassType.Silver);

        var dataGoldWeeklyPass = DataManager.GoldWeeklyPass.dataGroup;

        if (dataGoldWeeklyPass != null) goldenWeeklyPassItem.SetData(dataGoldWeeklyPass, WeeklyPassType.Gold);
    }
}