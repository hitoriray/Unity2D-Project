using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI
{
    /// <summary>
    /// Boss血条UI管理器
    /// 实现血量显示、Boss名称、阶段指示器和动画效果
    /// 重用现有UI架构和组件
    /// </summary>
    public class BossHealthBarUI : MonoBehaviour
    {
        public static BossHealthBarUI Instance { get; private set; }

        [Header("UI组件")]
        [Tooltip("Boss血条的Slider组件")]
        public Slider healthSlider;
        [Tooltip("Boss名称显示")]
        public TextMeshProUGUI bossNameText;
        [Tooltip("阶段指示器图标")]
        public Image phaseIndicator;
        [Tooltip("血条容器面板")]
        public GameObject healthBarPanel;

        [Header("动画设置")]
        [Tooltip("血条动画持续时间")]
        public float healthAnimationDuration = 0.5f;
        [Tooltip("淡入淡出动画时间")]
        public float fadeAnimationDuration = 0.8f;

        [Header("阶段配置")]
        [Tooltip("第一阶段眼睛图标")]
        public Sprite phase1EyeSprite;
        [Tooltip("第二阶段眼睛图标")]
        public Sprite phase2EyeSprite;

        [Header("血条颜色")]
        [Tooltip("第一阶段血条颜色（绿色）")]
        public Color phase1Color = Color.green;
        [Tooltip("第二阶段血条颜色（红色）")]
        public Color phase2Color = Color.red;

        // 私有变量
        private Canvas mainCanvas;
        private CanvasGroup canvasGroup;
        private Image healthFillImage;
        private float currentHealthValue;
        private float targetHealthValue;
        private Coroutine healthAnimationCoroutine;
        private Coroutine fadeAnimationCoroutine;
        private int currentPhase = 1;

        void Awake()
        {
            // 单例模式，参考DamageTextManager的实现
            if (Instance == null)
            {
                Instance = this;
                InitializeComponents();
                
                // 确保GameObject是激活的，但UI是隐藏的
                if (!gameObject.activeInHierarchy)
                {
                    gameObject.SetActive(true);
                }
                
                // 初始时隐藏血条
                if (healthBarPanel != null)
                    healthBarPanel.SetActive(false);
                
                // 设置初始透明度为0，避免闪烁
                if (canvasGroup != null)
                    canvasGroup.alpha = 0f;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            // 不在Start中调用Show()，避免游戏开始时显示
            // Show()将由外部系统（如NightBossSpawner）调用
        }

        /// <summary>
        /// 初始化组件，参考DamageTextManager的Canvas查找机制
        /// </summary>
        private void InitializeComponents()
        {
            // 查找主Canvas，重用DamageTextManager的方式
            GameObject canvasObj = GameObject.FindGameObjectWithTag("MainCanvas");
            if (canvasObj != null)
            {
                mainCanvas = canvasObj.GetComponent<Canvas>();
            }
            if (mainCanvas == null) // 备用查找
            {
                mainCanvas = FindObjectOfType<Canvas>();
            }

            if (mainCanvas == null)
            {
                Debug.LogError("[BossHealthBarUI] No MainCanvas found! Boss health bar might not display correctly.");
            }

            // 获取血条填充图像
            if (healthSlider != null)
            {
                healthFillImage = healthSlider.fillRect?.GetComponent<Image>();
                currentHealthValue = healthSlider.value;
                targetHealthValue = healthSlider.value;
            }

            // 获取CanvasGroup组件，用于淡入淡出
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // 设置初始透明度为0
            canvasGroup.alpha = 0f;

            // 确保血条在主Canvas下且层级正确
            if (mainCanvas != null && transform.parent != mainCanvas.transform)
            {
                transform.SetParent(mainCanvas.transform, false);
                // 设置为较高的层级，确保显示在游戏内容之上
                transform.SetAsLastSibling();
            }
        }

        /// <summary>
        /// 显示Boss血条
        /// </summary>
        /// <param name="bossName">Boss名称</param>
        /// <param name="maxHealth">最大血量</param>
        public void Show(string bossName = "克苏鲁之眼", float maxHealth = 100f)
        {
            // 确保GameObject是激活的
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }
            
            if (healthBarPanel != null)
                healthBarPanel.SetActive(true);

            // 设置Boss名称
            if (bossNameText != null)
                bossNameText.text = bossName;

            // 初始化血条
            if (healthSlider != null)
            {
                healthSlider.maxValue = maxHealth;
                healthSlider.value = maxHealth;
                currentHealthValue = maxHealth;
                targetHealthValue = maxHealth;
            }

            // 设置初始阶段
            SetPhase(1);

            // 淡入动画
            if (fadeAnimationCoroutine != null)
                StopCoroutine(fadeAnimationCoroutine);
            fadeAnimationCoroutine = StartCoroutine(FadeIn());
            
            Debug.Log($"[BossHealthBarUI] 显示Boss血条 - Boss: {bossName}, 血量: {maxHealth}");
        }

        /// <summary>
        /// 隐藏Boss血条
        /// </summary>
        public void Hide()
        {
            if (fadeAnimationCoroutine != null)
                StopCoroutine(fadeAnimationCoroutine);
            fadeAnimationCoroutine = StartCoroutine(FadeOut());
        }

        /// <summary>
        /// 更新Boss血量
        /// </summary>
        /// <param name="currentHealth">当前血量</param>
        /// <param name="maxHealth">最大血量</param>
        public void UpdateHealth(float currentHealth, float maxHealth)
        {
            if (healthSlider == null) return;

            // 计算目标血量百分比
            targetHealthValue = Mathf.Clamp(currentHealth / maxHealth, 0f, 1f);

            // 启动平滑动画
            if (healthAnimationCoroutine != null)
                StopCoroutine(healthAnimationCoroutine);
            healthAnimationCoroutine = StartCoroutine(AnimateHealthBar(targetHealthValue));

            // 根据血量判断是否需要切换阶段
            if (targetHealthValue <= 0.5f && currentPhase == 1)
            {
                SetPhase(2);
            }
        }

        /// <summary>
        /// 设置Boss阶段
        /// </summary>
        /// <param name="phase">阶段编号 (1或2)</param>
        public void SetPhase(int phase)
        {
            currentPhase = phase;
    
            // 更新阶段指示器图标
            if (phaseIndicator != null)
            {
                switch (phase)
                {
                    case 1:
                        if (phase1EyeSprite != null)
                            phaseIndicator.sprite = phase1EyeSprite;
                        break;
                    case 2:
                        if (phase2EyeSprite != null)
                            phaseIndicator.sprite = phase2EyeSprite;
                        break;
                }
            }

            // 更新血条颜色
            if (healthFillImage != null)
            {
                switch (phase)
                {
                    case 1:
                        healthFillImage.color = phase1Color;
                        break;
                    case 2:
                        healthFillImage.color = phase2Color;
                        break;
                }
            }
        }

        /// <summary>
        /// 血条平滑动画协程
        /// </summary>
        /// <param name="targetValue">目标血量值</param>
        private IEnumerator AnimateHealthBar(float targetValue)
        {
            float startValue = currentHealthValue;
            float elapsedTime = 0f;

            while (elapsedTime < healthAnimationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / healthAnimationDuration;
                
                // 使用缓出曲线让动画更自然
                float smoothT = 1f - Mathf.Pow(1f - t, 3f);
                
                currentHealthValue = Mathf.Lerp(startValue, targetValue, smoothT);
                
                if (healthSlider != null)
                    healthSlider.value = currentHealthValue * healthSlider.maxValue;

                yield return null;
            }

            // 确保最终值准确
            currentHealthValue = targetValue;
            if (healthSlider != null)
                healthSlider.value = currentHealthValue * healthSlider.maxValue;

            healthAnimationCoroutine = null;
        }

        /// <summary>
        /// 淡入动画
        /// </summary>
        private IEnumerator FadeIn()
        {
            float elapsedTime = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsedTime < fadeAnimationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / fadeAnimationDuration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            fadeAnimationCoroutine = null;
        }

        /// <summary>
        /// 淡出动画
        /// </summary>
        private IEnumerator FadeOut()
        {
            float elapsedTime = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsedTime < fadeAnimationDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / fadeAnimationDuration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            
            if (healthBarPanel != null)
                healthBarPanel.SetActive(false);

            fadeAnimationCoroutine = null;
        }

        /// <summary>
        /// 获取当前血量百分比
        /// </summary>
        public float GetHealthPercentage()
        {
            return currentHealthValue;
        }

        /// <summary>
        /// 检查Boss血条是否显示中
        /// </summary>
        public bool IsShowing()
        {
            return healthBarPanel != null && healthBarPanel.activeInHierarchy && canvasGroup.alpha > 0f;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #region 调试方法
        /// <summary>
        /// 调试：模拟血量变化
        /// </summary>
        [ContextMenu("Test Health Update")]
        private void TestHealthUpdate()
        {
            if (Application.isPlaying)
            {
                UpdateHealth(Random.Range(0f, 100f), 100f);
            }
        }

        /// <summary>
        /// 调试：切换阶段
        /// </summary>
        [ContextMenu("Test Phase Switch")]
        private void TestPhaseSwitch()
        {
            if (Application.isPlaying)
            {
                SetPhase(currentPhase == 1 ? 2 : 1);
            }
        }
        #endregion
    }
} 