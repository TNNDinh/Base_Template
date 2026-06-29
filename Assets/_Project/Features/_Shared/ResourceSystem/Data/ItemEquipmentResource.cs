using Ezg.Core.Utils;
using Ezg.Feature.Shared.Systems;

public class ItemEquipmentResource : ItemResource
{
    public EnumBase.EquipmentTypes equipmentType; //=> DataManager.ItemEquipment.GetTypeById(resId);

    public int Set => 0; //DataManager.ItemEquipment.GetSetById(resId);

    public override Resource Clone()
    {
        return (ItemEquipmentResource)MemberwiseClone();
    }
}