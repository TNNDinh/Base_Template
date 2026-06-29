using System;

[Serializable]
public struct ResourceExchangeModel
{
    public int sourceId;
    public int sourceType;
    public bool isGacha;
    public ResourceExchangeDetailModel[] rewards;
}

[Serializable]
public class ResourceExchangeDetailModel : Resource
{
    public float rate;

    // Giá trị tương ứng EnumBase.ItemRarities (int const) ở project tiêu thụ — giữ int để gỡ phụ thuộc EnumBase.
    public int rarity;
    public bool isEquipment;
}