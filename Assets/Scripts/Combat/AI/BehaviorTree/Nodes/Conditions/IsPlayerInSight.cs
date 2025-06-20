using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace Terraira.Combat.AI
{
    [TaskCategory("AI")]
    [TaskDescription("Checks if the player is in the AI's sight.")]
    public class IsPlayerInSight : Conditional
    {
        private EnemyController enemyController;

        public override void OnStart()
        {
            enemyController = GetComponent<EnemyController>();
        }

        public override TaskStatus OnUpdate()
        {
            if (enemyController == null)
            {
                return TaskStatus.Failure;
            }

            if (enemyController.IsPlayerInSight())
            {
                return TaskStatus.Success;
            }

            return TaskStatus.Failure;
        }
    }
}