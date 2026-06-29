using System;
using System.Collections.Generic;

[Serializable]
public struct ItemScaleStageModel
{
    public int id;
    public int[] rangeStageId;
    public List<ItemItemScaleStageDetailModel> heroOrbBonus;
    public List<ItemItemScaleStageDetailModel> scrollBonus;
}

[Serializable]
public class ItemItemScaleStageDetailModel
{
    public int index;
    public int bonusValue;
}