using System;
using System.Linq;
using Easygoing.Packages.Dictionary;
using Ezg.Feature.Shared;
using UnityEditor;
using UnityEngine;
using Ezg.Feature.Shared.GameData;

[Serializable]
public class ItemMergeCollection : ScriptableObject
{
    public ItemMergeModel[] dataGroup;
    public ItemMergeCache ItemMergeCache = new();

    private void OnValidate()
    {
        // Rebuild lookup cache immediately when the asset changes in the editor.
        // This also invalidates dependent recipe timing caches so UI reads stay fresh.
        Convert();
    }

    public ItemMergeModel GetById(string id)
    {
        if (ItemMergeCache.TryGetValue(id, out var item))
            return item;
        return default;
    }

    public ItemMergeModel[] GetAllByType(MergeEnum.MergeItemTypes type)
    {
        var prefix = ((int)type).ToString("D4");
        return dataGroup?.Where(x => x.id != null && x.id.StartsWith(prefix))
            .OrderBy(x => ParseNumericId(x.id))
            .ToArray() ?? Array.Empty<ItemMergeModel>();
    }

    public ItemSave GetItemHighestByType(MergeEnum.MergeItemTypes type)
    {
        var items = GetAllByType(type);
        if (items.Length == 0) return default;
        var highest = items.LastOrDefault(x => !x.canMerge);
        if (highest.id == null) highest = items[items.Length - 1];
        var key = MergeEnum.ItemKey.FromKeyString(highest.id);
        return new ItemSave { itemSaveType = key.type, idItem = key.id };
    }

    public int GetIndex(string id)
    {
        if (!MergeEnum.ItemKey.TryParseKeyString(id, out var key)) return -1;
        var items = GetAllByType(key.type);
        for (var i = 0; i < items.Length; i++)
            if (items[i].id == id)
                return i;
        return -1;
    }

    public bool IsMax(string id)
    {
        return !GetById(id).canMerge;
    }

    public string GetMergeTargetId(string id)
    {
        var item = GetById(id);
        if (!item.canMerge) return null;
        if (!MergeEnum.ItemKey.TryParseKeyString(id, out var key)) return null;
        return new MergeEnum.ItemKey(key.type, key.id + 1).ToKeyString();
    }

    public static int ParseNumericId(string id)
    {
        if (!MergeEnum.ItemKey.TryParseKeyString(id, out var key)) return 0;
        return key.id;
    }

    public void Convert()
    {
        ItemMergeCache.Clear();

        if (dataGroup == null)
            return;

        foreach (var item in dataGroup) ItemMergeCache[item.id] = item;

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }
}

[Serializable]
public class ItemMergeCache : SerializableDictionary<string, ItemMergeModel>
{
}