using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ItemContainerUI : MonoBehaviour
{
    [Header("UI设置")]
    public Vector2 slotOffset;
    public Vector2 multiplier;
    public GameObject slotPrefab;
    public Transform containerTransform;

    public Button sendAllButton;
    public Button sendSelectedButton;

    [Header("仓库交互")]
    [SerializeField] private WarehouseManager warehouseManager;

    private ItemContainer itemContainer;
    public GameObject[,] uiSlots { get; private set; }
    
    // 选择机制
    private HashSet<Vector2Int> selectedSlots = new HashSet<Vector2Int>();
    
    public void Initialize(ItemContainer container)
    {
        if (itemContainer != null)
        {
            itemContainer.OnItemsChanged -= UpdateUI;
        }

        itemContainer = container;
        
        if (itemContainer != null)
        {
            itemContainer.OnItemsChanged += UpdateUI;
        }
        
        // 自动查找WarehouseManager
        if (warehouseManager == null)
        {
            warehouseManager = FindObjectOfType<WarehouseManager>();
        }

        if (sendAllButton != null)
            sendAllButton.onClick.AddListener(OnSendAllButtonClicked);
        if (sendSelectedButton != null)
            sendSelectedButton.onClick.AddListener(OnSendSelectedButtonClicked);
        
        CreateAndPopulateUI();
    }

    private void OnDestroy()
    {
        if (itemContainer != null)
        {
            itemContainer.OnItemsChanged -= UpdateUI;
        }
    }

    void CreateAndPopulateUI()
    {
        foreach (Transform child in containerTransform)
        {
            Destroy(child.gameObject);
        }
        
        if (itemContainer == null) return;
        
        uiSlots = new GameObject[itemContainer.width, itemContainer.height];

        for (int y = 0; y < itemContainer.height; ++y)
        {
            for (int x = 0; x < itemContainer.width; ++x)
            {
                GameObject slotGO = Instantiate(slotPrefab, containerTransform);
                slotGO.GetComponent<RectTransform>().localPosition = new Vector3((x * multiplier.x) + slotOffset.x, (y * multiplier.y) + slotOffset.y);
                
                InventorySlotUI slotUIComponent = slotGO.GetComponent<InventorySlotUI>();
                if (slotUIComponent != null)
                {
                    slotUIComponent.Initialize(itemContainer, new Vector2Int(x, y));
                    // 设置ItemContainerUI引用，用于选择通信
                    slotUIComponent.SetContainerUI(this);
                }

                uiSlots[x, y] = slotGO;
            }
        }
        UpdateUI();
        UpdateSelectionDisplay(); // 确保选择状态也被正确显示
    }

    public void UpdateUI()
    {
        if (uiSlots == null) return;

        for (int y = 0; y < itemContainer.height; y++)
        {
            for (int x = 0; x < itemContainer.width; x++)
            {
                if (uiSlots[x, y] == null)
                {
                    Debug.LogWarning($"UI slot at [{x},{y}] is null!");
                    continue;
                }

                InventorySlotUI slotUIComponent = uiSlots[x, y].GetComponent<InventorySlotUI>();
                if (slotUIComponent != null)
                {
                    var trueContainer = slotUIComponent.GetAssignedContainer();
                    var truePosition = slotUIComponent.GetAssignedPosition();

                    if (trueContainer != null)
                    {
                        var slot = trueContainer.GetSlot(truePosition);
                        slotUIComponent.UpdateSlotDisplay(slot);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 更新选择状态显示（独立于UpdateUI，避免性能浪费）
    /// </summary>
    private void UpdateSelectionDisplay()
    {
        if (uiSlots == null) return;

        for (int y = 0; y < itemContainer.height; y++)
        {
            for (int x = 0; x < itemContainer.width; x++)
            {
                if (uiSlots[x, y] == null) continue;

                InventorySlotUI slotUIComponent = uiSlots[x, y].GetComponent<InventorySlotUI>();
                if (slotUIComponent != null)
                {
                    var truePosition = slotUIComponent.GetAssignedPosition();
                    bool isSelected = selectedSlots.Contains(truePosition);
                    slotUIComponent.SetSelected(isSelected);
                }
            }
        }
    }

    public ItemContainer GetContainer()
    {
        return itemContainer;
    }
    
    /// <summary>
    /// 切换物品槽的选择状态
    /// </summary>
    public void ToggleSlotSelection(Vector2Int position)
    {
        if (selectedSlots.Contains(position))
        {
            selectedSlots.Remove(position);
        }
        else
        {
            // 检查该位置是否有物品
            var slot = itemContainer.GetSlot(position);
            if (slot != null && slot.item != null)
            {
                selectedSlots.Add(position);
            }
        }
        
        // 只更新这个特定槽位的选择状态
        if (uiSlots != null && position.x < itemContainer.width && position.y < itemContainer.height)
        {
            var slotUI = uiSlots[position.x, position.y]?.GetComponent<InventorySlotUI>();
            if (slotUI != null)
            {
                bool isSelected = selectedSlots.Contains(position);
                slotUI.SetSelected(isSelected);
            }
        }
        
        Debug.Log($"槽位 ({position.x}, {position.y}) 选择状态: {selectedSlots.Contains(position)}");
    }
    
    /// <summary>
    /// 清除所有选择
    /// </summary>
    public void ClearSelection()
    {
        selectedSlots.Clear();
        UpdateSelectionDisplay(); // 只更新选择状态，不刷新整个UI
        Debug.Log("已清除所有选择");
    }
    
    /// <summary>
    /// 将选中的物品发送到仓库
    /// </summary>
    public void SendSelectedItemsToWarehouse()
    {
        if (warehouseManager == null)
        {
            Debug.LogError("WarehouseManager 未找到！无法发送物品到仓库");
            return;
        }
        
        if (selectedSlots.Count == 0)
        {
            Debug.Log("没有选中任何物品");
            return;
        }
        
        int successCount = 0;
        int totalSelected = selectedSlots.Count;
        
        // 创建副本列表以避免迭代过程中修改集合
        var selectedPositions = new List<Vector2Int>(selectedSlots);
        
        foreach (var position in selectedPositions)
        {
            var slot = itemContainer.GetSlot(position);
            if (slot != null && slot.item != null)
            {
                // 创建物品副本
                Item itemCopy = new Item(slot.item);
                
                // 尝试添加到仓库
                if (warehouseManager.AddItemToWarehouse(itemCopy))
                {
                    // 成功添加到仓库，从容器中移除
                    itemContainer.RemoveItem(position.x, position.y);
                    selectedSlots.Remove(position);
                    successCount++;
                    Debug.Log($"成功将 {itemCopy.itemName} x{itemCopy.quantity} 发送到仓库");
                }
                else
                {
                    Debug.LogWarning($"仓库已满，无法存储 {slot.item.itemName}");
                }
            }
        }
        
        Debug.Log($"发送完成：{successCount}/{totalSelected} 个物品成功发送到仓库");
        
        // 如果所有选中物品都成功发送，清空选择状态
        if (selectedSlots.Count == 0)
        {
            Debug.Log("所有选中物品都已成功发送到仓库");
        }
        
        // 物品内容已经通过RemoveItem自动触发了UpdateUI，这里只需要更新选择状态
        UpdateSelectionDisplay();
    }
    
    /// <summary>
    /// 将容器中的所有物品发送到仓库
    /// </summary>
    public void SendAllItemsToWarehouse()
    {
        if (warehouseManager == null)
        {
            Debug.LogError("WarehouseManager 未找到！无法发送物品到仓库");
            return;
        }
        
        if (itemContainer == null)
        {
            Debug.LogError("ItemContainer 未初始化！");
            return;
        }
        
        int successCount = 0;
        int totalItems = 0;
        int failedItems = 0;
        
        // 收集所有有物品的槽位
        List<Vector2Int> itemPositions = new List<Vector2Int>();
        
        for (int y = 0; y < itemContainer.height; y++)
        {
            for (int x = 0; x < itemContainer.width; x++)
            {
                var slot = itemContainer.GetSlot(new Vector2Int(x, y));
                if (slot != null && slot.item != null)
                {
                    itemPositions.Add(new Vector2Int(x, y));
                    totalItems++;
                }
            }
        }
        
        if (totalItems == 0)
        {
            Debug.Log("容器中没有物品可以发送到仓库");
            return;
        }
        
        Debug.Log($"开始发送 {totalItems} 个物品到仓库...");
        
        // 发送所有物品到仓库
        foreach (var position in itemPositions)
        {
            var slot = itemContainer.GetSlot(position);
            if (slot != null && slot.item != null)
            {
                // 创建物品副本
                Item itemCopy = new Item(slot.item);
                
                // 尝试添加到仓库
                if (warehouseManager.AddItemToWarehouse(itemCopy))
                {
                    // 成功添加到仓库，从容器中移除
                    itemContainer.RemoveItem(position.x, position.y);
                    
                    // 如果这个槽位被选中了，也要从选择列表中移除
                    if (selectedSlots.Contains(position))
                    {
                        selectedSlots.Remove(position);
                    }
                    
                    successCount++;
                    Debug.Log($"✓ {itemCopy.itemName} x{itemCopy.quantity} → 仓库");
                }
                else
                {
                    failedItems++;
                    Debug.LogWarning($"✗ 仓库已满，无法存储 {slot.item.itemName} x{slot.item.quantity}");
                }
            }
        }
        
        // 输出最终结果
        if (failedItems == 0)
        {
            Debug.Log($"🎉 全部发送完成！{successCount}/{totalItems} 个物品成功发送到仓库");
        }
        else
        {
            Debug.Log($"⚠️ 部分发送完成：{successCount}/{totalItems} 个物品发送成功，{failedItems} 个物品因仓库已满而失败");
        }
        
        // 更新选择状态显示（因为可能有选中的物品被移除了）
        UpdateSelectionDisplay();
    }

    // 发送选中物品按钮
    public void OnSendSelectedButtonClicked()
    {
        SendSelectedItemsToWarehouse();
    }

    // 发送全部物品按钮  
    public void OnSendAllButtonClicked()
    {
        SendAllItemsToWarehouse();
    }
    
    /// <summary>
    /// 获取当前选中的物品数量
    /// </summary>
    public int GetSelectedItemCount()
    {
        return selectedSlots.Count;
    }
    
    /// <summary>
    /// 检查是否有选中的物品
    /// </summary>
    public bool HasSelectedItems()
    {
        return selectedSlots.Count > 0;
    }
} 