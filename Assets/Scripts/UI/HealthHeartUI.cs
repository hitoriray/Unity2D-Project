using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace UI
{
    /// <summary>
    /// 泰拉瑞亚风格的单个心形血量UI组件
    /// 支持满血、半血、空血状态，以及不同颜色等级的心形
    /// </summary>
    public class HealthHeartUI : MonoBehaviour
    {
        [Header("心形图片配置")]
        [Tooltip("心形Image组件")]
        public Image heartImage;
        
        [Header("基础红心Sprites")]
        [Tooltip("满血状态的心形图片")]
        public Sprite fullHeartSprite;
        [Tooltip("半血状态的心形图片")]  
        public Sprite halfHeartSprite;
        [Tooltip("空血状态的心形图片")]
        public Sprite emptyHeartSprite;
        
        [Header("金心Sprites（第二层血量）")]
        [Tooltip("金心满血状态")]
        public Sprite goldenFullHeartSprite;
        [Tooltip("金心半血状态")]
        public Sprite goldenHalfHeartSprite;
        [Tooltip("金心空血状态")]
        public Sprite goldenEmptyHeartSprite;
        
        [Header("水晶心Sprites（第三层血量）")]
        [Tooltip("水晶心满血状态")]
        public Sprite crystalFullHeartSprite;
        [Tooltip("水晶心半血状态")]
        public Sprite crystalHalfHeartSprite;
        [Tooltip("水晶心空血状态")]
        public Sprite crystalEmptyHeartSprite;
        
        [Header("动画配置")]
        [Tooltip("心形跳动动画的缩放倍数")]
        public float pulseScale = 1.2f;
        [Tooltip("跳动动画持续时间")]
        public float pulseDuration = 0.3f;
        [Tooltip("淡入动画持续时间")]
        public float fadeInDuration = 0.5f;
        [Tooltip("淡出动画持续时间")]
        public float fadeOutDuration = 0.3f;

        // 心形状态枚举
        public enum HeartState
        {
            Empty,      // 空血
            Half,       // 半血
            Full        // 满血
        }

        // 心形类型枚举
        public enum HeartType
        {
            Normal,     // 普通红心
            Golden,     // 金心
            Crystal     // 水晶心
        }

        // 私有变量
        private HeartState currentState = HeartState.Empty;
        private HeartType currentType = HeartType.Normal;
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Vector3 originalScale;
        private Color originalColor;
        
        // 动画协程
        private Coroutine pulseCoroutine;
        private Coroutine fadeCoroutine;

        void Awake()
        {
            // 初始化组件引用
            if (heartImage == null)
                heartImage = GetComponent<Image>();
            
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            rectTransform = GetComponent<RectTransform>();
            
            // 保存原始属性
            originalScale = rectTransform.localScale;
            if (heartImage != null)
                originalColor = heartImage.color;
        }

        void Start()
        {
            // 设置初始状态
            UpdateHeartDisplay();
        }

        /// <summary>
        /// 设置心形状态
        /// </summary>
        /// <param name="state">心形状态</param>
        /// <param name="playAnimation">是否播放动画</param>
        public void SetHeartState(HeartState state, bool playAnimation = true)
        {
            if (currentState == state) return;

            currentState = state;
            UpdateHeartDisplay();

            if (playAnimation)
            {
                // 根据状态播放不同动画
                switch (state)
                {
                    case HeartState.Full:
                        PlayPulseAnimation();
                        break;
                    case HeartState.Half:
                        PlayPulseAnimation(0.8f); // 较小的跳动
                        break;
                    case HeartState.Empty:
                        // 空血时不播放跳动动画
                        break;
                }
            }
        }

        /// <summary>
        /// 设置心形类型
        /// </summary>
        /// <param name="type">心形类型</param>
        public void SetHeartType(HeartType type)
        {
            if (currentType == type) return;

            currentType = type;
            UpdateHeartDisplay();
        }

        /// <summary>
        /// 获取当前心形状态
        /// </summary>
        public HeartState GetHeartState()
        {
            return currentState;
        }

        /// <summary>
        /// 获取当前心形类型
        /// </summary>
        public HeartType GetHeartType()
        {
            return currentType;
        }

        /// <summary>
        /// 更新心形显示
        /// </summary>
        private void UpdateHeartDisplay()
        {
            if (heartImage == null) 
            {
                Debug.LogError("[HealthHeartUI] heartImage为空，无法更新显示");
                return;
            }

            Sprite targetSprite = null;

            // 根据类型和状态选择正确的Sprite
            switch (currentType)
            {
                case HeartType.Normal:
                    switch (currentState)
                    {
                        case HeartState.Empty: targetSprite = emptyHeartSprite; break;
                        case HeartState.Half: targetSprite = halfHeartSprite; break;
                        case HeartState.Full: targetSprite = fullHeartSprite; break;
                    }
                    break;
                    
                case HeartType.Golden:
                    switch (currentState)
                    {
                        case HeartState.Empty: targetSprite = goldenEmptyHeartSprite; break;
                        case HeartState.Half: targetSprite = goldenHalfHeartSprite; break;
                        case HeartState.Full: targetSprite = goldenFullHeartSprite; break;
                    }
                    break;
                    
                case HeartType.Crystal:
                    switch (currentState)
                    {
                        case HeartState.Empty: targetSprite = crystalEmptyHeartSprite; break;
                        case HeartState.Half: targetSprite = crystalHalfHeartSprite; break;
                        case HeartState.Full: targetSprite = crystalFullHeartSprite; break;
                    }
                    break;
            }

            if (targetSprite != null)
            {
                heartImage.sprite = targetSprite;
                heartImage.color = Color.white; // 使用正确Sprite时重置颜色
                heartImage.enabled = true;
            }
            else
            {
                // 如果没有找到对应的Sprite，使用普通红心的Sprite作为备用并调整颜色
                Debug.LogWarning($"[HealthHeartUI] 未找到对应的心形Sprite: {currentType}/{currentState}，使用默认Sprite");
                
                // 使用默认Sprite
                switch (currentState)
                {
                    case HeartState.Empty: 
                        heartImage.sprite = emptyHeartSprite; 
                        Debug.LogWarning($"[HealthHeartUI] 使用默认空血Sprite: {emptyHeartSprite?.name ?? "null"}");
                        break;
                    case HeartState.Half: 
                        heartImage.sprite = halfHeartSprite; 
                        Debug.LogWarning($"[HealthHeartUI] 使用默认半血Sprite: {halfHeartSprite?.name ?? "null"}");
                        break;
                    case HeartState.Full: 
                        heartImage.sprite = fullHeartSprite; 
                        Debug.LogWarning($"[HealthHeartUI] 使用默认满血Sprite: {fullHeartSprite?.name ?? "null"}");
                        break;
                }
                
                // 根据心形类型调整颜色，以便区分不同类型
                switch (currentType)
                {
                    case HeartType.Normal:
                        heartImage.color = Color.white; // 默认红心颜色
                        break;
                    case HeartType.Golden:
                        heartImage.color = Color.yellow; // 金心使用黄色调
                        break;
                    case HeartType.Crystal:
                        heartImage.color = Color.cyan; // 水晶心使用青色调
                        break;
                }
                
                heartImage.enabled = true;
            }
        }

        /// <summary>
        /// 播放心形跳动动画
        /// </summary>
        /// <param name="scaleMultiplier">缩放倍数</param>
        public void PlayPulseAnimation(float scaleMultiplier = 1f)
        {
            if (pulseCoroutine != null)
                StopCoroutine(pulseCoroutine);
            
            pulseCoroutine = StartCoroutine(PulseAnimationCoroutine(scaleMultiplier));
        }

        /// <summary>
        /// 播放淡入动画
        /// </summary>
        public void PlayFadeInAnimation()
        {
            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning("[HealthHeartUI] 尝试在非活动对象上播放淡入动画");
                return;
            }
            
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            
            try
            {
                fadeCoroutine = StartCoroutine(FadeInCoroutine());
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[HealthHeartUI] 播放淡入动画时出错: {e.Message}");
            }
        }

        /// <summary>
        /// 播放淡出动画
        /// </summary>
        public void PlayFadeOutAnimation()
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            
            fadeCoroutine = StartCoroutine(FadeOutCoroutine());
        }

        /// <summary>
        /// 心形跳动动画协程
        /// </summary>
        private IEnumerator PulseAnimationCoroutine(float scaleMultiplier)
        {
            float targetScale = pulseScale * scaleMultiplier;
            Vector3 targetScaleVector = originalScale * targetScale;
            
            // 放大阶段
            float elapsedTime = 0f;
            float halfDuration = pulseDuration * 0.5f;
            
            while (elapsedTime < halfDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / halfDuration;
                // 使用缓出曲线让动画更自然
                float smoothT = 1f - Mathf.Pow(1f - t, 2f);
                
                rectTransform.localScale = Vector3.Lerp(originalScale, targetScaleVector, smoothT);
                yield return null;
            }

            // 缩小阶段
            elapsedTime = 0f;
            while (elapsedTime < halfDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / halfDuration;
                // 使用缓入曲线
                float smoothT = Mathf.Pow(t, 2f);
                
                rectTransform.localScale = Vector3.Lerp(targetScaleVector, originalScale, smoothT);
                yield return null;
            }

            // 确保最终缩放正确
            rectTransform.localScale = originalScale;
            pulseCoroutine = null;
        }

        /// <summary>
        /// 淡入动画协程
        /// </summary>
        private IEnumerator FadeInCoroutine()
        {
            float elapsedTime = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / fadeInDuration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            fadeCoroutine = null;
        }

        /// <summary>
        /// 淡出动画协程
        /// </summary>
        private IEnumerator FadeOutCoroutine()
        {
            float elapsedTime = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsedTime < fadeOutDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / fadeOutDuration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            fadeCoroutine = null;
        }

        /// <summary>
        /// 停止所有动画
        /// </summary>
        public void StopAllAnimations()
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }
            
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }
            
            // 恢复原始状态
            rectTransform.localScale = originalScale;
            canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// 设置心形可见性
        /// </summary>
        /// <param name="visible">是否可见</param>
        public void SetVisible(bool visible)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            heartImage.enabled = visible;
        }

        void OnDestroy()
        {
            // 清理协程
            StopAllAnimations();
        }

        #region 编辑器辅助方法
        
        #if UNITY_EDITOR
        /// <summary>
        /// 编辑器中预览心形状态（仅用于调试）
        /// </summary>
        [ContextMenu("预览满血状态")]
        private void PreviewFullHeart()
        {
            SetHeartState(HeartState.Full, false);
        }
        
        [ContextMenu("预览半血状态")]
        private void PreviewHalfHeart()
        {
            SetHeartState(HeartState.Half, false);
        }
        
        [ContextMenu("预览空血状态")]
        private void PreviewEmptyHeart()
        {
            SetHeartState(HeartState.Empty, false);
        }
        
        [ContextMenu("测试跳动动画")]
        private void TestPulseAnimation()
        {
            PlayPulseAnimation();
        }
        #endif
        
        #endregion
    }
} 