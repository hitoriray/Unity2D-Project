using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace Terraira.Combat.AI
{
    [TaskCategory("AI")]
    [TaskDescription("Checks if the player is in the AI's attack range.")]
    public class IsPlayerInAttackRange : Conditional
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

            if (enemyController.IsPlayerInAttackRange())
            {
                return TaskStatus.Success;
            }

            return TaskStatus.Failure;
        }
    }
}