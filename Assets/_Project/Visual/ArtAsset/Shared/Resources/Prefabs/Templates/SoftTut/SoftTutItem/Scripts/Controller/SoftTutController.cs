using System;
using System.Collections.Generic;
using BlackFace.Libraries.Modules.UIModule;
using Sirenix.OdinInspector;
using UnityEngine;

public class SoftTutController : FeatureBaseController
{
    [SerializeField] [TabGroup("Cấu hình riêng")]
    private List<TutItem> _tutItmes;

    public override void LoadData(object data)
    {
        if (data is SoftTutType)
        {
            var tutType = (SoftTutType)data;
            foreach (var item in _tutItmes)
                if (item.softTutType == tutType)
                    item.tutItem.SetActive(true);
                else
                    item.tutItem.SetActive(false);
        }
        else
        {
            foreach (var item in _tutItmes) item.tutItem.SetActive(false);
        }
    }
}

[Serializable]
public enum SoftTutType
{
    None,
    SofTutItemTool,
    SoftTutPiggyBank
}

[Serializable]
public class TutItem
{
    public SoftTutType softTutType;
    public GameObject tutItem;
}