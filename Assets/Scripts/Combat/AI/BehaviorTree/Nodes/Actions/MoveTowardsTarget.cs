using UnityEngine;

namespace Combat.AI.BehaviorTree.Nodes.Actions
{
    /// <summary>
    /// 动作节点：向目标移动
    /// </summary>
    public class MoveTowardsTarget : Node
    {
        private Transform _targetTransform;
        private float _speed;
        private float _stopDistance;

        public MoveTowardsTarget(Transform bossTransform, Transform targetTransform, float speed, float stopDistance = 0.1f) : base(bossTransform)
        {
            _targetTransform = targetTransform;
            _speed = speed;
            _stopDistance = stopDistance;
        }

        public override NodeState Evaluate()
        {
            if (_targetTransform == null)
            {
                return NodeState.FAILURE;
            }

            float distance = Vector2.Distance(_bossTransform.position, _targetTransform.position);

            // 如果已经到达目标点，则成功
            if (distance <= _stopDistance)
            {
                return NodeState.SUCCESS;
            }
            
            // 否则，继续向目标移动，并返回RUNNING
            Vector2 direction = (_targetTransform.position - _bossTransform.position).normalized;
            _bossTransform.position += (Vector3)direction * _speed * Time.deltaTime;
            
            // 翻转朝向
            if (direction.x != 0)
            {
                _bossTransform.localScale = new Vector3(Mathf.Sign(direction.x), 1, 1);
            }

            return NodeState.RUNNING;
        }
    }
} 