using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Base cho collection stat unit (hero/enemy): mảng <see cref="dataGroup" /> <see cref="UnitStatModel" />,
    ///     tra theo id, tính stat theo level. Hero &amp; enemy = 2 collection riêng (2 CSV riêng) kế thừa class này.
    ///     Abstract → không có asset trực tiếp; subclass phải nằm ở file riêng (MonoScript ổn định).
    /// </summary>
    public abstract class UnitStatCollection : ScriptableObject
    {
        public UnitStatModel[] dataGroup;

        private Dictionary<string, UnitStatModel> _byId;

        private void OnEnable() => Convert();
        private void OnValidate() => Convert();

        public void Convert()
        {
            _byId = new Dictionary<string, UnitStatModel>();
            if (dataGroup == null) return;
            foreach (var u in dataGroup) _byId[u.id] = u;
        }

        public UnitStatModel GetById(string id)
        {
            if (_byId == null) Convert();
            return _byId.TryGetValue(id, out var v) ? v : default;
        }

        public bool Contains(string id)
        {
            if (_byId == null) Convert();
            return _byId.ContainsKey(id);
        }

        /// <summary>Stat của unit <paramref name="id" /> tại <paramref name="level" />.</summary>
        public UnitStats StatsAt(string id, int level) => UnitStats.Of(GetById(id), level);
    }
}
