using UnityEngine;

public class DebugTest : MonoBehaviour
{
    void Start()
    {
        Debug.Log("DebugTest 开始运行");
    }

    void Update()
    {
        // 简单的射线测试
        Debug.DrawRay(transform.position, Vector3.right * 2f, Color.red, 0.1f);
        Debug.DrawRay(transform.position, Vector3.up * 2f, Color.green, 0.1f);
        Debug.DrawRay(transform.position, Vector3.forward * 2f, Color.blue, 0.1f);
    }

    void OnDrawGizmos()
    {
        // Gizmos测试
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.2f);
        
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(transform.position, Vector3.right * 3f);
        Gizmos.DrawRay(transform.position, Vector3.up * 3f);
    }
}
