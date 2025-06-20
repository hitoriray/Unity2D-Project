using UnityEngine;

namespace Combat.AI.BehaviorTree.Nodes.Conditions
{
    /// <summary>
    /// 条件节点：检查玩家是否在指定范围内
    /// </summary>
    public class IsPlayerInRange : Node
    {
        private Transform _playerTransform;
        private float _range;

        public IsPlayerInRange(Transform bossTransform, Transform playerTransform, float range) : base(bossTransform)
        {
            _playerTransform = playerTransform;
            _range = range;
        }

        public override NodeState Evaluate()
        {
            if (_playerTransform == null)
            {
                // 如果找不到玩家，则此条件失败
                return NodeState.FAILURE;
            }

            float distance = Vector2.Distance(_bossTransform.position, _playerTransform.position);
            
            // 如果玩家在范围内，则成功，否则失败
            return distance <= _range ? NodeState.SUCCESS : NodeState.FAILURE;
        }
    }
} 