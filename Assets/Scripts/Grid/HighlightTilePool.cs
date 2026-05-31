using System;
using System.Collections.Generic;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Pool;

[Serializable]
public struct PrefabData {
    public HighlightType type;
    public GameObject prefab;
}

public class HighlightTilePool : MonoBehaviour {
    [SerializeField] private List<PrefabData> prefabList;

    private Dictionary<HighlightType, IObjectPool<GameObject>> PoolDictionary = new();
    private Dictionary<HighlightType, List<GameObject>> UsedTileDictionary = new();

    private void Awake() {
        foreach (var data in prefabList) {
            UsedTileDictionary[data.type] = new List<GameObject>();

            PoolDictionary[data.type] = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(data.prefab, this.transform),
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                collectionCheck: true,
                defaultCapacity: 20,
                maxSize: 100
            );
        }
    }

    public void GetHighLightTile(HighlightType type, Vector3 position) {
        if (!UsedTileDictionary.ContainsKey(type)) return;

        Vector3 tilePos = position + new Vector3(0, 0.01f, 0);
        GameObject tile = PoolDictionary[type].Get();
        tile.transform.position = tilePos;

        UsedTileDictionary[type].Add(tile);
    }

    public void ReturnHighLightTiles(HighlightType type) {
        if (!UsedTileDictionary.ContainsKey(type)) return;

        foreach (var tile in UsedTileDictionary[type]) {
            PoolDictionary[type].Release(tile);
        }

        UsedTileDictionary[type].Clear();
    }

    public void ReturnHighLightTiles() {
        foreach (var data in prefabList) {
            foreach (var tile in UsedTileDictionary[data.type]) {
                PoolDictionary[data.type].Release(tile);
            }
            UsedTileDictionary[data.type].Clear();
        }
    }
}