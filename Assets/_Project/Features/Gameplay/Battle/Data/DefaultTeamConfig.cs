using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     ScriptableObject cấu hình đội hình MẶC ĐỊNH: tướng main + danh sách tướng theo slot.
    ///     Đặt asset trong 1 thư mục <c>Resources/</c> tên <c>BattleDefaultTeam</c> để runtime load.
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Default Team Config", fileName = "BattleDefaultTeam")]
    public class DefaultTeamConfig : ScriptableObject
    {
        [global::System.Serializable]
        public struct Entry
        {
            public string heroId;
            public int star;
            public int level;
        }

        [Tooltip("Tướng MAIN — luôn đứng slot đầu (front). Đánh thường gây damage 1 mục tiêu.")]
        public string mainHeroId = "hero_1001";

        [Tooltip("Đội hình theo slot (slot 0 = main → phía sau). Nên để main ở đầu list.")]
        public List<Entry> team = new List<Entry>();
    }
}
