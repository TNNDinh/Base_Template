using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Collection thời tiết (biome) của map arena. Theo convention ItemMergeCollection: giữ mảng
    ///     <see cref="dataGroup" /> (serialize/sửa Inspector) + cache lookup dựng lại ở <see cref="Convert" />.
    ///     Tách RIÊNG file (primary class) để MonoScript ổn định — asset bind chắc sau domain reload.
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Arena/Weather Collection", fileName = "WeatherCollection")]
    public class WeatherCollection : ScriptableObject
    {
        public WeatherModel[] dataGroup;

        private Dictionary<string, WeatherModel> _byId;

        private void OnEnable() => Convert();
        private void OnValidate() => Convert();

        public void Convert()
        {
            _byId = new Dictionary<string, WeatherModel>();
            if (dataGroup == null) return;
            foreach (var w in dataGroup) _byId[w.id] = w;
        }

        public WeatherModel GetById(string id)
        {
            if (_byId == null) Convert();
            return id != null && _byId.TryGetValue(id, out var v) ? v : default;
        }

        public bool Contains(string id)
        {
            if (_byId == null) Convert();
            return id != null && _byId.ContainsKey(id);
        }

        public IReadOnlyList<WeatherModel> All => dataGroup ?? Array.Empty<WeatherModel>();
    }
}
