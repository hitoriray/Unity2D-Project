using System;
using UnityEngine;

namespace Combat.AI.BehaviorTree.Nodes.Actions
{
    public class SetAttackStep : Node
    {
        private readonly Action<int> _setAttackStep;
        private readonly int _newStep;

        public SetAttackStep(Transform transform, Action<int> setAttackStep, int newStep) : base(transform)
        {
            _setAttackStep = setAttackStep;
            _newStep = newStep;
        }

        public override NodeState Evaluate()
        {
            _setAttackStep(_newStep);
            return NodeState.SUCCESS;
        }
    }
} 