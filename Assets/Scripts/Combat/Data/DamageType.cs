using UnityEngine;

/// <summary>
/// 伤害类型枚举 - 定义不同的伤害元素类型
/// 支持元素相克和特殊效果
/// 位置: Combat/Data/ - 核心数据结构
/// </summary>
public enum DamageType
{
    /// <summary>
    /// 物理伤害 - 基础伤害类型，受护甲影响
    /// </summary>
    Physical = 0,
    
    /// <summary>
    /// 火焰伤害 - 可能造成燃烧效果
    /// </summary>
    Fire = 1,
    
    /// <summary>
    /// 冰霜伤害 - 可能造成减速或冰冻效果
    /// </summary>
    Ice = 2,
    
    /// <summary>
    /// 雷电伤害 - 可能造成麻痹或连锁伤害
    /// </summary>
    Lightning = 3,
    
    /// <summary>
    /// 毒素伤害 - 可能造成持续伤害效果
    /// </summary>
    Poison = 4,
    
    /// <summary>
    /// 神圣伤害 - 对亡灵生物额外伤害
    /// </summary>
    Holy = 5,
    
    /// <summary>
    /// 暗影伤害 - 对神圣生物额外伤害
    /// </summary>
    Shadow = 6,
    
    /// <summary>
    /// 真实伤害 - 无视护甲和抗性
    /// </summary>
    True = 7
}

/// <summary>
/// 伤害类型扩展方法 - 提供伤害类型相关的工具方法
/// </summary>
public static class DamageTypeExtensions
{
    /// <summary>
    /// 检查是否为元素伤害
    /// </summary>
    public static bool IsElemental(this DamageType damageType)
    {
        return damageType != DamageType.Physical && damageType != DamageType.True;
    }
    
    /// <summary>
    /// 检查是否受护甲影响
    /// </summary>
    public static bool IsAffectedByArmor(this DamageType damageType)
    {
        return damageType == DamageType.Physical;
    }
    
    /// <summary>
    /// 检查是否无视抗性
    /// </summary>
    public static bool IgnoresResistance(this DamageType damageType)
    {
        return damageType == DamageType.True;
    }
    
    /// <summary>
    /// 获取伤害类型的显示颜色
    /// </summary>
    public static Color GetDisplayColor(this DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Physical:
                return Color.red;
            case DamageType.Fire:
                return new Color(1f, 0.4f, 0f);      // 橙红色
            case DamageType.Ice:
                return new Color(0.4f, 0.8f, 1f);    // 冰蓝色
            case DamageType.Lightning:
                return new Color(1f, 1f, 0.2f);      // 电黄色
            case DamageType.Poison:
                return new Color(0.4f, 0.8f, 0.2f);  // 毒绿色
            case DamageType.Holy:
                return new Color(1f, 1f, 0.8f);      // 圣光色
            case DamageType.Shadow:
                return new Color(0.4f, 0.2f, 0.6f);  // 暗紫色
            case DamageType.True:
                return new Color(1f, 0.2f, 1f);      // 真伤紫色
            default:
                return Color.white;
        }
    }
    
    /// <summary>
    /// 获取伤害类型的显示名称
    /// </summary>
    public static string GetDisplayName(this DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Physical:
                return "物理";
            case DamageType.Fire:
                return "火焰";
            case DamageType.Ice:
                return "冰霜";
            case DamageType.Lightning:
                return "雷电";
            case DamageType.Poison:
                return "毒素";
            case DamageType.Holy:
                return "神圣";
            case DamageType.Shadow:
                return "暗影";
            case DamageType.True:
                return "真实";
            default:
                return "未知";
        }
    }
    
    /// <summary>
    /// 获取伤害类型的特效标识
    /// </summary>
    public static string GetEffectTag(this DamageType damageType)
    {
        switch (damageType)
        {
            case DamageType.Fire:
                return "Burning";
            case DamageType.Ice:
                return "Frozen";
            case DamageType.Lightning:
                return "Shocked";
            case DamageType.Poison:
                return "Poisoned";
            case DamageType.Holy:
                return "Blessed";
            case DamageType.Shadow:
                return "Cursed";
            default:
                return "";
        }
    }
    
    /// <summary>
    /// 获取对特定目标的伤害倍率
    /// </summary>
    public static float GetDamageMultiplier(this DamageType damageType, string targetTag)
    {
        // 基础倍率为1.0
        float multiplier = 1.0f;
        
        switch (damageType)
        {
            case DamageType.Holy:
                if (targetTag == "Undead" || targetTag == "Demon")
                    multiplier = 1.5f;  // 对亡灵和恶魔额外伤害
                break;
                
            case DamageType.Shadow:
                if (targetTag == "Angel" || targetTag == "Holy")
                    multiplier = 1.5f;  // 对天使和神圣生物额外伤害
                break;
                
            case DamageType.Fire:
                if (targetTag == "Ice" || targetTag == "Plant")
                    multiplier = 1.3f;  // 对冰系和植物额外伤害
                else if (targetTag == "Fire")
                    multiplier = 0.5f;  // 对火系生物减少伤害
                break;
                
            case DamageType.Ice:
                if (targetTag == "Fire")
                    multiplier = 1.3f;  // 对火系额外伤害
                else if (targetTag == "Ice")
                    multiplier = 0.5f;  // 对冰系减少伤害
                break;
        }
        
        return multiplier;
    }
}
