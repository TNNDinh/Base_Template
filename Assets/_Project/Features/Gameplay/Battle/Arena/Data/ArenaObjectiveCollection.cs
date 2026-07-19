using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Collection nhiệm vụ sao + bonus. Nhóm theo stageId.
    ///     Tách RIÊNG file (primary class) để MonoScript ổn định — asset bind chắc sau domain reload.
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Arena/Objective Collection", fileName = "ArenaObjectiveCollection")]
    public class ArenaObjectiveCollection : ScriptableObject
    {
        public ArenaObjectiveModel[] dataGroup;

        private Dictionary<string, List<ArenaObjectiveModel>> _byStage;

        private void OnEnable() => Convert();
        private void OnValidate() => Convert();

        public void Convert()
        {
            _byStage = new Dictionary<string, List<ArenaObjectiveModel>>();
            if (dataGroup == null) return;
            foreach (var o in dataGroup)
            {
                if (!_byStage.TryGetValue(o.stageId, out var list))
                {
                    list = new List<ArenaObjectiveModel>();
                    _byStage[o.stageId] = list;
                }

                list.Add(o);
            }
        }

        public IReadOnlyList<ArenaObjectiveModel> GetByStage(string stageId)
        {
            if (_byStage == null) Convert();
            return _byStage.TryGetValue(stageId, out var v) ? v : (IReadOnlyList<ArenaObjectiveModel>)Array.Empty<ArenaObjectiveModel>();
        }
    }
}
