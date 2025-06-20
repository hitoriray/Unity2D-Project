using UnityEngine;
using System.Collections.Generic;
using Combat.AI.BehaviorTree;
using Combat.Interfaces;
using Combat.AI.BehaviorTree.Nodes.Actions;
using Combat.AI.BehaviorTree.Nodes.Conditions;

// 我们将这个Controller也放入主命名空间，以确保可见性
namespace Combat.AI.BehaviorTree
{
    public class BossBehaviorTreeController : MonoBehaviour, IDamageable
    {
        [Header("AI Stats")]
        public float maxHealth = 1000f;
        public float currentHealth;
        public LayerMask playerLayer;

        [Header("Phase 1 Settings")]
        public float p1_chargeSpeed = 15f;
        public float p1_chargeDuration = 0.5f;
        public int p1_servantCount = 3;
        public float p1_spawnRadius = 3f;

        [Header("Phase 2 Settings")]
        public float p2_chargeSpeed = 25f;
        public float p2_chargeDuration = 0.4f;

        [Header("Behavior Timings")]
        public float shortWaitBetweenCharges = 0.5f;
        public float longWaitAfterCycle = 2f;
        
        [Header("Assets")]
        public GameObject servantPrefab;

        private Node _root;
        private Transform _playerTransform;
        
        private bool _isEnraged = false;
        private int _attackStep = 0;

        private void Start()
        {
            currentHealth = maxHealth;
            _playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (_playerTransform == null)
            {
                Debug.LogError("Player not found! Make sure the player has the 'Player' tag.");
                this.enabled = false;
                return;
            }
            _root = BuildTree();
        }

        private void Update()
        {
            _root?.Evaluate();
        }

        private int GetAttackStep() => _attackStep;
        private void SetAttackStep(int step) => _attackStep = step;
        private void ResetAttackCycle()
        {
            _attackStep = 0;
            Debug.Log("Attack cycle reset.");
        }
        
        public void TakeDamage(DamageInfo damageInfo)
        {
            currentHealth -= damageInfo.baseDamage;
            Debug.Log($"Boss has taken {damageInfo.baseDamage} damage. Current health: {currentHealth}/{maxHealth}");

            if (!_isEnraged && currentHealth <= maxHealth / 2)
            {
                _isEnraged = true;
                Debug.LogWarning("Boss is ENRAGED! Entering Phase 2.");
                ResetAttackCycle();
                _root = BuildTree(); 
            }
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private Node BuildTree()
        {
            return !_isEnraged ? BuildEoCPhase1Tree() : BuildEoCPhase2Tree();
        }
        
        private Node BuildEoCPhase1Tree()
        {
             Debug.Log("Building CORRECTED Eye of Cthulhu Phase 1 Tree");
             return new Selector(transform, new List<Node>
             {
                new Sequence(transform, new List<Node>
                {
                    new CheckAttackStep(transform, GetAttackStep, 0),
                    new Charge(transform, _playerTransform, p1_chargeSpeed, p1_chargeDuration),
                    new Wait(transform, shortWaitBetweenCharges),
                    new SetAttackStep(transform, SetAttackStep, 1)
                }),
                new Sequence(transform, new List<Node>
                {
                    new CheckAttackStep(transform, GetAttackStep, 1),
                    new Charge(transform, _playerTransform, p1_chargeSpeed, p1_chargeDuration),
                    new Wait(transform, shortWaitBetweenCharges),
                    new SetAttackStep(transform, SetAttackStep, 2)
                }),
                new Sequence(transform, new List<Node>
                {
                    new CheckAttackStep(transform, GetAttackStep, 2),
                    new Charge(transform, _playerTransform, p1_chargeSpeed, p1_chargeDuration),
                    new Wait(transform, shortWaitBetweenCharges),
                    new SetAttackStep(transform, SetAttackStep, 3)
                }),
                new Sequence(transform, new List<Node>
                {
                    new CheckAttackStep(transform, GetAttackStep, 3),
                    new SpawnMinions(transform, servantPrefab, p1_servantCount, p1_spawnRadius),
                    new Wait(transform, longWaitAfterCycle),
                    new SetAttackStep(transform, SetAttackStep, 0)
                })
             });
        }

        private Node BuildEoCPhase2Tree()
        {
            Debug.Log("Building CORRECTED Eye of Cthulhu Phase 2 Tree");
            return new Selector(transform, new List<Node>
            {
                new Sequence(transform, new List<Node>
                {
                    new CheckAttackStep(transform, GetAttackStep, 0),
                    new Charge(transform, _playerTransform, p2_chargeSpeed, p2_chargeDuration),
                    new Wait(transform, shortWaitBetweenCharges / 2),
                    new SetAttackStep(transform, SetAttackStep, 1)
                }),
                new Sequence(transform, new List<Node>
                {
                    new CheckAttackStep(transform, GetAttackStep, 1),
                    new Charge(transform, _playerTransform, p2_chargeSpeed, p2_chargeDuration),
                    new Wait(transform, shortWaitBetweenCharges / 2),
                    new SetAttackStep(transform, SetAttackStep, 2)
                }),
                new Sequence(transform, new List<Node>
                {
                    new CheckAttackStep(transform, GetAttackStep, 2),
                    new Charge(transform, _playerTransform, p2_chargeSpeed, p2_chargeDuration),
                    new Wait(transform, longWaitAfterCycle / 1.5f),
                    new SetAttackStep(transform, SetAttackStep, 0)
                })
            });
        }

        private void Die()
        {
            Debug.Log("Boss has been defeated!");
            Destroy(gameObject);
        }
    }
}