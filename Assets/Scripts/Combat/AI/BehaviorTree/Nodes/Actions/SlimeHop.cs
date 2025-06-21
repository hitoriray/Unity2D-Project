using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;


namespace SlimeAI.BehaviorTree.Actions
{
    [TaskCategory("Slime")]
    [TaskDescription("泰拉瑞亚风格史莱姆跳跃节点。智能地根据玩家位置和距离选择跳跃类型：闲逛、追击或攻击")]
    public class SlimeHop : Action
    {
        [Header("Behavior Settings")]
        [BehaviorDesigner.Runtime.Tasks.Tooltip("攻击距离阈值")]
        public SharedFloat attackDistance = 1.5f;
        
        [BehaviorDesigner.Runtime.Tasks.Tooltip("是否在攻击范围内时强制攻击")]
        public SharedBool forceAttackInRange = true;
        
        [BehaviorDesigner.Runtime.Tasks.Tooltip("是否显示调试信息")]
        public SharedBool showDebugInfo = false;

        private SlimeController slimeController;
        private Transform playerTransform;

        public override void OnStart()
        {
            slimeController = GetComponent<SlimeController>();
            if (slimeController == null)
            {
                Debug.LogError($"[SlimeHop] GameObject '{gameObject.name}' 缺少 SlimeController 组件！");
                return;
            }

            var playerController = Object.FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                playerTransform = playerController.transform;
            }
            else
            {
                Debug.LogWarning("[SlimeHop] 场景中未找到 PlayerController！");
            }
        }

        public override TaskStatus OnUpdate()
        {
            if (slimeController == null)
            {
                Debug.LogError("[SlimeHop] SlimeController 组件未找到，任务失败");
                return TaskStatus.Failure;
            }

            // 根据玩家状态智能选择跳跃类型
            JumpType jumpType = DetermineJumpType();
            
            // 执行对应的跳跃行为
            ExecuteJump(jumpType);
            
            if (showDebugInfo.Value)
            {
                Debug.Log($"[SlimeHop] 执行 {jumpType} 跳跃");
            }

            return TaskStatus.Success;
        }

        /// <summary>
        /// 跳跃类型枚举
        /// </summary>
        private enum JumpType
        {
            Idle,    // 闲逛跳跃
            Chase,   // 追击跳跃
            Attack   // 攻击跳跃
        }

        /// <summary>
        /// 智能决定跳跃类型
        /// </summary>
        private JumpType DetermineJumpType()
        {
            // 如果没有玩家或玩家不在视野内，执行闲逛跳跃
            if (playerTransform == null || !slimeController.IsPlayerInSight())
            {
                return JumpType.Idle;
            }

            // 计算到玩家的距离
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

            // 如果在攻击范围内且设置了强制攻击，执行攻击跳跃
            if (forceAttackInRange.Value && distanceToPlayer <= attackDistance.Value)
            {
                return JumpType.Attack;
            }

            // 玩家在视野内但不在攻击范围，执行追击跳跃
            return JumpType.Chase;
        }

        /// <summary>
        /// 执行指定类型的跳跃
        /// </summary>
        private void ExecuteJump(JumpType jumpType)
        {
            switch (jumpType)
            {
                case JumpType.Idle:
                    slimeController.PerformIdleHop();
                    if (showDebugInfo.Value)
                        Debug.Log("[SlimeHop] 执行闲逛跳跃 - 玩家不在视野内");
                    break;

                case JumpType.Chase:
                    slimeController.PerformHopTowardsTarget();
                    if (showDebugInfo.Value)
                        Debug.Log("[SlimeHop] 执行追击跳跃 - 朝向玩家");
                    break;

                case JumpType.Attack:
                    // 对于攻击跳跃，使用追击跳跃但可以在SlimeController内部处理攻击逻辑
                    slimeController.PerformHopTowardsTarget();
                    if (showDebugInfo.Value)
                        Debug.Log("[SlimeHop] 执行攻击跳跃 - 玩家在攻击范围内");
                    break;
            }
        }

        /// <summary>
        /// 在编辑器中绘制调试信息
        /// </summary>
        public override void OnDrawGizmos()
        {
            if (!showDebugInfo.Value) return;

            // 绘制攻击范围
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackDistance.Value);

            // 绘制到玩家的连线
            if (playerTransform != null)
            {
                Gizmos.color = slimeController != null && slimeController.IsPlayerInSight() ? Color.green : Color.gray;
                Gizmos.DrawLine(transform.position, playerTransform.position);
            }
        }

        /// <summary>
        /// 重置节点状态
        /// </summary>
        public override void OnReset()
        {
            attackDistance = 1.5f;
            forceAttackInRange = true;
            showDebugInfo = false;
        }
    }
}