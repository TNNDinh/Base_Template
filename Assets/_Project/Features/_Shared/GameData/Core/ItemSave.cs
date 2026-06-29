using System;

[Serializable]
public class ItemSave
{
    public MergeEnum.MergeItemTypes itemSaveType;
    public int idItem;

    public override bool Equals(object obj)
    {
        if (obj is ItemSave other) return idItem == other.idItem && itemSaveType == other.itemSaveType;
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(idItem, itemSaveType);
    }
}

[Serializable]
public class ItemPos
{
    public int x;
    public int y;

    public ItemPos(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public ItemPos Clone()
    {
        return new ItemPos(x, y);
    }
}
