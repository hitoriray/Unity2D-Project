using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSplitUI : MonoBehaviour
{
    [Header("UI组件")]
    public GameObject splitPanel;
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI totalQuantityText;
    public Slider quantitySlider;
    public TextMeshProUGUI splitQuantityText;
    public Button confirmButton;
    public Button cancelButton;
    
    // 私有变量
    private Item sourceItem;
    private InventorySlotUI sourceSlot;
    private System.Action<int> onConfirmSplit;
    private Canvas canvas;
    
    public static ItemSplitUI Instance { get; private set; }
    
    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            canvas = GetComponentInParent<Canvas>();
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 设置按钮事件
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmSplit);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelSplit);
        if (quantitySlider != null)
            quantitySlider.onValueChanged.AddListener(OnSliderValueChanged);

        // 初始时隐藏面板
        if (splitPanel != null)
            splitPanel.SetActive(false);

        // 设置按钮文字（使用英文避免字体问题）
        SetButtonTexts();
    }

    private void SetButtonTexts()
    {
        if (confirmButton != null)
        {
            var buttonText = confirmButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = "Confirm";
        }

        if (cancelButton != null)
        {
            var buttonText = cancelButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
                buttonText.text = "Cancel";
        }
    }
    
    /// <summary>
    /// 显示分割界面
    /// </summary>
    public void ShowSplitUI(Item item, InventorySlotUI slot, Vector3 screenPosition, System.Action<int> onConfirm)
    {
        if (item == null || item.quantity <= 1)
        {
            Debug.LogWarning("物品数量不足，无法分割");
            return;
        }
        
        sourceItem = item;
        sourceSlot = slot;
        onConfirmSplit = onConfirm;
        
        // 设置UI内容
        if (itemIcon != null)
            itemIcon.sprite = item.itemSprite;
        if (itemNameText != null)
            itemNameText.text = item.itemName;
        if (totalQuantityText != null)
            totalQuantityText.text = $"总数: {item.quantity}";
            
        // 设置滑块
        if (quantitySlider != null)
        {
            quantitySlider.minValue = 1;
            quantitySlider.maxValue = item.quantity - 1; // 最多分割到剩余1个
            quantitySlider.value = Mathf.Floor(item.quantity / 2f); // 默认分割一半
            quantitySlider.wholeNumbers = true;
        }
        
        // 更新分割数量显示
        UpdateSplitQuantityText();
        
        // 设置面板位置
        SetPanelPosition(screenPosition);
        
        // 显示面板
        if (splitPanel != null)
            splitPanel.SetActive(true);
    }
    
    /// <summary>
    /// 隐藏分割界面
    /// </summary>
    public void HideSplitUI()
    {
        if (splitPanel != null)
            splitPanel.SetActive(false);
            
        sourceItem = null;
        sourceSlot = null;
        onConfirmSplit = null;
    }
    
    /// <summary>
    /// 设置面板位置
    /// </summary>
    private void SetPanelPosition(Vector3 screenPosition)
    {
        if (splitPanel == null || canvas == null) return;
        
        // 将屏幕坐标转换为Canvas坐标
        Vector2 localPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPosition,
            canvas.worldCamera,
            out localPosition);
        
        RectTransform panelRect = splitPanel.GetComponent<RectTransform>();
        panelRect.localPosition = localPosition;
        
        // 确保面板在屏幕范围内
        ClampPanelToScreen(panelRect);
    }
    
    /// <summary>
    /// 确保面板在屏幕范围内
    /// </summary>
    private void ClampPanelToScreen(RectTransform panelRect)
    {
        Vector3[] corners = new Vector3[4];
        panelRect.GetWorldCorners(corners);
        
        RectTransform canvasRect = canvas.transform as RectTransform;
        Vector3[] canvasCorners = new Vector3[4];
        canvasRect.GetWorldCorners(canvasCorners);
        
        Vector3 position = panelRect.position;
        
        // 检查右边界
        if (corners[2].x > canvasCorners[2].x)
            position.x -= corners[2].x - canvasCorners[2].x;
        // 检查左边界
        if (corners[0].x < canvasCorners[0].x)
            position.x += canvasCorners[0].x - corners[0].x;
        // 检查上边界
        if (corners[2].y > canvasCorners[2].y)
            position.y -= corners[2].y - canvasCorners[2].y;
        // 检查下边界
        if (corners[0].y < canvasCorners[0].y)
            position.y += canvasCorners[0].y - corners[0].y;
            
        panelRect.position = position;
    }
    
    /// <summary>
    /// 滑块值改变时的回调
    /// </summary>
    private void OnSliderValueChanged(float value)
    {
        UpdateSplitQuantityText();
    }
    
    /// <summary>
    /// 更新分割数量显示
    /// </summary>
    private void UpdateSplitQuantityText()
    {
        if (splitQuantityText != null && quantitySlider != null && sourceItem != null)
        {
            int splitAmount = Mathf.RoundToInt(quantitySlider.value);
            int remainingAmount = sourceItem.quantity - splitAmount;
            splitQuantityText.text = $"分割: {splitAmount} | 保留: {remainingAmount}";
        }
    }
    
    /// <summary>
    /// 确认分割
    /// </summary>
    private void OnConfirmSplit()
    {
        if (quantitySlider != null && onConfirmSplit != null)
        {
            int splitAmount = Mathf.RoundToInt(quantitySlider.value);
            onConfirmSplit.Invoke(splitAmount);
        }
        
        HideSplitUI();
    }
    
    /// <summary>
    /// 取消分割
    /// </summary>
    private void OnCancelSplit()
    {
        HideSplitUI();
    }
    
    /// <summary>
    /// 检查分割界面是否显示
    /// </summary>
    public bool IsShowing()
    {
        return splitPanel != null && splitPanel.activeInHierarchy;
    }
    
    void Update()
    {
        // ESC键取消分割
        if (IsShowing() && Input.GetKeyDown(KeyCode.Escape))
        {
            OnCancelSplit();
        }
    }
}
