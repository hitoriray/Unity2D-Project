# 史莱姆Unity设置指导

## 📋 史莱姆GameObject层级结构

```
Slime (主对象)
├── 主碰撞体 (Collider2D) - 用于物理碰撞
├── 攻击触发器 (Collider2D) - 用于攻击检测
├── Sprite Renderer - 显示史莱姆图像
├── Rigidbody2D - 物理组件
├── SlimeController脚本
└── GroundCheck (空GameObject)
    └── 地面检测点
```

## ⚙️ 详细组件设置

### **1. 史莱姆主对象设置**

**Layer设置:**
- 将史莱姆设置为 `Enemy` 层 (Layer 8)
- 如果没有Enemy层，在 `Edit > Project Settings > Tags and Layers` 中创建

**Transform:**
- Position: 根据场景需要
- Scale: (1, 1, 1) 或根据需要调整

### **2. Rigidbody2D设置**

```
Mass: 1
Linear Drag: 0.5
Angular Drag: 0.05
Gravity Scale: 3
Freeze Rotation: Z轴勾选 ✓
```

### **3. 主碰撞体设置 (用于物理交互)**

**组件:** `BoxCollider2D` 或 `CircleCollider2D`
```
Is Trigger: ❌ (不勾选)
Size: 根据史莱姆sprite调整
Offset: (0, 0) 或微调到合适位置
```

### **4. 攻击触发器设置 (用于伤害检测)**

**添加第二个碰撞体:** `Add Component > BoxCollider2D`
```
Is Trigger: ✅ (必须勾选)
Size: 比主碰撞体稍大一些
Offset: (0, 0)
```

### **5. GroundCheck子对象设置**

**创建空GameObject:**
1. 右键史莱姆 → `Create Empty`
2. 命名为 "GroundCheck"
3. 位置设置在史莱姆底部稍下方

**Transform设置:**
```
Position: (0, -0.6, 0) // 相对于史莱姆
Scale: (1, 1, 1)
```

### **6. SlimeController脚本配置**

**Stats设置:**
- 拖拽 `AIStats` ScriptableObject 到 `Stats` 字段
- 如果没有，右键Project → `Create > AI > AIStats`

**Ground Check设置:**
```
Ground Check: 拖拽GroundCheck子对象
Ground Check Radius: 0.1
Ground Layer: 选择地面层级 (通常是Default或Ground)
```

**Attack System设置:**
```
Attack Damage: 15
Knockback Force: 5
Attack Cooldown: 2
Attack Range: 1.2
Player Layer: 选择Player层 (Layer 3)
```

**Animation Sprites:**
```
Idle Sprite: 史莱姆静止图片
Preparing Jump Sprite: 准备跳跃图片  
In Air Sprite: 空中图片
Landing Sprite: 着陆图片
```

**Animation Settings:**
```
Prepare Jump Duration: 0.3
Landing Squash Intensity: 0.8
Jump Stretch Intensity: 1.3
Scale Recovery Time: 0.2
Breathing Intensity: 0.05
Breathing Speed: 2
```

**AI Behavior:**
```
Wander Hop Force: 8
Chase Hop Force: 12
Attack Hop Force: 15
Min Wander Interval: 2
Max Wander Interval: 4
Chase Hop Interval: 1.2
```

**Audio设置:**
```
Hop Sound: 跳跃音效
Attack Sound: 攻击音效
Hurt Sound: 受伤音效
Death Sound: 死亡音效
```

## 🎯 Layer层级设置

### **创建所需层级:**

1. 打开 `Edit > Project Settings > Tags and Layers`
2. 添加以下层级:
   - Layer 3: Player
   - Layer 8: Enemy
   - Layer 9: Ground (如果需要)

### **Layer Collision Matrix设置:**

1. 打开 `Edit > Project Settings > Physics 2D`
2. 在 `Layer Collision Matrix` 中：
   - **Player (3) vs Enemy (8)**: ❌ 取消勾选 (避免物理碰撞)
   - **Enemy (8) vs Ground (9)**: ✅ 保持勾选
   - **Player (3) vs Ground (9)**: ✅ 保持勾选

## 🎮 玩家设置调整

### **玩家Layer设置:**
- 将玩家GameObject设置为 `Player` 层 (Layer 3)

### **玩家碰撞体设置:**
```
主碰撞体: Is Trigger = ❌ (物理碰撞)
攻击触发器: Is Trigger = ✅ (如果有攻击检测)
```

## 🔧 AIStats ScriptableObject设置

如果没有AIStats资产:

1. 在Project窗口右键
2. `Create > ScriptableObject` 
3. 选择AIStats
4. 设置参数:
   ```
   Max Health: 50
   Detection Radius: 5-10 (索敌范围)
   Move Speed: 5
   Attack Damage: 15
   ```

## 🎨 Sprite设置建议

**史莱姆动画Sprite要求:**
- **Idle**: 正常状态，稍微扁一点的圆形
- **Preparing Jump**: 更扁的形状，准备蓄力
- **In Air**: 拉长的椭圆形，在空中
- **Landing**: 压扁的形状，刚落地

**Sprite Import设置:**
```
Sprite Mode: Single
Pixels Per Unit: 100
Filter Mode: Point (适合像素艺术)
Compression: None
```

## 🐛 调试和测试

### **Scene视图调试:**
1. 选中史莱姆
2. 在Scene视图中会看到：
   - 黄色圆圈: 索敌范围
   - 红色圆圈: 攻击范围  
   - 绿色/红色圆圈: 主地面检测
   - 青色/紫色圆圈: 备用地面检测
   - 白色/灰色射线: 射线地面检测

### **Console调试信息:**
每3秒会打印史莱姆状态，包括：
- 当前AI状态
- 动画状态
- 地面检测详情
- 到玩家距离
- 各种冷却时间

### **常见问题解决:**

**Q: 史莱姆不跳跃**
- 检查 `Ground Layer` 设置是否正确
- 检查 `GroundCheck` 位置是否合适
- 查看Console调试信息中的地面检测状态

**Q: 史莱姆不攻击玩家**
- 检查 `Player Layer` 设置
- 确认攻击触发器 `Is Trigger` 已勾选
- 检查Layer Collision Matrix设置

**Q: 玩家和史莱姆会碰撞**
- 在Physics 2D设置中取消Player和Enemy层的碰撞
- 确认史莱姆有两个碰撞体（物理+触发器）

**Q: 史莱姆动画不流畅**
- 检查Sprite设置是否正确
- 确认所有4个动画Sprite都已分配
- 调整Animation Settings参数

这样设置后，史莱姆就能正常工作，有流畅的动画和攻击系统，同时避免与玩家的物理碰撞！ 