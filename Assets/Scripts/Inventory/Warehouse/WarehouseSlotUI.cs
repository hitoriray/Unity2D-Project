using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class WarehouseSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI组件")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemQuantity;
    [SerializeField] private Button button;
    [SerializeField] private Image detailSelectionHighlight; // 详情选中高亮（蓝色边框）
    [SerializeField] private Image batchSelectionHighlight; // 批量取出选中高亮（绿色背景）

    [Header("选择状态颜色")]
    public Color detailSelectedColor = new Color(0f, 0.5f, 1f, 0.8f); // 蓝色边框，用于详情选中
    public Color batchSelectedColor = new Color(0f, 1f, 0f, 0.3f); // 绿色半透明背景，用于批量取出选中

    private Item currentItem;
    private WarehouseUI warehouseUI; // 对主UI的引用
    private ItemType itemType;
    private bool isDetailSelected = false; // 详情选中状态
    private bool isBatchSelected = false; // 批量取出选中状态

    public void Initialize(WarehouseUI ui, Item item, ItemType type)
    {
        warehouseUI = ui;
        currentItem = item;
        this.itemType = type;
        
        switch (type)
        {
            case ItemType.Weapon:
                if (item.weapon != null)
                    itemIcon.sprite = item.weapon.weaponSprite;
                break;
            case ItemType.Tool:
                if (item.tool != null)
                    itemIcon.sprite = item.tool.toolSprite;
                break;
            default:
                // 对于方块、材料等，使用默认的itemSprite
                itemIcon.sprite = item.itemSprite;
                break;
        }

        itemName.text = item.itemName;
        if (item.quantity == 0) itemQuantity.text = "";
        else itemQuantity.text = item.quantity.ToString();
        
        // 绑定点击事件
        button.onClick.AddListener(OnSlotClicked);
        
        // 初始化选择高亮
        InitializeSelectionHighlights();
        
        // 默认不选中
        SetDetailSelected(false);
        SetBatchSelected(false);
    }

    private void InitializeSelectionHighlights()
    {
        // 初始化详情选中高亮
        if (detailSelectionHighlight == null)
        {
            detailSelectionHighlight = transform.Find("DetailSelectionHighlight")?.GetComponent<Image>();
            
            if (detailSelectionHighlight == null)
            {
                GameObject detailHighlightGO = new GameObject("DetailSelectionHighlight");
                detailHighlightGO.transform.SetParent(transform);
                detailHighlightGO.transform.SetAsFirstSibling();
                
                RectTransform rectTransform = detailHighlightGO.AddComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.sizeDelta = Vector2.zero;
                rectTransform.anchoredPosition = Vector2.zero;
                
                detailSelectionHighlight = detailHighlightGO.AddComponent<Image>();
                detailSelectionHighlight.color = detailSelectedColor;
                detailSelectionHighlight.gameObject.SetActive(false);
            }
        }

        // 初始化批量取出选中高亮
        if (batchSelectionHighlight == null)
        {
            batchSelectionHighlight = transform.Find("BatchSelectionHighlight")?.GetComponent<Image>();
            
            if (batchSelectionHighlight == null)
            {
                GameObject batchHighlightGO = new GameObject("BatchSelectionHighlight");
                batchHighlightGO.transform.SetParent(transform);
                batchHighlightGO.transform.SetAsFirstSibling();
                
                RectTransform rectTransform = batchHighlightGO.AddComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.sizeDelta = Vector2.zero;
                rectTransform.anchoredPosition = Vector2.zero;
                
                batchSelectionHighlight = batchHighlightGO.AddComponent<Image>();
                batchSelectionHighlight.color = batchSelectedColor;
                batchSelectionHighlight.gameObject.SetActive(false);
            }
        }
    }

    private void OnSlotClicked()
    {
        // 当这个槽被点击时，把自己(这个UI组件)和物品信息一起通知主UI
        warehouseUI.OnItemSelected(this, currentItem, itemType);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Ctrl+左键点击 - 选择/取消选择物品（用于批量取出）
        if (eventData.button == PointerEventData.InputButton.Left && eventData.clickCount == 1)
        {
            // 检查是否按住了Ctrl键
            bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            
            if (ctrlPressed && currentItem != null)
            {
                warehouseUI.ToggleItemSelection(this, currentItem);
            }
        }
    }

    /// <summary>
    /// 设置详情选中状态（用于显示物品详情）
    /// </summary>
    public void SetDetailSelected(bool selected)
    {
        isDetailSelected = selected;
        if (detailSelectionHighlight != null)
        {
            detailSelectionHighlight.gameObject.SetActive(selected);
        }
    }

    /// <summary>
    /// 设置批量取出选中状态（用于批量取出）
    /// </summary>
    public void SetBatchSelected(bool selected)
    {
        isBatchSelected = selected;
        if (batchSelectionHighlight != null)
        {
            batchSelectionHighlight.gameObject.SetActive(selected);
        }
    }

    /// <summary>
    /// 获取批量取出选中状态
    /// </summary>
    public bool IsBatchSelected()
    {
        return isBatchSelected;
    }

    /// <summary>
    /// 获取详情选中状态
    /// </summary>
    public bool IsDetailSelected()
    {
        return isDetailSelected;
    }

    /// <summary>
    /// 获取当前物品
    /// </summary>
    public Item GetCurrentItem()
    {
        return currentItem;
    }

    // 保持向后兼容的方法
    public void Select()
    {
        SetDetailSelected(true);
    }

    public void Deselect()
    {
        SetDetailSelected(false);
    }

    // 旧方法保持兼容，但现在指向批量选中状态
    public void SetSelected(bool selected)
    {
        SetBatchSelected(selected);
    }

    public bool IsSelected()
    {
        return IsBatchSelected();
    }
} 