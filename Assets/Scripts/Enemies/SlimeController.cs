using Combat.Interfaces;
using UnityEngine;

/// <summary>
/// 控制史莱姆行为的核心脚本。
/// 实现了IDamageable接口来处理伤害，并提供供行为树调用的方法。
/// </summary>
public class SlimeController : MonoBehaviour, IDamageable
{
    [Tooltip("史莱姆的属性数据，引用一个AIStats ScriptableObject")]
    [SerializeField] private AIStats stats;
    [Header("地面检测")]
    public Transform groundCheck;
    public float groundCheckRadius;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private Transform target;
    private float currentHealth;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = stats.maxHealth;
        
        // 初始时尝试寻找玩家
        FindPlayerTarget();
    }

    void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    /// <summary>
    /// 通过寻找PlayerController组件来稳定地定位玩家。
    /// </summary>
    private void FindPlayerTarget()
    {
        // 优先通过寻找PlayerController组件来定位玩家，这比Tag更稳定
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            target = playerController.transform;
        }
        else
        {
            // 作为备用方案，如果找不到组件，再尝试用Tag，并给出警告
            Debug.LogWarning("SlimeController: 无法通过组件找到PlayerController，回退到使用 'Player' 标签查找。请检查玩家对象上是否有PlayerController脚本。");
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if(playerObject != null)
            {
                // 警告：这可能会找到错误的UI对象，而不是真实的角色
                Debug.LogWarning($"SlimeController: 通过Tag找到了名为 '{playerObject.name}' 的对象。如果这不是您的真实角色(PlayerRoot)，请修正标签设置。");
                target = playerObject.transform;
            }
        }
    }

    /// <summary>
    /// 实现IDamageable接口，处理受到的伤害。
    /// </summary>
    /// <param name="damageInfo">包含伤害信息的结构体。</param>
    public void TakeDamage(DamageInfo damageInfo)
    {
        currentHealth -= damageInfo.baseDamage;
        Debug.Log($"{gameObject.name} 受到 {damageInfo.baseDamage} 点伤害，剩余血量: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} 已被击败！");
        // 在此处可以添加死亡动画、掉落物等逻辑
        Destroy(gameObject, 1f); // 延迟1秒销毁物体
    }

    #region Behavior Tree Methods

    /// <summary>
    /// 检查玩家是否在视野范围内。
    /// </summary>
    /// <returns>如果玩家在视野内，返回true；否则返回false。</returns>
    public bool IsPlayerInSight()
    {
        if (target == null)
        {
            FindPlayerTarget();
            if (target == null)
            {
                 Debug.LogError("史莱姆无法找到任何玩家目标！请检查场景中是否存在带有PlayerController组件或'Player'标签的对象。");
                 return false;
            }
        }

        if (stats == null)
        {
            Debug.LogError($"错误：史莱姆 '{gameObject.name}' 的 'stats' 字段未在 Inspector 中分配！");
            return false;
        }

        Debug.Log("玩家：" + target.transform.position);
        Debug.Log("史莱姆：" + transform.position);
        return Vector2.Distance(transform.position, target.position) < stats.detectionRadius;
    }

    /// <summary>
    /// 朝着目标执行一次跳跃。
    /// </summary>
    public void PerformHopTowardsTarget()
    {
        if (!isGrounded)
        {
            Debug.Log("没有着地！");
            return;
        }
        if (target == null)
        {
            Debug.LogWarning("没有目标！");
            return;
        }
        Debug.Log("跳向玩家");
        Vector2 direction = (target.position - transform.position).normalized;
        rb.AddForce(direction * stats.moveSpeed, ForceMode2D.Impulse);
    }

    /// <summary>
    /// 执行一次原地跳跃。
    /// </summary>
    public void PerformIdleHop()
    {
        if (!isGrounded)
        {
            Debug.Log("IDLE: 没有着地！");
            return;
        }
        Debug.Log("原地跳跃");
        rb.AddForce(Vector2.up * (stats.moveSpeed * 0.5f), ForceMode2D.Impulse);
    }

    #endregion
    
    // 在编辑器中绘制调试范围，方便观察
    private void OnDrawGizmosSelected()
    {
        if (stats != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, stats.detectionRadius);
        }

        if(groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}