using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[Serializable]
public struct PrefabData {
    public HighlightType type;
    public GameObject prefab;
}

// [추가] 화살표 리소스 전용 매핑 구조체
[Serializable]
public struct ArrowPrefabData {
    public ArrowResourceType type;
    public GameObject prefab;
}

public class HighlightTilePool : MonoBehaviour {
    [SerializeField] private List<PrefabData> prefabList;
    [SerializeField] private List<ArrowPrefabData> arrowPrefabList; // 인스펙터에 노출될 화살표 슬롯

    private Dictionary<HighlightType, IObjectPool<GameObject>> PoolDictionary = new();
    private Dictionary<HighlightType, List<GameObject>> UsedTileDictionary = new();

    // 화살표 전용 풀과 사용 리스트
    private Dictionary<ArrowResourceType, IObjectPool<GameObject>> ArrowPoolDictionary = new();
    private Dictionary<ArrowResourceType, List<GameObject>> UsedArrowDictionary = new();

    private void Awake() {
        // 기존 타일 하이라이트 풀 초기화
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

        // [추가] 화살표 전용 풀 초기화
        foreach (var data in arrowPrefabList) {
            UsedArrowDictionary[data.type] = new List<GameObject>();

            ArrowPoolDictionary[data.type] = new ObjectPool<GameObject>(
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
        tile.transform.rotation = Quaternion.identity;

        UsedTileDictionary[type].Add(tile);
    }

    // ArrowResourceType을 다이렉트로 받아서 전용 풀에서 에셋을 꺼냅니다.
    public void SpawnArrowSprite(Vector3 position, Quaternion rotation, ArrowResourceType resourceType) {
        if (!UsedArrowDictionary.ContainsKey(resourceType)) return;

        Vector3 tilePos = position + new Vector3(0, 0.03f, 0);

        GameObject arrow = ArrowPoolDictionary[resourceType].Get();
        arrow.transform.position = tilePos;
        arrow.transform.rotation = rotation;

        UsedArrowDictionary[resourceType].Add(arrow);
    }

    public void ReturnHighLightTiles(HighlightType type) {
        if (!UsedTileDictionary.ContainsKey(type)) return;

        foreach (var tile in UsedTileDictionary[type]) {
            PoolDictionary[type].Release(tile);
        }
        UsedTileDictionary[type].Clear();
    }

    // [추가] 화살표 전용 반환 함수
    public void ReturnArrowTiles(ArrowResourceType resourceType) {
        if (!UsedArrowDictionary.ContainsKey(resourceType)) return;

        foreach (var arrow in UsedArrowDictionary[resourceType]) {
            ArrowPoolDictionary[resourceType].Release(arrow);
        }
        UsedArrowDictionary[resourceType].Clear();
    }

    public void ReturnHighLightTiles() {
        // 일반 타일 반환
        foreach (var data in prefabList) {
            foreach (var tile in UsedTileDictionary[data.type]) {
                PoolDictionary[data.type].Release(tile);
            }
            UsedTileDictionary[data.type].Clear();
        }

        // 화살표 리소스도 함께 전체 반환
        foreach (var data in arrowPrefabList) {
            foreach (var arrow in UsedArrowDictionary[data.type]) {
                ArrowPoolDictionary[data.type].Release(arrow);
            }
            UsedArrowDictionary[data.type].Clear();
        }
    }
}