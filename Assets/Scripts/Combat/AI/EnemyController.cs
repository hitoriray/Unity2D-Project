using Combat.Interfaces;
using UnityEngine;

/// <summary>
/// AI敌人的核心控制器
/// 负责管理状态机、处理伤害、并连接其他组件（如动画、物理）
/// </summary>
public class EnemyController : MonoBehaviour, IDamageable
{
    [Tooltip("AI的属性数据，引用一个AIStats ScriptableObject")]
    [SerializeField] private AIStats stats;
    
    // 公开属性，方便状态脚本访问
    public AIStats Stats => stats;
    public Transform Target { get; private set; } // 存储玩家或其他目标

    private void Start()
    {
        // 临时找到玩家作为目标，后续可优化为更高效的索敌系统
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Target = player.transform;
        }
    }

    private void Update()
    {
        // 状态机逻辑已移除，由行为树驱动
    }

    #region Behavior Tree Actions

    /// <summary>
    /// 检查玩家是否在视野范围内
    /// </summary>
    public bool IsPlayerInSight()
    {
        if (Target == null) return false;
        return Vector3.Distance(transform.position, Target.position) < stats.detectionRadius;
    }

    /// <summary>
    /// 检查玩家是否在攻击范围内
    /// </summary>
    public bool IsPlayerInAttackRange()
    {
        if (Target == null) return false;
        return Vector3.Distance(transform.position, Target.position) < stats.attackRange;
    }

    /// <summary>
    /// 执行攻击动作
    /// </summary>
    public void ExecuteAttack()
    {
        // 在此实现攻击逻辑，例如播放动画和音效
        Debug.Log("执行攻击！");
    }
    
    /// <summary>
    /// 获取当前目标
    /// </summary>
    public Transform GetTarget()
    {
        return Target;
    }

    #endregion

    #region IDamageable Implementation
    
    /// <summary>
    /// 实现IDamageable接口，处理受到的伤害
    /// </summary>
    public void TakeDamage(DamageInfo damageInfo)
    {
        // 这里是基础的扣血逻辑，后续会添加硬直、状态效果等
        stats.maxHealth -= damageInfo.baseDamage;
        Debug.Log($"{gameObject.name} 受到 {damageInfo.baseDamage} 点伤害，剩余血量: {stats.maxHealth}");

        if (stats.maxHealth <= 0)
        {
            Die();
        }
        else
        {
            // 可选：触发一个受击状态
        }
    }
    
    #endregion

    private void Die()
    {
        Debug.Log($"{gameObject.name} 已被击败！");
        // 触发死亡状态，播放死亡动画、掉落物品等
        Destroy(gameObject, 2f); // 临时处理，延迟销毁
    }
    
    // 可选：在编辑器中绘制一些调试信息，方便观察
    private void OnDrawGizmosSelected()
    {
        if (stats == null) return;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stats.detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stats.attackRange);
        
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, stats.loseSightRadius);
    }
}