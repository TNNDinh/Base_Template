using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Bảng tra prefab theo id cho arena (enemy 11xxx / hero 44xxx / weapon 41xxx) — vì prefab ST09 nằm
    ///     ngoài Resources nên map qua tham chiếu trực tiếp (gán trong Inspector). Dùng bởi
    ///     <see cref="ArenaSceneController" /> để spawn model đúng id.
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Arena/Prefab Registry", fileName = "ArenaPrefabRegistry")]
    public class ArenaPrefabRegistry : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string id;
            public GameObject prefab;
        }

        public Entry[] heroes;
        public Entry[] enemies;
        public Entry[] weapons;

        private Dictionary<string, GameObject> _heroMap;
        private Dictionary<string, GameObject> _enemyMap;
        private Dictionary<string, GameObject> _weaponMap;

        private void OnEnable() => Rebuild();
        private void OnValidate() => Rebuild();

        public void Rebuild()
        {
            _heroMap = Build(heroes);
            _enemyMap = Build(enemies);
            _weaponMap = Build(weapons);
        }

        public GameObject Hero(string id) => Lookup(ref _heroMap, heroes, id);
        public GameObject Enemy(string id) => Lookup(ref _enemyMap, enemies, id);
        public GameObject Weapon(string id) => Lookup(ref _weaponMap, weapons, id);

        private static Dictionary<string, GameObject> Build(Entry[] entries)
        {
            var map = new Dictionary<string, GameObject>();
            if (entries == null) return map;
            for (int i = 0; i < entries.Length; i++)
                if (!string.IsNullOrEmpty(entries[i].id))
                    map[entries[i].id] = entries[i].prefab;
            return map;
        }

        private static GameObject Lookup(ref Dictionary<string, GameObject> map, Entry[] entries, string id)
        {
            if (map == null) map = Build(entries);
            return map.TryGetValue(id, out var v) ? v : null;
        }
    }
}
