# 泰拉瑞亚风格血量UI系统设置指南

## 系统概述

这个血量UI系统模仿泰拉瑞亚的心形血量显示，支持：
- 每个心形代表20HP
- 从左往右，从上往下的布局
- 多种心形类型（普通红心、金心、水晶心）
- 平滑的动画效果
- 低血量时的闪烁警告

## 设置步骤

### 1. 创建心形预制体

1. 在Unity中创建一个新的GameObject，命名为 `HealthHeart`
2. 添加以下组件：
   - Image (UI组件)
   - HealthHeartUI 脚本
   - CanvasGroup (自动添加)
3. 设置Image组件：
   - Source Image: 选择你的心形图片
   - Image Type: Simple
   - Preserve Aspect: 启用
4. 设置RectTransform：
   - Width: 30
   - Height: 30
   - Anchor: Middle Center
5. 将GameObject保存为预制体到 `Assets/Prefabs/UI/` 目录

### 2. 准备心形图片

你需要准备以下图片资源：

#### 普通红心 (基础血量层)
- `RedHeart_Full.png` - 满血红心
- `RedHeart_Half.png` - 半血红心  
- `RedHeart_Empty.png` - 空血红心

#### 金心 (第二血量层)
- `GoldHeart_Full.png` - 满血金心
- `GoldHeart_Half.png` - 半血金心
- `GoldHeart_Empty.png` - 空血金心

#### 水晶心 (第三血量层)
- `CrystalHeart_Full.png` - 满血水晶心
- `CrystalHeart_Half.png` - 半血水晶心
- `CrystalHeart_Empty.png` - 空血水晶心

### 3. 配置HealthHeartUI组件

在心形预制体的HealthHeartUI组件中设置：

```
Heart Image: 拖入Image组件引用

基础红心Sprites:
- Full Heart Sprite: RedHeart_Full
- Half Heart Sprite: RedHeart_Half  
- Empty Heart Sprite: RedHeart_Empty

金心Sprites:
- Golden Full Heart Sprite: GoldHeart_Full
- Golden Half Heart Sprite: GoldHeart_Half
- Golden Empty Heart Sprite: GoldHeart_Empty

水晶心Sprites:
- Crystal Full Heart Sprite: CrystalHeart_Full
- Crystal Half Heart Sprite: CrystalHeart_Half
- Crystal Empty Heart Sprite: CrystalHeart_Empty

动画配置:
- Pulse Scale: 1.2 (跳动时的缩放倍数)
- Pulse Duration: 0.3 (跳动动画时长)
- Fade In Duration: 0.5 (淡入时长)
- Fade Out Duration: 0.3 (淡出时长)
```

### 4. 设置PlayerHealthUI管理器

1. 在主Canvas下创建一个GameObject，命名为 `PlayerHealthUI`
2. 添加PlayerHealthUI脚本
3. 配置组件参数：

```
UI组件引用:
- Hearts Container: 可以为空，会自动创建
- Heart Prefab: 拖入刚才创建的心形预制体

布局配置:
- Hearts Per Row: 10 (每行心形数量)
- Heart Spacing: 35 (心形间距)
- Row Spacing: 35 (行间距)
- Heart Size: 30 (心形大小)

血量配置:
- Red Heart Health: 20 (红心的血量值)
- Golden Heart Health: 25 (金心的血量值)  
- Crystal Heart Health: 30 (水晶心的血量值)
- Max Heart Slots: 20 (最大心形槽位数量，固定)

动画配置:
- Health Change Animation Duration: 0.5
- New Heart Delay: 0.1
- Enable Low Health Blink: true (启用低血量闪烁)
- Low Health Threshold: 0.25 (低血量阈值25%)
- Low Health Blink Interval: 1.0 (闪烁间隔)
```

### 5. 测试系统

在场景中运行游戏，可以通过以下方式测试：

