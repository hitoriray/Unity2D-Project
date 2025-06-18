using System.Collections.Generic;
using UnityEngine;

// 为了方便在编辑器中按类别筛选，我们定义一个枚举
public enum CraftingCategory
{
    Tools,
    Weapons,
    Armor,
    Blocks,
    Misc
}

[CreateAssetMenu(fileName = "New Recipe", menuName = "Recipe")]
public class Recipe : ScriptableObject
{
    [Header("基本信息")]
    public CraftingCategory category;
    public Item outputItem; // 产出的物品模板

    [Header("制作需求")]
    public List<Ingredient> requiredIngredients;
}