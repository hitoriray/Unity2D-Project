using System.Collections.Generic;
using UnityEngine;

namespace Combat.AI.BehaviorTree
{
    /// <summary>
    /// 行为树节点的状态
    /// </summary>
    public enum NodeState
    {
        RUNNING, // 正在运行
        SUCCESS, // 成功
        FAILURE  // 失败
    }

    /// <summary>
    /// 所有行为树节点的抽象基类
    /// </summary>
    public abstract class Node
    {
        protected NodeState state;
        
        // 新增一个对Boss Transform的引用
        protected Transform _bossTransform;

        public Node parent;
        protected List<Node> children = new List<Node>();

        // 修改构造函数以接收Transform
        public Node(Transform bossTransform)
        {
            parent = null;
            _bossTransform = bossTransform;
        }

        public Node(Transform bossTransform, List<Node> children)
        {
            _bossTransform = bossTransform;
            foreach (Node child in children)
            {
                Attach(child);
            }
        }

        private void Attach(Node node)
        {
            node.parent = this;
            children.Add(node);
        }
        
        /// <summary>
        /// 评估该节点是否可以执行。这是行为树的核心。
        /// </summary>
        /// <returns>返回节点当前的执行状态</returns>
        public abstract NodeState Evaluate();
    }
} 