1. 在PlayerHealthUI组件上右键，选择测试方法：
   - "测试增加血量上限" - 增加40HP上限
   - "测试减少血量上限" - 减少40HP上限
   - "测试受伤" - 扣除25HP
   - "测试治疗" - 恢复25HP
   - "测试低血量" - 设置为20%血量

2. 或者在代码中调用：
```csharp
// 增加最大血量
PlayerController.Instance.IncreaseMaxHealth(40f);

// 治疗
PlayerController.Instance.Heal(25f);

// 受伤 (通过伤害系统)
var damageInfo = new DamageInfo(25f, DamageType.Physical, false, null, transform.position);
PlayerController.Instance.TakeDamage(damageInfo);
```

## 系统特性

### 血量层级系统（真正的泰拉瑞亚逻辑）

#### 心形槽位系统：
- **固定20个心形槽位**（2排，每排10个）
- **从左上角开始依次升级心形类型**
- **红心(20HP) → 金心(25HP) → 水晶心(30HP)**

#### 升级示例：
- **100HP**: 💎💎💎💎❤️ (3个水晶心 + 2个红心)
- **200HP**: 💎💎💎💎💎💎💎💎💎💛 (6个水晶心 + 1个金心)
- **445HP**: 💎💎💎💎💎💎💎💎💎💎💎💎💎💎💎❤️❤️❤️❤️❤️ (14个水晶心 + 5个红心)

#### 关键特点：
1. **固定槽位数量**: 最多20个心形槽位，不会无限增加
2. **优先级升级**: 优先使用最高级的心形来容纳血量
3. **从左到右**: 升级总是从最左边的心形开始
4. **效率最大化**: 系统会自动选择最有效的心形组合

### 动画效果
- **跳动动画**: 血量变化时心形会跳动
- **淡入/淡出**: 新增或移除心形时的平滑过渡
- **低血量闪烁**: 血量低于25%时剩余心形会闪烁

### 布局特点
- **网格布局**: 从左到右，从上到下排列
- **动态调整**: 根据最大血量自动调整心形数量
- **响应式**: 支持不同分辨率和屏幕比例

## 扩展功能

### 添加新的心形类型
1. 在HealthHeartUI.HeartType枚举中添加新类型
2. 准备对应的图片资源
3. 在HealthHeartUI组件中添加新的Sprite字段
4. 在UpdateHeartDisplay方法中添加新的处理逻辑
5. 在PlayerHealthUI.GetHeartType方法中定义新类型的血量范围

### 自定义动画效果
可以修改HealthHeartUI中的动画协程来实现：
- 不同的缓动曲线
- 颜色变化动画
- 旋转效果
- 粒子特效

### 添加音效
在血量变化时播放音效，可以在以下位置添加：
- HealthHeartUI.SetHeartState - 心形状态改变音效
- PlayerHealthUI.UpdateHealth - 血量变化音效
- PlayerController.TakeDamage/Heal - 受伤/治疗音效

## 故障排除

### 常见问题

1. **心形不显示**
   - 检查心形预制体是否正确设置
   - 确认Canvas和UI组件层级关系
   - 查看Console是否有错误信息

2. **动画不播放**
   - 确认MonoBehaviour组件正常工作
   - 检查协程是否被正确启动
   - 验证动画参数设置

3. **布局错乱**
   - 检查RectTransform的Anchor设置
   - 确认Canvas Scaler的设置
   - 验证心形大小和间距参数

4. **血量同步问题**
   - 确认PlayerController正确调用UpdateHealth
   - 检查单例模式是否正常工作
   - 验证血量值的计算逻辑

## 性能优化建议

1. **对象池**: 对于频繁创建/销毁心形的情况，可以使用对象池
2. **批量更新**: 避免频繁的单独更新，合并更新操作
3. **LOD系统**: 在心形数量很多时，可以考虑简化显示
4. **动画优化**: 合理控制同时播放的动画数量

## 总结

这个血量UI系统提供了完整的泰拉瑞亚风格血量显示功能，支持多层血量、动画效果和响应式布局。通过合理的配置和扩展，可以满足大多数游戏的血量显示需求。 