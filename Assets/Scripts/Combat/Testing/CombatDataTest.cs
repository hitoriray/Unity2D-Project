using UnityEngine;

/// <summary>
/// 战斗数据结构测试脚本 - 用于验证核心数据结构的功能
/// 可以在Inspector中查看各种数据结构的行为
/// 位置: Combat/Testing/ - 测试工具
/// </summary>
public class CombatDataTest : MonoBehaviour
{
    [Header("测试配置")]
    [Tooltip("测试用的伤害值")]
    public float testDamage = 50f;
    
    [Tooltip("测试用的武器类型")]
    public WeaponType testWeaponType = WeaponType.Melee;
    
    [Tooltip("测试用的伤害类型")]
    public DamageType testDamageType = DamageType.Physical;
    
    [Tooltip("测试用的战斗状态")]
    public CombatState testCombatState = CombatState.Idle;
    
    [Tooltip("测试用的武器资产")]
    public Weapon testWeapon;
    
    [Header("测试结果显示")]
    [SerializeField, TextArea(3, 5)]
    private string weaponTypeInfo;
    
    [SerializeField, TextArea(3, 5)]
    private string damageTypeInfo;
    
    [SerializeField, TextArea(3, 5)]
    private string combatStateInfo;
    
    [SerializeField, TextArea(3, 5)]
    private string damageInfoTest;
    
    [SerializeField, TextArea(3, 5)]
    private string weaponTest;
    
    void Start()
    {
        Debug.Log("=== 战斗数据结构测试开始 ===");
        TestAllDataStructures();
    }
    
    void Update()
    {
        // 实时更新测试信息（仅在编辑器中）
        #if UNITY_EDITOR
        UpdateTestInfo();
        #endif
    }
    
    /// <summary>
    /// 测试所有数据结构
    /// </summary>
    [ContextMenu("运行完整测试")]
    public void TestAllDataStructures()
    {
        TestWeaponType();
        TestDamageType();
        TestCombatState();
        TestDamageInfo();
        TestWeaponAsset();
        
        Debug.Log("=== 战斗数据结构测试完成 ===");
    }
    
    /// <summary>
    /// 测试武器类型
    /// </summary>
    [ContextMenu("测试武器类型")]
    public void TestWeaponType()
    {
        Debug.Log($"--- 武器类型测试: {testWeaponType} ---");
        Debug.Log($"是否近战: {testWeaponType.IsMelee()}");
        Debug.Log($"是否远程: {testWeaponType.IsRanged()}");
        Debug.Log($"是否魔法: {testWeaponType.IsMagic()}");
        Debug.Log($"需要弹药: {testWeaponType.RequiresAmmo()}");
        Debug.Log($"需要魔法值: {testWeaponType.RequiresMana()}");
        Debug.Log($"显示名称: {testWeaponType.GetDisplayName()}");
        Debug.Log($"默认射程: {testWeaponType.GetDefaultRange()}");
        Debug.Log($"默认攻击速度: {testWeaponType.GetDefaultAttackSpeed()}");
    }
    
    /// <summary>
    /// 测试伤害类型
    /// </summary>
    [ContextMenu("测试伤害类型")]
    public void TestDamageType()
    {
        Debug.Log($"--- 伤害类型测试: {testDamageType} ---");
        Debug.Log($"是否元素伤害: {testDamageType.IsElemental()}");
        Debug.Log($"受护甲影响: {testDamageType.IsAffectedByArmor()}");
        Debug.Log($"无视抗性: {testDamageType.IgnoresResistance()}");
        Debug.Log($"显示名称: {testDamageType.GetDisplayName()}");
        Debug.Log($"显示颜色: {testDamageType.GetDisplayColor()}");
        Debug.Log($"特效标识: {testDamageType.GetEffectTag()}");
        Debug.Log($"对亡灵伤害倍率: {testDamageType.GetDamageMultiplier("Undead")}");
    }
    
    /// <summary>
    /// 测试战斗状态
    /// </summary>
    [ContextMenu("测试战斗状态")]
    public void TestCombatState()
    {
        Debug.Log($"--- 战斗状态测试: {testCombatState} ---");
        Debug.Log($"可以移动: {testCombatState.CanMove()}");
        Debug.Log($"可以攻击: {testCombatState.CanAttack()}");
        Debug.Log($"可以受伤: {testCombatState.CanTakeDamage()}");
        Debug.Log($"可以被打断: {testCombatState.CanBeInterrupted()}");
        Debug.Log($"是攻击状态: {testCombatState.IsAttackState()}");
        Debug.Log($"是负面状态: {testCombatState.IsNegativeState()}");
        Debug.Log($"显示名称: {testCombatState.GetDisplayName()}");
        Debug.Log($"状态优先级: {testCombatState.GetPriority()}");
    }
    
