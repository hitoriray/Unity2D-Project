using UnityEngine;

/// <summary>
/// 武器物品测试脚本 - 验证武器在库存系统中的功能
/// 位置: Combat/Testing/ - 测试工具
/// </summary>
public class WeaponItemTest : MonoBehaviour
{
    [Header("测试配置")]
    [Tooltip("测试用的武器资产")]
    public Weapon testWeapon;
    
    [Tooltip("测试用的工具资产")]
    public Tool testTool;
    
    [Header("测试结果显示")]
    [SerializeField, TextArea(3, 5)]
    private string weaponItemInfo;
    
    [SerializeField, TextArea(3, 5)]
    private string stackingTestInfo;
    
    void Start()
    {
        Debug.Log("=== 武器物品测试开始 ===");
        TestWeaponItemCreation();
    }
    
    void Update()
    {
        // 实时更新测试信息（仅在编辑器中）
        #if UNITY_EDITOR
        UpdateTestInfo();
        #endif
    }
    
    /// <summary>
    /// 测试武器物品创建
    /// </summary>
    [ContextMenu("测试武器物品创建")]
    public void TestWeaponItemCreation()
    {
        if (testWeapon == null)
        {
            Debug.LogWarning("请在Inspector中设置测试武器资产");
            return;
        }
        
        Debug.Log("--- 武器物品创建测试 ---");
        
        // 创建武器物品
        Item weaponItem = new Item(testWeapon);
        
        Debug.Log($"武器物品创建成功:");
        Debug.Log($"  物品名称: {weaponItem.itemName}");
        Debug.Log($"  物品类型: {weaponItem.itemType}");
        Debug.Log($"  最大堆叠: {weaponItem.maxStackSize}");
        Debug.Log($"  数量: {weaponItem.quantity}");
        Debug.Log($"  武器引用: {weaponItem.weapon != null}");
        
        if (weaponItem.weapon != null)
        {
            Debug.Log($"  武器名称: {weaponItem.weapon.weaponName}");
            Debug.Log($"  武器类型: {weaponItem.weapon.weaponType}");
            Debug.Log($"  武器伤害: {weaponItem.weapon.damage}");
        }
    }
    
    /// <summary>
    /// 测试物品堆叠功能
    /// </summary>
    [ContextMenu("测试物品堆叠")]
    public void TestItemStacking()
    {
        if (testWeapon == null)
        {
            Debug.LogWarning("请在Inspector中设置测试武器资产");
            return;
        }
        
        Debug.Log("--- 物品堆叠测试 ---");
        
        // 创建两个相同的武器物品
        Item weaponItem1 = new Item(testWeapon);
        Item weaponItem2 = new Item(testWeapon);
        
        // 测试武器是否可以堆叠（应该不可以）
        bool canStack = weaponItem1.CanStackWith(weaponItem2);
        Debug.Log($"相同武器是否可以堆叠: {canStack} (应该为false)");
        
        // 如果有工具，测试工具堆叠
        if (testTool != null)
        {
            Item toolItem1 = new Item(testTool);
            Item toolItem2 = new Item(testTool);
            
            bool toolCanStack = toolItem1.CanStackWith(toolItem2);
            Debug.Log($"相同工具是否可以堆叠: {toolCanStack} (应该为false)");
            
            // 测试武器和工具是否可以堆叠
            bool weaponToolStack = weaponItem1.CanStackWith(toolItem1);
            Debug.Log($"武器和工具是否可以堆叠: {weaponToolStack} (应该为false)");
        }
        
        // 测试复制构造函数
        Item weaponCopy = new Item(weaponItem1);
        Debug.Log($"复制构造函数测试:");
        Debug.Log($"  原始武器名称: {weaponItem1.itemName}");
        Debug.Log($"  复制武器名称: {weaponCopy.itemName}");
        Debug.Log($"  武器引用复制: {weaponCopy.weapon != null && weaponCopy.weapon == weaponItem1.weapon}");
    }
    
