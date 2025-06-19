using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingUI : MonoBehaviour
{
    [Header("依赖")]
    [SerializeField] private CraftingManager craftingManager;

    [Header("UI组件 - 核心")]
    [SerializeField] private GameObject craftingWindow;
    [SerializeField] private Transform recipeListContainer;
    [SerializeField] private GameObject recipeSlotPrefab;

    [Header("UI组件 - 详情面板")]
    [SerializeField] private TextMeshProUGUI detailItemName;
    [SerializeField] private Image detailItemIcon;
    [SerializeField] private TextMeshProUGUI detailItemDescription;
    [SerializeField] private TextMeshProUGUI detailItemSpecificDescription;
    [SerializeField] private Transform ingredientsContainer;
    [SerializeField] private GameObject ingredientSlotPrefab;
    [SerializeField] private Button craftButton;

    [Header("分类按钮")]
    [SerializeField] private List<CategoryButtonUI> categoryButtons;
    
    private List<GameObject> recipeSlots = new List<GameObject>();
    private List<GameObject> ingredientSlots = new List<GameObject>();
    private Recipe selectedRecipe;
    private RecipeSlotUI selectedSlotUI;
    private CraftingCategory selectedCategory;

    private void Start()
    {
        craftingWindow.SetActive(false);
        
        // 动态绑定按钮事件，假设按钮列表顺序与枚举一致
        for (int i = 0; i < categoryButtons.Count; i++)
        {
            int categoryIndex = i; // 闭包陷阱
            categoryButtons[i].GetComponent<Button>().onClick.AddListener(() => ShowRecipesForCategory((CraftingCategory)categoryIndex));
        }

        craftButton.onClick.AddListener(OnCraftButtonClicked);
        
        // 默认显示第一个分类
        ShowRecipesForCategory(CraftingCategory.Weapons);
    }

    private void Update()
    {
        // 交互逻辑已移至 Workbench.cs，这里不再需要按键控制
        // if (Input.GetKeyDown(KeyCode.C))
        // {
        //     craftingWindow.SetActive(!craftingWindow.activeSelf);
        //     if(craftingWindow.activeSelf)
        //     {
        //         // 每次打开时都刷新一下，确保显示最新的可制作状态
        //         ShowRecipesForCategory(CraftingCategory.Tools); // 或者上次选择的分类
        //     }
        // }
    }

    public void ShowRecipesForCategory(CraftingCategory category)
    {
        selectedCategory = category;
        // 更新分类按钮的选中状态
        for (int i = 0; i < categoryButtons.Count; i++)
        {
            if (i == (int)category)
            {
                categoryButtons[i].Select();
            }
            else
            {
                categoryButtons[i].Deselect();
            }
        }

        foreach (GameObject slot in recipeSlots)
        {
            Destroy(slot);
        }
        recipeSlots.Clear();
        selectedSlotUI = null;

        List<Recipe> filteredRecipes = craftingManager.GetAllRecipesForCategory(category);

        foreach (var recipe in filteredRecipes)
        {
            GameObject newSlot = Instantiate(recipeSlotPrefab, recipeListContainer);
            newSlot.GetComponent<RecipeSlotUI>().Initialize(this, recipe, category);
            recipeSlots.Add(newSlot);
        }

        if (recipeSlots.Count > 0)
        {
            recipeSlots[0].GetComponent<Button>().onClick.Invoke();
        }
        else
        {
            ClearDetailPanel();
        }
    }
    
    public void OnRecipeSelected(RecipeSlotUI slotUI, Recipe recipe, CraftingCategory category)
    {
        if (selectedSlotUI != null)
        {
            selectedSlotUI.Deselect();
        }

        selectedSlotUI = slotUI;
        selectedSlotUI.Select();

        selectedRecipe = recipe;
        if (category == CraftingCategory.Tools && recipe.outputItem.tool != null)
            recipe.outputItem = new Item(recipe.outputItem.tool);
        else if (category == CraftingCategory.Weapons && recipe.outputItem.weapon != null)
            recipe.outputItem = new Item(recipe.outputItem.weapon);
        else if (category == CraftingCategory.Blocks && recipe.outputItem.tile != null)
            recipe.outputItem = new Item(recipe.outputItem.tile);
        
        detailItemName.text = recipe.outputItem.itemName;
        detailItemIcon.sprite = recipe.outputItem.itemSprite;
        detailItemDescription.text = recipe.outputItem.description;
        detailItemSpecificDescription.text = recipe.outputItem.specificDescription;
        
        foreach (GameObject slot in ingredientSlots)
        {
            Destroy(slot);
        }
        ingredientSlots.Clear();
        
        foreach (var ingredient in recipe.requiredIngredients)
        {
            GameObject newIngredientSlot = Instantiate(ingredientSlotPrefab, ingredientsContainer);
            int currentAmount = craftingManager.GetInventory().items.GetTotalItemCount(ingredient.item.itemName);
            newIngredientSlot.GetComponent<IngredientSlotUI>().Display(ingredient, currentAmount);
            ingredientSlots.Add(newIngredientSlot);
        }
        
        craftButton.interactable = craftingManager.CanCraft(recipe);
    }

    private void OnCraftButtonClicked()
    {
        if(selectedRecipe != null)
        {
            craftingManager.Craft(selectedRecipe);
            OnRecipeSelected(selectedSlotUI, selectedRecipe, selectedCategory);
        }
    }

    private void ClearDetailPanel()
    {
        selectedRecipe = null;
        selectedSlotUI = null;
        detailItemName.text = "";
        detailItemIcon.sprite = null;
        detailItemDescription.text = "请选择一个配方";
        detailItemSpecificDescription.text = "";
        
        foreach (GameObject slot in ingredientSlots)
        {
            Destroy(slot);
        }
        ingredientSlots.Clear();
        
        craftButton.interactable = false;
    }
} 