    /// <summary>
    /// 测试伤害信息
    /// </summary>
    [ContextMenu("测试伤害信息")]
    public void TestDamageInfo()
    {
        Debug.Log($"--- 伤害信息测试 ---");
        
        // 创建基础伤害信息
        DamageInfo basicDamage = new DamageInfo(testDamage, testDamageType, gameObject, transform.position);
        Debug.Log($"基础伤害: {basicDamage}");
        Debug.Log($"是否有效: {basicDamage.IsValid()}");
        Debug.Log($"显示伤害: {basicDamage.GetDisplayDamage()}");
        
        // 测试链式调用
        DamageInfo criticalDamage = basicDamage.WithCritical(true).WithKnockback(5f).WithPierce(2);
        Debug.Log($"暴击伤害: {criticalDamage}");
        Debug.Log($"击退力度: {criticalDamage.knockbackForce}");
        Debug.Log($"穿透次数: {criticalDamage.pierceCount}");
        
        // 测试完整构造函数
        Vector2 hitDirection = Vector2.right;
        DamageInfo fullDamage = new DamageInfo(testDamage * 2, DamageType.Fire, true, gameObject, 
                                              transform.position, hitDirection, 10f, 1);
        Debug.Log($"完整伤害信息: {fullDamage}");
    }
    
    /// <summary>
    /// 测试武器资产
    /// </summary>
    [ContextMenu("测试武器资产")]
    public void TestWeaponAsset()
    {
        if (testWeapon == null)
        {
            Debug.LogWarning("请在Inspector中设置测试武器资产");
            return;
        }
        
        Debug.Log($"--- 武器资产测试: {testWeapon.weaponName} ---");
        Debug.Log($"武器信息: {testWeapon}");
        Debug.Log($"配置有效: {testWeapon.ValidateConfiguration()}");
        Debug.Log($"需要弹药: {testWeapon.RequiresAmmo}");
        Debug.Log($"需要魔法: {testWeapon.RequiresMana}");
        Debug.Log($"有投射物: {testWeapon.HasProjectile}");
        
        // 测试伤害信息创建
        DamageInfo weaponDamage = testWeapon.CreateDamageInfo(gameObject, transform.position, Vector2.right);
        Debug.Log($"武器伤害信息: {weaponDamage}");
    }
    
    /// <summary>
    /// 更新测试信息显示
    /// </summary>
    private void UpdateTestInfo()
    {
        // 更新武器类型信息
        weaponTypeInfo = $"武器类型: {testWeaponType.GetDisplayName()}\n" +
                        $"近战: {testWeaponType.IsMelee()} | 远程: {testWeaponType.IsRanged()} | 魔法: {testWeaponType.IsMagic()}\n" +
                        $"射程: {testWeaponType.GetDefaultRange()} | 攻击速度: {testWeaponType.GetDefaultAttackSpeed()}";
        
        // 更新伤害类型信息
        damageTypeInfo = $"伤害类型: {testDamageType.GetDisplayName()}\n" +
                        $"元素: {testDamageType.IsElemental()} | 受护甲影响: {testDamageType.IsAffectedByArmor()}\n" +
                        $"颜色: {testDamageType.GetDisplayColor()} | 特效: {testDamageType.GetEffectTag()}";
        
        // 更新战斗状态信息
        combatStateInfo = $"战斗状态: {testCombatState.GetDisplayName()}\n" +
                         $"移动: {testCombatState.CanMove()} | 攻击: {testCombatState.CanAttack()} | 受伤: {testCombatState.CanTakeDamage()}\n" +
                         $"优先级: {testCombatState.GetPriority()} | 负面状态: {testCombatState.IsNegativeState()}";
        
        // 更新伤害信息测试
        DamageInfo testInfo = new DamageInfo(testDamage, testDamageType, gameObject, transform.position);
        damageInfoTest = $"伤害信息测试:\n{testInfo}\n" +
                        $"有效: {testInfo.IsValid()} | 显示伤害: {testInfo.GetDisplayDamage()}";
        
        // 更新武器测试
        if (testWeapon != null)
        {
            weaponTest = $"武器测试: {testWeapon.weaponName}\n" +
                        $"类型: {testWeapon.weaponType.GetDisplayName()} | 伤害: {testWeapon.damage}\n" +
                        $"配置有效: {testWeapon.ValidateConfiguration()}";
        }
        else
        {
            weaponTest = "请设置测试武器资产";
        }
    }
}
