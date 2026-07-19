using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Collection reward resource. Nhóm theo stageId.
    ///     Tách RIÊNG file (primary class) để MonoScript ổn định — asset bind chắc sau domain reload.
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Arena/Reward Collection", fileName = "ArenaRewardCollection")]
    public class ArenaRewardCollection : ScriptableObject
    {
        public ArenaRewardModel[] dataGroup;

        private Dictionary<string, List<ArenaRewardModel>> _byStage;

        private void OnEnable() => Convert();
        private void OnValidate() => Convert();

        public void Convert()
        {
            _byStage = new Dictionary<string, List<ArenaRewardModel>>();
            if (dataGroup == null) return;
            foreach (var r in dataGroup)
            {
                if (!_byStage.TryGetValue(r.stageId, out var list))
                {
                    list = new List<ArenaRewardModel>();
                    _byStage[r.stageId] = list;
                }

                list.Add(r);
            }
        }

        public IReadOnlyList<ArenaRewardModel> GetByStage(string stageId)
        {
            if (_byStage == null) Convert();
            return _byStage.TryGetValue(stageId, out var v) ? v : (IReadOnlyList<ArenaRewardModel>)Array.Empty<ArenaRewardModel>();
        }
    }
}