    /// <summary>
    /// 测试ItemType枚举
    /// </summary>
    [ContextMenu("测试ItemType枚举")]
    public void TestItemTypeEnum()
    {
        Debug.Log("--- ItemType枚举测试 ---");
        
        foreach (ItemType itemType in System.Enum.GetValues(typeof(ItemType)))
        {
            Debug.Log($"ItemType.{itemType} = {(int)itemType}");
        }
        
        // 验证Weapon类型存在
        bool hasWeaponType = System.Enum.IsDefined(typeof(ItemType), ItemType.Weapon);
        Debug.Log($"ItemType.Weapon 是否存在: {hasWeaponType}");
    }
    
    /// <summary>
    /// 测试物品类型判断
    /// </summary>
    [ContextMenu("测试物品类型判断")]
    public void TestItemTypeChecking()
    {
        if (testWeapon == null)
        {
            Debug.LogWarning("请在Inspector中设置测试武器资产");
            return;
        }
        
        Debug.Log("--- 物品类型判断测试 ---");
        
        Item weaponItem = new Item(testWeapon);
        
        Debug.Log($"武器物品类型检查:");
        Debug.Log($"  是否为武器: {weaponItem.itemType == ItemType.Weapon}");
        Debug.Log($"  是否为工具: {weaponItem.itemType == ItemType.Tool}");
        Debug.Log($"  是否为方块: {weaponItem.itemType == ItemType.Block}");
        
        if (testTool != null)
        {
            Item toolItem = new Item(testTool);
            Debug.Log($"工具物品类型检查:");
            Debug.Log($"  是否为工具: {toolItem.itemType == ItemType.Tool}");
            Debug.Log($"  是否为武器: {toolItem.itemType == ItemType.Weapon}");
        }
    }
    
    /// <summary>
    /// 测试空值处理
    /// </summary>
    [ContextMenu("测试空值处理")]
    public void TestNullHandling()
    {
        Debug.Log("--- 空值处理测试 ---");
        
        // 测试空武器创建（这会导致错误，但不应该崩溃）
        try
        {
            Item nullWeaponItem = new Item((Weapon)null);
            Debug.LogWarning("空武器物品创建成功，但武器引用为空");
            Debug.Log($"  物品名称: '{nullWeaponItem.itemName}'");
            Debug.Log($"  武器引用: {nullWeaponItem.weapon}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"空武器创建失败: {e.Message}");
        }
        
        // 测试堆叠空值处理
        if (testWeapon != null)
        {
            Item weaponItem = new Item(testWeapon);
            bool canStackWithNull = weaponItem.CanStackWith(null);
            Debug.Log($"武器与null堆叠: {canStackWithNull} (应该为false)");
        }
    }
    
    /// <summary>
    /// 更新测试信息显示
    /// </summary>
    private void UpdateTestInfo()
    {
        if (testWeapon != null)
        {
            Item weaponItem = new Item(testWeapon);
            weaponItemInfo = $"武器物品信息:\n" +
                           $"名称: {weaponItem.itemName}\n" +
                           $"类型: {weaponItem.itemType}\n" +
                           $"堆叠: {weaponItem.maxStackSize}\n" +
                           $"武器: {weaponItem.weapon?.weaponName}";
        }
        else
        {
            weaponItemInfo = "请设置测试武器资产";
        }
        
        // 堆叠测试信息
        if (testWeapon != null)
        {
            Item item1 = new Item(testWeapon);
            Item item2 = new Item(testWeapon);
            
            stackingTestInfo = $"堆叠测试:\n" +
                             $"武器可堆叠: {item1.CanStackWith(item2)}\n" +
                             $"与null堆叠: {item1.CanStackWith(null)}\n" +
                             $"ItemType.Weapon存在: {System.Enum.IsDefined(typeof(ItemType), ItemType.Weapon)}";
        }
        else
        {
            stackingTestInfo = "请设置测试武器资产";
        }
    }
}
