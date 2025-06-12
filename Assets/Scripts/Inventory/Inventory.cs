using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    public Tool PickupAxe;  // 镐
    public Tool Axe;        // 斧
    public Tool Hammer;     // 锤
    public Vector2 inventoryOffset;
    public Vector2 hotbarOffset;
    public Vector2 multiplier;
    public GameObject inventoryUI;
    public GameObject hotbarUI;
    public GameObject inventorySlotPrefab;
    public GameObject hotbarSlotPrefab;
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
                inventoryUISlots[x, y] = inventorySlot;
                inventorySlots[x, y] = null;
            }
        // setup hotbar
        for (int x = 0; x < inventoryWidth; ++x)
        {
            GameObject hotbarSlot = Instantiate(hotbarSlotPrefab, hotbarUI.transform.GetChild(0).transform);
            hotbarSlot.GetComponent<RectTransform>().localPosition = new Vector3((x * multiplier.x) + hotbarOffset.x, hotbarOffset.y);
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
                if (inventoryUISlots[x, y] == null) continue;
                if (inventorySlots[x, y] == null)
                {
                    var imageComponent = inventoryUISlots[x, y].transform.GetChild(0).GetComponent<Image>();
                    if (imageComponent != null)
                    {
                        imageComponent.sprite = null;
                        imageComponent.enabled = false;
                    }
                    var textComponent = inventoryUISlots[x, y].transform.GetChild(1).GetComponent<TextMeshProUGUI>();
                    if (textComponent != null)
                    {
                        textComponent.text = "0";
                        textComponent.enabled = false;
                    }
                }
                else
                {
                    var imageComponent = inventoryUISlots[x, y].transform.GetChild(0).GetComponent<Image>();
                    if (imageComponent != null && inventorySlots[x, y].item != null)
                    {
                        imageComponent.enabled = true;
                        imageComponent.sprite = inventorySlots[x, y].item.itemSprite;
                    }
                    var textComponent = inventoryUISlots[x, y].transform.GetChild(1).GetComponent<TextMeshProUGUI>();
                    if (textComponent != null)
                    {
                        textComponent.enabled = true;
                        if (inventorySlots[x, y].item.quantity == 1) textComponent.text = "";
                        else textComponent.text = inventorySlots[x, y].item.quantity.ToString();
                    }
                }
            }

        // update hotbar
        int col = inventoryHeight - 1;
        for (int x = 0; x < inventoryWidth; ++x)
        {
            if (inventoryUISlots[x, col] == null) continue;
            if (inventorySlots[x, col] == null)
            {
                var imageComponent = hotbarUISlots[x].transform.GetChild(0).GetComponent<Image>();
                if (imageComponent != null)
                {
                    imageComponent.sprite = null;
                    imageComponent.enabled = false;
                }
                var textComponent = hotbarUISlots[x].transform.GetChild(1).GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = "0";
                    textComponent.enabled = false;
                }
            }
            else
            {
                var imageComponent = hotbarUISlots[x].transform.GetChild(0).GetComponent<Image>();
                if (imageComponent != null && inventorySlots[x, col].item != null)
                {
                    imageComponent.enabled = true;
                    imageComponent.sprite = inventorySlots[x, col].item.itemSprite;
                }
                var textComponent = hotbarUISlots[x].transform.GetChild(1).GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.enabled = true;
                    if (inventorySlots[x, col].item.quantity == 1) textComponent.text = "";
                    else textComponent.text = inventorySlots[x, col].item.quantity.ToString();
                }
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

}
