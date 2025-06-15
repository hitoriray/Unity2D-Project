using UnityEngine;

public class ItemDrop : MonoBehaviour
{
    [Header("掉落物信息")]
    public Item item;

    [Header("拾取设置")]
    public float pickupRange = 3.0f;
    public LayerMask playerLayer = -1;  // -1表示所有层

    [Header("碰撞器设置")]
    public string triggerColliderName = "PickupTrigger";

    [Header("移动设置")]
    public float moveSpeed = 50f;
    public float accelerationRate = 50f;
    public bool passThroughTerrain = true; // 是否穿过地形移动到玩家

    [Header("视觉效果")]
    public float bobSpeed = 2f;
    public float bobHeight = 0.1f;

    [Header("拾取冷却设置")]
    public float dropCooldownTime = 1.0f; // 丢弃后的冷却时间
    public float exitDistance = 2.0f; // 玩家需要离开的距离

    private Vector3 startPosition;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private bool canBePickedUp = false;
    private bool isMovingToPlayer = false;
    private Transform playerTransform;
    private float currentMoveSpeed = 0f;
    private PlayerController playerController;

    // 拾取冷却相关
    private bool isDroppedByPlayer = false; // 是否是玩家丢弃的
    private bool playerHasExited = false; // 玩家是否已经离开过
    private float dropTime; // 丢弃时间

    

    #region 生命周期函数
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;

        SetupItemDrop();

