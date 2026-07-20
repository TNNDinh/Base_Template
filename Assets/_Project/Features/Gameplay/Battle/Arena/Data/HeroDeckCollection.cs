using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Collection bộ bài mặc định theo hero (khoá = heroId). Tách RIÊNG file (primary class) để MonoScript
    ///     ổn định — asset bind chắc sau domain reload (tránh m_Script=0 → load null → hero không có bài).
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Arena/Hero Deck Collection", fileName = "HeroDeckCollection")]
    public class HeroDeckCollection : ScriptableObject
    {
        public HeroDeckModel[] dataGroup;

        private Dictionary<string, HeroDeckModel> _byId;

        private void OnEnable() => Convert();
        private void OnValidate() => Convert();

        public void Convert()
        {
            _byId = new Dictionary<string, HeroDeckModel>();
            if (dataGroup == null) return;
            foreach (var d in dataGroup) _byId[d.heroId] = d;
        }

        public bool Contains(string heroId)
        {
            if (_byId == null) Convert();
            return _byId.ContainsKey(heroId);
        }

        /// <summary>Danh sách id thẻ trong bộ bài mặc định của hero (rỗng nếu chưa cấu hình).</summary>
        public List<string> DeckOf(string heroId)
        {
            if (_byId == null) Convert();
            var list = new List<string>();
            if (!_byId.TryGetValue(heroId, out var d) || string.IsNullOrWhiteSpace(d.cards)) return list;
            var parts = d.cards.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                var id = parts[i].Trim();
                if (!string.IsNullOrEmpty(id)) list.Add(id);
            }

            return list;
        }
    }
}
