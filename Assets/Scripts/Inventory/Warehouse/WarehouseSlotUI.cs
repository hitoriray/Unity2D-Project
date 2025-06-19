using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WarehouseSlotUI : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemQuantity;
    [SerializeField] private Button button;
    [SerializeField] private Image selectionHighlight; // 用于高亮选中的图片

    private Item currentItem;
    private WarehouseUI warehouseUI; // 对主UI的引用
    private ItemType itemType;

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
        
        // 默认不选中
        Deselect();
    }

    private void OnSlotClicked()
    {
        // 当这个槽被点击时，把自己(这个UI组件)和配方信息一起通知主UI
        warehouseUI.OnItemSelected(this, currentItem, itemType);
    }

    public void Select()
    {
        selectionHighlight.gameObject.SetActive(true);
    }

    public void Deselect()
    {
        selectionHighlight.gameObject.SetActive(false);
    }
} 