using System;
using System.Collections.Generic;
using System.Linq;
using Ezg.Package.CsvReader;
using UnityEngine;

[Serializable]
public class ItemScaleStageCollection : ScriptableObject, ICsvCustomData
{
    public ItemScaleStageModel[] dataGroups;

    public void ImportData(string dataCsv)
    {
        var datas = CSVReaderManager.SplitCsvGrid(dataCsv);

        var columnName = CSVReaderManager.GetColumnNameIndex(datas);

        var countHeroOrb = columnName.Keys.Count(x => x.StartsWith("hero_orb_bonus"));
        var countScroll = columnName.Keys.Count(x => x.StartsWith("scroll_bonus"));

        var rowData = CSVReaderManager.Read(dataCsv);

        var result = new List<ItemScaleStageModel>();

        var count = 0;
        foreach (var data in rowData)
        {
            var item = new ItemScaleStageModel
            {
                id = count
            };

            var splitStage = data["stage_id"].Split('~');
            item.rangeStageId = new[] { int.Parse(splitStage[0]), int.Parse(splitStage[1]) };
            item.rangeStageId = new[] { int.Parse(splitStage[0]), int.Parse(splitStage[1]) };

            item.heroOrbBonus = new List<ItemItemScaleStageDetailModel>();
            var keyHeroOrb = "hero_orb_bonus_{0}";
            for (var i = 0; i < countHeroOrb; i++)
            {
                var fullKey = string.Format(keyHeroOrb, i);
                if (!data.ContainsKey(fullKey)) break;
                item.heroOrbBonus.Add(new ItemItemScaleStageDetailModel
                {
                    index = i,
                    bonusValue = int.Parse(data[fullKey])
                });
            }

            item.scrollBonus = new List<ItemItemScaleStageDetailModel>();
            var keyScroll = "scroll_bonus_{0}";
            for (var i = 0; i < countScroll; i++)
            {
                var fullKey = string.Format(keyScroll, i);
                if (!data.ContainsKey(fullKey)) break;
                item.scrollBonus.Add(new ItemItemScaleStageDetailModel
                {
                    index = i,
                    bonusValue = int.Parse(data[fullKey])
                });
            }

            count++;
            result.Add(item);
        }

        dataGroups = result.ToArray();
    }

    public ItemScaleStageModel GetByStage(int stageId)
    {
        return dataGroups.FirstOrDefault(x => x.rangeStageId[0] <= stageId && stageId <= x.rangeStageId[1]);
    }
}