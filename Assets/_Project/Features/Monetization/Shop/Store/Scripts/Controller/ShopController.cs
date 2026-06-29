using System;
using System.Collections.Generic;
using System.Linq;
using BlackFace.Libraries.Modules.UIModule;
using Ezg.Core.Extensions;
using Ezg.Feature.Meta.HomeScene;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Ezg.Feature.Shared.Systems;

public class ShopController : MonoBehaviour
{
    [SerializeField] [TabGroup("Cấu hình tính năng")]
    protected ScrollRect _scroll;

    [SerializeField] [TabGroup("Cấu hình tính năng")]
    protected RectTransform _gachaGroup;

    private List<RectTransform> _shopRawContent;

    protected void Start()
    {
        _shopRawContent = _scroll.content.Cast<Transform>().Select(t => t.GetComponent<RectTransform>())
            .Where(t => t != null)
            .ToList();
        ShopService.Controller = this;
        //SnapTo(2);
        //base.Start();
        // if (TutorialContainer.CheckTutorial(Location.Shop))
        // {
        //     SnapTo(5);
        // }
        // if (TutorialContainer.GetTutorialStatus(TutorialStep.ShopDailyGift) == TutorialStatus.Open)
        // {
        //     SnapTo(2);
        // }
    }

    private void OnEnable()
    {
        HomeSceneManager.ShopHomeController = this;
        // removed: TutorialContainer, Location
    }

    public void LoadData(object data)
    {
        //base.LoadData(data);
        SnapTo((int)data);
    }


    public void SnapTo(int index)
    {
        this.DelayMethod(0.01f, () => _scroll.SnapTo(_scroll.content, _shopRawContent, index));
    }

    //public override void CloseMe(Action completeAction = null)
    //{
    //    base.CloseMe(completeAction);
    //    TutorialContainer.CheckTutorial(Location.UnlockFeature);
    //    //LocalNotificationManager.PushDailyReward();
    //}
}

[Serializable]
public class ShopProperties
{
    public int indexScroll;
    public UIManager.UIGroupName groupName;
}