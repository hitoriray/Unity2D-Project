using UnityEngine;

/// <summary>
/// 伤害信息数据结构 - 封装所有伤害相关的数据
/// 使用struct提高性能，避免GC分配
/// 位置: Combat/Data/ - 核心数据结构
/// </summary>
[System.Serializable]
public struct DamageInfo
{
    #region 基础伤害信息
    [Header("基础伤害")]
    [Tooltip("基础伤害值")]
    public float baseDamage;
    
    [Tooltip("伤害类型")]
    public DamageType damageType;
    
    [Tooltip("是否为暴击")]
    public bool isCritical;
    #endregion
    
    #region 来源信息
    [Header("伤害来源")]
    [Tooltip("伤害来源对象")]
    public GameObject source;
    
    [Tooltip("击中点位置")]
    public Vector2 hitPoint;
    
    [Tooltip("击中方向")]
    public Vector2 hitDirection;
    #endregion
    
    #region 特殊效果
    [Header("特殊效果")]
    [Tooltip("击退力度")]
    public float knockbackForce;
    
    [Tooltip("穿透次数")]
    public int pierceCount;
    
    [Tooltip("状态效果持续时间")]
    public float statusEffectDuration;
    
    [Tooltip("造成伤害的武器资产引用")]
    public Weapon sourceWeaponAsset; // 新增字段，用于传递源武器信息
    #endregion
    
    /// <summary>
    /// 构造函数 - 创建基础伤害信息
    /// </summary>
    /// <param name="damage">基础伤害值</param>
    /// <param name="type">伤害类型</param>
    /// <param name="sourceObject">伤害来源</param>
    /// <param name="hitPos">击中位置</param>
    public DamageInfo(float damage, DamageType type, GameObject sourceObject, Vector2 hitPos)
    {
        baseDamage = damage;
        damageType = type;
        isCritical = false;
        source = sourceObject;
        hitPoint = hitPos;
        hitDirection = Vector2.zero;
        knockbackForce = 0f;
        pierceCount = 0;
        statusEffectDuration = 0f;
        sourceWeaponAsset = null; // 初始化新增字段
    }
    
    /// <summary>
    /// 构造函数 - 创建完整伤害信息
    /// </summary>
    public DamageInfo(float damage, DamageType type, bool critical, GameObject sourceObject,
                     Vector2 hitPos, Vector2 hitDir, float knockback = 0f, int pierce = 0, Weapon weaponAsset = null) // 添加 weaponAsset 参数
    {
        baseDamage = damage;
        damageType = type;
        isCritical = critical;
        source = sourceObject;
        hitPoint = hitPos;
        hitDirection = hitDir.normalized;
        knockbackForce = knockback;
        pierceCount = pierce;
        statusEffectDuration = 0f;
        sourceWeaponAsset = weaponAsset; // 初始化新增字段
    }
    
    /// <summary>
    /// 设置暴击
    /// </summary>
    public readonly DamageInfo WithCritical(bool critical = true)
    {
        DamageInfo newInfo = this;
        newInfo.isCritical = critical;
        return newInfo;
    }
    
    /// <summary>
    /// 设置击退力度
    /// </summary>
    public readonly DamageInfo WithKnockback(float force)
    {
        DamageInfo newInfo = this;
        newInfo.knockbackForce = force;
        return newInfo;
    }
    
    /// <summary>
    /// 设置穿透次数
    /// </summary>
    public readonly DamageInfo WithPierce(int count)
    {
        DamageInfo newInfo = this;
        newInfo.pierceCount = count;
        return newInfo;
    }
    
    /// <summary>
    /// 获取最终伤害值（用于显示）
    /// </summary>
    public readonly float GetDisplayDamage()
    {
        return Mathf.Max(1f, baseDamage);
    }
    
    /// <summary>
    /// 检查是否有效的伤害信息
    /// </summary>
    public readonly bool IsValid()
    {
        return baseDamage > 0f && source != null;
    }
    
    /// <summary>
    /// 调试信息
    /// </summary>
    public override readonly string ToString()
    {
        return $"Damage: {baseDamage:F1} ({damageType}){(isCritical ? " [CRIT]" : "")} from {(source ? source.name : "Unknown")}";
    }
}
