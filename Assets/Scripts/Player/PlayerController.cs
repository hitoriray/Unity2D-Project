using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Combat.Weapons; // 新增：为了能引用 StarProjectile
using Utility;        // 新增：为了能引用 ObjectPool
// SoundEffectManager 在全局命名空间，不需要额外 using

public class PlayerController : MonoBehaviour
{
    [Header("依赖")]
    [SerializeField] private CraftingUI craftingUI; // 引用UI控制器

    public LayerMask layerMask;

    public TerrainGeneration terrainGen;

    public GameObject weapon;

    public AudioClip[] digAudios; // 已有的挖掘音效数组
    [Header("Audio")]
    [Tooltip("角色挥剑或攻击时的音效")]
    public AudioClip swordSwingSound;
    // [Tooltip("星星开始降落时的音效")] // 下面这行将被移除，音效移至Weapon.cs
    // public AudioClip starFallSound;

    #region 背包相关的变量

    [Header("Inventory")]
    public Inventory inventory;
    private bool inventoryShowing = false;
    private bool hotbarShowing = true;
    private int selectedSlotIndex = 0;
    public GameObject hotbarSelector;
    public Item selectedItem;

    [Header("Object Pools")]
    [Tooltip("星星投射物的对象池")]
    public ObjectPool starProjectilePool; // 新增星星对象池的引用

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
    private bool isAttacking = false;
    private bool wasAttacking = false;

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

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 如果正在拖拽且要关闭库存，取消拖拽
            if (inventoryShowing && DragManager.Instance != null && DragManager.Instance.IsDragging())
            {
                CancelDrag();
            }
            if (craftingUI.gameObject.activeSelf)
            {
                craftingUI.gameObject.SetActive(!craftingUI.gameObject.activeSelf);
            }

