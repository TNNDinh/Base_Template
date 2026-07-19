using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Collection stage arena. Theo convention ItemMergeCollection: giữ mảng <see cref="dataGroup" />
    ///     (serialize + sửa trong Inspector) và cache lookup dựng lại ở <see cref="Convert" />.
    ///     Tách RIÊNG file (primary class) để MonoScript ổn định — asset bind chắc sau domain reload.
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Arena/Stage Collection", fileName = "ArenaStageCollection")]
    public class ArenaStageCollection : ScriptableObject
    {
        public ArenaStageModel[] dataGroup;

        private Dictionary<string, ArenaStageModel> _byId;

        private void OnEnable() => Convert();
        private void OnValidate() => Convert();

        public void Convert()
        {
            _byId = new Dictionary<string, ArenaStageModel>();
            if (dataGroup == null) return;
            foreach (var s in dataGroup) _byId[s.id] = s;
        }

        public ArenaStageModel GetById(string id)
        {
            if (_byId == null) Convert();
            return _byId.TryGetValue(id, out var v) ? v : default;
        }

        public IReadOnlyList<ArenaStageModel> All => dataGroup ?? Array.Empty<ArenaStageModel>();
    }
}
