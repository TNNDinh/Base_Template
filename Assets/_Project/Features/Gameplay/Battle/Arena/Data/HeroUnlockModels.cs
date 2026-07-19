using System;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>Cách mở khóa 1 hero.</summary>
    public enum HeroUnlockMethod
    {
        Default,  // mở sẵn từ đầu
        Level,    // đạt player level ≥ reqLevel
        Currency, // mua bằng gold (cost)
        IAP,      // mua thật (iapProduct)
        Stage,    // clear stage reqStage
        Gacha     // chỉ quay gacha mới ra (theo gachaWeight)
    }

    /// <summary>
    ///     1 dòng cấu hình MỞ KHÓA hero (data-driven). Chỉ cần 1 list trong <c>HeroUnlocks.csv</c> — mỗi hero
    ///     1 cách mở. Trạng thái đã-mở lưu ở roster <see cref="PlayerBattleHero" />.
    /// </summary>
    [Serializable]
    public struct HeroUnlockModel
    {
        public string heroId;
        public int method;       // HeroUnlockMethod
        public int reqLevel;     // Level: player level cần đạt
        public int cost;         // Currency/Gacha: giá gold
        public string iapProduct;// IAP: product id
        public string reqStage;  // Stage: stage id cần clear
        public int gachaWeight;  // Gacha: trọng số quay (0 = không nằm trong pool)
    }
}