            inventoryShowing = !inventoryShowing;
            hotbarShowing = !hotbarShowing;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            // 这里我们直接开关UI，而不是在CraftingUI里再用一个按键控制
            craftingUI.gameObject.SetActive(!craftingUI.gameObject.activeSelf);
        }
        

        // scoll through hotbar UI
        if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            // scoll up
            if (selectedSlotIndex < inventory.items.width - 1)
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
        if (inventory.hotbarUI != null && inventory.hotbarUI.uiSlots != null && inventory.hotbarUI.uiSlots[selectedSlotIndex, 0] != null)
        {
            hotbarSelector.transform.position = inventory.hotbarUI.uiSlots[selectedSlotIndex, 0].transform.position;
        }
        // set selected item from the last row of the main inventory
        selectedItem = inventory.items?.GetSlot(new Vector2Int(selectedSlotIndex, inventory.items.height - 1))?.item;
        weapon.GetComponent<SpriteRenderer>().sprite = selectedItem?.itemSprite;

        bool isUIDisplayed = DragManager.Instance != null && DragManager.Instance.IsDragging() ||
                             (ItemSplitUI.Instance != null && ItemSplitUI.Instance.IsShowing());
        isUIDisplayed |= GetComponent<WarehouseController>().IsWarehouseUIShowing();
        isUIDisplayed |= craftingUI.gameObject.activeInHierarchy;

        HandleJumpInput();

        if (!isUIDisplayed)
        {
            HandleActionInput();
        }

        inventory.inventoryUI.gameObject.SetActive(inventoryShowing);
        inventory.hotbarUI.gameObject.SetActive(hotbarShowing);
    }

    private void FixedUpdate()
    {
        // --- 最终诊断修改：直接检测按键，绕过输入轴系统 ---
        float horizontal = 0f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            horizontal = 1f;
        }
        else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            horizontal = -1f;
        }
        // --- 修改结束 ---

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

        if (Vector2.Distance(currentPos, mousePos) <= miningRange)
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
        if (tileType == TileType.Air || tileType == TileType.Wall || tileType == TileType.Flower)
        {
            if(tileType == TileType.Flower)
            {
                 terrainGen.RemoveTile(x, y, TileType.Dirt);
            }
            bool succesed = terrainGen.PlaceTile(x, y, defaultTile, itemTileType, "Ground", biomeName);
            if (succesed)
            {
                if (SoundEffectManager.Instance != null && digAudios != null && digAudios.Length > 0)
                    SoundEffectManager.Instance.PlaySoundAtPoint(digAudios[Random.Range(0, digAudios.Length)], transform.position);
                else if (digAudios != null && digAudios.Length > 0) AudioSource.PlayClipAtPoint(digAudios[Random.Range(0, digAudios.Length)], transform.position); // Fallback
                
                // LightingManager.RemoveLightSource(terrainGen, x, y);
                SkyLightManager.OnBlockChanged(x, terrainGen);
                LightingManager.UpdateBlockLighting(terrainGen, x, y);

                // Use the new method to decrease item quantity from the last row of the inventory
                inventory.items.DecreaseItemQuantity(new Vector2Int(selectedSlotIndex, inventory.items.height - 1), 1);
                // Update selectedItem reference as it might have been removed
                selectedItem = inventory.items.GetSlot(new Vector2Int(selectedSlotIndex, inventory.items.height - 1))?.item;
            }
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
            bool succesed = terrainGen.PlaceTile(x, y, defaultWallTile, TileType.Wall, "Wall", biomeName);
            if (succesed)
            {
                if (SoundEffectManager.Instance != null && digAudios != null && digAudios.Length > 0)
                    SoundEffectManager.Instance.PlaySoundAtPoint(digAudios[Random.Range(0, digAudios.Length)], transform.position);
                else if (digAudios != null && digAudios.Length > 0) AudioSource.PlayClipAtPoint(digAudios[Random.Range(0, digAudios.Length)], transform.position); // Fallback
                
                // LightingManager.RemoveLightSource(terrainGen, x, y);
                SkyLightManager.OnBlockChanged(x, terrainGen);
                LightingManager.UpdateBlockLighting(terrainGen, x, y);

                // Use the new method to decrease item quantity from the last row of the inventory
                inventory.items.DecreaseItemQuantity(new Vector2Int(selectedSlotIndex, inventory.items.height - 1), 1);
                // Update selectedItem reference as it might have been removed
                selectedItem = inventory.items.GetSlot(new Vector2Int(selectedSlotIndex, inventory.items.height - 1))?.item;
            }
        }
    }

    #endregion


    #region 移除方块、墙
    private void RemovingTile(Vector2Int pos)
    {
        if (selectedItem != null && selectedItem.itemType == ItemType.Tool)
        {
            // 移除方块
            bool succesed = false;
            if (selectedItem.toolType == ToolType.PickAxe)
            {
                succesed = terrainGen.RemoveTile(pos.x, pos.y, TileType.Dirt);
            }
            else if (selectedItem.toolType == ToolType.Axe)
            {
                succesed = terrainGen.RemoveTile(pos.x, pos.y, TileType.Tree);
            }
            // 移除墙
            else if (selectedItem.toolType == ToolType.Hammer)
            {
                succesed = terrainGen.RemoveTile(pos.x, pos.y, TileType.Wall); // Hammer removal might not need 'succesed' check if it always works
                // Hammer sound is played regardless of RemoveTile success for walls, as it's an action sound.
                if (SoundEffectManager.Instance != null && digAudios != null && digAudios.Length > 0)
                    SoundEffectManager.Instance.PlaySoundAtPoint(digAudios[Random.Range(0, digAudios.Length)], transform.position);
                else if (digAudios != null && digAudios.Length > 0) AudioSource.PlayClipAtPoint(digAudios[Random.Range(0, digAudios.Length)], transform.position); // Fallback
            }
            if (succesed && selectedItem.toolType != ToolType.Hammer) { // Avoid double sound for hammer
                if (SoundEffectManager.Instance != null && digAudios != null && digAudios.Length > 0)
                    SoundEffectManager.Instance.PlaySoundAtPoint(digAudios[Random.Range(0, digAudios.Length)], transform.position);
                else if (digAudios != null && digAudios.Length > 0)
                    AudioSource.PlayClipAtPoint(digAudios[Random.Range(0, digAudios.Length)], transform.position); // Fallback
            }
            // LightingManager.RemoveLightSource(terrainGen, x, y);
            SkyLightManager.OnBlockChanged(pos.x, terrainGen);
            LightingManager.UpdateBlockLighting(terrainGen, pos.x, pos.y);
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
        if (DragManager.Instance != null && DragManager.Instance.IsDragging())
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
                    case ItemType.Weapon:
                        if (selectedItem != null && selectedItem.weapon != null)
                        {
                            if (selectedItem.weapon.isStarfuryWeapon)
                            {
                                isAttacking = true;
                            }
                            else
                            {
                                isAttacking = true;
                            }
                        }
                        else if (selectedItem != null && selectedItem.weapon == null)
                        {
                            Debug.LogWarning($"[PlayerController] HandleActionInput: Selected item '{selectedItem.itemName}' is ItemType.Weapon but has no Weapon scriptable object assigned.");
                            isAttacking = true;
                        }
                        else if (selectedItem == null)
                        {
                             isAttacking = true; // Or handle as an error/no action
                        }
                        else // selectedItem is not null, but not a weapon, or some other case
                        {
                             isAttacking = true; // Default behavior if not handled by other cases like Block/Wall/Tool
                        }
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
            isAttacking = false;
        }
    }

    private void HandleNumberInput()
    {
        if (inventory.items == null) return;
        for (int i = 0; i < inventory.items.width; ++i)
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
        bool uiBlocking = (DragManager.Instance != null && DragManager.Instance.IsDragging()) || (ItemSplitUI.Instance != null && ItemSplitUI.Instance.IsShowing());
        var isPerformingAction = !uiBlocking && (isMining || isPlacing || isPlacingWall || isAttacking);
        var wasPerformingAction = wasMining || wasPlacing || wasAttacking;

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
        wasAttacking = isAttacking;
        wasMoving = isMoving;
    }

    private IEnumerator PlayActionAnimation()
    {
        while ((isMining || isPlacing || isPlacingWall || isAttacking) &&
               !(DragManager.Instance != null && DragManager.Instance.IsDragging()) &&
               (ItemSplitUI.Instance == null || !ItemSplitUI.Instance.IsShowing()))
        {
            // 根据当前动作类型播放对应动画
            if (isAttacking)
            {
                // 播放攻击动画
                string attackAnim = GetAttackAnimationName();
                // if (attackAnim == null) weapon.SetActive(false);
                if (isMoving)
                    spumPrefabs.PlayAnimation(attackAnim); // 考虑移动时的攻击动画是否不同
                else
                    spumPrefabs.PlayAnimation(attackAnim);

                // 播放挥剑音效
                if (swordSwingSound != null)
                {
                    if (SoundEffectManager.Instance != null)
                        SoundEffectManager.Instance.PlaySoundAtPoint(swordSwingSound, transform.position);
                    else AudioSource.PlayClipAtPoint(swordSwingSound, transform.position); // Fallback
                }

                // 如果是星怒，在播放攻击动画的同时（或紧随其后）生成星星
                if (selectedItem != null && selectedItem.weapon != null && selectedItem.weapon.isStarfuryWeapon)
                {
                    PerformStarfuryAttack();
                }
                else if (selectedItem != null && selectedItem.weapon != null && selectedItem.weapon.isZenith)
                {
                    StartCoroutine(PerformZenithAttack());
                }
            }
            else if (isMining || isPlacing || isPlacingWall)
            {
                // 播放挖掘/建造动画
                if (isMoving)
                    spumPrefabs.PlayAnimation("6_Mining_Run");
                else
                    spumPrefabs.PlayAnimation("6_Mining_Idle");

                // 如果是使用工具挖掘 (isMining)，播放挥舞音效 (swordSwingSound)
                if (isMining && selectedItem != null && selectedItem.itemType == ItemType.Tool)
                {
                    if (swordSwingSound != null)
                    {
                        if (SoundEffectManager.Instance != null)
                            SoundEffectManager.Instance.PlaySoundAtPoint(swordSwingSound, transform.position);
                        else AudioSource.PlayClipAtPoint(swordSwingSound, transform.position); // Fallback
                    }
                }
                
                // 挖掘或放置的特定音效 (digAudios) 将在 PlacingTile, PlacingWall, RemovingTile 方法中
                // 成功执行操作时播放，此处不再重复播放以避免每帧/每动画周期播放。
            }

            // 根据动作类型使用不同的等待时间
            if (isAttacking && selectedItem?.weapon != null)
            {
                // 武器攻击使用武器的攻击速度
                yield return new WaitForSeconds(selectedItem.weapon.ActualAttackSpeed);
            }
            else
            {
                // 其他动作使用默认时间
                yield return new WaitForSeconds(actionAnimationDuration);
            }
        }

        _playActionCoroutine = null;

        if (isMoving)
            spumPrefabs.PlayAnimation("run");
        else
            spumPrefabs.PlayAnimation("idle");
    }

    /// <summary>
    /// 获取当前武器的攻击动画名称
    /// </summary>
    private string GetAttackAnimationName()
    {
        if (selectedItem != null && selectedItem.itemType == ItemType.Weapon && selectedItem.weapon != null)
        {
            if (!string.IsNullOrEmpty(selectedItem.weapon.attackAnimationName))
                return selectedItem.weapon.attackAnimationName;
        }
        return null;
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
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.3f, direction * 1.2f);

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position + Vector3.up * 1.5f, direction * 1.2f);

        // 可视化挖矿范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, miningRange);

        // 可视化拾取范围
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, currentPickupRange);

    }

    #endregion


    #region 拖拽管理

    /// <summary>
    /// 取消当前的拖拽操作
    /// </summary>
    private void CancelDrag()
    {
        if (DragManager.Instance != null && DragManager.Instance.IsDragging())
        {
            // Reset the visual state of the dragged slot
            if (InventorySlotUI.draggedSlot != null)
            {
                InventorySlotUI.draggedSlot.ResetSlotVisual();
            }

            // End the drag operation via the manager
            DragManager.Instance.EndDrag();
            
            // Clear the static reference
            InventorySlotUI.draggedSlot = null;
        }
    }

    #endregion

    #region 星怒攻击实现
    void PerformStarfuryAttack()
    {
        if (selectedItem == null) { Debug.LogError("[PlayerController] PerformStarfuryAttack: selectedItem is NULL. Aborting."); return; }
        if (selectedItem.weapon == null) { Debug.LogError($"[PlayerController] PerformStarfuryAttack: selectedItem '{selectedItem.itemName}' has no Weapon data. Aborting."); return; }
        if (!selectedItem.weapon.isStarfuryWeapon) { Debug.LogError($"[PlayerController] PerformStarfuryAttack: Weapon '{selectedItem.weapon.weaponName}' is not marked as Starfury. Aborting."); return; }
        if (selectedItem.weapon.starProjectilePrefab == null) { Debug.LogError($"[PlayerController] PerformStarfuryAttack: Weapon '{selectedItem.weapon.weaponName}' has no StarProjectilePrefab assigned. Aborting."); return; }
        
        Weapon starfuryWeapon = selectedItem.weapon;
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        for (int i = 0; i < starfuryWeapon.numberOfStarsPerAttack; i++)
        {
            // 计算星星应该经过的瞄准点（鼠标位置 + 随机偏移）
            Vector2 randomAimOffset = UnityEngine.Random.insideUnitCircle * starfuryWeapon.starTargetRandomRadius;
            Vector2 aimThroughPoint = mouseWorldPos + randomAimOffset;

            // 计算星星的起始生成位置 (屏幕顶部外侧，与瞄准点的X轴对齐)
            float screenTopWorldY = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 1f)).y;
            float spawnY = screenTopWorldY + starfuryWeapon.starSpawnVerticalOffset;
            Vector2 spawnPos = new Vector2(aimThroughPoint.x, spawnY);

            // 计算星星的最终飞行目标点 (在瞄准点正下方很远的位置)
            // 确保星星是向下飞行的，即使瞄准点在生成点上方 (虽然一般不会这样)
            float veryFarDistance = 200f; // 一个足够大的距离，确保星星会穿过整个屏幕
            Vector2 projectileFinalTargetPos = new Vector2(aimThroughPoint.x, aimThroughPoint.y - veryFarDistance);
            if (spawnPos.y < aimThroughPoint.y) // 如果生成点意外地在瞄准点下方，则反转方向确保向下
            {
                 projectileFinalTargetPos = new Vector2(aimThroughPoint.x, aimThroughPoint.y + veryFarDistance);
            }


            GameObject starInstance;
            if (starProjectilePool != null)
            {
                starInstance = starProjectilePool.GetPooledObject();
            }
            else
            {
                Debug.LogWarning("[PlayerController] PerformStarfuryAttack: StarProjectilePool is NULL. Falling back to Instantiate.");
                starInstance = Instantiate(starfuryWeapon.starProjectilePrefab);
            }

            if (starInstance == null)
            {
                Debug.LogError("[PlayerController] PerformStarfuryAttack: Failed to get or instantiate StarProjectile. Skipping this star.");
                continue;
            }
            
            starInstance.transform.position = spawnPos;
            starInstance.transform.rotation = Quaternion.identity;
            starInstance.SetActive(true);

            // 在星星设置好位置并激活后，播放武器专属的星星降落音效
            if (starfuryWeapon.starSpecificFallSound != null)
            {
                if (SoundEffectManager.Instance != null)
                    SoundEffectManager.Instance.PlaySoundAtPoint(starfuryWeapon.starSpecificFallSound, spawnPos);
                else AudioSource.PlayClipAtPoint(starfuryWeapon.starSpecificFallSound, spawnPos); // Fallback
            }

            StarProjectile starProjectile = starInstance.GetComponent<StarProjectile>();

            if (starProjectile != null)
            {
                // 伤害信息的 hitPoint 应该是玩家瞄准的点 (aimThroughPoint)
                // hitDirection 应该是从生成点到最终目标点的方向
                // 星星的 Initialize 方法接收的是最终的飞行目标点 (projectileFinalTargetPos)
                DamageInfo damageInfo = starfuryWeapon.CreateDamageInfo(gameObject, aimThroughPoint, (projectileFinalTargetPos - spawnPos).normalized);
                starProjectile.Initialize(
                    projectileFinalTargetPos,
                    damageInfo,
                    starProjectilePool,
                    terrainGen, // 传递 TerrainGeneration 引用
                    starfuryWeapon.starCreatesLight,
                    starfuryWeapon.starLightIntensity,
                    starfuryWeapon.starLightRadius,
                    starfuryWeapon.starLightDuration
                );
            }
            else
            {
                Debug.LogError($"[PlayerController] PerformStarfuryAttack: Star instance '{starInstance.name}' is missing StarProjectile script!");
                if (starProjectilePool != null)
                    starProjectilePool.ReturnObjectToPool(starInstance);
                else
                    Destroy(starInstance);
            }
        }

        // 播放星怒武器自身的攻击动画和音效 (如果星怒武器有挥舞动作)
        // if (!string.IsNullOrEmpty(starfuryWeapon.attackAnimationName))
        // {
        //     spumPrefabs?.PlayAnimation(starfuryWeapon.attackAnimationName);
        // }
        // if (starfuryWeapon.attackSound != null)
        // {
        //     AudioSource.PlayClipAtPoint(starfuryWeapon.attackSound, transform.position);
        // }
        
        // 消耗魔法或弹药 (如果星怒武器需要)
        // if (starfuryWeapon.RequiresMana) { /* 扣除魔法值 */ }
        // if (starfuryWeapon.RequiresAmmo) { /* 扣除弹药 */ }
    }

    private IEnumerator PerformZenithAttack()
    {
        var weaponData = selectedItem?.weapon;
        if (weaponData == null || !weaponData.isZenith) yield break;

        if (weaponData.phantomSwordPrefab == null)
        {
            Debug.LogError($"[PlayerController] Zenith weapon '{weaponData.weaponName}' is missing phantomSwordPrefab.");
            yield break;
        }

        // --- 全新的幻影剑列表生成逻辑 ---
        List<SwordAppearance> swordsToSpawn = new List<SwordAppearance>();
        
        // 1. 创建本体剑的外观实例 (假设本体剑的sprite就是武器图标)
        SwordAppearance baseSwordAppearance = new SwordAppearance { swordSprite = weaponData.weaponSprite, trailColor = weaponData.baseSwordTrailColor }; // 使用新的可配置颜色

        // 2. 添加指定数量的本体剑
        for (int i = 0; i < weaponData.baseSwordCount; i++)
        {
            swordsToSpawn.Add(baseSwordAppearance);
        }

        // 3. 添加其他剑，填满剩余的位置
        int remainingSwords = weaponData.totalSwordsPerSwing - weaponData.baseSwordCount;
        if (remainingSwords > 0 && weaponData.swordAppearances != null && weaponData.swordAppearances.Count > 0)
        {
            for (int i = 0; i < remainingSwords; i++)
            {
                // 循环使用外观列表里的剑
                swordsToSpawn.Add(weaponData.swordAppearances[i % weaponData.swordAppearances.Count]);
            }
        }
        
        // 打乱列表，让剑的出现顺序是随机的
        swordsToSpawn.Shuffle();


        Vector2 targetPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        for (int i = 0; i < swordsToSpawn.Count; i++)
        {
            // 在玩家周围随机一个位置生成幻影剑的起点 (这个可以去掉，如果都想从一个点出来的话)
            Vector2 spawnPos = (Vector2)transform.position + Random.insideUnitCircle * 1.5f; 
            
            GameObject swordGO = Instantiate(weaponData.phantomSwordPrefab, spawnPos, Quaternion.identity);
            
            if (swordGO.TryGetComponent<Terraira.Combat.PhantomSword>(out var phantomSword))
            {
                // 创建一个 DamageInfo 实例
                DamageInfo damageInfo = selectedItem.weapon.CreateDamageInfo(gameObject, targetPos, Vector2.zero);
                if (damageInfo.baseDamage != 0)
                {
                    // 调用新的Initialize方法
                    phantomSword.Initialize(damageInfo, transform, targetPos, swordsToSpawn[i]);
                }
            }
            else
            {
                 Debug.LogError($"[PlayerController] Phantom Sword Prefab is missing the PhantomSword script.");
                 Destroy(swordGO);
            }
            
            // 短暂的延迟，让剑一把接一把地出现
            yield return new WaitForSeconds(weaponData.swordFreq);
        }
    }
    #endregion
}

// 在 PlayerController 类的外面，或者一个专门的工具类文件中，添加这个扩展方法
public static class ListExtensions
{
    private static System.Random rng = new System.Random();

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}