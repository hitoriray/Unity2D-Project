using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecipeSlotUI : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private Button button;
    [SerializeField] private Image selectionHighlight; // 用于高亮选中的图片

    private Recipe currentRecipe;
    private CraftingUI craftingUI; // 对主UI的引用
    private CraftingCategory category;

    public void Initialize(CraftingUI ui, Recipe recipe, CraftingCategory category)
    {
        craftingUI = ui;
        currentRecipe = recipe;
        this.category = category;
        
        // 根据传入的分类，决定使用哪个Sprite
        switch (category)
        {
            case CraftingCategory.Tools:
                if (recipe.outputItem.tool != null)
                    itemIcon.sprite = recipe.outputItem.tool.toolSprite;
                break;
            case CraftingCategory.Weapons:
                if (recipe.outputItem.weapon != null)
                    itemIcon.sprite = recipe.outputItem.weapon.weaponSprite;
                break;
            default:
                // 对于方块、材料等，使用默认的itemSprite
                itemIcon.sprite = recipe.outputItem.itemSprite;
                break;
        }

        itemName.text = recipe.outputItem.itemName;
        
        // 绑定点击事件
        button.onClick.AddListener(OnSlotClicked);
        
        // 默认不选中
        Deselect();
    }

    private void OnSlotClicked()
    {
        // 当这个槽被点击时，把自己(这个UI组件)和配方信息一起通知主UI
        craftingUI.OnRecipeSelected(this, currentRecipe, category);
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