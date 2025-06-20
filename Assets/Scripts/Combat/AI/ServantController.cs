using UnityEngine;

public class ServantController : MonoBehaviour
{
    public float speed = 3f;
    private Transform _playerTransform;
    private Rigidbody2D _rb;

    void Start()
    {
        // 查找玩家对象，实际项目中建议使用更高效的管理器来获取引用
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
        }
        _rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (_playerTransform != null)
        {
            Vector2 direction = (_playerTransform.position - transform.position).normalized;
            _rb.velocity = direction * speed;
        }
    }

    // 可以添加碰撞伤害逻辑
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 在这里对玩家造成伤害
            Debug.Log("Servant hit the player!");
            Destroy(gameObject); // 撞到玩家后自我毁灭
        }
    }
} 