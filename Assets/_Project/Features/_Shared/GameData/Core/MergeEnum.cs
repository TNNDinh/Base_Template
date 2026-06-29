using System;

public class MergeEnum
{
    public enum ChefsType
    {
        Merge = 0,
        Tool = 1,
        GameBoost = 2,
        Recipes = 3,
        Event = 4
    }

    [Serializable]
    public enum ItemIdentify
    {
        Generators = 1,
        Tools = 2,
        Consumable = 3,
        Recipes = 4,
        Booster = 5,
        ChestAndBox = 6,
        ItemRaw = 7
    }

    [Serializable]
    public enum MergeItemTypes
    {
        None = 0,

        DrinkGenerator = 1001,
        FruitAndSugarGenerator = 1002,
        ProteinGenerator = 1003,
        VegetableGenerator = 1004,
        SeafoodGenerator = 1005,
        GrainGenerator = 1006,
        AlcoholGenerator = 1007,

        Juicer = 2001,
        ChefCounter = 2002,
        Grill = 2003,
        Pan = 2004,
        Oven = 2005,

        Gold = 3001,
        Energy = 3002,
        Diamond = 3003,

        JamAndYogurtMix = 4001,
        SmoothieAndJuice = 4002,
        CoffeeDrinks = 4003,
        MixedGrill = 4004,
        StirFriedDish = 4005,
        Salad = 4006,
        Desserts = 4007,
        BakeryProducts = 4008,
        FastFood = 4009,
        Dough = 4010,

        CardPlus = 5001,
        Sandglass = 5002,
        Scissors = 5003,
        StandardUnlimitedEnergy = 5004,
        UpgradedUnlimitedEnergy = 5005,
        DuplicateCamera = 5006,
        MagicWand = 5007,
        RocketCracker = 5008,
        AnimalTimer = 5009,
        WildCard = 5010,
        SpeedBoost = 5011,

        EnergyChest = 6001,
        ChefChest = 6002,
        EquipmentChest = 6003,
        AssistantsChest = 6004,
        DailyGift = 6005,
        LuckyHandbag = 6006,
        LuckyBox = 6007,
        Gift = 6008,
        EquipmentBox = 6009,
        CoinBox = 6010,
        ChoiceChest = 6011,
        FlushGift = 6012,
        TraineeBox = 6013,

        Coffee = 7001,
        SoftDrinks = 7002,
        DairyProducts = 7003,
        Glassware = 7004,
        Fruit = 7005,
        SugarAndCandy = 7006,
        CoconutWaterProducts = 7007,
        CoconutShellProducts = 7008,
        RedMeat = 7009,
        EggAndPoultry = 7010,
        Vegetables = 7011,
        LeafyVegetables = 7012,
        Seafood = 7013,
        Shellfish = 7014,
        Grain = 7015,
        Nut = 7016,
        PeanutProducts = 7017
    }

    [Serializable]
    public struct ItemKey : IEquatable<ItemKey>
    {
        public MergeItemTypes itemSaveType;
        public int idItem;

        public ItemKey(MergeItemTypes type, int id)
        {
            itemSaveType = type;
            idItem = id;
        }

        // BẮT BUỘC phải có 2 cái này
        public override bool Equals(object obj)
        {
            return obj is ItemKey other && itemSaveType == other.itemSaveType && idItem == other.idItem;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(itemSaveType, idItem);
        }

        // Để TryGetValue hoạt động tốt hơn
        public bool Equals(ItemKey other)
        {
            return itemSaveType == other.itemSaveType && idItem == other.idItem;
        }

        // Optional: hiển thị đẹp trong Inspector
        public override string ToString()
        {
            return $"({itemSaveType}, {idItem})";
        }

        // Format: full id, first 4 digits = type, e.g. "700101" → type 7001, id 700101
        public string ToKeyString()
        {
            return idItem.ToString();
        }

        public static (MergeItemTypes type, int id) FromKeyString(string key)
        {
            var type = (MergeItemTypes)int.Parse(key.Substring(0, 4));
            var id = int.Parse(key);
            return (type, id);
        }

        public static bool TryParseKeyString(string key, out (MergeItemTypes type, int id) result)
        {
            result = default;
            if (key == null || key.Length < 5) return false;
            if (!int.TryParse(key.Substring(0, 4), out var typeInt)) return false;
            if (!int.TryParse(key, out var id)) return false;
            result = ((MergeItemTypes)typeInt, id);
            return true;
        }
    }
}