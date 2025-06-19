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
        if (selectedSlotUI != null)
        {
            selectedSlotUI.Deselect();
        }

        selectedSlotUI = slotUI;
        selectedSlotUI.Select();

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
        Debug.Log("desc: " + detailItemDescription.text);
        detailItemSpecificDescription.text = item.specificDescription;
        moreDetailButton.interactable = true;
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
        detailItemName.text = "";
        detailItemIcon.sprite = null;
        detailItemDescription.text = "请选择一个物品";
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