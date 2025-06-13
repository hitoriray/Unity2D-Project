using UnityEngine;

/// <summary>
/// 武器创建器 - 用于在代码中创建示例武器资产
/// 位置: Combat/Weapons/ - 武器系统工具
/// </summary>
public class WeaponCreator : MonoBehaviour
{
    [Header("武器创建测试")]
    [Tooltip("是否在Start时创建示例武器")]
    public bool createSampleWeaponsOnStart = false;
    
    void Start()
    {
        if (createSampleWeaponsOnStart)
        {
            CreateSampleWeapons();
        }
    }
    
    /// <summary>
    /// 创建示例武器
    /// </summary>
    [ContextMenu("创建示例武器")]
    public void CreateSampleWeapons()
    {
        Debug.Log("=== 开始创建示例武器 ===");
        
        CreateMeleeWeapon();
        CreateRangedWeapon();
        CreateMagicWeapon();
        
        Debug.Log("=== 示例武器创建完成 ===");
    }
    
    /// <summary>
    /// 创建近战武器示例
    /// </summary>
    [ContextMenu("创建近战武器")]
    public void CreateMeleeWeapon()
    {
        // 在运行时创建武器实例（仅用于测试）
        Weapon sword = ScriptableObject.CreateInstance<Weapon>();
        
        // 基本信息
        sword.weaponName = "铁剑";
        sword.weaponType = WeaponType.Melee;
        sword.description = "一把普通的铁制长剑，适合近战战斗。";
        
        // 战斗属性
        sword.damage = 25;
        sword.attackSpeed = 1.2f;
        sword.range = 2.5f;
        sword.criticalChance = 0.1f;
        sword.criticalMultiplier = 2.0f;
        sword.damageType = DamageType.Physical;
        
        // 特殊属性
        sword.knockbackForce = 3f;
        sword.pierceCount = 0;
        sword.ammoConsumption = 0;
        sword.manaCost = 0;
        
        // 动画配置
        sword.attackAnimationName = "SwordAttack";
        sword.attackAnimationDuration = 0.6f;
        sword.canMoveWhileAttacking = false;
        
        Debug.Log($"创建近战武器: {sword}");
        Debug.Log($"配置验证: {sword.ValidateConfiguration()}");
        
        // 测试伤害信息创建
        DamageInfo damage = sword.CreateDamageInfo(gameObject, transform.position, Vector2.right);
        Debug.Log($"伤害信息: {damage}");
    }
    
    /// <summary>
    /// 创建远程武器示例
    /// </summary>
    [ContextMenu("创建远程武器")]
    public void CreateRangedWeapon()
    {
        Weapon bow = ScriptableObject.CreateInstance<Weapon>();
        
        // 基本信息
        bow.weaponName = "猎弓";
        bow.weaponType = WeaponType.Ranged;
        bow.description = "一把精制的猎弓，可以发射箭矢攻击远距离目标。";
        
        // 战斗属性
        bow.damage = 20;
        bow.attackSpeed = 0.8f;
        bow.range = 12f;
        bow.criticalChance = 0.15f;
        bow.criticalMultiplier = 2.5f;
        bow.damageType = DamageType.Physical;
        
        // 特殊属性
        bow.knockbackForce = 1f;
        bow.pierceCount = 1;
        bow.ammoConsumption = 1;
        bow.manaCost = 0;
        
        // 投射物配置
        bow.projectileSpeed = 15f;
        bow.projectileLifetime = 3f;
        
        // 动画配置
        bow.attackAnimationName = "BowShoot";
        bow.attackAnimationDuration = 0.4f;
        bow.canMoveWhileAttacking = true;
        
        Debug.Log($"创建远程武器: {bow}");
        Debug.Log($"配置验证: {bow.ValidateConfiguration()}");
        Debug.Log($"需要弹药: {bow.RequiresAmmo}");
        
        // 测试伤害信息创建
        DamageInfo damage = bow.CreateDamageInfo(gameObject, transform.position, Vector2.right);
        Debug.Log($"伤害信息: {damage}");
    }
    
