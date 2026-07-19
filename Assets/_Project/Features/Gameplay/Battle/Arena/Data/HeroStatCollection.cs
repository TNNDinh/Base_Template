using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Collection stat HERO (khoá = id prefab, vd "44000"). Data: HeroStats.csv.
    ///     Tách RIÊNG file (primary class) để MonoScript ổn định — asset bind chắc chắn sau domain reload.
    /// </summary>
    [CreateAssetMenu(menuName = "Battle/Arena/Hero Stat Collection", fileName = "HeroStatCollection")]
    public class HeroStatCollection : UnitStatCollection { }
}
