using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IngredientSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI quantityText;

    public void Display(Ingredient ingredient, int currentAmount)
    {
        if (ingredient == null || ingredient.item == null) return;
        
        // 显示图标
        icon.sprite = ingredient.item.itemSprite;
        if (ingredient.item.tool != null && icon.sprite == null)
            icon.sprite = ingredient.item.tool.toolSprite;
        else if (ingredient.item.weapon != null && icon.sprite == null)
            icon.sprite = ingredient.item.weapon.weaponSprite;
        
        // 显示数量 (例如 "50/10")
        quantityText.text = $"{currentAmount}/{ingredient.quantity}";
        
        // 如果材料足够，文字为白色；如果不足，则为红色
        quantityText.color = (currentAmount >= ingredient.quantity) ? Color.white : Color.red;
    }
}