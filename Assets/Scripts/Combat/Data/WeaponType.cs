/// <summary>
/// 武器类型枚举 - 定义不同的武器分类
/// 遵循项目中ToolType和TileType的命名约定
/// 位置: Combat/Data/ - 核心数据结构
/// </summary>
public enum WeaponType
{
    /// <summary>
    /// 近战武器 - 使用射线检测进行攻击判定
    /// 例如：剑、斧头、锤子、匕首等
    /// </summary>
    Melee = 0,
    
    /// <summary>
    /// 远程武器 - 发射投射物进行攻击
    /// 例如：弓箭、枪械、弩、投掷武器等
    /// </summary>
    Ranged = 1,
    
    /// <summary>
    /// 魔法武器 - 使用魔法效果进行攻击
    /// 例如：法杖、魔法书、水晶球等
    /// </summary>
    Magic = 2,
    
    /// <summary>
    /// 召唤武器 - 召唤生物或物体协助战斗
    /// 例如：召唤法杖、图腾、宠物召唤器等
    /// </summary>
    Summon = 3,
    
    /// <summary>
    /// 工具武器 - 既可以作为工具使用，也可以战斗
    /// 例如：镐子、斧头、锤子等（复用现有Tool系统）
    /// </summary>
    Tool = 4
}

/// <summary>
/// 武器类型扩展方法 - 提供便捷的类型判断和属性获取
/// </summary>
public static class WeaponTypeExtensions
{
    /// <summary>
    /// 检查是否为近战武器
    /// </summary>
    public static bool IsMelee(this WeaponType weaponType)
    {
        return weaponType == WeaponType.Melee || weaponType == WeaponType.Tool;
    }
    
    /// <summary>
    /// 检查是否为远程武器
    /// </summary>
    public static bool IsRanged(this WeaponType weaponType)
    {
        return weaponType == WeaponType.Ranged;
    }
    
    /// <summary>
    /// 检查是否为魔法武器
    /// </summary>
    public static bool IsMagic(this WeaponType weaponType)
    {
        return weaponType == WeaponType.Magic || weaponType == WeaponType.Summon;
    }
    
    /// <summary>
    /// 检查是否需要弹药
    /// </summary>
    public static bool RequiresAmmo(this WeaponType weaponType)
    {
        return weaponType == WeaponType.Ranged;
    }
    
    /// <summary>
    /// 检查是否需要魔法值
    /// </summary>
    public static bool RequiresMana(this WeaponType weaponType)
    {
        return weaponType == WeaponType.Magic || weaponType == WeaponType.Summon;
    }
    
    /// <summary>
    /// 获取武器类型的显示名称
    /// </summary>
    public static string GetDisplayName(this WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.Melee:
                return "近战武器";
            case WeaponType.Ranged:
                return "远程武器";
            case WeaponType.Magic:
                return "魔法武器";
            case WeaponType.Summon:
                return "召唤武器";
            case WeaponType.Tool:
                return "工具武器";
            default:
                return "未知武器";
        }
    }
    
    /// <summary>
    /// 获取武器类型的默认攻击范围
    /// </summary>
    public static float GetDefaultRange(this WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.Melee:
            case WeaponType.Tool:
                return 2.5f;  // 近战武器默认范围
            case WeaponType.Ranged:
                return 15f;   // 远程武器默认范围
            case WeaponType.Magic:
                return 10f;   // 魔法武器默认范围
            case WeaponType.Summon:
                return 8f;    // 召唤武器默认范围
            default:
                return 2f;
        }
    }
    
    /// <summary>
    /// 获取武器类型的默认攻击速度
    /// </summary>
    public static float GetDefaultAttackSpeed(this WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.Melee:
                return 0.3f;  // 近战武器默认攻击间隔（更快）
            case WeaponType.Ranged:
                return 0.4f;  // 远程武器默认攻击间隔
            case WeaponType.Magic:
                return 0.5f;  // 魔法武器默认攻击间隔
            case WeaponType.Summon:
                return 1.0f;  // 召唤武器默认攻击间隔
            case WeaponType.Tool:
                return 0.6f;  // 工具武器默认攻击间隔
            default:
                return 0.3f;
        }
    }
}
