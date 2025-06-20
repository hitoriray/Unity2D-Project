using UnityEngine;

namespace Combat.AI.BehaviorTree.Nodes.Actions
{
    public class Charge : Node
    {
        private readonly Transform _playerTransform;
        private readonly Rigidbody2D _rb;

        private readonly float _chargeSpeed;
        private readonly float _chargeDuration;
        
        private Vector2 _chargeTargetPosition;
        private float _startTime;

        public Charge(Transform transform, Transform playerTransform, float chargeSpeed, float chargeDuration) : base(transform)
        {
            _playerTransform = playerTransform;
            _rb = transform.GetComponent<Rigidbody2D>();
            _chargeSpeed = chargeSpeed;
            _chargeDuration = chargeDuration;
        }

        public override NodeState Evaluate()
        {
            if (state != NodeState.RUNNING)
            {
                // OnEnter logic
                _chargeTargetPosition = _playerTransform.position;
                _startTime = Time.time;
            
                Vector2 direction = (_chargeTargetPosition - (Vector2)_bossTransform.position).normalized;
                _rb.velocity = direction * _chargeSpeed;
                
                state = NodeState.RUNNING;
            }

            // OnUpdate logic
            if (Time.time - _startTime >= _chargeDuration || _playerTransform == null)
            {
                _rb.velocity = Vector2.zero; // Charge ends, stop moving
                // OnExit logic would be here
                return NodeState.SUCCESS;
            }

            return NodeState.RUNNING;
        }
    }
} 