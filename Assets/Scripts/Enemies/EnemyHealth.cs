using UnityEngine;
using Combat.Interfaces; // 使用我们创建的接口命名空间
using Combat.Weapons;   // 如果需要直接引用 Weapon 类型 (例如 DamageInfo 中的 sourceWeaponAsset)
using UI;               // 新增：为了能引用 DamageTextManager
// 移除了 using GameAudio; 因为 SoundEffectManager 现在在全局命名空间

namespace Enemies // 建议为敌人脚本也添加命名空间
{
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("Stats")]
        [Tooltip("敌人的最大生命值")]
        public float maxHealth = 100f;
        [Tooltip("当前生命值")]
        public float currentHealth;

        [Header("Feedback")]
        [Tooltip("受伤时播放的音效 (可选, 更通用的音效应来自武器)")]
        public AudioClip[] hurtSound;
        [Tooltip("死亡时播放的特效 (可选)")]
        public GameObject deathEffectPrefab;

        // 可以添加其他如护甲、抗性等属性
        // public float armor = 0f;

        void Awake()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (currentHealth <= 0) return; // 如果已经死亡，不再处理伤害

            // TODO: 在这里可以加入更复杂的伤害计算逻辑，例如考虑护甲、抗性、伤害类型等
            // float actualDamage = CalculateActualDamage(damageInfo);
            float actualDamage = damageInfo.baseDamage; // 简化处理，直接使用基础伤害

            currentHealth -= actualDamage;

            // 播放击中特效和音效 (优先使用 DamageInfo 中携带的武器特效)
            PlayHitEffects(damageInfo);

            // 显示伤害数字
            if (DamageTextManager.Instance != null)
            {
                DamageTextManager.Instance.ShowDamage(transform.position, Mathf.RoundToInt(actualDamage), damageInfo.isCritical, damageInfo.damageType);
            }
            else
            {
                Debug.LogError($"[EnemyHealth '{gameObject.name}'] DamageTextManager.Instance is NULL. Cannot show damage text. Ensure DamageTextManager is in the scene and initialized.");
            }

            // 播放敌人自身的受伤音效 (如果配置了)
            if (hurtSound != null && hurtSound.Length > 0) // 确保数组不为空
            {
                if (SoundEffectManager.Instance != null)
                {
                    SoundEffectManager.Instance.PlaySoundAtPoint(hurtSound[Random.Range(0, hurtSound.Length)], transform.position);
                }
                else
                {
                    Debug.LogWarning("[EnemyHealth] SoundEffectManager.Instance is null. Cannot play hurt sound via manager.");
                    AudioSource.PlayClipAtPoint(hurtSound[Random.Range(0, hurtSound.Length)], transform.position); // Fallback
                }
            }

            // 检查是否死亡
            if (currentHealth <= 0)
            {
                Die(damageInfo);
            }
        }

        void PlayHitEffects(DamageInfo damageInfo)
        {
            GameObject effectToPlay = null;
            AudioClip soundToPlay = null;

            // 优先从 DamageInfo 携带的 sourceWeaponAsset 获取特效
            if (damageInfo.sourceWeaponAsset != null)
            {
                effectToPlay = damageInfo.sourceWeaponAsset.hitEffect;
                soundToPlay = damageInfo.sourceWeaponAsset.hitSound;
            }
            // 可选: 如果 sourceWeaponAsset 没有特效，但伤害源是 StarProjectile 且 StarProjectile 有自己的特效
            else if (damageInfo.source != null)
            {
                StarProjectile star = damageInfo.source.GetComponent<StarProjectile>(); // 假设星星是伤害源
                if (star != null)
                {
                    if (star.hitEffectPrefab != null) effectToPlay = star.hitEffectPrefab;
                    if (star.hitSound != null) soundToPlay = star.hitSound;
                }
            }


            if (effectToPlay != null)
            {
                Instantiate(effectToPlay, damageInfo.hitPoint, Quaternion.identity); // 在击中点播放特效
            }
            if (soundToPlay != null)
            {
                if (SoundEffectManager.Instance != null)
                {
                    SoundEffectManager.Instance.PlaySoundAtPoint(soundToPlay, damageInfo.hitPoint);
                }
                else
                    Debug.LogWarning("[EnemyHealth] SoundEffectManager.Instance is null. Cannot play weapon hit sound via manager.");
                {
                    AudioSource.PlayClipAtPoint(soundToPlay, damageInfo.hitPoint); // Fallback
                }
            }
        }

        void Die(DamageInfo damageInfo)
        {
            Debug.Log($"{gameObject.name} has died.");

            // 播放死亡特效
            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }

            // TODO: 在这里可以添加掉落物品、增加分数、通知其他系统等逻辑

            // 销毁敌人对象
            // 可以考虑延迟销毁或使用对象池
            Destroy(gameObject);
        }

        // 示例：更复杂的伤害计算 (可选)
        // float CalculateActualDamage(DamageInfo damageInfo)
        // {
        //     float damage = damageInfo.baseDamage;
        //     // 根据伤害类型和敌人抗性调整伤害
        //     // damage *= GetResistanceMultiplier(damageInfo.damageType);
        //     // 减去护甲
        //     // damage -= armor;
        //     return Mathf.Max(1, damage); // 至少造成1点伤害
        // }
    }
}