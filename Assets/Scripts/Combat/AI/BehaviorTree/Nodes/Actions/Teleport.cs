using UnityEngine;

namespace Combat.AI.BehaviorTree.Nodes.Actions
{
    /// <summary>
    /// 动作节点：随机传送到目标附近的一个位置
    /// </summary>
    public class Teleport : Node
    {
        private Transform _targetTransform;
        private float _minDistance;
        private float _maxDistance;

        public Teleport(Transform bossTransform, Transform targetTransform, float minDistance, float maxDistance) : base(bossTransform)
        {
            _targetTransform = targetTransform;
            _minDistance = minDistance;
            _maxDistance = maxDistance;
        }

        public override NodeState Evaluate()
        {
            if (_targetTransform == null)
            {
                return NodeState.FAILURE;
            }
            
            // 计算一个随机角度和距离
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float randomDistance = Random.Range(_minDistance, _maxDistance);
            
            // 计算目标位置
            Vector2 offset = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * randomDistance;
            Vector2 targetPosition = (Vector2)_targetTransform.position + offset;

            // 执行传送
            _bossTransform.position = targetPosition;
            Debug.Log($"Boss 传送到了 {_bossTransform.position}");

            // 瞬移动作是瞬间完成的，所以直接返回成功
            return NodeState.SUCCESS;
        }
    }
} 