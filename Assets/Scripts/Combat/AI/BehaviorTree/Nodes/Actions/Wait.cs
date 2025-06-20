using UnityEngine;

namespace Combat.AI.BehaviorTree.Nodes.Actions
{
    /// <summary>
    /// 动作节点：等待指定的时间
    /// </summary>
    public class Wait : Node
    {
        private float _duration;
        private float _startTime;

        // 节点状态需要一个内部变量来跟踪，而不是依赖基类的state，以避免多次进入时重置问题
        private bool _isWaiting = false;

        public Wait(Transform bossTransform, float duration) : base(bossTransform)
        {
            _duration = duration;
        }

        public override NodeState Evaluate()
        {
            // 在第一次进入该节点时，记录开始时间并标记为正在等待
            if (!_isWaiting)
            {
                _startTime = Time.time;
                _isWaiting = true;
            }

            // 如果已经等待了足够长的时间，则成功，并重置等待标记
            if (Time.time >= _startTime + _duration)
            {
                _isWaiting = false;
                return NodeState.SUCCESS;
            }
            
            // 否则，继续等待，并返回RUNNING
            return NodeState.RUNNING;
        }
    }
} 