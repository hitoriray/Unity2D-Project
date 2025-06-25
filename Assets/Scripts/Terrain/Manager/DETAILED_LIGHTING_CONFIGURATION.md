# 高级光照系统详细配置指南

## ⚠️ 重要提示
- 本指南基于Unity 2022.3+ LTS版本和URP 14+
- Unity 2D Light组件只有四种类型：**Global Light 2D**、**Spot Light 2D**、**Freeform Light 2D**、**Sprite Light 2D**
- 不同Unity版本的API可能略有差异，如遇到属性名称不匹配，请参考实际Inspector面板
- 本指南已根据实际可用的Light类型更新，移除了不存在的"Point Light 2D"引用

## 第一步：准备工作

### 1.1 检查URP设置
1. 打开 `Window > Package Manager`
2. 切换到 `In Project`，确保已安装：
   - Universal RP (版本 12.0.0 或更高)
   - 2D Renderer (应该包含在URP中)

### 1.2 配置渲染管线
1. 在Project窗口中找到你的URP Asset (通常在 `Settings` 文件夹)
2. 选中URP Asset，在Inspector中：
   - 确保 `Renderer List` 中有 `2D Renderer`
   - 如果没有，点击 `+` 添加，选择 `2D Renderer (Forward)`

3. 打开 `Edit > Project Settings > Graphics`
4. 在 `Scriptable Render Pipeline Settings` 中拖入你的URP Asset

### 1.3 配置2D Renderer
1. 找到你的2D Renderer Asset
2. 在Inspector中检查：
   - `Blend Styles` 应该包含至少一个 `Additive` 模式
   - 确保 `Use Depth/Stencil Buffer` 已启用

### 1.4 了解2D Light组件类型
Unity 2D Light系统提供以下四种光源类型：
- **Global Light 2D**: 全局光源，用于太阳、月亮等环境光照
- **Spot Light 2D**: 聚光灯，用于手电筒、探照灯等定向光源
- **Freeform Light 2D**: 自由形状光源，可自定义光照形状，**推荐用于火把、玩家光源等圆形光照**
- **Sprite Light 2D**: 精灵光源，使用精灵纹理作为光照形状，适合特殊形状的光源

## 第二步：创建光源预制体

### 2.1 创建火把光源预制体

#### 步骤1：创建基础GameObject
1. 在Hierarchy中右键 > `Create Empty`
2. 重命名为 `TorchLight`
3. 位置设置为 `(0, 0, 0)`

#### 步骤2：添加Light 2D组件
1. 选中TorchLight对象
2. 点击 `Add Component` > 搜索 `Light 2D`
3. 配置参数：
   ```
   Light Type: Freeform
   Color: 金黄色 (R:255, G:215, B:0) 或 #FFD700
   Intensity: 1.0
   Falloff: 0.5
   Radius: 5.0
   Blend Style: Additive (通常是Index 0)
   Sort Order: 0
   Target Sorting Layers: Default (或包含你的地形图层)
   ```
4. 设置Freeform形状：
   - 在Inspector中找到"Shape Path"部分
   - 点击路径编辑器，创建一个圆形路径（推荐）
   - 或者保持默认的方形，调整"Shape Falloff Size"控制光照范围
   - "Shape Falloff Offset"可以微调光照中心位置

#### 步骤3：添加粒子系统（可选）
1. 右键TorchLight > `Effects > Particle System`
2. 重命名为 `FlameParticles`
3. 配置粒子参数：
   ```
   Duration: 5.0
   Looping: ✓
   Start Lifetime: 1.0
   Start Speed: 2.0
   Start Size: 0.2
   Start Color: 橙色到红色渐变
   
   Emission:
   - Rate over Time: 20
   
   Shape:
   - Shape: Circle
   - Radius: 0.2
   
   Velocity over Lifetime:
   - Linear Y: 1.0
   
   Color over Lifetime:
   - 从橙色(1,0.5,0)渐变到红色(1,0,0,0)
   
   Size over Lifetime:
   - 从1.0缩小到0.0
   ```

#### 步骤4：创建预制体
1. 将TorchLight拖到Project窗口的 `Assets/Prefabs` 文件夹
2. 删除场景中的TorchLight对象

### 2.2 创建玩家光源预制体

#### 步骤1：创建PlayerLight
1. 创建空GameObject，命名为 `PlayerLight`
2. 添加Light 2D组件：
   ```
   Light Type: Freeform
   Color: 柔和白色 (R:255, G:248, B:220) 或 #FFF8DC
   Intensity: 0.8
   Radius: 8.0
   Falloff: 0.3
   Blend Style: Additive
   Quality: High
   ```
3. 设置Freeform形状为圆形以模拟传统点光源效果

#### 步骤2：保存为预制体
1. 拖到 `Assets/Prefabs` 文件夹
2. 删除场景中的对象

### 2.3 创建投射物光源预制体

