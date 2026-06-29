using System;
using Ezg.Core.Utils;
using Ezg.Feature.Shared.Systems;

public class ItemResource : Resource
{
    //public EnumBase.ItemTypes ItemType => DataManager.Items.GetTypeById(resId);
    public virtual EnumBase.ItemRarities Rarity { get; set; }
    public virtual Guid InventoryId { get; set; } = Guid.NewGuid();

    public override Resource Clone()
    {
        return (ItemResource)MemberwiseClone();
    }
}