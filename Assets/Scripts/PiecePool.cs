using System.Collections.Generic;
using UnityEngine;

namespace Match3
{
    public class PiecePool : MonoBehaviour
    {
        public static PiecePool Instance { get; private set; }

        private Dictionary<PieceType, Queue<GameObject>> _pools = new Dictionary<PieceType, Queue<GameObject>>();
        private Dictionary<PieceType, GameObject> _prefabs = new Dictionary<PieceType, GameObject>();

        private int _initialSize = 20;

        private void Awake()
        {
            Instance = this;
        }

        public void InitPool(PieceType type, GameObject prefab, int size = 0)
        {
            if (size <= 0) size = _initialSize;
            _prefabs[type] = prefab;

            if (!_pools.ContainsKey(type))
            {
                _pools[type] = new Queue<GameObject>();
            }

            for (int i = 0; i < size; i++)
            {
                GameObject obj = Instantiate(prefab, transform);
                obj.SetActive(false);
                _pools[type].Enqueue(obj);
            }
        }

        public GameObject Get(PieceType type, Vector3 position, Transform parent)
        {
            GameObject obj;

            if (_pools.ContainsKey(type) && _pools[type].Count > 0)
            {
                obj = _pools[type].Dequeue();
                obj.transform.SetParent(parent);
                obj.transform.position = position;
                obj.transform.localScale = Vector3.one;
                obj.SetActive(true);
            }
            else
            {
                // 풀 부족 시 새로 생성
                if (_prefabs.ContainsKey(type))
                {
                    obj = Instantiate(_prefabs[type], position, Quaternion.identity, parent);
                }
                else
                {
                    return null;
                }
            }

            return obj;
        }

        public void Return(PieceType type, GameObject obj)
        {
            if (obj == null) return;

            obj.SetActive(false);
            obj.transform.SetParent(transform);
            obj.transform.localScale = Vector3.one;

            if (!_pools.ContainsKey(type))
            {
                _pools[type] = new Queue<GameObject>();
            }

            _pools[type].Enqueue(obj);
        }
    }
}
