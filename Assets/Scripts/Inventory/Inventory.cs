using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    public Tool tool;
    public Vector2 offset;
    public Vector2 multiplier;
    public GameObject inventoryUI;
    public GameObject inventorySlotsPrefab;
    public int inventoryWidth;
    public int inventoryHeight;
    public InventorySlot[,] inventorySlots;
    public GameObject[,] uiSlots;

    private void Start()
    {
        inventorySlots = new InventorySlot[inventoryWidth, inventoryHeight];
        uiSlots = new GameObject[inventoryWidth, inventoryHeight];

        // 检查必要的组件是否已配置
        if (inventorySlotsPrefab == null)
        {
            Debug.LogError($"[{gameObject.name}] inventorySlotsPrefab 未配置！请在Inspector中设置Slot预制体。");
            return;
        }

        if (inventoryUI == null)
        {
            Debug.LogError($"[{gameObject.name}] inventoryUI 未配置！请在Inspector中设置UI容器。");
            return;
        }

        SetupUI();
        UpdateInventoryUI();

        // 只有在tool不为null时才添加初始物品
        if (tool != null)
        {
            Add(new Item(tool));
            Add(new Item(tool));
            Add(new Item(tool));
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] tool 未配置，跳过添加初始物品。");
        }
    }

    void SetupUI()
    {
        if (inventorySlotsPrefab == null || inventoryUI == null)
        {
            Debug.LogError($"[{gameObject.name}] SetupUI失败：inventorySlotsPrefab或inventoryUI为null");
            return;
        }

        Transform parentTransform = inventoryUI.transform.GetChild(0).transform;
        if (parentTransform == null)
        {
            Debug.LogError($"[{gameObject.name}] inventoryUI的第一个子对象不存在！");
            return;
        }

        for (int x = 0; x < inventoryWidth; ++x)
            for (int y = 0; y < inventoryHeight; ++y)
            {
                GameObject inventorySlot = Instantiate(inventorySlotsPrefab, parentTransform);
                inventorySlot.GetComponent<RectTransform>().localPosition = new Vector3((x * multiplier.x) + offset.x, (y * multiplier.y) + offset.y);
                uiSlots[x, y] = inventorySlot;
                inventorySlots[x, y] = null;
            }
    }

    void UpdateInventoryUI()
    {
        if (uiSlots == null)
        {
            Debug.LogError($"[{gameObject.name}] UpdateInventoryUI失败：uiSlots为null");
            return;
        }

        for (int x = 0; x < inventoryWidth; ++x)
            for (int y = 0; y < inventoryHeight; ++y)
            {
                if (uiSlots[x, y] == null) continue;
                if (inventorySlots[x, y] == null)
                {
                    var imageComponent = uiSlots[x, y].transform.GetChild(0).GetComponent<Image>();
                    if (imageComponent != null)
                    {
                        imageComponent.sprite = null;
                        imageComponent.enabled = false;
                    }
                    var textComponent = uiSlots[x, y].transform.GetChild(1).GetComponent<TextMeshProUGUI>();
                    if (textComponent != null)
                    {
                        textComponent.text = "0";
                        textComponent.enabled = false;
                    }
                }
                else
                {
                    var imageComponent = uiSlots[x, y].transform.GetChild(0).GetComponent<Image>();
                    if (imageComponent != null && inventorySlots[x, y].item != null)
                    {
                        imageComponent.enabled = true;
                        imageComponent.sprite = inventorySlots[x, y].item.itemSprite;
                    }
                    var textComponent = uiSlots[x, y].transform.GetChild(1).GetComponent<TextMeshProUGUI>();
                    if (textComponent != null)
                    {
                        textComponent.enabled = true;
                        textComponent.text = inventorySlots[x, y].item.quantity.ToString();
                    }
                }
            }
    }

    public void Add(Item item)
    {
        if (item == null) return;
        for (int y = inventoryHeight - 1; y >= 0; --y)
            for (int x = 0; x < inventoryWidth; ++x)
            {
                if (inventorySlots[x, y] == null)
                {
                    inventorySlots[x, y] = new InventorySlot
                    {
                        position = new Vector2Int(x, y),
                        item = item,
                    };
                    UpdateInventoryUI();
                    return;
                }
                else
                {
                    if (inventorySlots[x, y].item.CanStackWith(item))
                    {
                        int remaining = inventorySlots[x, y].item.TryStack(item);
                        if (remaining == 0)
                        {
                            UpdateInventoryUI();
                            return;
                        }
                        item.quantity = remaining;
                        
                    }
                }
            }
        Debug.LogWarning($"[{gameObject.name}] 背包已满，无法添加物品: {item.itemName}");
    }

    public void Remove()
    {

    }
}
