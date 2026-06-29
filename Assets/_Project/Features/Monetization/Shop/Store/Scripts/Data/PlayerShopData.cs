using System;
using System.Collections.Generic;
using Ezg.Core.Utils;
using Ezg.Package.Factory;
using Ezg.Feature.Shared.Systems;

[Serializable]
public class PlayerShopData : DataBase
{
    public EnumBase.MoneyTypes currentScrollReshuffle;

    public long lastTimeCheckinDaily;

    public long lastTimeRefreshJewelPack;
    public List<string> pack2SpawnToDay = new();

    public int IAPCount;
    public int IAACount;

    public Dictionary<int, int> AdventurePassPurchaseHistory = new();
    public decimal IAPRevenue;
    public Dictionary<string, int> packDailyDeals2History = new();
    public Dictionary<string, int> packDailyDeals2RefreshCount = new();

    public Dictionary<string, Resource> packDailyDeals2Resource = new();

    public Dictionary<string, int> packDailyDealsHistory = new();

    public Dictionary<string, int> packDailyDealsRefreshCount = new();

    public Dictionary<string, Resource> packDailyDealsResource = new();

    public Dictionary<string, long> packLimitPurchaseDict = new();

    public Dictionary<string, int> packPurchaseDict = new();

    public Dictionary<int, long> starterPackLastTimeShow = new();
}