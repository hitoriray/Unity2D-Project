using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    [Header("工具")]
    public Tool PickupAxe;  // 镐
    public Tool Axe;        // 斧
    public Tool Hammer;     // 锤

    [Header("UI设置")]
    public Vector2 inventoryOffset;
    public Vector2 hotbarOffset;
    public Vector2 multiplier;
    public GameObject inventoryUI;
    public GameObject hotbarUI;
    public GameObject inventorySlotPrefab;
    public GameObject hotbarSlotPrefab;

    [Header("拖拽设置")]
    public GameObject itemDropPrefab; // ItemDrop预制体引用

    [Header("库存设置")]
    public int inventoryWidth;
    public int inventoryHeight;
    public InventorySlot[,] inventorySlots;
    public InventorySlot[] hotbarSlots;
    public GameObject[,] inventoryUISlots;
    public GameObject[] hotbarUISlots;


    #region 生命周期函数
    private void Start()
    {
        inventorySlots = new InventorySlot[inventoryWidth, inventoryHeight];
        inventoryUISlots = new GameObject[inventoryWidth, inventoryHeight];
        hotbarSlots = new InventorySlot[inventoryWidth];
        hotbarUISlots = new GameObject[inventoryWidth];


        SetupUI();
        UpdateInventoryUI();
        
        Add(new Item(PickupAxe));
        Add(new Item(Axe));
        Add(new Item(Hammer));
    }

    #endregion


    #region UI设置、更新

    void SetupUI()
    {
        // setup inventory
        for (int x = 0; x < inventoryWidth; ++x)
            for (int y = 0; y < inventoryHeight; ++y)
            {
                GameObject inventorySlot = Instantiate(inventorySlotPrefab, inventoryUI.transform.GetChild(0).transform);
                inventorySlot.GetComponent<RectTransform>().localPosition = new Vector3((x * multiplier.x) + inventoryOffset.x, (y * multiplier.y) + inventoryOffset.y);

                // 添加拖拽功能组件
                InventorySlotUI slotUI = inventorySlot.GetComponent<InventorySlotUI>();
                if (slotUI == null)
                    slotUI = inventorySlot.AddComponent<InventorySlotUI>();
                slotUI.Initialize(new Vector2Int(x, y), this, false);

                inventoryUISlots[x, y] = inventorySlot;
                inventorySlots[x, y] = null;
            }
        // setup hotbar
        for (int x = 0; x < inventoryWidth; ++x)
        {
            GameObject hotbarSlot = Instantiate(hotbarSlotPrefab, hotbarUI.transform.GetChild(0).transform);
            hotbarSlot.GetComponent<RectTransform>().localPosition = new Vector3((x * multiplier.x) + hotbarOffset.x, hotbarOffset.y);

            // 添加拖拽功能组件
            InventorySlotUI slotUI = hotbarSlot.GetComponent<InventorySlotUI>();
            if (slotUI == null)
                slotUI = hotbarSlot.AddComponent<InventorySlotUI>();
            slotUI.Initialize(new Vector2Int(x, 0), this, true);

            hotbarUISlots[x] = hotbarSlot;
            hotbarSlots[x] = null;
        }
    }

    public void UpdateInventoryUI()
    {
        // update inventory
        for (int x = 0; x < inventoryWidth; ++x)
            for (int y = 0; y < inventoryHeight; ++y)
            {
                if (inventoryUISlots[x, y] == null)
                {
                    Debug.LogWarning($"库存UI槽位[{x},{y}]为null！");
                    continue;
                }

                UpdateSlotUI(inventoryUISlots[x, y], inventorySlots[x, y], x, y, false);
            }

        // update hotbar
        int col = inventoryHeight - 1;
        for (int x = 0; x < inventoryWidth; ++x)
        {
            if (hotbarUISlots[x] == null)
            {
                Debug.LogWarning($"热键栏UI槽位[{x}]为null！");
                continue;
            }

            UpdateSlotUI(hotbarUISlots[x], inventorySlots[x, col], x, col, true);
        }

    }

    // 更新单个槽位UI
    private void UpdateSlotUI(GameObject slotUI, InventorySlot slot, int x, int y, bool isHotbar)
    {
        if (slotUI == null) return;

        var imageComponent = slotUI.transform.GetChild(0).GetComponent<Image>();
        var textComponent = slotUI.transform.GetChild(1).GetComponent<TextMeshProUGUI>();

        if (slot == null || slot.item == null)
        {
            // 空槽位
            if (imageComponent != null)
            {
                imageComponent.sprite = null;
                imageComponent.enabled = false;
            }
            if (textComponent != null)
            {
                textComponent.text = "";
                textComponent.enabled = false;
            }
        }
        else
        {
            // 有物品的槽位
            if (imageComponent != null)
            {
                imageComponent.enabled = true;
                imageComponent.sprite = slot.item.itemSprite;
            }
            if (textComponent != null)
            {
                textComponent.enabled = true;
                if (slot.item.quantity == 1)
                    textComponent.text = "";
                else
                    textComponent.text = slot.item.quantity.ToString();
            }
        }
    }

    #endregion


    #region 增、删、查
    public bool Add(Item item)
    {
        if (item == null || item.quantity <= 0) return false;
        Vector2Int? firstEmptySlot = null;
        for (int y = inventoryHeight - 1; y >= 0; --y)
        {
            for (int x = 0; x < inventoryWidth; ++x)
            {
                if (inventorySlots[x, y] != null && inventorySlots[x, y].item.CanStackWith(item))
                {
                    int remain = inventorySlots[x, y].TryStack(item);
                    item.quantity = remain;
                    if (remain == 0)
                    {
                        UpdateInventoryUI();
                        return true;
                    }
                }

                if (inventorySlots[x, y] == null && !firstEmptySlot.HasValue)
                {
                    firstEmptySlot = new Vector2Int(x, y);
                }
            }
        }

        if (item.quantity > 0 && firstEmptySlot.HasValue)
        {
            Vector2Int pos = firstEmptySlot.Value;
            inventorySlots[pos.x, pos.y] = new InventorySlot
            {
                position = pos,
                item = new Item(item),
            };

            item.quantity = 0;
            UpdateInventoryUI();
            return true;
        }
        return false;
    }

    public void Remove(Item item)
    {
        Vector2Int pos = Contains(item);
        if (pos != Vector2Int.one * -1)
        {
            inventorySlots[pos.x, pos.y] = null;
            UpdateInventoryUI();
        }
    }

    public Vector2Int Contains(Item item) {
        for (int y = inventoryHeight - 1; y >= 0; --y)
        {
            for (int x = 0; x < inventoryWidth; ++x)
            {
                if (inventorySlots[x, y] != null && 
                    inventorySlots[x, y].item.itemName == item.itemName && 
                    inventorySlots[x, y].item.quantity == item.quantity)
                    return new Vector2Int(x, y);
            }
        }
        return Vector2Int.one * -1;
    }

    public bool IsFull(Item item) {
        for (int y = inventoryHeight - 1; y >= 0; --y)
        {
            for (int x = 0; x < inventoryWidth; ++x)
            {
                if (inventorySlots[x, y] == null) return false;
                if (inventorySlots[x, y].item.itemName == item.itemName && 
                    inventorySlots[x, y].item.quantity < inventorySlots[x, y].item.maxStackSize)
                    return false;
            }
        }
        return true;
    }

    #endregion


    #region 拖拽相关功能

    // 交换两个槽位的物品
    public void SwapItems(Vector2Int fromPos, bool fromIsHotbar, Vector2Int toPos, bool toIsHotbar)
    {
        InventorySlot fromSlot = GetSlot(fromPos, fromIsHotbar);
        InventorySlot toSlot = GetSlot(toPos, toIsHotbar);

        // 如果源槽位没有物品，不执行操作
        if (fromSlot == null || fromSlot.item == null)
        {
            Debug.LogWarning("源槽位为空，取消交换");
            return;
        }

        // 如果目标槽位为空，直接移动
        if (toSlot == null || toSlot.item == null)
        {
            MoveItem(fromPos, fromIsHotbar, toPos, toIsHotbar);
            return;
        }

        // 如果两个物品可以堆叠，尝试堆叠
        if (fromSlot.item.CanStackWith(toSlot.item))
        {
            int remainingQuantity = toSlot.TryStack(fromSlot.item);
            if (remainingQuantity == 0)
            {
                // 完全堆叠，清空源槽位
                SetSlot(fromPos, fromIsHotbar, null);
            }
            else
            {
                // 部分堆叠，更新源槽位数量
                fromSlot.item.quantity = remainingQuantity;
            }
        }
        else
        {
            // 不能堆叠，执行交换
            Item tempItem = new Item(fromSlot.item);
            fromSlot.item = new Item(toSlot.item);
            toSlot.item = tempItem;
        }

        StartCoroutine(ForceUIRefresh());
    }

    // 移动物品到指定槽位
    private void MoveItem(Vector2Int fromPos, bool fromIsHotbar, Vector2Int toPos, bool toIsHotbar)
    {
        InventorySlot fromSlot = GetSlot(fromPos, fromIsHotbar);
        if (fromSlot == null || fromSlot.item == null)
        {
            Debug.LogWarning("移动失败：源槽位为空");
            return;
        }

        // 创建新的槽位
        InventorySlot newSlot = new InventorySlot
        {
            position = toPos,
            item = new Item(fromSlot.item)
        };

        // 设置到目标位置
        SetSlot(toPos, toIsHotbar, newSlot);
        // 清空源位置
        SetSlot(fromPos, fromIsHotbar, null);

        StartCoroutine(ForceUIRefresh());
    }

    // 丢弃物品到世界中
    public void DropItem(Vector2Int pos, bool isHotbar)
    {
        InventorySlot slot = GetSlot(pos, isHotbar);
        if (slot == null || slot.item == null) return;

        // 获取玩家位置 - 多种方式查找
        Vector3 dropPosition = GetPlayerDropPosition();
        CreateItemDrop(slot.item, dropPosition);

        // 清空槽位
        SetSlot(pos, isHotbar, null);
        UpdateInventoryUI();
    }

    // 获取玩家丢弃位置
    private Vector3 GetPlayerDropPosition()
    {
        // 通过当前Inventory组件的GameObject（通常挂在Player上）
        Vector3 playerPosition = transform.position;

        // 如果上面获取不到，尝试查找Player标签
        if (playerPosition == Vector3.zero)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerPosition = player.transform.position;
            }
        }

        // 如果还是不行，尝试查找PlayerController组件
        if (playerPosition == Vector3.zero)
        {
            PlayerController playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                playerPosition = playerController.transform.position;
            }
        }

        // 如果都找不到，使用默认位置并警告
        if (playerPosition == Vector3.zero)
        {
            Debug.LogWarning("找不到玩家位置！物品将在原点丢弃。请确保玩家有Player标签或PlayerController组件。");
            playerPosition = Vector3.zero;
        }

        // 在玩家位置上方一点丢弃
        return playerPosition + Vector3.up * 0.5f;
    }

    // 创建掉落物
    private void CreateItemDrop(Item item, Vector3 position)
    {
        // 使用直接引用的ItemDrop预制体
        if (itemDropPrefab == null)
        {
            Debug.LogError("ItemDrop预制体未设置！请在Inventory组件中拖拽Assets/Prefabs/Item.prefab到ItemDropPrefab字段");
            return;
        }

        GameObject dropObject = Instantiate(itemDropPrefab, position, Quaternion.identity);
        ItemDrop itemDrop = dropObject.GetComponent<ItemDrop>();

        if (itemDrop != null)
        {
            itemDrop.SetItem(item);
            // 标记为玩家丢弃的物品
            itemDrop.MarkAsPlayerDropped();

            // 添加一个小的随机力，让物品散开
            Rigidbody2D rb = dropObject.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 randomForce = new Vector2(
                    Random.Range(-2f, 2f),
                    Random.Range(1f, 3f)
                );
                rb.AddForce(randomForce, ForceMode2D.Impulse);
            }
        }
    }

    // 获取指定位置的槽位
    private InventorySlot GetSlot(Vector2Int pos, bool isHotbar)
    {
        if (isHotbar)
        {
            if (pos.x >= 0 && pos.x < hotbarSlots.Length)
            {
                return hotbarSlots[pos.x];
            }
            else
            {
                Debug.LogWarning($"热键栏索引越界: {pos.x}, 长度: {hotbarSlots.Length}");
            }
        }
        else
        {
            if (pos.x >= 0 && pos.x < inventoryWidth && pos.y >= 0 && pos.y < inventoryHeight)
            {
                return inventorySlots[pos.x, pos.y];
            }
            else
            {
                Debug.LogWarning($"库存索引越界: ({pos.x},{pos.y}), 大小: ({inventoryWidth},{inventoryHeight})");
            }
        }
        return null;
    }

    // 设置指定位置的槽位
    private void SetSlot(Vector2Int pos, bool isHotbar, InventorySlot slot)
    {
        if (isHotbar)
        {
            if (pos.x >= 0 && pos.x < hotbarSlots.Length)
            {
                hotbarSlots[pos.x] = slot;
            }
            else
            {
                Debug.LogWarning($"设置热键栏失败，索引越界: {pos.x}");
            }
        }
        else
        {
            if (pos.x >= 0 && pos.x < inventoryWidth && pos.y >= 0 && pos.y < inventoryHeight)
            {
                inventorySlots[pos.x, pos.y] = slot;
            }
            else
            {
                Debug.LogWarning($"设置库存失败，索引越界: ({pos.x},{pos.y})");
            }
        }
    }

    // 强制UI刷新协程
    private IEnumerator ForceUIRefresh()
    {
        yield return null; // 等待一帧
        UpdateInventoryUI();
    }

    public void SplitItem(Vector2Int pos, bool isHotbar, int splitAmount)
    {
        InventorySlot sourceSlot = GetSlot(pos, isHotbar);
        if (sourceSlot == null || sourceSlot.item == null || splitAmount <= 0 || splitAmount >= sourceSlot.item.quantity)
        {
            Debug.LogWarning("分割失败：无效的分割数量或源槽位");
            return;
        }

        Item splitItem = new Item(sourceSlot.item);
        splitItem.quantity = splitAmount;
        sourceSlot.item.quantity -= splitAmount;
        Vector2Int emptySlot = FindEmptySlot();
        if (emptySlot.x != -1)
        {
            // 找到空槽位，放置分割的物品
            InventorySlot newSlot = new InventorySlot
            {
                position = emptySlot,
                item = splitItem
            };
            SetSlot(emptySlot, false, newSlot);
        }
        else
        {
            // 没有空槽位，在玩家位置丢弃分割的物品
            Vector3 dropPosition = GetPlayerDropPosition();
            CreateItemDrop(splitItem, dropPosition);
        }

        UpdateInventoryUI();
    }

    // 查找空的库存槽位
    private Vector2Int FindEmptySlot()
    {
        // 按照指定顺序检查主库存：从下到上，从左到右
        for (int y = inventoryHeight - 1; y >= 0; --y)
        {
            for (int x = 0; x < inventoryWidth; ++x)
            {
                if (inventorySlots[x, y] == null || inventorySlots[x, y].item == null)
                {
                    return new Vector2Int(x, y);
                }
            }
        }

        // 没找到空槽位
        return new Vector2Int(-1, -1);
    }

    #endregion

}
