using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("正常状态下的颜色")]
    [SerializeField] private Color normalColor = Color.white;

    [Tooltip("鼠标悬停时的颜色")]
    [SerializeField] private Color hoverColor = new Color(1f, 0.8f, 0.4f); // 示例：明亮的橙黄色

    private TextMeshProUGUI textComponent;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        // 初始设置为正常颜色
        textComponent.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        textComponent.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        textComponent.color = normalColor;
    }
} 