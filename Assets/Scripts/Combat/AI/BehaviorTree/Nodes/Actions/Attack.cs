using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

// Assuming the EnemyController is in a namespace that can be found.
// The user has indicated that EnemyController.cs exists and has an ExecuteAttack method.
namespace Terraira.Combat.AI
{
    [TaskCategory("AI")]
    [TaskDescription("Executes an attack using the EnemyController.")]
    public class Attack : Action
    {
        private EnemyController enemyController;

        public override void OnStart()
        {
            enemyController = GetComponent<EnemyController>();
        }

        public override TaskStatus OnUpdate()
        {
            if (enemyController != null)
            {
                enemyController.ExecuteAttack();
                return TaskStatus.Success;
            }
            else
            {
                Debug.LogWarning("EnemyController not found!");
                return TaskStatus.Failure;
            }
        }
    }
}