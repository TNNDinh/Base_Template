using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Catalog thẻ bài (khoá = id). Tách RIÊNG file (primary class) để MonoScript ổn định — asset bind
    ///     chắc sau domain reload (tránh m_Script=0 → load null → mất data thẻ). Xem
    ///     <see cref="WeaponCollection" /> cho cùng convention.
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Arena/Card Collection", fileName = "CardCollection")]
    public class CardCollection : ScriptableObject
    {
        public CardModel[] dataGroup;

        private Dictionary<string, CardModel> _byId;

        private void OnEnable() => Convert();
        private void OnValidate() => Convert();

        public void Convert()
        {
            _byId = new Dictionary<string, CardModel>();
            if (dataGroup == null) return;
            foreach (var c in dataGroup) _byId[c.id] = c;
        }

        /// <summary>Toàn bộ thẻ trong catalog.</summary>
        public CardModel[] All => dataGroup ?? new CardModel[0];

        public bool Contains(string id)
        {
            if (_byId == null) Convert();
            return _byId.ContainsKey(id);
        }

        public CardModel GetById(string id)
        {
            if (_byId == null) Convert();
            return _byId.TryGetValue(id, out var v) ? v : default;
        }

        /// <summary>Chuỗi bước đẩy lùi của thẻ (Damage/Trap) — dùng chung <see cref="CellSet" /> với vũ khí.</summary>
        public List<Vector2Int> KnockbackSteps(string id) => CellSet.Parse(GetById(id).knockback);

        /// <summary>Các thẻ dùng được cho <paramref name="heroClass" /> (khớp class HOẶC "neutral").</summary>
        public List<CardModel> GetByClass(string heroClass)
        {
            var list = new List<CardModel>();
            if (dataGroup == null) return list;
            foreach (var c in dataGroup)
                if (c.heroClass == heroClass || c.heroClass == "neutral")
                    list.Add(c);
            return list;
        }
    }
}
