using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public LayerMask layerMask;

    public TerrainGeneration terrainGen;

    public GameObject weapon;

    #region 背包相关的变量

    [Header("Inventory")]
    public Inventory inventory;
    private bool inventoryShowing = false;
    private bool hotbarShowing = true;
    private int selectedSlotIndex = 0;
    public GameObject hotbarSelector;
    public Item selectedItem;

    // 公共属性用于外部访问库存状态
    public bool IsInventoryShowing => inventoryShowing;

    #endregion

    #region 范围相关的变量

    [Header("Range Setting")]
    public float miningRange = 5f;
    public float basePickupRange = 1.5f;  // 基础拾取范围
    public float currentPickupRange = 1.5f;  // 当前拾取范围（可被装备修改）

    #endregion

    #region 角色属性相关的变量

    [Header("Player Attributes")]
    public float moveSpeed = 10f;

    public float minJumpForce = 8f;      // 短按跳跃力度
    public float maxJumpForce = 15f;     // 最大跳跃力度
    public float jumpHoldTime = 0.5f;    // 最大按住时间
    public float jumpGraceTime = 0.1f;   // 跳跃缓冲时间

    #endregion

    #region 角色动画相关的变量

    [Header("SPUM Animation")]
    public SPUM_Prefabs spumPrefabs; // SPUM角色预制体组件

    private Rigidbody2D rb;

    private float actionAnimationDuration = 0.5f;
    private Coroutine _playActionCoroutine;

    #endregion

    #region 角色状态、位置相关的变量
    [HideInInspector]
    public Vector2 spawnPos;
    [HideInInspector]
    public Vector2Int mousePos;
    private bool onGround;
    private bool isMoving = false;
    private bool wasMoving = false;
    private bool isMining = false;
    private bool wasMining = false;
    private bool isPlacing = false;
    private bool wasPlacing = false;
    private bool isPlacingWall = false;

    private bool isJumping = false;
    private bool jumpKeyPressed = false;
    private bool jumpKeyReleased = false;
    private float jumpKeyHoldTime = 0f;
    private float jumpStartTime = 0f;

    #endregion



    /* 成员方法 */


    #region 生命周期函数

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        inventory = GetComponent<Inventory>();
    }

    private void Update()
    {
        mousePos.x = Mathf.RoundToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition).x - 0.5f);
        mousePos.y = Mathf.RoundToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition).y - 0.5f);

        if (Input.GetKeyDown(KeyCode.B))
        {
            // 如果正在拖拽且要关闭库存，取消拖拽
            if (inventoryShowing && InventorySlotUI.isDragging)
            {
                CancelDrag();
            }

            inventoryShowing = !inventoryShowing;
            hotbarShowing = !hotbarShowing;
        }

        // ESC键关闭库存界面
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (inventoryShowing)
            {
                // 如果正在拖拽，取消拖拽
                if (InventorySlotUI.isDragging)
                {
                    CancelDrag();
                }

                inventoryShowing = false;
                hotbarShowing = true;
            }
        }

        // scoll through hotbar UI
        if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            // scoll up
            if (selectedSlotIndex < inventory.inventoryWidth - 1)
                ++selectedSlotIndex;

        }
        else if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            // scoll down
            if (selectedSlotIndex > 0)
                --selectedSlotIndex;
        }

        HandleNumberInput();

        // set selected slot UI
        hotbarSelector.transform.position = inventory.hotbarUISlots[selectedSlotIndex].transform.position;
        // set selected item
        selectedItem = inventory.inventorySlots[selectedSlotIndex, inventory.inventoryHeight - 1]?.item;
        weapon.GetComponent<SpriteRenderer>().sprite = selectedItem?.itemSprite;

        HandleJumpInput();
        HandleActionInput();

        inventory.inventoryUI.SetActive(inventoryShowing);
        inventory.hotbarUI.SetActive(hotbarShowing);
    }

    private void FixedUpdate()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");

        Vector3 currentPos = transform.position;
        Vector2 movement = new Vector2(horizontal * moveSpeed, rb.velocity.y);
        Vector3 targetPos = new Vector3(currentPos.x + movement.x * Time.fixedDeltaTime, currentPos.y, currentPos.z);
        if (targetPos.x < 0 || targetPos.x > terrainGen.worldSize)
        {
            movement.x = 0;
        }

        isMoving = Mathf.Abs(horizontal) > 0.1f;

        // 角色翻转
        if (horizontal > 0) transform.localScale = new Vector3(-1.3f, 1.3f, 1.3f);
        else if (horizontal < 0) transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);

        // 处理跳跃逻辑
        HandleJump();

        rb.velocity = new Vector2(movement.x, rb.velocity.y);


        // 防止边缘滑落（可选）
        // PreventEdgeSlipping();

        // 处理SPUM动画
        HandleSPUMAnimation();


        // 检查是否正在拖拽库存物品或显示分割界面
        if (!InventorySlotUI.isDragging &&
            (ItemSplitUI.Instance == null || !ItemSplitUI.Instance.IsShowing()) &&
            Vector2.Distance(currentPos, mousePos) <= miningRange)
        {
            if (isMining)
            {
                RemovingTile(mousePos);
            }
            else if (isPlacing)
            {
                PlacingTile();
            }
            else if (isPlacingWall)
            {
                PlacingWall();
            }
        }
    }

    #endregion


    #region 角色生成

    public void Spawn()
    {
        GetComponent<Transform>().position = spawnPos;
        rb = GetComponent<Rigidbody2D>();

        if (spumPrefabs == null)
        {
            spumPrefabs = GetComponentInChildren<SPUM_Prefabs>();
        }

        spumPrefabs?.PlayAnimation("idle");

        // 初始化拾取范围
        currentPickupRange = basePickupRange;
    }

    #endregion


    #region 放置方块、墙

    void PlacingTile()
    {
        int x = mousePos.x, y = mousePos.y;
        Vector3 playerPos = transform.position;
        int playerCenterX = Mathf.RoundToInt(playerPos.x - 0.5f);
        int playerCenterY = Mathf.RoundToInt(playerPos.y - 0.5f);
        // 玩家占据3*2的空间，检查目标位置是否在玩家身体范围内
        bool inPlayerHorizontalRange = (x >= playerCenterX - 1 && x <= playerCenterX + 1);
        bool inPlayerVerticalRange = (y >= playerCenterY && y <= playerCenterY + 3);
        if (inPlayerHorizontalRange && inPlayerVerticalRange) return;

        TileType itemTileType;
        string biomeName;
        Tile defaultTile;
        if (selectedItem != null &&
            selectedItem.tile != null &&
            selectedItem.quantity > 0 &&
            selectedItem.itemType == ItemType.Block)
        {
            defaultTile = selectedItem.tile;
            itemTileType = selectedItem.tileType;
            biomeName = selectedItem.sourceBiome;
        }
        else
        {
            return;
        }


        if (defaultTile.tileName.ToLower().Contains("tree"))
        {
            Debug.LogWarning("暂不支持树的方块放置功能！");
            return;
        }

        TileType tileType = terrainGen.GetTileType(x, y);
        if (tileType == TileType.Air || tileType == TileType.Wall)
        {
            LightingManager.RemoveLightSource(terrainGen, x, y);
            terrainGen.PlaceTile(x, y, defaultTile, itemTileType, "Ground", biomeName);
            --selectedItem.quantity;
        }
        else if (tileType == TileType.Flower)
        {
            LightingManager.RemoveLightSource(terrainGen, x, y);
            terrainGen.RemoveTile(x, y, TileType.Dirt);
            terrainGen.PlaceTile(x, y, defaultTile, itemTileType, "Ground", biomeName);
            --selectedItem.quantity;
        }
        inventory.UpdateInventoryUI();
        if (selectedItem.quantity == 0)
        {
            inventory.Remove(selectedItem);
            inventory.UpdateInventoryUI();
            selectedItem = null;
        }
    }

    void PlacingWall()
    {
        int x = mousePos.x, y = mousePos.y;
        Tile defaultWallTile;
        string biomeName;
        if (selectedItem != null && selectedItem.itemType == ItemType.Wall)
        {
            defaultWallTile = selectedItem.tile;
            biomeName = selectedItem.sourceBiome;
        }
        else
        {
            return;
        }

        if (terrainGen.GetTileType(x, y) != TileType.Wall)
        {
            LightingManager.RemoveLightSource(terrainGen, x, y);
            terrainGen.PlaceTile(x, y, defaultWallTile, TileType.Wall, "Wall", biomeName);
            --selectedItem.quantity;
        }

        inventory.UpdateInventoryUI();
        if (selectedItem.quantity == 0)
        {
            inventory.Remove(selectedItem);
            inventory.UpdateInventoryUI();
            selectedItem = null;
        }
    }

    #endregion


    #region 移除方块、墙
    private void RemovingTile(Vector2Int pos)
    {
        if (selectedItem != null && selectedItem.itemType == ItemType.Tool)
        {
            // 移除方块
            if (selectedItem.toolType == ToolType.PickAxe)
                terrainGen.RemoveTile(pos.x, pos.y, TileType.Dirt);
            else if (selectedItem.toolType == ToolType.Axe)
                terrainGen.RemoveTile(pos.x, pos.y, TileType.Tree);
            // 移除墙
            else if (selectedItem.toolType == ToolType.Hammer)
                terrainGen.RemoveTile(pos.x, pos.y, TileType.Wall);
        }

    }

    #endregion


    #region 输入处理
    private void HandleJumpInput()
    {
        // 只有在按下空格键的瞬间才设置跳跃标志
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpKeyPressed = true;
            jumpKeyReleased = false;
            jumpKeyHoldTime = 0f;
        }

        // 松开空格键时设置释放标志
        if (Input.GetKeyUp(KeyCode.Space))
        {
            jumpKeyReleased = true;
        }

        // 计算按键持续时间（仅用于跳跃高度控制）
        if (Input.GetKey(KeyCode.Space) && !jumpKeyReleased)
        {
            jumpKeyHoldTime += Time.deltaTime;
        }
    }

    private void HandleActionInput()
    {
        // 检查是否正在拖拽库存物品
        if (InventorySlotUI.isDragging)
        {
            return;
        }

        // 检查是否显示分割界面
        if (ItemSplitUI.Instance != null && ItemSplitUI.Instance.IsShowing())
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (selectedItem != null)
            {
                switch (selectedItem.itemType)
                {
                    case ItemType.Block:
                        isPlacing = true;
                        break;
                    case ItemType.Wall:
                        isPlacingWall = true;
                        break;
                    case ItemType.Tool:
                    default:
                        isMining = true;
                        break;
                }
            }
            else
            {
                // no item selected, default to mining
                isMining = true;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isMining = false;
            isPlacing = false;
            isPlacingWall = false;
        }
    }

    private void HandleNumberInput()
    {
        for (int i = 0; i < inventory.hotbarUISlots.Length; ++i)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                selectedSlotIndex = i;
                break;
            }
        }
    }

    #endregion


    #region 处理跳跃

    private void HandleJump()
    {
        // 开始跳跃
        // 自动跳跃条件：正在移动 + 脚部有障碍 + 头部无障碍
        bool autoJump = isMoving && FootRaycast() && !HeadRaycast();
        if ((jumpKeyPressed || autoJump) && onGround && !isJumping)
        {
            isJumping = true;
            jumpStartTime = Time.time;
            float jumpForce = autoJump ? minJumpForce * 0.6f : minJumpForce;
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpKeyPressed = false;
        }

        // 持续跳跃（长按增加高度）
        if (isJumping && Input.GetKey(KeyCode.Space) && !jumpKeyReleased)
        {
            float jumpTime = Time.time - jumpStartTime;
            // 在允许的时间内且向上移动时，继续施加向上的力
            if (jumpTime < jumpHoldTime && rb.velocity.y > 0)
            {
                // 计算额外的跳跃力度
                float jumpProgress = jumpTime / jumpHoldTime;
                float additionalForce = Mathf.Lerp(0, maxJumpForce - minJumpForce, jumpProgress);
                // 应用额外的向上力
                rb.velocity = new Vector2(rb.velocity.x, minJumpForce + additionalForce * Time.fixedDeltaTime * 10f);
            }
            else
            {
                isJumping = false;
            }
        }

        // 释放跳跃键或达到最大时间时停止跳跃
        if (jumpKeyReleased || (isJumping && Time.time - jumpStartTime >= jumpHoldTime))
        {
            isJumping = false;
            jumpKeyReleased = false;
        }

        // 着地时重置跳跃状态
        if (onGround && rb.velocity.y <= 0)
        {
            isJumping = false;
            // 只有在松开空格键后才重置jumpKeyReleased，防止连跳
            if (jumpKeyReleased || !Input.GetKey(KeyCode.Space))
            {
                jumpKeyReleased = false;
            }
        }
    }

    #endregion


    #region 处理SPUM动画
    private void HandleSPUMAnimation()
    {
        if (spumPrefabs == null) return;

        // 拖拽时或分割界面显示时不播放动作动画
        bool uiBlocking = InventorySlotUI.isDragging || (ItemSplitUI.Instance != null && ItemSplitUI.Instance.IsShowing());
        var isPerformingAction = !uiBlocking && (isMining || isPlacing || isPlacingWall);
        var wasPerformingAction = wasMining || wasPlacing || wasPlacing;

        if (isPerformingAction && !wasPerformingAction)
        {
            if (_playActionCoroutine != null) StopCoroutine(_playActionCoroutine);
            _playActionCoroutine = StartCoroutine(PlayActionAnimation());
        }

        // 拖拽时或分割界面显示时强制停止动作动画
        if (uiBlocking && _playActionCoroutine != null)
        {
            StopCoroutine(_playActionCoroutine);
            _playActionCoroutine = null;
        }

        if (_playActionCoroutine == null && !isPerformingAction)
        {
            if (isMoving)
                spumPrefabs.PlayAnimation("run");
            else
                spumPrefabs.PlayAnimation("idle");
        }

        wasMining = isMining;
        wasPlacing = isPlacing;
        wasMoving = isMoving;
    }

    private IEnumerator PlayActionAnimation()
    {
        while ((isMining || isPlacing || isPlacingWall) &&
               !InventorySlotUI.isDragging &&
               (ItemSplitUI.Instance == null || !ItemSplitUI.Instance.IsShowing()))
        {
            if (isMoving)
                spumPrefabs.PlayAnimation("6_Mining_Run");
            else
                spumPrefabs.PlayAnimation("6_Mining_Idle");

            yield return new WaitForSeconds(actionAnimationDuration);
        }

        _playActionCoroutine = null;

        if (isMoving)
            spumPrefabs.PlayAnimation("run");
        else
            spumPrefabs.PlayAnimation("idle");
    }

    public SPUM_Prefabs GetSpumPrefabs()
    {
        return spumPrefabs;
    }

    #endregion    


    #region 处理拾取范围

    // 设置拾取范围（供装备系统调用）
    public void SetPickupRange(float range)
    {
        currentPickupRange = range;
    }

    // 重置拾取范围到基础值
    public void ResetPickupRange()
    {
        currentPickupRange = basePickupRange;
    }

    // 增加拾取范围（供装备系统调用）
    public void AddPickupRange(float additionalRange)
    {
        currentPickupRange = basePickupRange + additionalRange;
    }

    #endregion


    #region 处理碰撞检测

    private void OnTriggerStay2D(Collider2D col)
    {
        if (col.CompareTag("Ground"))
        {
            onGround = true;
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("Ground"))
        {
            onGround = false;
        }
    }

    private void OnCollisionStay2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Ground"))
        {
            onGround = true;
        }
    }

    void OnCollisionExit2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Ground"))
        {
            onGround = false;
        }
    }

    #endregion


    #region 处理射线检测

    public bool FootRaycast()
    {
        Vector2 direction = transform.localScale.x < 0 ? Vector2.right : Vector2.left;
        RaycastHit2D hit = Physics2D.Raycast(transform.position + Vector3.up * 0.3f, direction, 1.2f, layerMask);
        return hit;
    }

    public bool HeadRaycast()
    {
        Vector2 direction = transform.localScale.x < 0 ? Vector2.right : Vector2.left;
        RaycastHit2D hit = Physics2D.Raycast(transform.position + Vector3.up * 1.5f, direction, 1.2f, layerMask);
        return hit;
    }

    // 防止边缘滑落
    private void PreventEdgeSlipping()
    {
        if (!onGround || isMoving) return;

        // 检测脚下是否有地面支撑
        Vector3 leftFootPos = transform.position + Vector3.left * 0.4f - Vector3.down * 0.1f;
        Vector3 rightFootPos = transform.position + Vector3.right * 0.4f - Vector3.down * 0.1f;

        RaycastHit2D leftFootHit = Physics2D.Raycast(leftFootPos, Vector2.down, 0.5f, layerMask);
        RaycastHit2D rightFootHit = Physics2D.Raycast(rightFootPos, Vector2.down, 0.5f, layerMask);

        // 如果两只脚都没有支撑，停止水平移动
        if (!leftFootHit && !rightFootHit)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
        // 如果只有一只脚有支撑，轻微向有支撑的方向调整
        else if (leftFootHit && !rightFootHit)
        {
            // 右脚悬空，轻微向左调整
            rb.velocity = new Vector2(Mathf.Max(-0.5f, rb.velocity.x), rb.velocity.y);
        }
        else if (!leftFootHit && rightFootHit)
        {
            // 左脚悬空，轻微向右调整
            rb.velocity = new Vector2(Mathf.Min(0.5f, rb.velocity.x), rb.velocity.y);
        }
    }

    private void OnDrawGizmos()
    {
        Vector2 direction = transform.localScale.x < 0 ? Vector2.right : Vector2.left;

        // 绘制脚部检测射线（红色）
        Gizmos.color = Color.red;
        Vector3 footPos = transform.position + Vector3.up * 0.3f;
        Gizmos.DrawRay(footPos, direction * 1.2f);

        // 绘制头部检测射线（蓝色）
        Gizmos.color = Color.blue;
        Vector3 headPos = transform.position + Vector3.up * 1.5f;
        Gizmos.DrawRay(headPos, direction * 1.2f);

        // 绘制边缘检测射线（绿色）
        Gizmos.color = Color.green;
        Vector3 leftFootPos = transform.position + Vector3.left * 0.4f - Vector3.down * 0.1f;
        Vector3 rightFootPos = transform.position + Vector3.right * 0.4f - Vector3.down * 0.1f;
        Gizmos.DrawRay(leftFootPos, Vector3.down * 0.5f);
        Gizmos.DrawRay(rightFootPos, Vector3.down * 0.5f);

        // 绘制起点
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(footPos, 0.1f);
        Gizmos.DrawSphere(headPos, 0.1f);
        Gizmos.DrawSphere(leftFootPos, 0.05f);
        Gizmos.DrawSphere(rightFootPos, 0.05f);

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(transform.position, 0.3f);
    }

    #endregion

    #region 拖拽管理

    /// <summary>
    /// 取消当前的拖拽操作
    /// </summary>
    private void CancelDrag()
    {
        if (InventorySlotUI.isDragging && InventorySlotUI.draggedSlot != null)
        {
            // 重置拖拽槽位的视觉状态
            InventorySlotUI.draggedSlot.ResetSlotVisual();

            // 重置拖拽状态
            InventorySlotUI.isDragging = false;
            InventorySlotUI.draggedSlot = null;

            // 销毁拖拽预览
            if (DragManager.Instance != null)
            {
                DragManager.Instance.EndDrag();
            }
        }
    }

    #endregion

}