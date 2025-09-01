using UnityEngine;

public class SingletonAutoMono<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T GetInstance() {
        if (instance == null) {
            GameObject gameObject = new GameObject(typeof(T).Name);
            instance = gameObject.AddComponent<T>();
            DontDestroyOnLoad(gameObject);
        }
        return instance;
    }
}