        Invoke(nameof(EnablePickup), 0.2f);
    }

    void Update()
    {
        if (!canBePickedUp)
        {
            // Debug.Log($"[ItemDrop '{gameObject.name}'] Update: canBePickedUp is false. Time: {Time.time}");
            return;
        }

        // Debug.Log($"[ItemDrop '{gameObject.name}'] Update: Processing. isMovingToPlayer: {isMovingToPlayer}, playerTransform: {(playerTransform != null ? playerTransform.name : "null")}");

        if (isMovingToPlayer && playerTransform != null)
        {
            MoveToPlayer();
        }
        else
        {
            // 上下浮动效果
            if (rb != null && rb.velocity.magnitude < 0.1f) // 只有在静止时才浮动
            {
                float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }
            else if (rb != null) // 只有当rb存在时才更新startPosition
            {
                startPosition = transform.position; // 更新起始位置
            }
            // Debug.Log($"[ItemDrop '{gameObject.name}'] Update: Calling CheckForPlayer(). Time: {Time.time}");
            CheckForPlayer();
        }
    }

    #endregion

    
    #region 拾取相关
    void EnablePickup()
    {
        canBePickedUp = true;
    }
    
    void CheckForPlayer()
    {
        // Debug.Log($"[ItemDrop '{gameObject.name}'] CheckForPlayer: Entered. Time: {Time.time}");
        if (playerController == null)
        {
            // Debug.Log($"[ItemDrop '{gameObject.name}'] CheckForPlayer: playerController is null, calling FindPlayerController().");
            FindPlayerController();
        }

        if (playerController == null)
        {
            // Debug.LogWarning($"[ItemDrop '{gameObject.name}'] CheckForPlayer: playerController is still null after FindPlayerController(). Cannot check for player.");
            return;
        }
        
        // Debug.Log($"[ItemDrop '{gameObject.name}'] CheckForPlayer: playerController found: {playerController.name}. Item: {(item != null ? item.itemName : "N/A")}");

        float distance = Vector2.Distance(transform.position, playerController.transform.position);
        // Debug.Log($"[ItemDrop '{gameObject.name}'] CheckForPlayer: Distance to player: {distance:F2}. Player pickup range: {playerController.currentPickupRange}. ItemDrop pickupRange: {pickupRange}");

        // 检查玩家是否已经离开过（如果是玩家丢弃的物品）
        if (isDroppedByPlayer && !playerHasExited)
        {
            if (distance > exitDistance)
            {
                playerHasExited = true;
                // Debug.Log($"[ItemDrop '{gameObject.name}'] CheckForPlayer: Player has exited pickup cooldown area.");
            }
            else
            {
                // Debug.Log($"[ItemDrop '{gameObject.name}'] CheckForPlayer: Player has not exited pickup cooldown area yet (Distance: {distance:F2} <= ExitDistance: {exitDistance}). Cannot pickup.");
                return;
            }
        }

        // 检查基础冷却时间
        if (isDroppedByPlayer && Time.time - dropTime < dropCooldownTime)
        {
            // Debug.Log($"[ItemDrop '{gameObject.name}'] CheckForPlayer: Item is in drop cooldown (Time remaining: {dropCooldownTime - (Time.time - dropTime):F2}s). Cannot pickup.");
            return;
        }
        
        if (distance > pickupRange) // ItemDrop自身的检测范围，比玩家的拾取范围大，用于启动吸附
        {
            // Debug.Log($"[ItemDrop '{gameObject.name}'] CheckForPlayer: Player is outside item's general pickupRange (Distance: {distance:F2} > pickupRange: {pickupRange}). Not starting move.");
            return;
        }

        // 只有当玩家在ItemDrop的pickupRange内，才进一步检查玩家的currentPickupRange
        // Debug.Log($"[ItemDrop '{gameObject.name}'] CheckForPlayer: Player is within item's general pickupRange.");

        bool canBeMagnetized = distance <= playerController.currentPickupRange; // 是否在玩家的“磁铁”范围内
        bool inventoryHasSpace = playerController.GetComponent<Inventory>().CanAddItem(item);
        // Debug.Log($"[ItemDrop '{gameObject.name}'] CheckForPlayer: canBeMagnetized: {canBeMagnetized}, inventoryNotFull: {inventoryNotFull}");

        if (canBeMagnetized && inventoryHasSpace)
        {
            // Debug.Log($"[ItemDrop '{gameObject.name}'] CheckForPlayer: Conditions met. Calling StartMovingToPlayer().");
            StartMovingToPlayer(playerController.transform);
        }
        else
        {
            // 之前的日志逻辑，现在只在未能启动移动时触发
            string reason = "";
            if (!canBeMagnetized) reason += $"不在玩家磁铁范围内 (玩家拾取范围: {playerController.currentPickupRange}, 实际距离: {distance:F2}). ";
            if (!inventoryHasSpace) reason += "玩家背包已满. ";
            Debug.Log($"[ItemDrop '{(item != null ? item.itemName : gameObject.name)}'] CheckForPlayer: Cannot start moving to player. Reason: {reason}");
        }
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
        // 显示文本信息
        ShowPickupText();
        // 添加到背包
        if (playerController.GetComponent<Inventory>().TryAddItem(item))
        {
            // 播放拾取音效
            // AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            // 销毁掉落物
            Destroy(gameObject);
        }
    }

    void ShowPickupText()
    {
        if (playerTransform != null)
        {
            int quantity = item != null ? item.quantity : 0;
            string displayText = "";
            if (quantity > 1)
                displayText = $"{item.itemName}({quantity})";
            else
                displayText = $"{item.itemName}";

            // 使用3D文本
            if (PickupText3DManager.Instance != null)
                PickupText3DManager.Instance.ShowPickupText(playerTransform.position, displayText);
            else
                Debug.LogWarning("没有找到文本管理器，无法显示拾取信息");
        }
    }
    
    #endregion
    

    #region 掉落物相关

    void SetupItemDrop()
    {
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

        if (rb != null)
        {
            rb.drag = 0.5f; // 添加一些阻力
            rb.angularDrag = 0.5f;
        }
    }

    public void SetItem(Item newItem)
    {
        item = new Item(newItem);
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            if (item.itemType == ItemType.Tool)
                spriteRenderer.sprite = item.tool?.toolSprite;
            else
                spriteRenderer.sprite = item.tile?.itemSprite;
        }
        gameObject.name = $"ItemDrop_{item.itemName}";
    }

    /// <summary>
    /// 标记此物品为玩家丢弃的物品
    /// </summary>
    public void MarkAsPlayerDropped()
    {
        isDroppedByPlayer = true;
        playerHasExited = false;
        dropTime = Time.time;
    }

    #endregion
    

    #region GUI相关
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

    #endregion

}