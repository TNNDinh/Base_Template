using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Collection stat ENEMY (khoá = id prefab, vd "11001"). Data: EnemyStats.csv. Spawn link bằng enemyId.
    ///     Tách RIÊNG file (primary class) để MonoScript ổn định — asset bind chắc sau domain reload.
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Arena/Enemy Stat Collection", fileName = "EnemyStatCollection")]
    public class EnemyStatCollection : UnitStatCollection { }
}
