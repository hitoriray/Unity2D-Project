/// <summary>
/// 战斗状态枚举 - 定义角色在战斗中的各种状态
/// 用于状态机管理和动画控制
/// 位置: Combat/Data/ - 核心数据结构
/// </summary>
public enum CombatState
{
    /// <summary>
    /// 空闲状态 - 角色没有进行任何战斗行为
    /// </summary>
    Idle = 0,
    
    /// <summary>
    /// 攻击准备状态 - 角色正在准备攻击（举起武器等）
    /// </summary>
    AttackPrepare = 1,
    
    /// <summary>
    /// 攻击执行状态 - 角色正在执行攻击动作
    /// </summary>
    Attacking = 2,
    
    /// <summary>
    /// 攻击恢复状态 - 攻击后的恢复时间（无法再次攻击）
    /// </summary>
    AttackRecovery = 3,
    
    /// <summary>
    /// 受伤状态 - 角色受到伤害时的状态
    /// </summary>
    Hurt = 4,
    
    /// <summary>
    /// 格挡状态 - 角色正在格挡攻击
    /// </summary>
    Blocking = 5,
    
    /// <summary>
    /// 眩晕状态 - 角色被眩晕，无法行动
    /// </summary>
    Stunned = 6,
    
    /// <summary>
    /// 死亡状态 - 角色生命值归零
    /// </summary>
    Dead = 7,
    
    /// <summary>
    /// 无敌状态 - 角色处于无敌时间，不受伤害
    /// </summary>
    Invulnerable = 8
}

/// <summary>
/// 战斗状态扩展方法 - 提供状态相关的工具方法
/// </summary>
public static class CombatStateExtensions
{
    /// <summary>
    /// 检查是否可以移动
    /// </summary>
    public static bool CanMove(this CombatState state)
    {
        switch (state)
        {
            case CombatState.Idle:
            case CombatState.AttackPrepare:
            case CombatState.Invulnerable:
                return true;
            case CombatState.Attacking:
            case CombatState.AttackRecovery:
            case CombatState.Hurt:
            case CombatState.Blocking:
            case CombatState.Stunned:
            case CombatState.Dead:
                return false;
            default:
                return true;
        }
    }
    
    /// <summary>
    /// 检查是否可以攻击
    /// </summary>
    public static bool CanAttack(this CombatState state)
    {
        switch (state)
        {
            case CombatState.Idle:
                return true;
            case CombatState.AttackPrepare:
            case CombatState.Attacking:
            case CombatState.AttackRecovery:
            case CombatState.Hurt:
            case CombatState.Blocking:
            case CombatState.Stunned:
            case CombatState.Dead:
            case CombatState.Invulnerable:
                return false;
            default:
                return false;
        }
    }
    
    /// <summary>
    /// 检查是否可以受到伤害
    /// </summary>
    public static bool CanTakeDamage(this CombatState state)
    {
        switch (state)
        {
            case CombatState.Idle:
            case CombatState.AttackPrepare:
            case CombatState.Attacking:
            case CombatState.AttackRecovery:
            case CombatState.Hurt:
                return true;
            case CombatState.Blocking:
                return false;  // 格挡状态减少伤害但仍可受伤
            case CombatState.Stunned:
                return true;   // 眩晕状态更容易受伤
            case CombatState.Dead:
            case CombatState.Invulnerable:
                return false;
            default:
                return true;
        }
    }
    
    /// <summary>
    /// 检查是否可以被打断
    /// </summary>
    public static bool CanBeInterrupted(this CombatState state)
    {
        switch (state)
        {
            case CombatState.Idle:
            case CombatState.AttackPrepare:
                return true;
            case CombatState.Attacking:
            case CombatState.AttackRecovery:
            case CombatState.Hurt:
            case CombatState.Blocking:
            case CombatState.Stunned:
            case CombatState.Dead:
            case CombatState.Invulnerable:
                return false;
            default:
                return true;
        }
    }
    
    /// <summary>
    /// 检查是否为攻击相关状态
    /// </summary>
    public static bool IsAttackState(this CombatState state)
    {
        return state == CombatState.AttackPrepare || 
               state == CombatState.Attacking || 
               state == CombatState.AttackRecovery;
    }
    
    /// <summary>
    /// 检查是否为负面状态
    /// </summary>
    public static bool IsNegativeState(this CombatState state)
    {
        return state == CombatState.Hurt || 
               state == CombatState.Stunned || 
               state == CombatState.Dead;
    }
    
    /// <summary>
    /// 获取状态的显示名称
    /// </summary>
    public static string GetDisplayName(this CombatState state)
    {
        switch (state)
        {
            case CombatState.Idle:
                return "空闲";
            case CombatState.AttackPrepare:
                return "准备攻击";
            case CombatState.Attacking:
                return "攻击中";
            case CombatState.AttackRecovery:
                return "攻击恢复";
            case CombatState.Hurt:
                return "受伤";
            case CombatState.Blocking:
                return "格挡";
            case CombatState.Stunned:
                return "眩晕";
            case CombatState.Dead:
                return "死亡";
            case CombatState.Invulnerable:
                return "无敌";
            default:
                return "未知状态";
        }
    }
    
    /// <summary>
    /// 获取状态的优先级（数值越高优先级越高）
    /// </summary>
    public static int GetPriority(this CombatState state)
    {
        switch (state)
        {
            case CombatState.Dead:
                return 100;  // 死亡状态优先级最高
            case CombatState.Stunned:
                return 90;
            case CombatState.Hurt:
                return 80;
            case CombatState.Attacking:
                return 70;
            case CombatState.AttackRecovery:
                return 60;
            case CombatState.AttackPrepare:
                return 50;
            case CombatState.Blocking:
                return 40;
            case CombatState.Invulnerable:
                return 30;
            case CombatState.Idle:
                return 10;   // 空闲状态优先级最低
            default:
                return 0;
        }
    }
}
