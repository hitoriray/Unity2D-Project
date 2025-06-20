using UnityEngine;

namespace Combat.AI.BehaviorTree.Nodes.Actions
{
    public class SpawnMinions : Node
    {
        private readonly GameObject _minionPrefab;
        private readonly int _spawnCount;
        private readonly float _spawnRadius;

        public SpawnMinions(Transform transform, GameObject minionPrefab, int spawnCount, float spawnRadius) : base(transform)
        {
            _minionPrefab = minionPrefab;
            _spawnCount = spawnCount;
            _spawnRadius = spawnRadius;
        }

        public override NodeState Evaluate()
        {
            if (_minionPrefab == null)
            {
                Debug.LogError("Minion Prefab not set in SpawnMinions node!");
                return NodeState.FAILURE;
            }

            for (int i = 0; i < _spawnCount; i++)
            {
                Vector2 spawnPos = (Vector2)_bossTransform.position + Random.insideUnitCircle * _spawnRadius;
                Object.Instantiate(_minionPrefab, spawnPos, Quaternion.identity);
            }

            return NodeState.SUCCESS; // Spawning is instantaneous
        }
    }
} 