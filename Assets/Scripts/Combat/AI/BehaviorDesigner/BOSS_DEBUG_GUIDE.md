# 克苏鲁之眼Boss调试指南

## 🐛 常见问题与解决方案

### 问题1: Boss一直围绕玩家飞行，不发起冲撞攻击

**原因分析：**
- 距离检测条件设置不当
- 行为树中的条件节点配置错误
- 攻击触发频率设置过低

**解决方案：**

1. **检查行为树配置**
   - 确保在行为树中有`EoCCheckDistance`条件节点
   - 设置合理的距离范围：最小距离3-5单位，最大距离6-8单位
   - 在条件节点后正确连接`EoCChargeAttack`动作节点

2. **调整距离检测参数**
   ```
   EoCCheckDistance组件设置：
   - Min Distance: 3-5
   - Max Distance: 6-8
   ```

3. **检查Composite节点**
   - 使用`Selector`节点组合飞行和攻击行为
   - 确保攻击条件的优先级高于飞行行为

### 问题2: Boss朝向错误（正面不朝向玩家）

**原因分析：**
- Unity中Sprite的默认朝向与Boss设计不符
- 角度计算中未考虑Sprite的初始方向

**解决方案：**

✅ **已修复：** 角度计算已调整为：
```csharp
float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg + 90f;
```

**验证方法：**
1. 播放游戏
2. Boss应该始终"正面"（如眼睛）朝向玩家
3. 如果还是不对，可能需要调整Sprite的Pivot点

### 问题3: 不同阶段Sprite大小不一致

**原因分析：**
- 第一阶段和第二阶段的Sprite像素密度不同
- Unity的Pixels Per Unit设置不一致

**解决方案：**

✅ **已修复：** 添加了阶段缩放配置：

1. **在Boss预制体中设置缩放：**
   ```
   Phase 1 Sprite Scale: (1, 1, 1)
   Phase 2 Sprite Scale: (1.2, 1.2, 1) // 根据需要调整
   ```

2. **自动应用机制：**
   - 第一阶段：初始化和切换时立即应用缩放
   - 第二阶段：通过转换特效应用缩放（包含放大→缩回到正确大小的动画）
   - 可通过右键Boss选择"测试第二阶段缩放"进行测试

3. **替代方案：**
   - 统一所有Sprite的Pixels Per Unit值
   - 在图像编辑软件中调整Sprite的实际尺寸

### 问题4: Boss吼叫音效和时机

**特性说明：**
- 第一阶段：吼叫0.5秒后冲撞
- 第二阶段：吼叫0.3秒后冲撞（更激进）

**配置方法：**
1. 在Boss预制体上设置`Roar Sound`音效
2. 调整`Roar Delay`和`Roar Delay Phase2`参数

## 🔧 行为树配置建议

### 第一阶段行为树结构：
```
Selector
├── Sequence (攻击序列)
│   ├── EoCCheckDistance (距离检查 3-8)
│   └── EoCChargeAttack (冲撞攻击)
└── EoCFlyAroundPlayer (默认飞行)
```

### 第二阶段行为树结构：
```
Selector
├── Sequence (频繁攻击)
│   ├── EoCCheckDistance (距离检查 3-6)
│   └── EoCChargeAttack (快速冲撞)
├── Sequence (短暂待机)
│   ├── Random (20%概率)
│   └── EoCIdle (0.5秒)
└── EoCFlyAroundPlayer (激进飞行)
```

## 🎮 测试步骤

1. **基础功能测试**
   - Boss能否正确生成和初始化
   - 血条UI是否正常显示
   - Sprite动画是否播放

2. **AI行为测试**
   - Boss是否围绕玩家飞行
   - 在合适距离时是否发起冲撞
   - 受伤后是否有正确反馈

3. **阶段切换测试**
   - 血量降到50%时是否切换阶段
   - 第二阶段行为是否更激进
   - Sprite缩放是否正确

4. **音效测试**
   - 生成、吼叫、受伤、死亡音效
   - 不同阶段的吼叫延迟差异

## 🐞 调试工具

1. **控制台输出**
   - 启用`Enable Debug Info`查看详细日志
   - 观察距离检测的实时输出

2. **Scene视图可视化**
   - 选中Boss查看Gizmos绘制的距离圈和朝向线
   - 绿色圆圈：目标位置
   - 黄色圆圈：维持距离
   - 红色圆圈：攻击范围

3. **上下文菜单测试**
   - 右键Boss选择"测试受伤"
   - 使用"立即死亡"测试死亡流程
   - "测试动画系统"检查动画状态
   - "测试接触伤害"检查接触伤害设置
   - "测试第二阶段缩放"强制切换阶段测试缩放效果

## 📊 推荐参数值

### EoCFlyAroundPlayer
- Fly Speed: 8-12
- Maintain Distance: 5-7
- Distance Tolerance: 1-2
- Turn Speed: 3-5

### EoCChargeAttack
- Charge Speed: 15-25
- Charge Duration: 1.0-2.0
- Charge Damage: 20-30
- Roar Delay: 0.5 (第一阶段), 0.3 (第二阶段)

### EoCCheckDistance
- Min Distance: 3-5
- Max Distance: 6-8

### Sprite缩放配置
- Phase 1 Sprite Scale: (1, 1, 1) 
- Phase 2 Sprite Scale: (1.2, 1.2, 1) 或根据实际Sprite大小调整
- 建议第二阶段比第一阶段大20%-50%以体现Boss的愤怒状态

## 🚨 紧急修复

如果Boss完全不攻击：
1. 检查PlayerController组件是否存在
2. 确认行为树Asset是否正确指定
3. 验证Layer Mask设置（玩家应该在Player层）
4. 检查Behavior Tree组件是否启用

如果Boss朝向还是不对：
1. 尝试调整角度偏移：`-90f` → `-180f` 或 `0f`
2. 检查Sprite的Pivot设置（应该是Center）
3. 验证Transform.rotation是否被其他脚本影响 