# 库存拖拽系统使用说明

## 功能概述

这个拖拽系统实现了以下功能：
1. **物品拖拽** - 可以拖拽库存中的物品
2. **物品交换** - 将A物品拖到B物品槽位时自动交换
3. **物品移动** - 将物品拖到空槽位时直接移动
4. **物品堆叠** - 相同物品会自动尝试堆叠
5. **物品丢弃** - 拖拽到库存界面外会丢弃物品到世界中
6. **拖拽预览** - 拖拽时显示半透明的物品预览跟随鼠标
7. **拖拽保护** - 拖拽时禁用方块放置功能，避免误操作
8. **拾取冷却** - 丢弃的物品有拾取冷却机制，防止立即被拾取

## 文件结构

### 新增文件：
- `InventorySlotUI.cs` - 处理单个槽位的拖拽事件
- `DragManager.cs` - 管理拖拽视觉效果的单例管理器

### 修改文件：
- `Inventory.cs` - 添加了交换、移动、丢弃功能

## 设置步骤

### 1. 添加DragManager到场景
1. 创建一个空的GameObject，命名为"DragManager"
2. 添加`DragManager`脚本
3. 确保场景中有Canvas组件

### 2. 设置ItemDrop预制体引用
1. 选中场景中的Inventory组件
2. 在Inspector中找到"拖拽设置"部分
3. 将 `Assets/Prefabs/Item.prefab` 拖拽到 `Item Drop Prefab` 字段中

### 3. 槽位预制体设置
库存槽位预制体应该有以下结构：
```
InventorySlot (GameObject)
├── Image (子物体) - 显示物品图标
└── Amount (子物体) - 显示数量文本
```

## 使用方法

### 基本拖拽操作：
1. **移动物品**：拖拽物品到空槽位
2. **交换物品**：拖拽物品A到有物品B的槽位
3. **堆叠物品**：拖拽相同物品到已有该物品的槽位
4. **丢弃物品**：拖拽物品到库存界面外

### 代码调用示例：
```csharp
// 手动交换两个槽位的物品
inventory.SwapItems(
    new Vector2Int(0, 0), false,  // 源位置(x,y)，是否为热键栏
    new Vector2Int(1, 0), false   // 目标位置(x,y)，是否为热键栏
);

// 手动丢弃指定槽位的物品
inventory.DropItem(new Vector2Int(0, 0), false);
```

## 技术细节

### 拖拽检测
- 使用Unity的EventSystem接口：`IBeginDragHandler`, `IDragHandler`, `IEndDragHandler`, `IDropHandler`
- 通过Raycast检测拖拽目标

### 视觉反馈
- 拖拽时原槽位变透明
- 显示拖拽预览物体
- 预览物体跟随鼠标移动

### 物品处理逻辑
1. **堆叠优先**：相同物品优先尝试堆叠
2. **交换备选**：不能堆叠时执行交换
3. **移动简单**：目标为空时直接移动

## 注意事项

1. **预制体依赖**：确保ItemDrop预制体路径正确
2. **Canvas设置**：需要正确的Canvas和GraphicRaycaster
3. **性能考虑**：拖拽时会进行Raycast检测，频繁拖拽可能影响性能
4. **UI层级**：确保拖拽预览在最上层显示

## 扩展功能

可以轻松扩展以下功能：
- 右键分割物品
- Shift+点击快速移动
- 双击整理物品
- 拖拽到快捷栏的特殊处理
- 物品过滤和搜索

## 故障排除

### 常见问题：
1. **拖拽无响应**：检查是否添加了InventorySlotUI组件
2. **丢弃失败**：检查ItemDrop预制体路径
3. **视觉效果异常**：检查DragManager是否正确设置
4. **交换失败**：检查Inventory的SwapItems方法调用

### 调试建议：
- 在Console中查看错误信息
- 确保所有必需的组件都已添加
- 检查UI层级和Canvas设置
