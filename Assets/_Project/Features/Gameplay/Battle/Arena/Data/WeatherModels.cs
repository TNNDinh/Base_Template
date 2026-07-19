using System;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>Loại thời tiết (biome) của 1 map arena. Mỗi map có 1 thời tiết → aura hiệu ứng mỗi round.</summary>
    public enum WeatherType
    {
        None = 0,
        Forest = 1,  // rừng (mưa)
        Desert = 2,  // sa mạc (nóng)
        Volcano = 3, // núi lửa (mắc ma)
        Snow = 4     // tuyết (lạnh)
    }

    /// <summary>
    ///     Config 1 loại thời tiết (CSV <c>Weather.csv</c>). TỰ ĐỊNH NGHĨA hiệu ứng bằng data (giống skill):
    ///     DoT/regen mỗi round cho hero &amp; enemy + hệ số nhân damage 2 phía + màu phủ môi trường.
    ///     Stage trỏ tới thời tiết bằng <c>weather</c> id (xem <see cref="ArenaStageModel.weather" />).
    ///     Tất cả % tính theo MÁU TỐI ĐA của unit; hệ số damage &lt;=0 coi như 1 (không đổi).
    /// </summary>
    [Serializable]
    public struct WeatherModel
    {
        public string id;
        public string name;
        public int type;           // WeatherType

        public float heroDotPct;   // % maxHp hero MẤT mỗi player round (nắng/lạnh/dung nham)
        public float heroRegenPct; // % maxHp hero HỒI mỗi player round (mưa)
        public float enemyDotPct;  // % maxHp enemy MẤT mỗi enemy round (bỏng/tê cóng)
        public float enemyRegenPct;// % maxHp enemy HỒI mỗi enemy round (rừng tốt tươi)

        public float heroDmgMul;   // nhân damage đòn HERO (vd 1.2 = +20%, 0.9 = -10%)
        public float enemyDmgMul;  // nhân damage đòn ENEMY đánh hero

        public float activeChance; // xác suất MỖI ROUND biome "phát tác" (vd forest: mưa). 0..1. <=0 = luôn bật.
                                   // Round bật → áp DoT/regen/hệ số damage + phủ màu; round tắt → tạnh (không hiệu ứng).

        public string tint;        // màu phủ môi trường "#RRGGBBAA" (view). Rỗng = không phủ.
    }
}
