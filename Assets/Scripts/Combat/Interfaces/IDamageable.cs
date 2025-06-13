using UnityEngine;

namespace Combat.Interfaces // 建议添加命名空间以更好地组织代码
{
    /// <summary>
    /// 代表任何可以受到伤害的实体（如敌人、可破坏物体等）。
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// 对实体施加伤害。
        /// </summary>
        /// <param name="damageInfo">包含伤害量、类型、来源等信息的结构体。</param>
        void TakeDamage(DamageInfo damageInfo);
    }
}