    /// <summary>
    /// 创建魔法武器示例
    /// </summary>
    [ContextMenu("创建魔法武器")]
    public void CreateMagicWeapon()
    {
        Weapon staff = ScriptableObject.CreateInstance<Weapon>();
        
        // 基本信息
        staff.weaponName = "火焰法杖";
        staff.weaponType = WeaponType.Magic;
        staff.description = "一根蕴含火焰魔法的法杖，可以释放火球攻击敌人。";
        
        // 战斗属性
        staff.damage = 35;
        staff.attackSpeed = 1.5f;
        staff.range = 8f;
        staff.criticalChance = 0.08f;
        staff.criticalMultiplier = 3.0f;
        staff.damageType = DamageType.Fire;
        
        // 特殊属性
        staff.knockbackForce = 2f;
        staff.pierceCount = 0;
        staff.ammoConsumption = 0;
        staff.manaCost = 15;
        
        // 投射物配置
        staff.projectileSpeed = 8f;
        staff.projectileLifetime = 2f;
        
        // 动画配置
        staff.attackAnimationName = "StaffCast";
        staff.attackAnimationDuration = 0.8f;
        staff.canMoveWhileAttacking = false;
        
        Debug.Log($"创建魔法武器: {staff}");
        Debug.Log($"配置验证: {staff.ValidateConfiguration()}");
        Debug.Log($"需要魔法: {staff.RequiresMana}");
        
        // 测试伤害信息创建
        DamageInfo damage = staff.CreateDamageInfo(gameObject, transform.position, Vector2.right);
        Debug.Log($"伤害信息: {damage}");
    }
    
    /// <summary>
    /// 测试武器类型扩展方法
    /// </summary>
    [ContextMenu("测试武器类型扩展")]
    public void TestWeaponTypeExtensions()
    {
        Debug.Log("=== 武器类型扩展方法测试 ===");
        
        foreach (WeaponType weaponType in System.Enum.GetValues(typeof(WeaponType)))
        {
            Debug.Log($"{weaponType}:");
            Debug.Log($"  显示名称: {weaponType.GetDisplayName()}");
            Debug.Log($"  近战: {weaponType.IsMelee()}");
            Debug.Log($"  远程: {weaponType.IsRanged()}");
            Debug.Log($"  魔法: {weaponType.IsMagic()}");
            Debug.Log($"  需要弹药: {weaponType.RequiresAmmo()}");
            Debug.Log($"  需要魔法: {weaponType.RequiresMana()}");
            Debug.Log($"  默认射程: {weaponType.GetDefaultRange()}");
            Debug.Log($"  默认攻击速度: {weaponType.GetDefaultAttackSpeed()}");
            Debug.Log("");
        }
    }
    
    /// <summary>
    /// 测试伤害类型扩展方法
    /// </summary>
    [ContextMenu("测试伤害类型扩展")]
    public void TestDamageTypeExtensions()
    {
        Debug.Log("=== 伤害类型扩展方法测试 ===");
        
        foreach (DamageType damageType in System.Enum.GetValues(typeof(DamageType)))
        {
            Debug.Log($"{damageType}:");
            Debug.Log($"  显示名称: {damageType.GetDisplayName()}");
            Debug.Log($"  元素伤害: {damageType.IsElemental()}");
            Debug.Log($"  受护甲影响: {damageType.IsAffectedByArmor()}");
            Debug.Log($"  无视抗性: {damageType.IgnoresResistance()}");
            Debug.Log($"  显示颜色: {damageType.GetDisplayColor()}");
            Debug.Log($"  特效标识: {damageType.GetEffectTag()}");
            Debug.Log($"  对亡灵倍率: {damageType.GetDamageMultiplier("Undead")}");
            Debug.Log("");
        }
    }
}
