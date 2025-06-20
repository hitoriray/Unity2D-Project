using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace SlimeAI.BehaviorTree.Actions
{
    [TaskCategory("Slime")]
    [TaskDescription("执行一次跳跃。如果玩家在视野内，则朝向玩家跳跃；否则，执行原地跳跃。")]
    public class SlimeHop : Action
    {
        // hopTowardsTarget 字段不再需要，因为节点会根据上下文智能判断
        // [BehaviorDesigner.Runtime.Tasks.Tooltip("Should the slime hop towards the target?")]
        // public bool hopTowardsTarget = false;

        private SlimeController slimeController;

        public override void OnStart()
        {
            slimeController = GetComponent<SlimeController>();
        }

        public override TaskStatus OnUpdate()
        {
            if (slimeController == null)
            {
                Debug.LogWarning("SlimeController component not found on the GameObject.");
                return TaskStatus.Failure;
            }

            // 节点现在会根据是否能看到玩家来智能地决定执行哪种跳跃
            if (slimeController.IsPlayerInSight())
            {
                Debug.Log("hop to player");
                slimeController.PerformHopTowardsTarget();
            }
            else
            {
                Debug.Log("hop idle");
                slimeController.PerformIdleHop();
            }

            return TaskStatus.Success;
        }
    }
}