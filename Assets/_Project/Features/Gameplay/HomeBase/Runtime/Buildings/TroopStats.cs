namespace Ezg.Feature.HomeBase
{
    /// <summary>
    /// Chỉ số của một con lính ở một cấp, đọc ra từ bảng CSV của loại lính đó.
    /// Là struct chỉ đọc để cầm đi nơi khác không ai lỡ tay sửa được số gốc.
    /// </summary>
    public readonly struct TroopStats
    {
        #region Public - Properties

        public int Health { get; }

        public int Damage { get; }

        /// <summary>Số đòn mỗi giây.</summary>
        public float AttackSpeed { get; }

        /// <summary>Tốc độ chạy, mét mỗi giây.</summary>
        public float MoveSpeed { get; }

        /// <summary>Tầm đánh, tính bằng mét.</summary>
        public float AttackRange { get; }

        /// <summary>Sát thương mỗi giây, tiện để so hai cấp với nhau.</summary>
        public float DamagePerSecond => Damage * AttackSpeed;

        #endregion

        #region Initialize

        public TroopStats(int health, int damage, float attackSpeed, float moveSpeed, float attackRange)
        {
            Health = health;
            Damage = damage;
            AttackSpeed = attackSpeed;
            MoveSpeed = moveSpeed;
            AttackRange = attackRange;
        }

        #endregion

        #region Public

        public override string ToString()
        {
            return $"HP={Health} DMG={Damage} DPS={DamagePerSecond:F1} tốc={MoveSpeed} tầm={AttackRange}";
        }

        #endregion
    }
}
