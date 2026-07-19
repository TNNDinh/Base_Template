using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Thời tiết ĐANG áp cho trận (wrap 1 <see cref="WeatherModel" />). THUẦN LOGIC + null-safe:
    ///     controller hỏi hệ số/độ % rồi tự áp lên hero/enemy + cập nhật view. <see cref="Active" /> = false
    ///     khi map không có thời tiết (mọi hệ số về trung tính: mul=1, dot/regen=0).
    /// </summary>
    public class ArenaWeather
    {
        public WeatherModel Model { get; }
        public bool Active { get; }

        public ArenaWeather(WeatherModel model)
        {
            Model = model;
            Active = !string.IsNullOrEmpty(model.id);
        }

        public WeatherType Type => Active ? (WeatherType)Model.type : WeatherType.None;
        public string Name => Active ? Model.name : null;

        // ----- Hệ số nhân damage (mul <=0 coi như 1) -----
        public float HeroDamageMul => Active && Model.heroDmgMul > 0f ? Model.heroDmgMul : 1f;
        public float EnemyDamageMul => Active && Model.enemyDmgMul > 0f ? Model.enemyDmgMul : 1f;

        // ----- % máu tối đa mỗi round (âm bị kẹp về 0) -----
        public float HeroDotPct => Active ? Mathf.Max(0f, Model.heroDotPct) : 0f;
        public float HeroRegenPct => Active ? Mathf.Max(0f, Model.heroRegenPct) : 0f;
        public float EnemyDotPct => Active ? Mathf.Max(0f, Model.enemyDotPct) : 0f;
        public float EnemyRegenPct => Active ? Mathf.Max(0f, Model.enemyRegenPct) : 0f;

        /// <summary>Nhân damage đòn HERO theo thời tiết.</summary>
        public float ScaleHeroDamage(float dmg) => dmg * HeroDamageMul;

        /// <summary>Nhân damage đòn ENEMY (đánh hero) theo thời tiết.</summary>
        public float ScaleEnemyDamage(float dmg) => dmg * EnemyDamageMul;

        /// <summary>Màu phủ môi trường (view). Trả về false nếu không có tint hợp lệ.</summary>
        public bool TryGetTint(out Color color)
        {
            color = default;
            if (!Active || string.IsNullOrEmpty(Model.tint)) return false;
            return ColorUtility.TryParseHtmlString(Model.tint, out color);
        }
    }
}
