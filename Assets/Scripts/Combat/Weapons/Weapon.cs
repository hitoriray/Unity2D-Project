using UnityEngine;

/// <summary>
/// 武器数据资产 - 使用ScriptableObject模式存储武器配置
/// 遵循项目中Tool.cs的设计模式，支持在Inspector中创建和配置武器数据
/// 位置: Combat/Weapons/ - 武器系统
/// </summary>
[CreateAssetMenu(fileName = "newWeapon", menuName = "Combat/Weapon", order = 1)]
public class Weapon : ScriptableObject
{
    #region 基本信息
    [Header("基本信息")]
    [Tooltip("武器名称")]
    public string weaponName;
    
    [Tooltip("武器图标")]
    public Sprite weaponSprite;
    
    [Tooltip("武器类型")]
    public WeaponType weaponType = WeaponType.Melee;
    
    [Tooltip("武器描述")]
    [TextArea(2, 4)]
    public string description;
    #endregion
    
    #region 战斗属性
    [Header("战斗属性")]
    [Tooltip("基础伤害值")]
    [Range(1, 1000)]
    public int damage = 10;
    
    [Tooltip("攻击速度（攻击间隔秒数）")]
    [Range(0.1f, 5f)]
    public float attackSpeed = 0.3f;
    
    [Tooltip("攻击范围")]
    [Range(0.5f, 20f)]
    public float range = 2.5f;
    
    [Tooltip("暴击率（0-1）")]
    [Range(0f, 1f)]
    public float criticalChance = 0.05f;
    
    [Tooltip("暴击倍率")]
    [Range(1f, 5f)]
    public float criticalMultiplier = 2f;
    
    [Tooltip("伤害类型")]
    public DamageType damageType = DamageType.Physical;
    #endregion
    
    #region 特殊属性
    [Header("特殊属性")]
    [Tooltip("击退力度")]
    [Range(0f, 20f)]
    public float knockbackForce = 2f;
    
    [Tooltip("穿透次数（远程武器）")]
    [Range(0, 10)]
    public int pierceCount = 0;
    
    [Tooltip("弹药消耗（远程武器）")]
    [Range(0, 10)]
    public int ammoConsumption = 1;
    
    [Tooltip("魔法消耗（魔法武器）")]
    [Range(0, 100)]
    public int manaCost = 0;
    #endregion
    
    #region 投射物配置（远程武器）
    [Header("投射物配置")]
    [Tooltip("投射物预制体（远程武器使用）")]
    public GameObject projectilePrefab;
    
    [Tooltip("投射物速度")]
    [Range(1f, 50f)]
    public float projectileSpeed = 10f;
    
    [Tooltip("投射物生存时间")]
    [Range(0.5f, 10f)]
    public float projectileLifetime = 5f;
    #endregion
    
    #region 特效配置
    [Header("特效配置")]
    [Tooltip("攻击音效")]
    public AudioClip attackSound;
    
    [Tooltip("击中音效")]
    public AudioClip hitSound;
    
    [Tooltip("攻击特效预制体")]
    public GameObject attackEffect;
    
    [Tooltip("击中特效预制体")]
    public GameObject hitEffect;
    
    [Tooltip("武器轨迹特效")]
    public GameObject trailEffect;
    #endregion
    
    #region 动画配置
    [Header("动画配置")]
    [Tooltip("攻击动画名称")]
    public string attackAnimationName = "Attack";
    
    [Tooltip("攻击动画持续时间")]
    [Range(0.1f, 3f)]
    public float attackAnimationDuration = 0.5f;
    
    [Tooltip("是否在攻击时可以移动")]
    public bool canMoveWhileAttacking = false;
    #endregion

    #region 星怒武器专属配置
    [Header("星怒武器配置")]
    [Tooltip("是否为星怒类武器")]
    public bool isStarfuryWeapon = false;

    [Tooltip("星怒武器的星星投射物预制体")]
    public GameObject starProjectilePrefab;

    [Tooltip("每次攻击产生的星星数量")]
    [Range(1, 10)]
    public int numberOfStarsPerAttack = 1;

    [Tooltip("星星在目标位置上方多高处生成（用于确保从屏幕外生成）")]
    [Range(1f, 20f)]
    public float starSpawnVerticalOffset = 10f;

    [Tooltip("星星最终目标点在鼠标指针周围的随机半径")]
    [Range(0f, 5f)]
    public float starTargetRandomRadius = 1f;
    [Tooltip("（星怒武器专属）星星开始降落时播放的音效")]
    public AudioClip starSpecificFallSound;
    [Header("星怒光照效果")]
    [Tooltip("星星是否产生临时光照")]
    public bool starCreatesLight = true;
    [Tooltip("星星光照强度")]
    [Range(0.1f, 1.5f)]
    public float starLightIntensity = 0.8f;
    [Tooltip("星星光照半径（格子数）")]
    [Range(1, 10)]
    public int starLightRadius = 5;
    [Tooltip("星星单次脉冲光照的持续时间（秒）")]
    [Range(0.1f, 5f)]
    public float starLightDuration = 1.0f;
    #endregion
    
