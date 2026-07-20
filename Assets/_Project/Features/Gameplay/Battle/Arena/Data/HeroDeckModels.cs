using System;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Bộ bài MẶC ĐỊNH của 1 hero (CSV <c>HeroDecks.csv</c>). Đây là config gốc; khi có deck-builder
    ///     người chơi có thể tuỳ biến và lưu đè (qua <c>ArenaUpgradeService</c>) — mặc định này là điểm bắt đầu.
    ///     <para><b>cards</b>: danh sách id thẻ ngăn bằng ';' (cho phép lặp id = nhiều bản cùng lá). Vd
    ///     "card_bash;card_bash;card_stomp;card_guard".</para>
    /// </summary>
    [Serializable]
    public struct HeroDeckModel
    {
        public string heroId; // id prefab hero (vd "44001")
        public string cards;  // id thẻ ngăn bằng ';' (được lặp)
    }
}
