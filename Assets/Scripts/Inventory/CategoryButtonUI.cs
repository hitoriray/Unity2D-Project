using UnityEngine;
using UnityEngine.UI;

public class CategoryButtonUI : MonoBehaviour
{
    [SerializeField] private Image selectionHighlight;
    [SerializeField] private Image selectedUnderline;

    public void Select()
    {
        selectionHighlight.gameObject.SetActive(true);
        selectedUnderline.gameObject.SetActive(true);
    }

    public void Deselect()
    {
        selectionHighlight.gameObject.SetActive(false);
        selectedUnderline.gameObject.SetActive(false);
    }
} 