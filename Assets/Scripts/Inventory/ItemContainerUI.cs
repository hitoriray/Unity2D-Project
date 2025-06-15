using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemContainerUI : MonoBehaviour
{
    [Header("UI设置")]
    public Vector2 slotOffset;
    public Vector2 multiplier;
    public GameObject slotPrefab;
    public Transform containerTransform;

    private ItemContainer itemContainer;
    public GameObject[,] uiSlots { get; private set; }

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
                }

                uiSlots[x, y] = slotGO;
            }
        }
        UpdateUI();
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
                        slotUIComponent.UpdateSlotDisplay(trueContainer.GetSlot(truePosition));
                    }
                }
            }
        }
    }

    public ItemContainer GetContainer()
    {
        return itemContainer;
    }
} 