#### 步骤1：创建ProjectileLight
1. 创建空GameObject，命名为 `ProjectileLight`
2. 添加Light 2D组件：
   ```
   Light Type: Freeform
   Color: 白色 (可在运行时修改)
   Intensity: 1.5
   Radius: 3.0
   Falloff: 0.8
   Blend Style: Additive
   ```
3. 保持默认的Freeform形状，适合各种投射物光效

#### 步骤2：保存为预制体
1. 保存到预制体文件夹

## 第三步：配置AdvancedLightingSystem

### 3.1 创建光照管理器
1. 在Hierarchy中创建空GameObject
2. 重命名为 `AdvancedLightingSystem`
3. 添加 `AdvancedLightingSystem` 脚本

### 3.2 配置参数

#### 基础设置
```
Terrain Gen: 拖入场景中的TerrainGeneration对象
Enable Advanced Lighting: ✓
```

#### 环境光照设置
1. **Day Night Gradient**：
   - 点击颜色条，添加以下关键帧：
   ```
   Time 0%: 深蓝色 (R:10, G:10, B:46) #0A0A2E
   Time 20%: 深蓝色 (R:10, G:10, B:46) #0A0A2E  
   Time 25%: 橙色 (R:255, G:107, B:53) #FF6B35
   Time 35%: 暖白色 (R:255, G:242, B:204) #FFF2CC
   Time 50%: 纯白色 (R:255, G:255, B:255) #FFFFFF
   Time 75%: 橙红色 (R:255, G:69, B:0) #FF4500
   Time 85%: 深蓝色 (R:10, G:10, B:46) #0A0A2E
   Time 100%: 深蓝色 (R:10, G:10, B:46) #0A0A2E
   ```

2. **Ambient Intensity Curve**：
   - 创建动画曲线，添加关键点：
   ```
   Time 0, Value 0.1 (夜晚最暗)
   Time 0.25, Value 0.3 (黎明)
   Time 0.5, Value 1.0 (正午最亮)
   Time 0.75, Value 0.4 (黄昏)
   Time 1.0, Value 0.1 (夜晚)
   ```

3. **Ambient Update Interval**: 0.1

#### 光源预制体
```
Torch Light Prefab: 拖入TorchLight预制体
Player Light Prefab: 拖入PlayerLight预制体
Projectile Light Prefab: 拖入ProjectileLight预制体
```

#### 性能设置
```
Max Dynamic Lights: 50
Light Culling Distance: 30.0
```

## 第四步：配置昼夜循环系统

### 4.1 创建昼夜循环管理器
1. 创建空GameObject，命名为 `EnhancedDayNightCycle`
2. 添加 `EnhancedDayNightCycle` 脚本

### 4.2 配置时间设置
```
Day Duration Seconds: 300 (5分钟白天)
Night Duration Seconds: 180 (3分钟夜晚)
Current Time Normalized: 0.5 (从正午开始)
```

### 4.3 配置光照设置
```
Use Advanced Lighting: ✓
```

### 4.4 配置视觉效果
```
Main Camera: 拖入Main Camera
Max Fog Density: 0.05
```

### 4.5 配置太阳/月亮（可选）
1. 创建空GameObject，命名为 `SunMoon`
2. 添加Light 2D组件，配置参数：
   ```
   Light Type: Global
   Color: 白色
   Intensity: 1.0
   Blend Style: Additive
   Target Sorting Layers: Default (或包含你的所有可见图层)
   ```
3. 创建太阳光颜色渐变（可选）：
   - 在EnhancedDayNightCycle中创建Sun Moon Color Gradient
   - 建议设置：
   ```
   Time 0%: 深蓝色 #4B6CB7 (夜晚月光)
   Time 25%: 橙色 #FFB347 (日出)
   Time 50%: 亮黄色 #FFF2B2 (正午阳光)
   Time 75%: 橙红色 #FF6B35 (日落)
   Time 100%: 深蓝色 #4B6CB7 (夜晚月光)
   ```

4. 在EnhancedDayNightCycle中：
   ```
   Sun Moon Transform: 拖入SunMoon的Transform
   Sun Moon Light: 拖入SunMoon的Light2D组件
   Sun Moon Color Gradient: 配置上述颜色渐变
   ```

**注意**: Global Light 2D提供全局照明，类似于传统的Directional Light但专为2D设计。它会影响场景中所有在Target Sorting Layers中指定的对象。

## 第五步：在玩家身上添加光源

### 5.1 修改PlayerController
1. 打开PlayerController脚本
2. 在Start方法中添加：

```csharp
void Start()
{
    // 其他初始化代码...
    
    // 为玩家添加光源
    if (AdvancedLightingSystem.Instance != null)
    {
        AdvancedLightingSystem.Instance.CreatePlayerLight(transform, 0.8f, 8f);
    }
}
```

## 第六步：测试和验证

### 6.1 运行时测试
1. 点击Play按钮
2. 检查Console是否有错误
3. 观察玩家周围是否有光源
4. 使用昼夜循环的右键菜单测试：
   - 右键EnhancedDayNightCycle > "切换到白天"
   - 右键EnhancedDayNightCycle > "切换到夜晚"

