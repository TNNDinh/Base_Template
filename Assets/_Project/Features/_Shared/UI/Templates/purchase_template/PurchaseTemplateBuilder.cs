using System;
using UnityEngine;

public class PurchaseTemplateBuilder
{
    public PurchaseType PurchaseType { get; set; }
    public bool ActivePack { get; set; }
    public string PackNameIap { get; set; }
    public string Localize { get; set; }
    public string Count { get; set; }
    public string ShowProgress { get; set; }
    public float Sale { get; set; }
    public Resource Require { get; set; }
    public Resource[] Rewards { get; set; }
    public Sprite SpritePack { get; set; }

    public PurchaseTemplateParameters Build()
    {
        return new PurchaseTemplateParameters
        {
            PurchaseType = PurchaseType
        };
    }

    #region Setter

    public PurchaseTemplateBuilder SetPurchaseType(PurchaseType purchaseType)
    {
        PurchaseType = purchaseType;
        return this;
    }

    public PurchaseTemplateBuilder SetActivePack(bool isActivePack)
    {
        ActivePack = isActivePack;
        return this;
    }

    #endregion
}

[Serializable]
public class PurchaseTemplateParameters
{
    public PurchaseType PurchaseType { get; set; }
    public bool ActivePack { get; set; }
    public string PackNameIap { get; set; }
    public string Localize { get; set; }
    public string Count { get; set; }
    public bool ShowProgress { get; set; }
    public float Sale { get; set; }
    public Resource Require { get; set; }
    public Resource[] Rewards { get; set; }
    public Sprite SpritePack { get; set; }

    // Thêm các thuộc tính khác nếu cần

    public static PurchaseTemplateBuilder Builder()
    {
        return new PurchaseTemplateBuilder();
    }
}