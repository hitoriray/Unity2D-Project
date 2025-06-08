using UnityEngine;

public class ItemDrop : MonoBehaviour
{
    [Header("掉落物信息")]
    public Item item;

    [Header("拾取设置")]
    public float pickupRange = 3.0f;    // 增大拾取范围
    public LayerMask playerLayer = -1;  // -1表示所有层

    [Header("碰撞器设置")]
    public string triggerColliderName = "PickupTrigger"; // 触发器碰撞器的名称

    [Header("移动设置")]
    public float moveSpeed = 50f;        // 增加移动速度
    public float accelerationRate = 50f;  // 增加加速度
    public bool passThroughTerrain = true; // 是否穿过地形移动到玩家

    [Header("视觉效果")]
    public float bobSpeed = 2f;
    public float bobHeight = 0.1f;

    private Vector3 startPosition;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private bool canBePickedUp = false;
    private bool isMovingToPlayer = false;
    private Transform playerTransform;
    private float currentMoveSpeed = 0f;

    private PlayerController playerController;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;

        SetupItemDrop();

        Invoke(nameof(EnablePickup), 0.2f);
    }

    void SetupItemDrop()
    {
        // 获取所有碰撞器
        Collider2D[] colliders = GetComponents<Collider2D>();

        if (colliders.Length >= 2)
        {
            // 第一个碰撞器：物理碰撞（与地形碰撞）
            colliders[0].isTrigger = false;
            colliders[0].name = "PhysicsCollider";

            // 第二个碰撞器：触发器（拾取检测）
            colliders[1].isTrigger = true;
            colliders[1].name = triggerColliderName;
        }

        // 设置刚体属性
        if (rb != null)
        {
            rb.drag = 0.5f;       // 添加一些阻力
            rb.angularDrag = 0.5f;
        }
    }
    
    void Update()
    {
        if (!canBePickedUp) return;

        if (isMovingToPlayer && playerTransform != null)
        {
            MoveToPlayer();
        }
        else
        {
            // 上下浮动效果
            if (rb.velocity.magnitude < 0.1f) // 只有在静止时才浮动
            {
                float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }
            else
            {
                startPosition = transform.position; // 更新起始位置
            }

            CheckForPlayer();
        }
    }
    
    void EnablePickup()
    {
        canBePickedUp = true;
    }
    
    void CheckForPlayer()
    {
        if (playerController == null)
            FindPlayerController();

        if (playerController == null) return;

        float distance = Vector2.Distance(transform.position, playerController.transform.position);
        if (distance > pickupRange) return;
        if (distance <= playerController.currentPickupRange)
            StartMovingToPlayer(playerController.transform);
    }

    void FindPlayerController()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        playerController = playerObj.GetComponent<PlayerController>();
        if (playerController == null)
            playerController = playerObj.GetComponentInParent<PlayerController>();
        if (playerController == null)
            playerController = playerObj.GetComponentInChildren<PlayerController>();
        if (playerController == null)
            Debug.LogWarning($"[{gameObject.name}] 未找到PlayerController组件");
    }

    void StartMovingToPlayer(Transform player)
    {
        if (!isMovingToPlayer)
        {
            isMovingToPlayer = true;
            playerTransform = player;
            currentMoveSpeed = 0f;

            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic; // 设置为运动学刚体，不受物理影响
            }

            if (passThroughTerrain)
            {
                Collider2D[] colliders = GetComponents<Collider2D>();
                foreach (Collider2D collider in colliders)
                    collider.enabled = false; // 禁用所有碰撞器，让掉落物穿过地形
            }
            else
            {
                Collider2D[] colliders = GetComponents<Collider2D>();
                foreach (Collider2D collider in colliders)
                    if (collider.isTrigger && (collider.name == triggerColliderName || collider.name.Contains("Trigger")))
                        collider.enabled = false; // 禁用触发器，避免重复拾取
            }
        }
    }

    void MoveToPlayer()
    {
        if (playerTransform == null) return;

        float distance = Vector2.Distance(transform.position, playerTransform.position);
        currentMoveSpeed += accelerationRate * Time.deltaTime;
        currentMoveSpeed = Mathf.Min(currentMoveSpeed, moveSpeed);
        transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, currentMoveSpeed * Time.deltaTime);
        if (distance < 0.8f)
            PickupItem();
    }
    
    void PickupItem()
    {
        // TODO: 添加到玩家背包
        // if (player.inventory.TryAddItem(item))
        // {
            ShowPickupText();

            // 播放拾取音效
            // AudioSource.PlayClipAtPoint(pickupSound, transform.position);

            Destroy(gameObject);
        // }
    }

    void ShowPickupText()
    {
        if (playerTransform != null)
        {
            string displayText = item != null ? $"{item.itemName}({item.quantity})" : gameObject.name;

            // 使用3D文本
            if (PickupText3DManager.Instance != null)
                PickupText3DManager.Instance.ShowPickupText(playerTransform.position, displayText);
            else
                Debug.LogWarning("没有找到文本管理器，无法显示拾取信息");
        }
    }
    
    public void SetItem(Item newItem)
    {
        item = new Item(newItem);
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();            
        if (spriteRenderer != null && item.tile != null)
            spriteRenderer.sprite = item.tile.itemSprite;
        gameObject.name = $"ItemDrop_{item.itemName}";
    }
    
    // void OnDrawGizmosSelected()
    // {
    //     // 在Scene视图中显示拾取范围
    //     Gizmos.color = Color.yellow;
    //     Gizmos.DrawWireSphere(transform.position, pickupRange);
    //     // 显示玩家检测范围
    //     if (playerController != null)
    //     {
    //         Gizmos.color = Color.green;
    //         Gizmos.DrawWireSphere(playerController.transform.position, playerController.currentPickupRange);

    //         // 显示连线
    //         float distance = Vector2.Distance(transform.position, playerController.transform.position);
    //         if (distance <= playerController.currentPickupRange)
    //         {
    //             Gizmos.color = Color.green;
    //         }
    //         else
    //         {
    //             Gizmos.color = Color.red;
    //         }
    //         Gizmos.DrawLine(transform.position, playerController.transform.position);
    //     }
    // }

    // void OnDrawGizmos()
    // {
    //     if (!canBePickedUp) return;
    //     Gizmos.color = new Color(1, 1, 0, 0.3f); // 半透明黄色
    //     Gizmos.DrawSphere(transform.position, pickupRange);
    // }
}