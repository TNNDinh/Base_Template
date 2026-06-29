using System;
using System.Collections.Generic;
using System.Reflection;
using Ezg.Core.Adapter;
using Ezg.Core.Extensions;
using Ezg.Feature.Shared;
using UnityEngine;
using Object = UnityEngine.Object;
using Ezg.Feature.Shared.Systems;

namespace Ezg.Feature.Shared.GameData
{
public partial class DataManager
{
    private static Dictionary<string, Object> cacheConfig = new();

    // ─────────────────────────────────────────────────────────────────────────

    private static readonly Dictionary<string, object> _cacheData = new();
    public static Dictionary<string, int> EnemiesId;

    public static Dictionary<int, Sprite> UserAvatars;

    public static Dictionary<int, Sprite> UserFrame;

    // CSV collection loader infrastructure
    public static T Get<T>() where T : Object
    {
        var key = typeof(T).Name;
        if (cacheConfig.TryGetValue(key, out var value))
            return value as T;
        var nameField = typeof(T).Name.ReplaceLast("Collection", "");
        var field = typeof(CsvAssetDir).GetField(nameField);
        var path = field.GetValue(null);
        var config = ResLoader.Load<T>(path.ToString());
        cacheConfig.Add(key, config);
        return config;
    }

    public static void Clear()
    {
        cacheConfig = new Dictionary<string, Object>();
    }

    public static void LoadAllData()
    {
        LoadGeneralAssets();
        LoadUserAvatar();
    }

    private static void LoadData()
    {
        var fields = typeof(CsvAssetDir).GetFields(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

        foreach (var field in fields)
        {
            var typeCollection = $"{field.Name}Collection";
            var type = Type.GetType(typeCollection);

            var loadMethodInfo = typeof(ResLoader).GetMethod("LoadPath");
            if (loadMethodInfo == null) continue;
            var genericLoadMethodInfo = loadMethodInfo.MakeGenericMethod(type);

            var loadedObject = genericLoadMethodInfo.Invoke(null, new[] { field.GetValue(null) });

            _cacheData.Add(field.Name, loadedObject);
        }
    }

    private static void LoadUserAvatar()
    {
        var assets = Resources.LoadAll(PathUtils.UserAvatar);
        var assets2 = Resources.LoadAll(PathUtils.UserFrame);
        UserAvatars ??= new Dictionary<int, Sprite>();
        UserAvatars.Clear();
        UserFrame ??= new Dictionary<int, Sprite>();
        UserFrame.Clear();

        var index = 0;
        foreach (var asset in assets)
            if (asset is Texture2D texture)
            {
                var sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
                UserAvatars.Add(index, sprite);
                index++;
            }

        index = 0;
        foreach (var asset in assets2)
            if (asset is Texture2D texture)
            {
                var sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
                UserFrame.Add(index, sprite);
                index++;
            }
    }

    #region General assets

    private static GeneralAssets _generalAssets;

    public static GeneralAssets GeneralAssets
    {
        get
        {
            if (_generalAssets == null) LoadGeneralAssets();

            return _generalAssets;
        }
        set => _generalAssets = value;
    }

    private static void LoadGeneralAssets()
    {
        GeneralAssets = Resources.Load<GeneralAssets>("GeneralAssets");
    }

    #endregion

    #region Sound config

    private static SoundConfig _soundConfig;

    public static SoundConfig SoundConfig
    {
        get
        {
            if (_soundConfig == null) LoadSoundConfig();

            return _soundConfig;
        }
        set => _soundConfig = value;
    }

    private static void LoadSoundConfig()
    {
        SoundConfig = Resources.Load<SoundConfig>("SoundConfig");
    }

    #endregion

    // #region Order System Config
    //
    //
    // private static OrderSystemCollection _orderSystemCollection;
    //
    // public static OrderSystemCollection OrderSystem
    // {
    //     get
    //     {
    //         if (_orderSystemCollection == null) LoadOrderSystemCollection();
    //
    //         return _orderSystemCollection;
    //     }
    //     set => _orderSystemCollection = value;
    // }
    //
    // private static void LoadOrderSystemCollection()
    // {
    //     OrderSystem = Resources.Load<OrderSystemCollection>(CsvAssetDir.OrderSystem);
    // }
    //
    //
    // #endregion
    //
    // #region Order Detail Config
    // private static OrderDetailCollection _orderDetailCollection;
    //
    // public static OrderDetailCollection OrderDetail
    // {
    //     get
    //     {
    //         if (_orderDetailCollection == null) LoadOrderDetailCollection();
    //
    //         return _orderDetailCollection;
    //     }
    //     set => _orderDetailCollection = value;
    // }
    // private static void LoadOrderDetailCollection()
    // {
    //     OrderDetail = Resources.Load<OrderDetailCollection>(CsvAssetDir.OrderDetail);
    // }
    // #endregion

    // #region Cooking recipe Config
    //
    //
    // private static CookingRecipesCollection _cookingRecipes;
    //
    // public static CookingRecipesCollection CookingRecipes
    // {
    //     get
    //     {
    //         if (_cookingRecipes == null) LoadCookingRecipesCollection();
    //
    //         return _cookingRecipes;
    //     }
    //     set => _cookingRecipes = value;
    // }
    //
    // private static void LoadCookingRecipesCollection()
    // {
    //     CookingRecipes = Resources.Load<CookingRecipesCollection>("Collection/GamePlay/CookingRecipes");
    // }
    //
    //
    //
    // #endregion
}
}