using UnityEngine;
using TMPro; // 如果使用 TextMeshProUGUI
using System.Collections;

// 确保 DamageTypeExtensions 能够被访问，可能需要 using Combat.Data 或者确保它在全局命名空间
// using Combat.Data; // 如果 DamageTypeExtensions 在这个命名空间下
using Utility;        // 新增：为了能引用 ObjectPool

namespace UI // 建议为UI脚本也添加命名空间
{
    public class DamageTextManager : MonoBehaviour
    {
        public static DamageTextManager Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("伤害文本的 ObjectPool (预制体应包含 TextMeshProUGUI 组件)")]
        public ObjectPool damageTextPool;

        [Tooltip("文本在目标世界坐标上方的偏移量")]
        public Vector3 textOffset = new Vector3(0, 1f, 0);

        [Header("Animation")]
        [Tooltip("文本向上漂浮的总高度")]
        public float floatHeight = 50f; // UI像素单位
        [Tooltip("文本漂浮和淡出的总时长（秒）")]
        public float animationDuration = 1f;

        [Header("Font Sizes")]
        [Tooltip("非暴击时的固定字体大小")]
        public float nonCriticalFontSize = 45f;
        [Tooltip("暴击时的固定字体大小")]
        public float criticalFontSize = 60f;

        private Canvas mainCanvas;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // DontDestroyOnLoad(gameObject); // 如果希望跨场景持久存在
                
                // 尝试找到场景中标签为 "MainCanvas" 或任意一个激活的Canvas
                GameObject canvasObj = GameObject.FindGameObjectWithTag("MainCanvas"); // 推荐给你的主Canvas添加 "MainCanvas" 标签
                if (canvasObj != null)
                {
                    mainCanvas = canvasObj.GetComponent<Canvas>();
                }
                if (mainCanvas == null) // 如果通过标签找不到，则查找任意Canvas
                {
                    mainCanvas = FindObjectOfType<Canvas>();
                }

                if (mainCanvas == null)
                {
                    Debug.LogError("[DamageTextManager] No Canvas found in the scene! Please ensure there is an active Canvas, preferably tagged 'MainCanvas'. Damage text might not display correctly.");
                }
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 在指定世界位置显示伤害数字。
        /// </summary>
        /// <param name="worldPosition">伤害发生的原始世界坐标。</param>
        /// <param name="damageAmount">显示的伤害数值。</param>
        /// <param name="isCritical">是否为暴击伤害。</param>
        /// <param name="damageType">伤害类型，用于确定颜色等。</param>
        public void ShowDamage(Vector3 worldPosition, int damageAmount, bool isCritical, DamageType damageType)
        {
            if (damageTextPool == null)
            {
                Debug.LogError("[DamageTextManager] DamageTextPool is NULL. Cannot show damage text. Ensure it's assigned in Inspector.");
                return;
            }
            if (damageTextPool.prefabToPool == null)
            {
                Debug.LogError("[DamageTextManager] DamageTextPool.prefabToPool is NULL. Cannot show damage text. Ensure the pool has a prefab assigned.");
                return;
            }

            GameObject textInstance = damageTextPool.GetPooledObject();
            if (textInstance == null)
            {
                Debug.LogError("[DamageTextManager] Failed to get object from DamageTextPool. Pool might be empty and not allowed to grow, or an error occurred in the pool.");
                return;
            }
            
            if (mainCanvas != null)
            {
                textInstance.transform.SetParent(mainCanvas.transform, false);
            }
            else
            {
                textInstance.transform.SetParent(transform, false);
                Debug.LogWarning($"[DamageTextManager] No mainCanvas found, text instance '{textInstance.name}' parented to DamageTextManager itself: {transform.name}. This might cause display issues.");
            }
            textInstance.SetActive(true);


            RectTransform rectTransform = textInstance.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector2 screenPosition = Camera.main.WorldToScreenPoint(worldPosition + textOffset);
                rectTransform.position = screenPosition;
            }


            TextMeshProUGUI tmpText = textInstance.GetComponent<TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.text = damageAmount.ToString();
                tmpText.color = DamageTypeExtensions.GetDisplayColor(damageType);
                tmpText.fontStyle = FontStyles.Normal; // 重置字体样式

                if (isCritical)
                {
                    tmpText.fontSize = criticalFontSize; // 使用Inspector中设置的暴击字体大小
                    tmpText.fontStyle = FontStyles.Bold;
                    tmpText.color = new Color(1f, 0.4f, 0f);
                }
                else
                {
                    tmpText.fontSize = nonCriticalFontSize; // 使用Inspector中设置的非暴击字体大小
                    tmpText.fontWeight = FontWeight.SemiBold;
                }
            }
            else
            {
                Debug.LogError($"[DamageTextManager] Text instance '{textInstance.name}' is missing TextMeshProUGUI component!");
                damageTextPool.ReturnObjectToPool(textInstance);
                return;
            }

            StartCoroutine(AnimateDamageText(textInstance, tmpText));
        }

        private IEnumerator AnimateDamageText(GameObject textInstance, TextMeshProUGUI textMesh)
        {
            RectTransform rectTransform = textInstance.GetComponent<RectTransform>();
            Vector3 startPosition = rectTransform.anchoredPosition; // 使用 anchoredPosition 进行 Canvas 内动画
            Color startColor = textMesh.color;
            float timer = 0;

            while (timer < animationDuration)
            {
                if (textInstance == null || textMesh == null) yield break; // 对象可能在中途被销毁

                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / animationDuration);

                // 上浮动画 (基于 anchoredPosition)
                rectTransform.anchoredPosition = startPosition + new Vector3(0, progress * floatHeight);

                // 淡出动画
                textMesh.color = new Color(startColor.r, startColor.g, startColor.b, 1f - progress);

                yield return null;
            }

            if (textInstance != null && damageTextPool != null)
            {
                damageTextPool.ReturnObjectToPool(textInstance);
            }
            else if (textInstance != null) // Fallback if pool is somehow null
            {
                Destroy(textInstance);
            }
        }
    }
}