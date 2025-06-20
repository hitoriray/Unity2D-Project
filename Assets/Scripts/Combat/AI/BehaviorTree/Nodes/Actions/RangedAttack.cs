using UnityEngine;

namespace Combat.AI.BehaviorTree.Nodes.Actions
{
    /// <summary>
    /// 动作节点：执行一次远程攻击，发射投射物
    /// </summary>
    public class RangedAttack : Node
    {
        private Transform _targetTransform;
        private GameObject _projectilePrefab;
        private float _attackCooldown;
        private float _lastAttackTime = -999f;

        public RangedAttack(Transform bossTransform, Transform targetTransform, GameObject projectilePrefab, float attackCooldown) : base(bossTransform)
        {
            _targetTransform = targetTransform;
            _projectilePrefab = projectilePrefab;
            _attackCooldown = attackCooldown;
        }

        public override NodeState Evaluate()
        {
            if (_targetTransform == null || _projectilePrefab == null)
            {
                return NodeState.FAILURE;
            }

            // 检查攻击冷却
            if (Time.time < _lastAttackTime + _attackCooldown)
            {
                // 技能在冷却中，返回失败，让行为树尝试其他行为
                return NodeState.FAILURE;
            }

            // 执行攻击
            Debug.Log("执行远程攻击！");
            
            Vector2 direction = (_targetTransform.position - _bossTransform.position).normalized;
            GameObject projectile = Object.Instantiate(_projectilePrefab, _bossTransform.position, Quaternion.identity);
            
            // 设置投射物的方向和伤害
            if (projectile.TryGetComponent<Projectile>(out var p))
            {
                p.Initialize(direction, 10, _bossTransform.gameObject.layer);
            }

            _lastAttackTime = Time.time;
            
            // 攻击动作是瞬间的（发射），所以直接返回成功
            return NodeState.SUCCESS;
        }
    }
} 