using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Collection enemy spawn (kèm STAT enemy). Nhóm theo stageId, sort theo round rồi ring.
    ///     Tách RIÊNG file (primary class) để MonoScript ổn định — asset bind chắc sau domain reload
    ///     (tránh m_Script = 0 khiến asset load null → arena trống, không spawn enemy).
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Arena/Spawn Collection", fileName = "ArenaSpawnCollection")]
    public class ArenaSpawnCollection : ScriptableObject
    {
        public ArenaSpawnModel[] dataGroup;

        private Dictionary<string, List<ArenaSpawnModel>> _byStage;

        private void OnEnable() => Convert();
        private void OnValidate() => Convert();

        public void Convert()
        {
            _byStage = new Dictionary<string, List<ArenaSpawnModel>>();
            if (dataGroup == null) return;
            foreach (var s in dataGroup)
            {
                if (!_byStage.TryGetValue(s.stageId, out var list))
                {
                    list = new List<ArenaSpawnModel>();
                    _byStage[s.stageId] = list;
                }

                list.Add(s);
            }

            foreach (var kv in _byStage)
                kv.Value.Sort((a, b) => a.round != b.round ? a.round.CompareTo(b.round) : a.ring.CompareTo(b.ring));
        }

        public IReadOnlyList<ArenaSpawnModel> GetByStage(string stageId)
        {
            if (_byStage == null) Convert();
            return _byStage.TryGetValue(stageId, out var v) ? v : (IReadOnlyList<ArenaSpawnModel>)Array.Empty<ArenaSpawnModel>();
        }
    }
}
