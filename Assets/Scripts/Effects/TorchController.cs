using UnityEngine;

public class TorchController : MonoBehaviour
{
    [Header("火把设置")]
    [SerializeField] private float lightIntensity = 1.2f;
    [SerializeField] private float lightRadius = 6f;
    [SerializeField] private Color lightColor = new Color(1f, 0.843f, 0f); // 暖黄色
    
    [Header("视觉效果")]
    [SerializeField] private GameObject flameParticles;
    [SerializeField] private AudioClip igniteSound;
    [SerializeField] private AudioClip extinguishSound;
    
    private GameObject lightObject;
    private bool isLit = true;
    private AudioSource audioSource;
    
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 自动创建光源
        if (AdvancedLightingSystem.Instance != null)
        {
            CreateLight();
        }
        else
        {
            // 如果高级光照系统不可用，使用备用方案
            CreateFallbackLight();
        }
        
        // 播放点燃音效
        if (igniteSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(igniteSound);
        }
    }
    
    private void CreateLight()
    {
        lightObject = AdvancedLightingSystem.Instance.CreateTorchLight(
            transform.position, 
            lightIntensity, 
            lightRadius
        );
        
        // 如果有火焰粒子效果
        if (flameParticles != null)
        {
            flameParticles.SetActive(true);
        }
    }
    
    private void CreateFallbackLight()
    {
        // 备用方案：使用传统的点光源
        lightObject = new GameObject("TorchLight");
        lightObject.transform.parent = transform;
        lightObject.transform.localPosition = Vector3.zero;
        
        // 添加Light组件（3D光源作为备用）
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = lightColor;
        light.intensity = lightIntensity;
        light.range = lightRadius * 2; // 3D光源的范围需要更大
        
        // 如果有火焰粒子效果
        if (flameParticles != null)
        {
            flameParticles.SetActive(true);
        }
    }
    
    /// <summary>
    /// 切换火把状态
    /// </summary>
    public void ToggleTorch()
    {
        if (isLit)
        {
            ExtinguishTorch();
        }
        else
        {
            IgniteTorch();
        }
    }
    
    /// <summary>
    /// 熄灭火把
    /// </summary>
    public void ExtinguishTorch()
    {
        if (!isLit) return;
        
        isLit = false;
        
        // 移除光源
        if (AdvancedLightingSystem.Instance != null && lightObject != null)
        {
            AdvancedLightingSystem.Instance.RemoveLight(lightObject);
        }
        else if (lightObject != null)
        {
            Destroy(lightObject);
        }
        
        // 关闭粒子效果
        if (flameParticles != null)
        {
            flameParticles.SetActive(false);
        }
        
        // 播放熄灭音效
        if (extinguishSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(extinguishSound);
        }
    }
    
    /// <summary>
    /// 重新点燃火把
    /// </summary>
    public void IgniteTorch()
    {
        if (isLit) return;
        
        isLit = true;
        
        // 重新创建光源
        if (AdvancedLightingSystem.Instance != null)
        {
            CreateLight();
        }
        else
        {
            CreateFallbackLight();
        }
        
        // 播放点燃音效
        if (igniteSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(igniteSound);
        }
    }
    
    private void OnDestroy()
    {
        // 清理光源
        if (AdvancedLightingSystem.Instance != null && lightObject != null)
        {
            AdvancedLightingSystem.Instance.RemoveLight(lightObject);
        }
        else if (lightObject != null)
        {
            Destroy(lightObject);
        }
    }
    
    // 可选：与玩家交互
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 显示交互提示
            // 例如：UIManager.Instance.ShowInteractionHint("按E键点燃/熄灭火把");
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 隐藏交互提示
            // 例如：UIManager.Instance.HideInteractionHint();
        }
    }
} 