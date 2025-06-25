using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class WarehouseUI : MonoBehaviour
{
    [Header("依赖")]
    [SerializeField] private WarehouseManager warehouseManager;

    [Header("UI组件 - 核心")]
    [SerializeField] private GameObject warehouseWindow;
    [SerializeField] private Transform warehouseListContainer;
    [SerializeField] private GameObject warehouseSlotPrefab;

    [Header("UI组件 - 详情面板")]
    [SerializeField] private TextMeshProUGUI detailItemName;
    [SerializeField] private Image detailItemIcon;
    [SerializeField] private TextMeshProUGUI detailItemDescription;
    [SerializeField] private TextMeshProUGUI detailItemSpecificDescription;
    [SerializeField] private Button moreDetailButton; // 更多详情

    [Header("UI组件 - 取出功能")]
    [SerializeField] private Button takeOutSelectedButton; // 取出选中物品按钮
    [SerializeField] private Button clearSelectionButton; // 清除选择按钮

    [Header("分类按钮")]
    [SerializeField] private List<CategoryButtonUI> categoryButtons;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private TextMeshProUGUI topLeftText;
    [SerializeField] private TextMeshProUGUI topRightText;
    public Button closeButton;
    public int currentNumber;
    public int maxNumber;

    private List<GameObject> itemSlots = new List<GameObject>();
    private Item selectedItem;
    private WarehouseSlotUI selectedSlotUI;
    private ItemType selectedCategory;
    private int currentCatagoryIndex;

    // 选择管理
    private HashSet<WarehouseSlotUI> selectedItemSlots = new HashSet<WarehouseSlotUI>();

    private void Start()
    {
        warehouseWindow.SetActive(false);

        // 动态绑定按钮事件，假设按钮列表顺序与枚举一致
        for (int i = 0; i < categoryButtons.Count; i++)
        {
            int itemTypeIndex = i; // 闭包陷阱
            categoryButtons[i].GetComponent<Button>().onClick.AddListener(() => ShowItemsForCategory((ItemType)itemTypeIndex));
        }

        moreDetailButton.onClick.AddListener(OnMoreDetailButtonClicked);

        // 绑定左右切换按钮事件
        leftButton.onClick.AddListener(() => ChangeCategory(-1));
        rightButton.onClick.AddListener(() => ChangeCategory(1));

        // 绑定取出功能按钮
        if (takeOutSelectedButton != null)
            takeOutSelectedButton.onClick.AddListener(TakeOutSelectedItems);
        if (clearSelectionButton != null)
            clearSelectionButton.onClick.AddListener(ClearSelection);

        // 默认显示第一个分类
        ShowItemsForCategory(ItemType.Weapon);
    }

    public void ShowItemsForCategory(ItemType itemType)
    {
        selectedCategory = itemType;
        currentCatagoryIndex = (int)itemType;
        topLeftText.text = GetStringByItemType(itemType);
        string str = GetStringByItemType(itemType);
        str += "   " + warehouseManager.GetWarehouseSizeForCategory(itemType).ToString() + "/" + warehouseManager.GetWarehouseCapacity().ToString();
        topRightText.text = str;

        // 更新分类按钮的选中状态
        for (int i = 0; i < categoryButtons.Count; i++)
        {
            if (i == (int)itemType)
            {
                categoryButtons[i].Select();
            }
            else
            {
                categoryButtons[i].Deselect();
            }
        }

        // 清除之前的选择（因为要切换分类了）
        ClearSelection();

        foreach (GameObject slot in itemSlots)
        {
            Destroy(slot);
        }
        itemSlots.Clear();
        selectedSlotUI = null;

        List<Item> filteredItems = warehouseManager.GetAllItemsForCategory(itemType);

        foreach (Item item in filteredItems)
        {
            GameObject newSlot = Instantiate(warehouseSlotPrefab, warehouseListContainer);
            newSlot.GetComponent<WarehouseSlotUI>().Initialize(this, item, itemType);
            itemSlots.Add(newSlot);
        }

        if (itemSlots.Count > 0)
        {
            itemSlots[0].GetComponent<Button>().onClick.Invoke();
        }
        else
        {
            ClearDetailPanel();
        }

        UpdateArrowButtonsState();
    }

    private void ChangeCategory(int direction)
    {
        int newIndex = currentCatagoryIndex + direction;

        // 确保索引在有效范围内
        if (newIndex >= 0 && newIndex < categoryButtons.Count)
        {
            ShowItemsForCategory((ItemType)newIndex);
        }
    }

    private void UpdateArrowButtonsState()
    {
        leftButton.interactable = currentCatagoryIndex > 0;
        rightButton.interactable = currentCatagoryIndex < categoryButtons.Count - 1;
    }

    public void OnItemSelected(WarehouseSlotUI slotUI, Item item, ItemType itemType)
    {
        // 清除之前的详情选中状态
        if (selectedSlotUI != null)
        {
            selectedSlotUI.SetDetailSelected(false);
        }

        // 设置新的详情选中状态
        selectedSlotUI = slotUI;
        selectedSlotUI.SetDetailSelected(true);

        selectedItem = item;
        if (itemType == ItemType.Tool && item.tool != null)
            item = new Item(item.tool);
        else if (itemType == ItemType.Weapon && item.weapon != null)
            item = new Item(item.weapon);
        else if (itemType == ItemType.Block && item.tile != null)
            item = new Item(item.tile);

        detailItemName.text = item.itemName;
        detailItemIcon.sprite = item.itemSprite;
        detailItemDescription.text = item.description;
        detailItemSpecificDescription.text = item.specificDescription;
        moreDetailButton.interactable = true;
    }

    /// <summary>
    /// 切换物品的选择状态（用于批量取出）
    /// </summary>
    public void ToggleItemSelection(WarehouseSlotUI slotUI, Item item)
    {
        if (selectedItemSlots.Contains(slotUI))
        {
            selectedItemSlots.Remove(slotUI);
            slotUI.SetBatchSelected(false);
            Debug.Log($"取消选择批量取出: {item.itemName}");
        }
        else
        {
            selectedItemSlots.Add(slotUI);
            slotUI.SetBatchSelected(true);
            Debug.Log($"选择批量取出: {item.itemName}");
        }

        Debug.Log($"当前选中物品数量: {selectedItemSlots.Count}");
    }

    /// <summary>
    /// 清除所有批量取出选择（不影响详情选中）
    /// </summary>
    public void ClearSelection()
    {
        foreach (var slotUI in selectedItemSlots)
        {
            if (slotUI != null)
            {
                slotUI.SetBatchSelected(false);
            }
        }
        selectedItemSlots.Clear();
        Debug.Log("已清除所有批量取出选择");
    }

    /// <summary>
    /// 取出选中的物品到背包
    /// </summary>
    public void TakeOutSelectedItems()
    {
        if (selectedItemSlots.Count == 0)
        {
            Debug.Log("没有选中任何物品");
            return;
        }

        // 获取玩家背包
        Inventory playerInventory = warehouseManager.GetInventory();
        if (playerInventory == null)
        {
            Debug.LogError("找不到玩家背包！");
            return;
        }

        int successCount = 0;
        int totalSelected = selectedItemSlots.Count;
        var selectedSlotsList = new List<WarehouseSlotUI>(selectedItemSlots);

        foreach (var slotUI in selectedSlotsList)
        {
            if (slotUI == null) continue;

            Item item = slotUI.GetCurrentItem();
            if (item == null) continue;

            // 检查背包是否有空间
            if (playerInventory.CanAddItem(item))
            {
                // 创建物品副本
                Item itemCopy = new Item(item);

                // 添加到背包
                if (playerInventory.TryAddItem(itemCopy))
                {
                    // 从仓库中移除物品
                    if (warehouseManager.RemoveItemFromWarehouse(item))
                    {
                        successCount++;
                        selectedItemSlots.Remove(slotUI);
                        Debug.Log($"✓ {item.itemName} x{item.quantity} → 背包");
                    }
                    else
                    {
                        Debug.LogWarning($"从仓库移除 {item.itemName} 失败");
                    }
                }
                else
                {
                    Debug.LogWarning($"添加 {item.itemName} 到背包失败");
                }
            }
            else
            {
                Debug.LogWarning($"背包空间不足，无法取出 {item.itemName}");
            }
        }

        // 输出结果
        if (successCount == totalSelected)
        {
            Debug.Log($"🎉 全部取出成功！{successCount}/{totalSelected} 个物品已取出到背包");
        }
        else
        {
            Debug.Log($"⚠️ 部分取出成功：{successCount}/{totalSelected} 个物品取出成功");
        }

        // 刷新当前分类显示
        ShowItemsForCategory(selectedCategory);
    }

    private void OnMoreDetailButtonClicked()
    {
        if (selectedItem != null)
        {
            warehouseManager.ShowMoreDetail(selectedItem);
            OnItemSelected(selectedSlotUI, selectedItem, selectedCategory);
        }
    }

    private void ClearDetailPanel()
    {
        selectedItem = null;
        selectedSlotUI = null;
        detailItemName.text = "请选择一个物品";
        detailItemDescription.text = "";
        detailItemSpecificDescription.text = "";
        moreDetailButton.interactable = false;
    }

    private string GetStringByItemType(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Weapon: return "武器";
            case ItemType.Tool: return "工具";
            case ItemType.Consumable: return "消耗品";
            case ItemType.Block: return "方块";
            case ItemType.Wall: return "墙";
            case ItemType.Material: return "材料";
        }
        return "";
    }
}