    #region 运行时属性（只读）
    /// <summary>
    /// 获取实际攻击范围（基于武器类型）
    /// </summary>
    public float ActualRange
    {
        get
        {
            if (range <= 0)
                return weaponType.GetDefaultRange();
            return range;
        }
    }
    
    /// <summary>
    /// 获取实际攻击速度（基于武器类型）
    /// </summary>
    public float ActualAttackSpeed
    {
        get
        {
            if (attackSpeed <= 0)
                return weaponType.GetDefaultAttackSpeed();
            return attackSpeed;
        }
    }
    
    /// <summary>
    /// 检查是否需要弹药
    /// </summary>
    public bool RequiresAmmo => weaponType.RequiresAmmo() && ammoConsumption > 0;
    
    /// <summary>
    /// 检查是否需要魔法值
    /// </summary>
    public bool RequiresMana => weaponType.RequiresMana() && manaCost > 0;
    
    /// <summary>
    /// 检查是否有投射物
    /// </summary>
    public bool HasProjectile => weaponType.IsRanged() && projectilePrefab != null;
    #endregion
    
    #region 工具方法
    /// <summary>
    /// 创建伤害信息
    /// </summary>
    /// <param name="source">伤害来源</param>
    /// <param name="hitPoint">击中点</param>
    /// <param name="hitDirection">击中方向</param>
    /// <returns>配置好的伤害信息</returns>
    public DamageInfo CreateDamageInfo(GameObject source, Vector2 hitPoint, Vector2 hitDirection)
    {
        // 计算是否暴击
        bool isCritical = Random.Range(0f, 1f) < criticalChance;
        
        // 计算最终伤害
        float finalDamage = damage;
        if (isCritical)
            finalDamage *= criticalMultiplier;
        
        return new DamageInfo(finalDamage, damageType, isCritical, source, hitPoint, hitDirection, knockbackForce, pierceCount, this); // 传递this作为weaponAsset
    }
    
    /// <summary>
    /// 验证武器配置
    /// </summary>
    /// <returns>配置是否有效</returns>
    public bool ValidateConfiguration()
    {
        if (string.IsNullOrEmpty(weaponName))
        {
            Debug.LogWarning($"武器 {name} 缺少武器名称");
            return false;
        }
        
        if (weaponSprite == null)
        {
            Debug.LogWarning($"武器 {weaponName} 缺少武器图标");
            return false;
        }
        
        if (damage <= 0)
        {
            Debug.LogWarning($"武器 {weaponName} 伤害值无效: {damage}");
            return false;
        }
        
        if (weaponType.IsRanged() && projectilePrefab == null)
        {
            Debug.LogWarning($"远程武器 {weaponName} 缺少投射物预制体");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 获取武器信息字符串
    /// </summary>
    public override string ToString()
    {
        return $"{weaponName} ({weaponType.GetDisplayName()}) - 伤害: {damage}, 攻击速度: {ActualAttackSpeed:F1}s, 射程: {ActualRange:F1}";
    }
    #endregion
    
    #region Unity回调
    /// <summary>
    /// 验证Inspector中的值
    /// </summary>
    void OnValidate()
    {
        // 确保基础值有效
        damage = Mathf.Max(1, damage);
        attackSpeed = Mathf.Max(0.1f, attackSpeed);
        range = Mathf.Max(0.5f, range);
        
        // 根据武器类型设置默认值
        if (range == 0)
            range = weaponType.GetDefaultRange();
        if (attackSpeed == 0)
            attackSpeed = weaponType.GetDefaultAttackSpeed();
        
        // 远程武器必须有弹药消耗
        if (weaponType.IsRanged() && ammoConsumption == 0)
            ammoConsumption = 1;
        
        // 魔法武器必须有魔法消耗
        if (weaponType.IsMagic() && manaCost == 0)
            manaCost = 10;

        // 星怒武器检查
        if (isStarfuryWeapon && starProjectilePrefab == null)
        {
            Debug.LogWarning($"武器 {weaponName} 被标记为星怒武器，但缺少 starProjectilePrefab 配置！");
        }
        if (!isStarfuryWeapon && starProjectilePrefab != null)
        {
            // 如果不是星怒武器但配置了星星预制体，可以选择清空或警告
            // starProjectilePrefab = null; // 或者 Debug.LogWarning($"武器 {weaponName} 不是星怒武器，但配置了 starProjectilePrefab。");
        }
        if (isStarfuryWeapon)
        {
            numberOfStarsPerAttack = Mathf.Max(1, numberOfStarsPerAttack);
            starSpawnVerticalOffset = Mathf.Max(0.1f, starSpawnVerticalOffset);
            starTargetRandomRadius = Mathf.Max(0f, starTargetRandomRadius);
        }
    }
    #endregion
}
