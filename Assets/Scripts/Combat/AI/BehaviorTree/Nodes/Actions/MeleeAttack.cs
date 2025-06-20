using UnityEngine;

namespace Combat.AI.BehaviorTree.Nodes.Actions
{
    /// <summary>
    /// 动作节点：执行一次近战攻击
    /// </summary>
    public class MeleeAttack : Node
    {
        private float _attackTime; // 整个攻击动作的持续时间
        private float _damageTime; // 在攻击动作开始后，造成伤害的时间点
        
        private float _startTime;
        private bool _hasDealtDamage;
        private bool _isAttacking = false;
        
        // 引用玩家层，用于伤害检测
        private LayerMask _playerLayer;

        public MeleeAttack(Transform bossTransform, float attackTime, float damageTime, LayerMask playerLayer) : base(bossTransform)
        {
            _attackTime = attackTime;
            _damageTime = damageTime;
            _playerLayer = playerLayer;
        }

        public override NodeState Evaluate()
        {
            if (!_isAttacking)
            {
                // 首次进入，开始攻击
                Debug.Log("开始近战攻击！");
                _startTime = Time.time;
                _isAttacking = true;
                _hasDealtDamage = false;
                // 在这里可以触发攻击动画
                // _bossTransform.GetComponent<Animator>().Play("Attack");
            }

            // --- 攻击逻辑 ---
            if (Time.time >= _startTime + _attackTime)
            {
                // 攻击时间到，攻击结束，返回成功
                Debug.Log("攻击结束。");
                _isAttacking = false;
                return NodeState.SUCCESS;
            }
            
            if (!_hasDealtDamage && Time.time >= _startTime + _damageTime)
            {
                // 到达伤害判定的时间点
                Debug.Log("造成伤害！");
                _hasDealtDamage = true;
                
                // 在Boss前方创建一个小范围的伤害检测
                Collider2D[] hits = Physics2D.OverlapCircleAll(
                    _bossTransform.position + _bossTransform.right * 0.5f, 
                    0.5f, 
                    _playerLayer);

                foreach (var hit in hits)
                {
                    // 假设玩家身上有IDamageable接口
                    if (hit.TryGetComponent<Combat.Interfaces.IDamageable>(out var damageable))
                    {
                        // 这里的DamageInfo应该是从Boss的Stats里获取
                        damageable.TakeDamage(new DamageInfo(10, DamageType.Physical, _bossTransform.gameObject, hit.transform.position));
                    }
                }
            }

            // 攻击动作还未结束，返回RUNNING
            return NodeState.RUNNING;
        }
    }
} 