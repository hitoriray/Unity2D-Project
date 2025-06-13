# 物品分割系统设置指南

## 功能概述

右键点击库存中的物品（数量>1）可以打开分割界面，允许玩家将物品分割成两部分。

## 设置步骤

### 1. 创建分割UI界面

在Canvas下创建以下UI结构：

```
ItemSplitPanel (GameObject)
├── Background (Image) - 背景面板
├── ItemIcon (Image) - 物品图标
├── ItemName (TextMeshPro) - 物品名称
├── TotalQuantity (TextMeshPro) - 总数量显示
├── QuantitySlider (Slider) - 数量滑块
├── SplitQuantity (TextMeshPro) - 分割数量显示
├── ConfirmButton (Button) - 确认按钮
└── CancelButton (Button) - 取消按钮
```

### 2. 设置ItemSplitUI组件

1. 将ItemSplitUI脚本添加到ItemSplitPanel上
2. 在Inspector中拖拽对应的UI组件到相应字段：
   - Split Panel: ItemSplitPanel
   - Item Icon: ItemIcon (Image)
   - Item Name Text: ItemName (TextMeshPro)
   - Total Quantity Text: TotalQuantity (TextMeshPro)
   - Quantity Slider: QuantitySlider (Slider)
   - Split Quantity Text: SplitQuantity (TextMeshPro)
   - Confirm Button: ConfirmButton (Button)
   - Cancel Button: CancelButton (Button)

### 3. 滑块设置

在QuantitySlider组件中设置：
- Min Value: 1
- Max Value: 10 (会在运行时动态调整)
- Whole Numbers: ✓ (勾选)

### 4. 样式建议

**背景面板**：
- 颜色：半透明黑色 (0, 0, 0, 180)
- 大小：300x200像素

**按钮样式**：
- 确认按钮：绿色背景
- 取消按钮：红色背景

## 使用方法

### 玩家操作：
1. **右键点击物品** → 打开分割界面（仅限数量>1的物品）
2. **拖动滑块** → 选择要分割的数量
3. **点击确认** → 执行分割
4. **点击取消或按ESC** → 取消分割

### 分割逻辑：
- 原物品数量减少
- 分割出的物品放入空槽位
- 如果没有空槽位，分割的物品会掉落到地面

## 代码结构

### 新增文件：
- `ItemSplitUI.cs` - 分割界面管理器
- `README_物品分割系统.md` - 本设置指南

### 修改文件：
- `InventorySlotUI.cs` - 添加右键点击检测
- `Inventory.cs` - 添加SplitItem方法

## 技术特性

### ✅ 已实现功能：
- 右键点击检测
- 分割界面显示/隐藏
- 滑块数量选择
- 分割逻辑处理
- 空槽位查找
- 物品掉落备选方案
- ESC键取消
- 界面位置自适应

### 🔧 可扩展功能：
- 键盘输入数量
- 快速分割按钮（1/2, 1/4等）
- 分割音效
- 分割动画效果

## 故障排除

### 常见问题：

1. **右键无响应**
   - 检查InventorySlotUI是否实现了IPointerClickHandler
   - 确保UI组件有Graphic Raycaster

2. **分割界面不显示**
   - 检查ItemSplitUI.Instance是否为null
   - 确保ItemSplitPanel在场景中存在

3. **分割后物品消失**
   - 检查FindEmptySlot方法是否正常工作
   - 查看Console是否有错误信息

### 调试建议：
- 在Console中查看分割相关的Debug.Log信息
- 确保所有UI组件都正确拖拽到ItemSplitUI脚本中
- 检查Canvas的设置是否正确

## 注意事项

1. **单例模式**：ItemSplitUI使用单例模式，确保场景中只有一个实例
2. **UI层级**：分割面板应该在较高的UI层级，确保不被其他UI遮挡
3. **性能考虑**：分割操作会触发UI更新，频繁分割可能影响性能
