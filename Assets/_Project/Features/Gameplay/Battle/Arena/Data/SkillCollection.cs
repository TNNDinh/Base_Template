using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Collection skill ultimate (khoá = skill id, vd "sk_nuke_1"). Data: Skills.csv. Nâng cấp skill = tra
    ///     <see cref="SkillModel.nextSkillId" /> để đổi sang skill mạnh hơn. File RIÊNG (primary class) để asset
    ///     bind ổn định sau domain reload.
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Arena/Skill Collection", fileName = "SkillCollection")]
    public class SkillCollection : ScriptableObject
    {
        public ArenaSkillModel[] dataGroup;

        private Dictionary<string, ArenaSkillModel> _byId;

        private void OnEnable() => Convert();
        private void OnValidate() => Convert();

        public void Convert()
        {
            _byId = new Dictionary<string, ArenaSkillModel>();
            if (dataGroup == null) return;
            foreach (var s in dataGroup) _byId[s.id] = s;
        }

        public ArenaSkillModel GetById(string id)
        {
            if (_byId == null) Convert();
            return id != null && _byId.TryGetValue(id, out var v) ? v : default;
        }

        public bool Contains(string id)
        {
            if (_byId == null) Convert();
            return id != null && _byId.ContainsKey(id);
        }
    }
}
