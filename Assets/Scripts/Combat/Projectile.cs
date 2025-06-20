using Combat.Interfaces;
using UnityEngine;

/// <summary>
/// 一个简单的投射物控制器
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 5f;

    private Rigidbody2D _rb;
    private float _damage;
    private int _ownerLayer;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifetime);
    }

    /// <summary>
    /// 初始化投射物
    /// </summary>
    public void Initialize(Vector2 direction, float damage, int ownerLayer)
    {
        _damage = damage;
        _ownerLayer = ownerLayer;
        _rb.velocity = direction * speed;
        
        // 忽略投射物与发射者之间的碰撞
        gameObject.layer = ownerLayer;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 避免伤害到发射者或同类
        if (other.gameObject.layer == _ownerLayer)
        {
            return;
        }

        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(new DamageInfo(_damage, DamageType.Magic, gameObject, transform.position));
        }

        // 击中任何物体后销毁
        Destroy(gameObject);
    }
} 