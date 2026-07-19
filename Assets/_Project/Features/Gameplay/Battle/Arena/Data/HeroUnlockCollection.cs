using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Collection cấu hình mở khóa hero (khoá = heroId). Data: HeroUnlocks.csv. File RIÊNG (primary class)
    ///     để asset bind ổn định sau domain reload.
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Arena/Hero Unlock Collection", fileName = "HeroUnlockCollection")]
    public class HeroUnlockCollection : ScriptableObject
    {
        public HeroUnlockModel[] dataGroup;

        private Dictionary<string, HeroUnlockModel> _byId;

        private void OnEnable() => Convert();
        private void OnValidate() => Convert();

        public void Convert()
        {
            _byId = new Dictionary<string, HeroUnlockModel>();
            if (dataGroup == null) return;
            foreach (var u in dataGroup) _byId[u.heroId] = u;
        }

        public HeroUnlockModel GetById(string heroId)
        {
            if (_byId == null) Convert();
            return heroId != null && _byId.TryGetValue(heroId, out var v) ? v : default;
        }

        public IReadOnlyList<HeroUnlockModel> All => dataGroup ?? new HeroUnlockModel[0];
    }
}
