using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 管理拖拽状态和视觉效果的单例管理器
/// </summary>
public class DragManager : MonoBehaviour
{
    public static DragManager Instance { get; private set; }
    
    [Header("拖拽设置")]
    public GameObject dragPreviewPrefab;
    public float dragAlpha = 0.8f;
    
    private GameObject currentDragPreview;
    private Canvas canvas;
    
    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            canvas = FindObjectOfType<Canvas>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // 实时更新拖拽预览位置
        if (currentDragPreview != null)
        {
            UpdateDragPosition(Input.mousePosition);
        }
    }
    
    /// <summary>
    /// 开始拖拽，创建预览物体
    /// </summary>
    public void StartDrag(Item item, Vector3 startPosition)
    {
        if (currentDragPreview != null)
        {
            Destroy(currentDragPreview);
        }

        currentDragPreview = CreateDragPreview(item);
        if (currentDragPreview != null)
        {
            Debug.Log("DragManager: 拖拽预览创建成功");
            UpdateDragPosition(Input.mousePosition);
        }
        else
        {
            Debug.LogError("DragManager: 拖拽预览创建失败！");
        }
    }

    /// <summary>
    /// 更新拖拽预览位置（使用屏幕坐标）
    /// </summary>
    public void UpdateDragPosition(Vector3 screenPosition)
    {
        if (currentDragPreview != null)
        {
            // 由于预览使用独立的ScreenSpaceOverlay Canvas，直接使用屏幕坐标
            RectTransform rectTransform = currentDragPreview.GetComponent<RectTransform>();
            rectTransform.position = screenPosition;
        }
    }
    
    /// <summary>
    /// 结束拖拽，销毁预览物体
    /// </summary>
    public void EndDrag()
    {
        if (currentDragPreview != null)
        {
            // 销毁整个Canvas（预览的父物体）
            if (currentDragPreview.transform.parent != null)
            {
                Destroy(currentDragPreview.transform.parent.gameObject);
            }
            else
            {
                Destroy(currentDragPreview);
            }
            currentDragPreview = null;
        }
    }
    
    /// <summary>
    /// 创建拖拽预览物体
    /// </summary>
    private GameObject CreateDragPreview(Item item)
    {
        // 创建一个完全独立的Canvas用于拖拽预览
        GameObject previewCanvasObj = new GameObject("DragPreviewCanvas");
        Canvas previewCanvas = previewCanvasObj.AddComponent<Canvas>();
        previewCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        previewCanvas.sortingOrder = 32767; // 最高层级

        CanvasScaler canvasScaler = previewCanvasObj.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);

        previewCanvasObj.AddComponent<GraphicRaycaster>();

        // 创建预览物体
        GameObject preview = new GameObject("DragPreview");
        preview.transform.SetParent(previewCanvasObj.transform, false);

        // 添加Image组件
        Image previewImage = preview.AddComponent<Image>();
        previewImage.sprite = item.itemSprite;
        previewImage.raycastTarget = false;

        // 设置大小（稍微大一点，更明显）
        RectTransform rectTransform = preview.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(80, 80);

        // 设置透明度 - 确保可见
        CanvasGroup canvasGroup = preview.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0.8f; // 固定透明度，确保可见
        canvasGroup.blocksRaycasts = false;

        // 如果数量大于1，添加数量显示
        if (item.quantity > 1)
        {
            GameObject quantityObj = new GameObject("Quantity");
            quantityObj.transform.SetParent(preview.transform, false);

            Text quantityText = quantityObj.AddComponent<Text>();
            quantityText.text = item.quantity.ToString();
            quantityText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            quantityText.fontSize = 16;
            quantityText.color = Color.white;
            quantityText.alignment = TextAnchor.LowerRight;
            quantityText.fontStyle = FontStyle.Bold;

            RectTransform quantityRect = quantityObj.GetComponent<RectTransform>();
            quantityRect.anchorMin = new Vector2(0.5f, 0);
            quantityRect.anchorMax = new Vector2(1, 0.5f);
            quantityRect.offsetMin = Vector2.zero;
            quantityRect.offsetMax = Vector2.zero;
        }

        return preview;
    }
    
    /// <summary>
    /// 检查是否正在拖拽
    /// </summary>
    public bool IsDragging()
    {
        return currentDragPreview != null;
    }
}
