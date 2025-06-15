using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Header("Data Containers")]
    public ItemContainer inventory;
    
    [Header("UI Panels")]
    public ItemContainerUI inventoryUI;
    public ItemContainerUI hotbarUI;
    public ItemContainerUI boxUI;

    [Header("Configuration")]
    [SerializeField] private int inventoryWidth = 8;
    [SerializeField] private int inventoryHeight = 4;
    [SerializeField] private int hotbarWidth = 8;

    [Header("拖拽设置")]
    public GameObject itemDropPrefab;

    [Header("初始物品")]
    public Weapon starSword;
    public Tool startingAxe;
    public Tool startingPickaxe;
    public Tool startingHammer;

    void Awake()
    {
        inventory = new ItemContainer(inventoryWidth, inventoryHeight);

        if (inventoryUI != null)
        {
            inventory.OnItemsChanged += inventoryUI.UpdateUI;
            inventoryUI.Initialize(inventory);
        }

        // Link the SAME inventory data to the hotbar UI, but remap the slots
        if (hotbarUI != null)
        {
            // The hotbar UI should also update when the main inventory changes
            inventory.OnItemsChanged += hotbarUI.UpdateUI;
            
            // We create a temporary, purely visual container for the hotbar UI's layout
            ItemContainer hotbarVisualContainer = new ItemContainer(hotbarWidth, 1);
            hotbarUI.Initialize(hotbarVisualContainer);

            // CRITICAL STEP: Re-assign each hotbar UI slot to point to the
            // corresponding slot in the LAST ROW of the main inventory data container.
            for (int x = 0; x < hotbarWidth; x++)
            {
                if (hotbarUI.uiSlots[x, 0] != null)
                {
                    InventorySlotUI slotUIComponent = hotbarUI.uiSlots[x, 0].GetComponent<InventorySlotUI>();
                    if (slotUIComponent != null)
                    {
                        // This slot's data comes from inventory at (x, inventoryHeight - 1)
                        slotUIComponent.AssignContainer(inventory, new Vector2Int(x, inventoryHeight - 1));
                    }
                }
            }
        }

        GiveStartingItems();
    }

    private void Update()
    {
        // 示例：按下I键切换背包UI的显示/隐藏
        if (Input.GetKeyDown(KeyCode.I))
        {
            inventoryUI.gameObject.SetActive(!inventoryUI.gameObject.activeSelf);
        }
    }

    public void DropItem(Item item)
    {
        if (item == null) return;

        Vector3 dropPosition = GetPlayerDropPosition(); 
        CreateItemDrop(item, dropPosition);
    }
    
    // 这些方法现在是 Inventory 特有的，因为它们与玩家的位置有关
    private Vector3 GetPlayerDropPosition()
    {
        return transform.position + Vector3.up * 10f + Vector3.right * 10f; // 示例位置
    }

    private void CreateItemDrop(Item item, Vector3 position)
    {
        if (itemDropPrefab != null)
        {
            GameObject drop = Instantiate(itemDropPrefab, position, Quaternion.identity);
            // 假设 ItemDrop 脚本有一个 Initialize 方法
            // drop.GetComponent<ItemDrop>().Initialize(item); 
        }
        else
        {
            Debug.LogWarning("ItemDrop 预制件未设置！");
        }
    }

    // Methods for adding/checking items now operate on the single inventory
    public bool CanAddItem(Item item)
    {
        return inventory.CanAddItem(item);
    }

    public bool TryAddItem(Item item)
    {
        return inventory.AddItem(item);
    }

    public void DropItem(Item item, Vector3 position)
    {
        if (itemDropPrefab != null)
        {
            GameObject dropGO = Instantiate(itemDropPrefab, position, Quaternion.identity);
            ItemDrop itemDrop = dropGO.GetComponent<ItemDrop>();
            if (itemDrop != null)
            {
                itemDrop.SetItem(new Item(item)); // Pass a copy to the drop
                itemDrop.MarkAsPlayerDropped();
            }
            else
            {
                Debug.LogError("itemDropPrefab does not have an ItemDrop component!");
                Destroy(dropGO);
            }
        }
        else
        {
            Debug.LogError("itemDropPrefab is not assigned in the Inventory component!");
        }
    }

    private void GiveStartingItems()
    {
        if (starSword != null)
            inventory.AddItem(new Item(starSword));
        if (startingPickaxe != null)
            inventory.AddItem(new Item(startingPickaxe));
        if (startingAxe != null)
            inventory.AddItem(new Item(startingAxe));
        if (startingHammer != null)
            inventory.AddItem(new Item(startingHammer));
    }

    public void ToggleInventory()
    {
        // ... existing code ...
    }
}
