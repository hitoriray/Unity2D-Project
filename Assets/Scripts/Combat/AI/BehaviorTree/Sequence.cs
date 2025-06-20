using System.Collections.Generic;
using UnityEngine;

namespace Combat.AI.BehaviorTree
{
    /// <summary>
    /// Sequence 节点（AND逻辑）
    /// 按顺序执行所有子节点。如果任何一个子节点失败，则整个序列失败。
    /// 如果一个子节点正在运行，则整个序列也处于运行状态。
    /// 只有当所有子节点都成功时，整个序列才成功。
    /// </summary>
    public class Sequence : Node
    {
        public Sequence(Transform bossTransform) : base(bossTransform) { }
        public Sequence(Transform bossTransform, List<Node> children) : base(bossTransform, children) { }

        public override NodeState Evaluate()
        {
            bool anyChildRunning = false;

            foreach (Node node in children)
            {
                switch (node.Evaluate())
                {
                    case NodeState.FAILURE:
                        state = NodeState.FAILURE;
                        return state; // 任何一个失败，整个序列就失败
                    case NodeState.SUCCESS:
                        continue; // 成功了，继续下一个
                    case NodeState.RUNNING:
                        anyChildRunning = true;
                        continue; // 正在运行，继续检查后面的，但要标记
                    default:
                        state = NodeState.SUCCESS;
                        return state;
                }
            }
            
            // 如果有正在运行的子节点，则返回RUNNING，否则返回SUCCESS
            state = anyChildRunning ? NodeState.RUNNING : NodeState.SUCCESS;
            return state;
        }
    }
} 