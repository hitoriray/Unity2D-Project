using System;
using UnityEngine;

namespace Combat.AI.BehaviorTree.Nodes.Conditions
{
    public class CheckAttackStep : Node
    {
        private readonly Func<int> _getAttackStep;
        private readonly int _expectedStep;

        public CheckAttackStep(Transform transform, Func<int> getAttackStep, int expectedStep) : base(transform)
        {
            _getAttackStep = getAttackStep;
            _expectedStep = expectedStep;
        }

        public override NodeState Evaluate()
        {
            return _getAttackStep() == _expectedStep ? NodeState.SUCCESS : NodeState.FAILURE;
        }
    }
} 