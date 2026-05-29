using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HighlightedTilePool : MonoBehaviour{

    [System.Serializable]
    public struct PoolConfig {
        public HighlightType type;
        public GameObject prefab;
        public int count;
    }

    public List<PoolConfig> configs;

    private Dictionary<HighlightType, Queue<GameObject>> PoolDictionary;
    private Dictionary<HighlightType, List<GameObject>> UsedTilesLists;

    private void Awake() {
        PoolDictionary = new Dictionary<HighlightType, Queue<GameObject>>();
        UsedTilesLists = new Dictionary<HighlightType, List<GameObject>>();
        CreateHighlightTiles();
    }

    private void CreateHighlightTiles() {
        foreach (PoolConfig it in configs) {
            Queue<GameObject> TileQueue = new Queue<GameObject>();
            for (int i = 0; i < it.count; i++) {
                GameObject obj = Instantiate(it.prefab, this.transform);
                obj.SetActive(false);
                TileQueue.Enqueue(obj);
            }
            PoolDictionary.TryAdd(it.type, TileQueue);
        }
    }

    public void GetHighLightTile(HighlightType type, Vector3 position) {
        if (!PoolDictionary.TryGetValue(type, out Queue<GameObject> TileQueue)) return;

        GameObject tile = null;

        if (TileQueue.Count > 0) {
            tile = TileQueue.Dequeue();
        }
        else {
            foreach (PoolConfig it in configs) {
                if (it.type == type) {
                    tile = Instantiate(it.prefab, this.transform);
                    break;
                }
            }
        }

        if (tile == null) return;

        if (!UsedTilesLists.TryGetValue(type, out List<GameObject> usedList)) {
            usedList = new List<GameObject>();
            UsedTilesLists[type] = usedList;
        }

        usedList.Add(tile);
        tile.transform.position = position;
        tile.SetActive(true);
    }

    public void ReturnHighLightTiles() {
        foreach (KeyValuePair<HighlightType, List<GameObject>> it in UsedTilesLists) {
            foreach (GameObject tile in it.Value) {
                tile.SetActive(false);
                PoolDictionary[it.Key].Enqueue(tile);
            }
        }
        UsedTilesLists.Clear();
    }

    public void ReturnHighLightTiles(HighlightType type) {
        if (!UsedTilesLists.TryGetValue(type, out List<GameObject> usedTileList)) return;

        foreach (GameObject tile in usedTileList) {
            tile.SetActive(false);
            PoolDictionary[type].Enqueue(tile);
        }
        usedTileList.Clear();
    }


}
