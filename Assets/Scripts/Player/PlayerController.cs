using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public TerrainGeneration terrainGen;

    #region 背包相关的变量

    [Header("Inventory")]
    public Inventory inventory;
    private bool inventoryShowing = false;
    private bool hotbarShowing = true;
    private int selectedSlotIndex = 0;
    public GameObject hotbarSelector;
    public Item selectedItem;

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
            --selectedItem.quantity;
            inventory.UpdateInventoryUI();
            if (selectedItem.quantity == 0)
            {
                inventory.Remove();
                inventory.UpdateInventoryUI();
                selectedItem = null;
            }
        }
        else
        {
            return;
        }


        if (defaultTile.tileName.ToLower().Contains("tree")) {
            Debug.LogWarning("暂不支持树的方块放置功能！");
            return;
        }

        TileType tileType = terrainGen.GetTileType(x, y);
        if (tileType == TileType.Air || tileType == TileType.Wall)
        {
            LightingManager.RemoveLightSource(terrainGen, x, y);
            terrainGen.PlaceTile(x, y, defaultTile, itemTileType, "Ground", biomeName);
        }
        else if (tileType == TileType.Flower)
        {
            LightingManager.RemoveLightSource(terrainGen, x, y);
            terrainGen.RemoveTile(x, y);
            terrainGen.PlaceTile(x, y, defaultTile, itemTileType, "Ground", biomeName);
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
            --selectedItem.quantity;
            inventory.UpdateInventoryUI();
            if (selectedItem.quantity == 0)
            {
                inventory.Remove();
                inventory.UpdateInventoryUI();
                selectedItem = null;
            }
        }
        else
        {
            defaultWallTile = terrainGen.GetCurrentBiome(x, y).tileAtlas.wall;
            biomeName = terrainGen.GetCurrentBiome(x, y).biomeName;
        }

        if (terrainGen.GetTileType(x, y) != TileType.Wall)
        {
            LightingManager.RemoveLightSource(terrainGen, x, y);
            terrainGen.PlaceTile(x, y, defaultWallTile, TileType.Wall, "Wall", biomeName);
        }
    }

    #endregion


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
            inventoryShowing = !inventoryShowing;
            hotbarShowing = !hotbarShowing;
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
        // set selected slot UI
        hotbarSelector.transform.position = inventory.hotbarUISlots[selectedSlotIndex].transform.position;
        // set selected item
        selectedItem = inventory.inventorySlots[selectedSlotIndex, inventory.inventoryHeight - 1]?.item;

        HandleJumpInput();
        HandleMiningInput();
        HandlePlacingTileInput();
        HandlePlacingWallInput();

        inventory.inventoryUI.SetActive(inventoryShowing);
        inventory.hotbarUI.SetActive(hotbarShowing);
    }

    private void FixedUpdate()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");

        Vector3 currentPos = transform.position;
        Vector2 movement = new Vector2(horizontal * moveSpeed, rb.velocity.y);
        Vector3 targetPos = new Vector3(currentPos.x + movement.x * Time.fixedDeltaTime, currentPos.y, currentPos.z);
        if (targetPos.x < 0 || targetPos.x > terrainGen.worldSize) {
            movement.x = 0;
        }

        isMoving = Mathf.Abs(horizontal) > 0.1f;

        // 角色翻转
        if (horizontal > 0) transform.localScale = new Vector3(-1.3f, 1.3f, 1.3f);
        else if (horizontal < 0) transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);

        // 处理跳跃逻辑
        HandleJump();

        rb.velocity = new Vector2(movement.x, rb.velocity.y);

        // 处理SPUM动画
        HandleSPUMAnimation();


        if (Vector2.Distance(currentPos, mousePos) <= miningRange)
        {
            if (isMining)
            {
                terrainGen.RemoveTile(mousePos.x, mousePos.y);
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


    #region 输入处理
    private void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpKeyPressed = true;
            jumpKeyReleased = false;
            jumpKeyHoldTime = 0f;
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            jumpKeyReleased = true;
        }

        // 计算按键持续时间
        if (Input.GetKey(KeyCode.Space) && !jumpKeyReleased)
        {
            jumpKeyHoldTime += Time.deltaTime;
        }
    }

    private void HandleMiningInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isMining = true;
        }
        if (Input.GetMouseButtonUp(0))
        {
            isMining = false;
        }
    }

    private void HandlePlacingTileInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isPlacing = true;
        }
        if (Input.GetMouseButtonUp(1))
        {
            isPlacing = false;
        }
    }
    private void HandlePlacingWallInput()
    {
        if (Input.GetMouseButtonDown(2))
        {
            isPlacingWall = true;
        }
        if (Input.GetMouseButtonUp(2))
        {
            isPlacingWall = false;
        }
    }
    
    #endregion


    #region 处理跳跃
    
    private void HandleJump()
    {
        // 开始跳跃
        if (jumpKeyPressed && onGround)
        {
            isJumping = true;
            jumpStartTime = Time.time;
            // 初始跳跃力度（最小跳跃）
            rb.velocity = new Vector2(rb.velocity.x, minJumpForce);
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
            jumpKeyReleased = false;
        }
    }

    #endregion


    #region 处理SPUM动画
    private void HandleSPUMAnimation()
    {
        if (spumPrefabs == null) return;

        if (isMining != wasMining || isPlacing != wasPlacing || isMoving != wasMoving)
        {
            // 根据挖矿、放置和移动的组合状态播放相应动画
            if ((isMining || isPlacing) && isMoving)
            {
                spumPrefabs.PlayAnimation("6_Mining_Run");
            }
            else if ((isMining || isPlacing) && !isMoving)
            {
                spumPrefabs.PlayAnimation("6_Mining_Idle");
            }
            else if (!isMining && !isPlacing && isMoving)
            {
                spumPrefabs.PlayAnimation("run");
            }
            else
            {
                spumPrefabs.PlayAnimation("idle");
            }

            wasMining = isMining;
            wasPlacing = isPlacing;
            wasMoving = isMoving;
        }
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
}