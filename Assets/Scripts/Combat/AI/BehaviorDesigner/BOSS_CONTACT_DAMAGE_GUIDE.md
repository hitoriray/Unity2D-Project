# Boss接触伤害系统指南

## 🔥 功能概述

Boss现在具备接触伤害功能，当玩家触碰到Boss时会受到伤害和击退效果。这让Boss战更加真实和具有挑战性。

## ⚙️ 配置参数

### 在Boss预制体上配置以下参数：

- **Contact Damage (接触伤害值)**: 15
  - 每次接触造成的伤害
  - 建议值：10-20点

- **Contact Damage Interval (接触伤害间隔)**: 1.0秒
  - 连续接触的伤害间隔时间
  - 防止每帧都造成伤害
  - 建议值：0.8-1.5秒

- **Contact Knockback (接触击退力度)**: 8
  - 接触时的击退力度
  - 建议值：5-12

- **Player Layer Mask (玩家层级)**: Player层（通常是第6层）
  - 用于识别玩家对象
  - 确保与项目中的玩家层级一致

## 🔧 技术实现

### 工作原理
1. **攻击触发器**: 创建专门的子对象作为攻击触发器，设置为EnemyAttack层
2. **触发检测**: 子对象使用`OnTriggerStay2D`检测玩家碰撞
3. **间隔控制**: 通过`lastContactDamageTime`控制伤害间隔
4. **伤害计算**: 自动计算击退方向（从Boss指向玩家）
5. **状态检查**: 只有在Boss存活时才造成接触伤害

### 关键代码逻辑
```csharp
// Boss控制器自动创建攻击触发器子对象
private void SetupAttackTrigger()
{
    attackTriggerObject = new GameObject("BossAttackTrigger");
    attackTriggerObject.layer = attackTriggerLayer; // EnemyAttack层
    
    // 添加触发器和处理脚本
    CircleCollider2D triggerCollider = attackTriggerObject.AddComponent<CircleCollider2D>();
    triggerCollider.isTrigger = true;
    
    var attackTrigger = attackTriggerObject.AddComponent<BossContactTrigger>();
    attackTrigger.bossController = this;
}

// 触发器脚本检测玩家接触
void OnTriggerStay2D(Collider2D other)
{
    if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
    {
        bossController.OnPlayerContact(other);
    }
}
```

## 🎮 使用方法

### 1. 确保Layer设置正确
- **Boss主体**: 可以保持原有Layer，不需要修改
- **攻击触发器**: 系统自动创建，使用EnemyAttack层(Layer 14)
- **玩家对象**: 必须在Player层，系统通过Layer名称"Player"识别
- 系统会自动创建并配置攻击触发器子对象

### 2. 配置合理的参数
```
推荐第一阶段参数：
- Contact Damage: 15
- Contact Damage Interval: 1.0s
- Contact Knockback: 8

推荐第二阶段参数：
- Contact Damage: 20 (更高伤害)
- Contact Damage Interval: 0.8s (更频繁)
- Contact Knockback: 10 (更强击退)
```

### 3. 阶段差异化
可以通过代码动态调整不同阶段的接触伤害：
```csharp
// 第二阶段增强接触伤害（可选实现）
if (currentPhase == 2)
{
    contactDamage = 20f;
    contactDamageInterval = 0.8f;
    contactKnockback = 10f;
}
```

## 🐞 调试功能

### Scene视图可视化
- **橙色半透明区域**: 接触伤害范围（攻击触发器）
- **文本显示**: 接触伤害冷却状态和距离信息
- **子对象显示**: 可以看到"BossAttackTrigger"子对象
- 选中Boss查看实时状态和触发器范围

### 控制台测试
- 右键Boss选择"测试接触伤害"
- 查看当前设置和状态信息
- 实时监控伤害间隔

### 调试信息
```
[BossBehaviorDesignerController] 已创建攻击触发器，Layer: 14
[BossBehaviorDesignerController] Boss接触伤害：15点伤害
```

## ⚠️ 注意事项

### 1. 层级设置
- **关键要求**: 玩家对象必须在名为"Player"的Layer上
- **攻击触发器Layer**: 默认使用Layer 14 (EnemyAttack)
- **物理交互**: 确保Player层和EnemyAttack层可以相互检测
- 在Physics2D设置中检查Layer碰撞矩阵

### 2. 性能考虑
- `OnTriggerStay2D`在玩家接触期间每帧调用
- 通过时间间隔控制，避免过频繁的伤害计算
- 只在需要时才进行伤害检测

### 3. 游戏平衡
- 接触伤害不应过高，避免"秒杀"玩家
- 间隔时间要合理，给玩家反应时间
- 击退力度要适中，不能太强或太弱

## 🎯 战术影响

### 对玩家的影响
- **位置控制**: 玩家需要保持安全距离
- **时机把握**: 攻击时要快进快出
- **风险收益**: 近战攻击风险更高但可能伤害更大

### 与其他系统的配合
- **冲刺攻击**: 冲刺伤害 + 接触伤害 = 高威胁
- **飞行行为**: Boss飞行时接近玩家更危险
- **阶段转换**: 第二阶段可以增强接触伤害

## 🔄 扩展建议

### 可选增强功能
1. **伤害类型多样化**: 火焰、毒素等特殊接触效果
2. **状态效果**: 接触时施加减速、中毒等debuff
3. **视觉反馈**: 接触时的特效、震屏效果
4. **音效增强**: 接触伤害的专用音效

### 高级配置
```csharp
// 示例：根据Boss血量动态调整接触伤害
float healthPercentage = currentHealth / maxHealth;
float dynamicContactDamage = contactDamage * (2f - healthPercentage); // 血量越低伤害越高
```

## 🔧 Layer设置指南

### 必要的Layer配置
1. **创建/确认Layer**:
   - Player层（玩家）
   - EnemyAttack层（敌人攻击触发器，默认Layer 14）

2. **Physics2D碰撞矩阵设置**:
   - 打开 Edit → Project Settings → Physics2D
   - 在Layer Collision Matrix中确保:
     - Player层与EnemyAttack层可以碰撞 ✓
     - 其他不需要的Layer交互可以取消勾选

3. **验证设置**:
   - 玩家对象的Layer = "Player"
   - Boss会自动创建子对象，Layer = EnemyAttack
   - 运行时检查Console是否有触发器创建成功的日志

这个接触伤害系统让Boss战斗更加紧张和真实，玩家需要更谨慎地选择攻击时机和位置！ 