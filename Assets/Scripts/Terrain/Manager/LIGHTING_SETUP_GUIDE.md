# 高级光照系统设置指南

## 概述
新的光照系统结合了Unity 2D Lights的视觉效果和自定义纹理光照的性能优势。

## ⚠️ 重要提示
Unity 2D Light只有四种类型：**Global Light 2D**、**Spot Light 2D**、**Freeform Light 2D**、**Sprite Light 2D**。本指南推荐使用Freeform Light 2D来替代传统的点光源效果。

## 设置步骤

### 1. 启用URP 2D Renderer
1. 确保项目已安装Universal Render Pipeline
2. 在Project Settings > Graphics中，设置Scriptable Render Pipeline Settings为你的URP Asset
3. 在URP Asset的Renderer List中，确保使用2D Renderer

### 2. 创建光源预制体

#### 火把光源预制体 (TorchLight)
1. 创建空GameObject
2. 添加Light 2D组件：
   - Light Type: Freeform
   - Intensity: 1.0
   - Shape Falloff Size: 5 (控制光照范围)
   - Color: 暖黄色 (#FFD700)
   - Blend Style: Additive
3. 添加粒子效果（可选）：火焰粒子
4. 保存为预制体
。
#### 玩家光源预制体 (PlayerLight)
1. 创建空GameObject
2. 添加Light 2D组件：
   - Light Type: Freeform
   - Intensity: 0.8
   - Shape Falloff Size: 8 (控制光照范围)
   - Color: 柔和白色 (#FFF8DC)
   - Blend Style: Additive
   - 设置Shape Path为圆形（推荐）
3. 保存为预制体

#### 投射物光源预制体 (ProjectileLight)
1. 创建空GameObject
2. 添加Light 2D组件：
   - Light Type: Freeform
   - Intensity: 1.5
   - Shape Falloff Size: 3 (控制光照范围)
   - Color: 根据需求设置
   - Blend Style: Additive
3. 保存为预制体

### 3. 配置AdvancedLightingSystem

1. 在场景中创建GameObject并添加AdvancedLightingSystem脚本
2. 配置参数：
   ```
   基础设置:
   - Terrain Gen: 拖入TerrainGeneration对象
   - Enable Advanced Lighting: ✓
   
   环境光照设置:
   - Day Night Gradient: 设置昼夜颜色渐变
     * 0%: 深蓝色 (#0A0A2E) - 夜晚
     * 25%: 橙色 (#FF6B35) - 日出
     * 50%: 亮白色 (#FFFFFF) - 正午
     * 75%: 橙红色 (#FF4500) - 日落
     * 100%: 深蓝色 (#0A0A2E) - 夜晚
   
   - Ambient Intensity Curve: 设置强度曲线
     * 0: 0.2 (夜晚)
     * 0.5: 1.0 (正午)
     * 1: 0.2 (夜晚)
   
   光源预制体:
   - 拖入对应的预制体
   
   性能设置:
   - Max Dynamic Lights: 50
   - Light Culling Distance: 30
   ```

### 4. 使用示例

#### 添加火把
```csharp
// 在指定位置放置火把
Vector3 torchPosition = new Vector3(10, 5, 0);
GameObject torch = AdvancedLightingSystem.Instance.CreateTorchLight(torchPosition, 1.2f, 6f);
```

#### 为玩家添加光源
```csharp
// 在PlayerController的Start方法中
void Start()
{
    AdvancedLightingSystem.Instance.CreatePlayerLight(transform, 0.8f, 8f);
}
```

#### 创建爆炸光效
```csharp
// 爆炸时的光效
Vector3 explosionPos = transform.position;
AdvancedLightingSystem.Instance.CreateTemporaryLight(
    explosionPos, 
    Color.yellow, 
    3f,     // 强度
    10f,    // 半径
    0.5f    // 持续时间
);
```

### 5. 昼夜循环集成

在DayNightCycleManager中：
```csharp
void Update()
{
    // 更新时间
    float timeOfDay = CalculateTimeOfDay(); // 0-1
    AdvancedLightingSystem.Instance.SetTimeOfDay(timeOfDay);
}
```

### 6. 性能优化建议

1. **光源数量控制**
   - 使用对象池避免频繁创建/销毁
   - 设置合理的最大光源数量
   - 利用视锥体剔除

2. **渲染优化**
   - 调整Light 2D的Quality设置
   - 使用合适的Blend Style
   - 考虑使用Light Cookies减少overdraw

3. **混合使用**
   - 静态环境光使用纹理系统
   - 动态光源使用Unity 2D Lights
   - 关键区域使用高质量光照

### 7. 常见问题

**Q: 光源不显示？**
A: 检查URP设置和2D Renderer配置

**Q: 性能问题？**
A: 减少Max Dynamic Lights，增加Culling Distance

**Q: 光照穿墙？**
A: Unity 2D Light默认不支持阴影遮挡，需要使用Shadow Caster 2D组件

### 8. 进阶功能

#### 添加阴影
1. 在需要投射阴影的物体上添加Shadow Caster 2D组件
2. 在Light 2D上启用"Cast Shadows"

#### 自定义光照效果
1. 使用Shader Graph创建自定义光照响应材质
2. 利用Light Texture功能创建特殊光照形状

#### 光照触发器
创建区域触发器，玩家进入时动态开启/关闭光源，进一步优化性能。 