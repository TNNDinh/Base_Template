using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Collection pattern di chuyển của enemy. Enemy lấy pattern theo <c>id</c> rồi
    ///     <see cref="GetBranches" /> để ra chuỗi bước (x,y) mà di chuyển.
    ///     Tách RIÊNG file (primary class) để MonoScript ổn định — asset bind chắc sau domain reload
    ///     (tránh m_Script=0 → load null → enemy KHÔNG có pattern → đứng im).
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Arena/Enemy Move Pattern Collection", fileName = "EnemyMovePatternCollection")]
    public class EnemyMovePatternCollection : ScriptableObject
    {
        public EnemyMovePatternModel[] dataGroup;

        private Dictionary<string, EnemyMovePatternModel> _byId;

        private void OnEnable() => Convert();
        private void OnValidate() => Convert();

        public void Convert()
        {
            _byId = new Dictionary<string, EnemyMovePatternModel>();
            if (dataGroup == null) return;
            foreach (var p in dataGroup) _byId[p.id] = p;
        }

        public EnemyMovePatternModel GetById(string id)
        {
            if (_byId == null) Convert();
            return _byId.TryGetValue(id, out var v) ? v : default;
        }

        public bool Contains(string id)
        {
            if (_byId == null) Convert();
            return _byId.ContainsKey(id);
        }

        /// <summary>Các NHÁNH bước (x,y) đã parse của 1 pattern (rỗng nếu không có id).</summary>
        public List<List<Vector2Int>> GetBranches(string id) => MovePattern.Parse(GetById(id).steps);
    }
}
