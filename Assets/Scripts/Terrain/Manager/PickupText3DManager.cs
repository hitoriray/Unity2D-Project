using UnityEngine;

public class PickupText3DManager : MonoBehaviour
{
    public static PickupText3DManager Instance { get; private set; }
    
    [Header("文本设置")]
    public float textLifetime = 2f;
    public float moveUpSpeed = 2f;
    public Color textColor = Color.white;
    public int fontSize = 20;
    
    private Camera mainCamera;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.transform.root.gameObject);
            mainCamera = Camera.main;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void ShowPickupText(Vector3 worldPosition, string text)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("找不到主摄像机");
                return;
            }
        }
        
        GameObject textObj = new GameObject("PickupText_" + text);
        
        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.fontSize = fontSize;
        textMesh.color = textColor;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        
        // 设置位置（在世界坐标中，玩家上方一点）
        textObj.transform.position = worldPosition + Vector3.up * 1.5f;
        
        // 让文本面向摄像机
        textObj.transform.LookAt(mainCamera.transform);
        textObj.transform.Rotate(0, 180, 0); // 翻转文本
        
        // 添加动画组件
        TextAnimation animation = textObj.AddComponent<TextAnimation>();
        animation.Initialize(textLifetime, moveUpSpeed);
    }
}

public class TextAnimation : MonoBehaviour
{
    private float lifetime;
    private float moveSpeed;
    private float timer;
    private Vector3 startPosition;
    private TextMesh textMesh;
    private Color originalColor;
    
    public void Initialize(float life, float speed)
    {
        lifetime = life;
        moveSpeed = speed;
        timer = 0f;
        startPosition = transform.position;
        textMesh = GetComponent<TextMesh>();
        originalColor = textMesh.color;
    }
    
    void Update()
    {
        timer += Time.deltaTime;
        
        // 向上移动
        transform.position = startPosition + Vector3.up * (moveSpeed * timer);
        
        // 淡出效果
        float alpha = Mathf.Lerp(1f, 0f, timer / lifetime);
        Color newColor = originalColor;
        newColor.a = alpha;
        textMesh.color = newColor;
        
        // 让文本始终面向摄像机
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            transform.Rotate(0, 180, 0);
        }
        
        // 生命周期结束时销毁
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
