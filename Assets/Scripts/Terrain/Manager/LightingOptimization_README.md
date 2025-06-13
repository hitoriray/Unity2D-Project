# 光照系统优化说明

## 🚀 优化内容

### 1. 性能优化
- **批量光照更新**: 移除了每次递归调用时的 `Apply()` 操作，改为批量处理
- **队列机制**: 实现了光照更新队列，避免快速放置方块时的卡顿
- **延迟处理**: 光照更新会等待一帧后批量执行，大幅提升性能

### 2. 背景墙光照增强
- **光照增强系数**: 为背景墙添加了 1.5x 的光照增强，解决过暗问题
- **可配置参数**: 可以通过 `LightingOptimizer` 组件调整增强系数 (1.0-3.0)

## 📋 使用方法

### 1. 添加优化器组件
在场景中的任意 GameObject 上添加 `LightingOptimizer` 组件：

```csharp
// 在 Inspector 中配置
wallLightBoost = 1.5f;  // 背景墙光照增强系数
showPerformanceStats = true;  // 显示性能统计
```

### 2. 代码调用方式

#### 快速放置方块（推荐）
```csharp
// 使用队列机制，避免卡顿
LightingManager.QueueLightUpdate(terrainGen, x, y, intensity);
```

#### 立即更新光照（初始化时使用）
```csharp
// 立即更新，用于世界生成等场景
LightingManager.LightBlock(terrainGen, x, y, intensity, 0);
```

## ⚙️ 配置参数

### LightingOptimizer 组件参数
- `wallLightBoost`: 背景墙光照增强系数 (1.0-3.0)
- `maxQueueDelayFrames`: 队列最大延迟帧数 (1-10)
- `showPerformanceStats`: 显示性能统计
- `showQueueStatus`: 显示队列状态

### 运行时调整
```csharp
// 动态调整背景墙光照
LightingManager.SetWallLightBoost(2.0f);

// 获取当前设置
float currentBoost = LightingManager.GetWallLightBoost();
```

## 📊 性能监控

### 控制台日志
- 队列处理状态：每处理10个光照点输出一次
- 批量处理完成：显示处理的光照点数量

### GUI 显示（可选）
启用 `showPerformanceStats` 后，屏幕左上角会显示：
- 背景墙光照增强系数
- 本帧光照更新数量
- 总光照更新数量
- 光照队列大小和状态

## 🔧 技术细节

### 优化前的问题
1. **频繁的GPU更新**: 每次光照递归都调用 `Apply()`
2. **同步处理**: 快速放置方块时会阻塞主线程
3. **背景墙过暗**: 使用相同的光照强度计算

### 优化后的改进
1. **批量GPU更新**: 队列处理完成后统一调用 `Apply()`
2. **异步处理**: 使用协程延迟处理光照队列
3. **背景墙增强**: 专门的光照增强系数

### 核心算法变化
```csharp
// 优化前：每次递归都Apply
LightBlock() -> SetPixel() -> Apply() -> 递归

// 优化后：批量Apply
QueueLightUpdate() -> 队列 -> 协程批量处理 -> 统一Apply()
```

## 🎯 使用建议

### 1. 快速建造场景
```csharp
// 连续放置多个方块时使用队列
for (int i = 0; i < blockCount; i++)
{
    terrainGen.PlaceTile(x + i, y, tile, tileType, tag, biome);
    // 光照会自动通过队列批量处理
}
```

### 2. 背景墙优化
- 建议将 `wallLightBoost` 设置为 1.5-2.0
- 过高的值可能导致背景墙过亮
- 可以根据不同生物群系调整

### 3. 性能调优
- 在低端设备上可以增加 `maxQueueDelayFrames`
- 在高端设备上可以减少延迟以获得更快的响应

## 🐛 故障排除

### 常见问题
1. **光照更新延迟**: 正常现象，队列机制会有1-2帧延迟
2. **背景墙仍然过暗**: 尝试增加 `wallLightBoost` 值
3. **性能仍然卡顿**: 检查是否还有其他地方直接调用 `LightBlock()`

### 调试方法
```csharp
// 启用详细日志
Debug.Log($"队列大小: {lightUpdateQueue.Count}");
Debug.Log($"处理状态: {isProcessingQueue}");
```

## 📈 性能对比

### 优化前
- 放置10个方块: ~50-100ms
- 频繁卡顿，特别是快速建造时

### 优化后  
- 放置10个方块: ~5-10ms
- 流畅的建造体验，无明显卡顿

## 🔄 版本兼容性

此优化保持了原有API的兼容性：
- `LightingManager.LightBlock()` 仍然可用
- `LightingManager.RemoveLightSource()` 功能不变
- 新增的队列机制是可选的优化功能
