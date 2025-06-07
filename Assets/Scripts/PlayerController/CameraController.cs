using System;
using UnityEngine;
using UnityEngine.Timeline;

public class CameraController : MonoBehaviour
{
    [Range(0, 1)]
    public float smoothTime;
    public Transform playerTransform;

    [HideInInspector]
    public int worldSize;
    private float orthoSize;

    public void Spawn(Vector3 pos)
    {
        GetComponent<Transform>().position = pos;
        orthoSize = GetComponent<Camera>().orthographicSize;
    }

    void FixedUpdate()
    {
        Vector3 pos = GetComponent<Transform>().position;

        pos.x = Mathf.Lerp(pos.x, playerTransform.position.x, smoothTime);
        pos.y = Mathf.Lerp(pos.y, playerTransform.position.y, smoothTime);

        pos.x = Mathf.Clamp(pos.x, 0 + (orthoSize * Camera.main.aspect), worldSize - (orthoSize * Camera.main.aspect));
        pos.y = Mathf.Clamp(pos.y, 0 + orthoSize, worldSize - orthoSize);

        GetComponent<Transform>().position = pos;
    }
}
