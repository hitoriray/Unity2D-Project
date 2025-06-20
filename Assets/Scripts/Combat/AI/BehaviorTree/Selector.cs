using System.Collections.Generic;
using UnityEngine;

namespace Combat.AI.BehaviorTree
{
    /// <summary>
    /// Selector 节点（OR逻辑）
    /// 按顺序执行其子节点，直到找到一个成功或正在运行的节点。
    /// 如果一个子节点成功或正在运行，Selector将立即返回相同的状态。
    /// 如果所有子节点都失败了，Selector才会失败。
    /// </summary>
    public class Selector : Node
    {
        public Selector(Transform bossTransform) : base(bossTransform) { }
        public Selector(Transform bossTransform, List<Node> children) : base(bossTransform, children) { }

        public override NodeState Evaluate()
        {
            foreach (Node node in children)
            {
                switch (node.Evaluate())
                {
                    case NodeState.FAILURE:
                        continue; // 失败了，尝试下一个
                    case NodeState.SUCCESS:
                        state = NodeState.SUCCESS;
                        return state; // 成功了，整个Selector就成功了
                    case NodeState.RUNNING:
                        state = NodeState.RUNNING;
                        return state; // 正在运行，整个Selector也正在运行
                    default:
                        continue;
                }
            }

            state = NodeState.FAILURE;
            return state;
        }
    }
} 