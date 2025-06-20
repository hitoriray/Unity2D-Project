using UnityEngine;

/// <summary>
/// 存储AI属性的数据容器（ScriptableObject）
/// 可以在Project窗口中创建和复用，方便策划调整数值
/// </summary>
[CreateAssetMenu(fileName = "NewAIStats", menuName = "Combat/AI Stats")]
public class AIStats : ScriptableObject
{
    [Header("核心属性")]
    public float maxHealth = 100f;

    [Header("移动")]
    public float moveSpeed = 3.5f;
    public float patrolSpeed = 1.5f;

    [Header("感知")]
    [Tooltip("发现玩家的范围")]
    public float detectionRadius = 10f;
    [Tooltip("丢失玩家的范围，通常比发现范围大")]
    public float loseSightRadius = 15f;

    [Header("攻击")]
    [Tooltip("进入攻击状态的距离")]
    public float attackRange = 1.5f;
    [Tooltip("两次攻击之间的冷却时间")]
    public float attackCooldown = 2f;
    public DamageInfo baseDamage;
} 