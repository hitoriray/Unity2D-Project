using UnityEngine;
using System.Collections.Generic;

namespace Utility // 建议为通用工具脚本也添加命名空间
{
    public class ObjectPool : MonoBehaviour
    {
        [Tooltip("要池化的预制体")]
        public GameObject prefabToPool;
        [Tooltip("池的初始大小")]
        public int initialPoolSize = 10;
        [Tooltip("当池耗尽时是否允许扩展")]
        public bool allowPoolToGrow = true;

        private List<GameObject> pooledObjects;
        private GameObject poolContainer; // 用于在层级视图中组织池化对象

        void Awake()
        {
            // 创建一个容器对象来存放池化对象，使其在编辑器中更整洁
            poolContainer = new GameObject(prefabToPool.name + "_Pool");
            poolContainer.transform.SetParent(this.transform); // 可选，将容器作为此对象的子对象

            pooledObjects = new List<GameObject>();
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateAndPoolObject();
            }
        }

        /// <summary>
        /// 从池中获取一个对象。
        /// </summary>
        /// <returns>一个来自池的激活的游戏对象，如果池为空且不允许增长则返回null。</returns>
        public GameObject GetPooledObject()
        {
            // 查找池中未激活的对象
            for (int i = 0; i < pooledObjects.Count; i++)
            {
                if (!pooledObjects[i].activeInHierarchy)
                {
                    // pooledObjects[i].SetActive(true); // 在返回前激活通常不是池的责任，而是使用者的责任
                    return pooledObjects[i];
                }
            }

            // 如果没有找到未激活的对象，并且允许池增长
            if (allowPoolToGrow)
            {
                return CreateAndPoolObject(true); // 创建新对象并立即返回（已激活）
            }

            // 如果不允许增长且池已耗尽
            Debug.LogWarning($"Object pool for {prefabToPool.name} is empty and not allowed to grow.");
            return null;
        }

        /// <summary>
        /// 将对象返回到池中（通过禁用它）。
        /// </summary>
        /// <param name="objectToReturn">要返回到池中的游戏对象。</param>
        public void ReturnObjectToPool(GameObject objectToReturn)
        {
            if (objectToReturn != null)
            {
                objectToReturn.SetActive(false);
                // 可选：重置对象的状态，例如位置、旋转、父对象等
                // objectToReturn.transform.SetParent(poolContainer.transform);
                // objectToReturn.transform.position = Vector3.zero; // 根据需要重置
            }
        }

        private GameObject CreateAndPoolObject(bool activateImmediately = false)
        {
            if (prefabToPool == null)
            {
                Debug.LogError("PrefabToPool is not set in ObjectPool!");
                return null;
            }
            GameObject newObj = Instantiate(prefabToPool);
            newObj.transform.SetParent(poolContainer.transform); // 放入容器
            newObj.SetActive(activateImmediately); // 新创建的对象默认不激活，除非立即使用
            pooledObjects.Add(newObj);
            return newObj;
        }

        /// <summary>
        /// 获取当前池中所有对象的数量。
        /// </summary>
        public int CurrentPoolSize => pooledObjects.Count;

        /// <summary>
        /// 获取当前池中活动对象的数量。
        /// </summary>
        public int ActiveObjectCount
        {
            get
            {
                int count = 0;
                foreach (var obj in pooledObjects)
                {
                    if (obj.activeInHierarchy) count++;
                }
                return count;
            }
        }
    }
}