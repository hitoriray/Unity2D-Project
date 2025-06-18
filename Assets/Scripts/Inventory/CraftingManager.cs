using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    [Header("依赖")]
    [SerializeField] private Inventory inventory; // 引用玩家的背包

    [Header("数据")]
    [SerializeField] private List<Recipe> allRecipes;

    private void Awake()
    {
        // 如果你的配方放在Resources/Recipes文件夹下，可以使用这种方式自动加载
        // allRecipes = new List<Recipe>(Resources.LoadAll<Recipe>("Recipes"));
    }

    public Inventory GetInventory()
    {
        return inventory;
    }

    /// <summary>
    /// 检查玩家是否拥有制作某个配方所需的所有材料
    /// </summary>
    public bool CanCraft(Recipe recipe)
    {
        if (recipe == null) return false;

        foreach (var ingredient in recipe.requiredIngredients)
        {
            if (inventory.inventory.GetTotalItemCount(ingredient.item.itemName) < ingredient.quantity)
            {
                // 只要有一种材料数量不够，就无法制作
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 尝试制作一个物品
    /// </summary>
    public void Craft(Recipe recipe)
    {
        if (!CanCraft(recipe))
        {
            Debug.LogWarning("材料不足，无法制作: " + recipe.outputItem.itemName);
            return;
        }

        // 消耗材料
        foreach (var ingredient in recipe.requiredIngredients)
        {
            inventory.inventory.RemoveItems(ingredient.item.itemName, ingredient.quantity);
        }

        // 添加产物
        // 注意：我们给予的是配方中定义的物品模板的一个"复制品"
        Item craftedItem = new Item(recipe.outputItem);
        inventory.TryAddItem(craftedItem);

        Debug.Log("制作成功: " + craftedItem.itemName);
    }

    /// <summary>
    /// 获取指定分类下的所有配方
    /// </summary>
    public List<Recipe> GetAllRecipesForCategory(CraftingCategory category)
    {
        List<Recipe> recipes = new List<Recipe>();
        foreach (var recipe in allRecipes)
        {
            if (recipe.category == category)
            {
                recipes.Add(recipe);
            }
        }
        return recipes;
    }
} 