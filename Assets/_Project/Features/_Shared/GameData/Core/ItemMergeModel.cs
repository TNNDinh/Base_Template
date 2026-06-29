using System;

[Serializable]
public struct ItemMergeModel
{
    public string id;
    public string nameItem;
    public bool canMerge;
    public int sellPrice;
    public int sumMerge;
}

[Serializable]
public struct ItemMergeExpand
{
    public int expandId;
    public float expandRate;
    public Resource rewards;
}