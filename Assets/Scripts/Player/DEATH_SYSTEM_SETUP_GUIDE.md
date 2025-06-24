# 🪦 玩家死亡系统设置指南

## 📋 概述

PlayerController现在包含完整的死亡系统功能，支持死亡音效播放、墓碑生成和自动重生机制。

## ⚙️ 配置步骤

### 1. 死亡音效配置

在PlayerController组件的**Audio**部分：

```
Death Sounds: 
- 添加一个或多个死亡音效AudioClip
- 系统会随机播放其中一个
- 如果为空，会显示警告但不影响其他功能
```

**推荐音效**：
- 玩家痛苦的呻吟声
- 死亡时的音效
- 可以准备2-3个不同的死亡音效增加变化

### 2. 墓碑系统配置

在PlayerController组件的**Death System**部分：

```
Tombstone Prefab: 墓碑预制体
Tombstone Offset: 墓碑生成偏移（默认Vector2.zero）
Auto Respawn: 是否自动重生（默认true）
Respawn Delay: 重生延迟时间（默认3秒）
Respawn Point: 重生点Transform（可选，默认使用出生点）
```

### 3. 创建墓碑预制体

1. **创建墓碑GameObject**：
   - 在场景中创建空GameObject，命名为"Tombstone"
   - 添加SpriteRenderer组件
   - 设置墓碑的Sprite图片

2. **添加碰撞体（可选）**：
   - 添加Collider2D组件
   - 如果需要玩家能与墓碑交互

3. **制作成预制体**：
   - 将墓碑拖拽到Project窗口的Prefabs文件夹
   - 删除场景中的临时墓碑

4. **配置到PlayerController**：
   - 将墓碑预制体拖拽到PlayerController的Tombstone Prefab字段

## 🎮 功能说明

### 死亡流程

1. **触发死亡**：玩家血量≤0时自动触发
2. **播放音效**：随机播放配置的死亡音效
3. **生成墓碑**：在玩家死亡位置生成墓碑
4. **禁用操作**：停止玩家移动和所有输入响应
5. **开始重生**：根据配置延迟后自动重生

### 重生机制

- **自动重生**：默认3秒后自动重生到出生点
- **手动重生**：可以调用`ManualRespawn()`方法立即重生
- **重生点**：优先使用配置的重生点，否则使用出生点
- **状态恢复**：重生时恢复满血量和正常状态

### 死亡保护

- **死亡状态**：死亡后不会再受到伤害
- **重复保护**：防止重复触发死亡流程
- **状态重置**：重生时完全重置玩家状态

## 🔧 高级配置

### 墓碑偏移

```csharp
// 在玩家脚下生成墓碑
tombstoneOffset = new Vector2(0, -1);

// 在玩家右侧生成墓碑  
tombstoneOffset = new Vector2(1, 0);
```

### 自定义重生点

```csharp
// 创建重生点GameObject
GameObject respawnPoint = new GameObject("RespawnPoint");
respawnPoint.transform.position = new Vector3(10, 5, 0);

// 配置到PlayerController
playerController.respawnPoint = respawnPoint.transform;
```

### 禁用自动重生

```csharp
// 关闭自动重生，需要手动调用重生
playerController.autoRespawn = false;

// 稍后手动重生
playerController.ManualRespawn();
```

## 🎯 公共API

### 可调用方法

```csharp
// 检查玩家是否死亡
bool isDead = playerController.IsDead();

// 手动重生玩家
playerController.ManualRespawn();

// 手动触发死亡（测试用）
playerController.TakeDamage(new DamageInfo { baseDamage = 9999 });
```

### 事件扩展（可选）

如果需要监听死亡事件，可以在Die()方法中添加事件回调：

```csharp
// 在Die()方法中添加
public static event System.Action OnPlayerDeath;
OnPlayerDeath?.Invoke();

// 在其他脚本中监听
PlayerController.OnPlayerDeath += () => {
    Debug.Log("玩家死亡了！");
};
```

## ⚠️ 注意事项

1. **音效资源**：确保死亡音效文件已正确导入Unity
2. **墓碑预制体**：必须配置才能正常生成墓碑
3. **重生点**：如果不设置会使用默认出生点
4. **性能优化**：墓碑会持续存在，考虑添加清理机制
5. **UI集成**：可以在UI中显示重生倒计时

## 🔄 与现有系统集成

- ✅ **血量UI系统**：完全兼容，死亡和重生时会自动更新
- ✅ **战斗系统**：死亡状态下不会受到伤害
- ✅ **音效系统**：使用SoundEffectManager播放音效
- ✅ **动画系统**：支持SPUM动画集成

## 🚀 未来扩展

系统预留了以下扩展接口：

1. **死亡惩罚**：可以在Die()方法中添加掉落金币、经验等
2. **墓碑交互**：可以为墓碑添加交互脚本
3. **复活道具**：可以集成复活道具系统
4. **死亡统计**：可以记录死亡次数和原因

此死亡系统为完整的泰拉瑞亚风格死亡机制奠定了基础！ 