### 6.2 放置火把测试
1. 在场景中创建测试脚本：

```csharp
public class LightingTester : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            // 在玩家位置放置火把
            Vector3 torchPos = transform.position + Vector3.right * 2f;
            AdvancedLightingSystem.Instance.CreateTorchLight(torchPos, 1.2f, 6f);
        }
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            // 创建爆炸光效
            AdvancedLightingSystem.Instance.CreateTemporaryLight(
                transform.position, 
                Color.yellow, 
                3f, 10f, 0.5f
            );
        }
    }
}
```

2. 将此脚本添加到玩家身上
3. 运行游戏，按T键放置火把，按E键创建爆炸光效

## 第七步：性能优化设置

### 7.1 Light 2D质量设置
对于每个Light 2D组件：
```
Quality: Medium (平衡性能和质量)
Use Normal Map: 关闭 (除非需要法线贴图)
Sorting Order: 根据需要调整
Target Sorting Layers: 确保包含所有需要光照的图层
```

#### 不同类型Light 2D的特殊设置：
- **Freeform Light 2D**: 适合局部光源，调整Radius和Falloff，可自定义Shape Path
- **Global Light 2D**: 影响整个场景，注意Target Sorting Layers设置
- **Spot Light 2D**: 适合定向光源，调整Inner/Outer Angle
- **Sprite Light 2D**: 使用纹理控制光照形状，适合特殊效果

### 7.2 URP质量设置
1. 打开 `Edit > Project Settings > XR Plug-in Management > URP`
2. 或者选择URP Asset，调整：
   ```
   Rendering:
   - Depth Texture: 关闭 (2D不需要)
   - Opaque Texture: 关闭
   
   Lighting:
   - Main Light: 启用
   - Additional Lights: 启用
   - Per Object Limit: 8-16 (根据需要)
   
   Shadows:
   - Max Distance: 0 (2D不需要阴影)
   ```

## 第八步：故障排除

### 8.1 常见问题

**问题1：光源不显示**
- 检查URP Asset配置
- 确认2D Renderer已正确设置
- 检查Light 2D的Target Sorting Layers
- 确认相机使用了正确的URP设置

**问题2：性能问题**
- 减少Max Dynamic Lights数量
- 增加Light Culling Distance
- 降低Light 2D的Quality设置
- 检查是否有过多的光源同时活跃

**问题3：光照颜色错误**
- 检查Blend Style设置（推荐Additive）
- 确认颜色值设置正确
- 检查Target Sorting Layers是否包含地形

**问题4：昼夜循环不工作**
- 确认AdvancedLightingSystem.Instance不为null
- 检查EnhancedDayNightCycle是否正确配置
- 验证Gradient和Curve设置

**问题5：Global Light 2D不起作用**
- 确认Target Sorting Layers包含所有可见的图层
- 检查Light 2D组件是否启用
- 验证Blend Style设置为Additive
- 确认2D Renderer Asset中的Light Blend Styles配置正确

**问题6：代码中的Light2D属性错误**
- Unity不同版本的Light2D API可能有差异
- 如果遇到编译错误，请检查Inspector中Light2D组件的实际属性名称
- 常见属性名称：
  - Freeform Light: `shapeLightFalloffSize`（控制光照范围）
  - Global Light: `intensity`（强度）
  - 颜色属性统一为：`color`

### 8.2 调试工具
在AdvancedLightingSystem中启用调试：
```csharp
[Header("调试")]
public bool showDebugInfo = true;
```

然后在OnGUI中添加：
```csharp
void OnGUI()
{
    if (showDebugInfo)
    {
        GUI.Label(new Rect(10, 10, 200, 20), $"活跃光源: {activeLights.Count}");
        GUI.Label(new Rect(10, 30, 200, 20), $"时间: {currentTimeOfDay:F2}");
    }
}
```

## 第九步：扩展使用

### 9.1 添加物品光源
为特定物品添加光源：

```csharp
public class GlowingItem : MonoBehaviour
{
    private GameObject itemLight;
    
    void Start()
    {
        // 为发光物品添加光源
        itemLight = AdvancedLightingSystem.Instance.CreateTemporaryLight(
            transform.position,
            Color.cyan,
            0.5f, 3f, -1f // -1表示永久
        );
    }
    
    void OnDestroy()
    {
        if (itemLight != null)
        {
            AdvancedLightingSystem.Instance.RemoveLight(itemLight);
        }
    }
}
```

### 9.2 动态光照事件
监听昼夜变化：

```csharp
void Start()
{
    EnhancedDayNightCycle.Instance.OnTimeChanged += OnTimeChanged;
}

void OnTimeChanged(bool isDay, float normalizedTime)
{
    if (isDay)
    {
        // 白天逻辑
        Debug.Log("现在是白天");
    }
    else
    {
        // 夜晚逻辑
        Debug.Log("现在是夜晚");
    }
}
```

## 完成！

现在你应该有一个完全配置好的高级光照系统。记住：
- 先在小场景测试
- 逐步增加复杂性
- 监控性能表现
- 根据需要调整参数 