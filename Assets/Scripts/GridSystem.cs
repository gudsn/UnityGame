using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

public class GridSystem : MonoBehaviour {

    public static GridSystem Instance { get; private set; }

    [SerializeField] private GameObject gridPrefab;
    [SerializeField] private TileSO tileSO;

    private TileData[,] tileData;
    private TileScript[,] tileScript;

    private Vector3 gridCenter = Vector3.zero;
    private int gridSize = 1;

    private HashSet<TileData> highlightedTileHash;

    private int instanceNumber = 0;
    void Awake() {

        if (Instance != null) {
            Destroy(gameObject);
            return;
        }

        Instance = this;


        gridCenter = transform.position;

        tileData = new TileData[tileSO.numberOfTilesToSpawnX, tileSO.numberOfTilesToSpawnY];
        tileScript = new TileScript[tileSO.numberOfTilesToSpawnX, tileSO.numberOfTilesToSpawnY];

        highlightedTileHash = new HashSet<TileData>();
    }
    public void spawnSquareGrid() {
        Vector3 gridLeftBottom = gridCenter - new Vector3((tileSO.numberOfTilesToSpawnX * gridSize) / 2, 0f, (tileSO.numberOfTilesToSpawnY * gridSize) / 2);

        for (int gridX = 0; gridX < tileSO.numberOfTilesToSpawnX; gridX++) {
            for (int gridY = 0; gridY < tileSO.numberOfTilesToSpawnY; gridY++) {

                Vector3 spawnPosition = gridLeftBottom + new Vector3(gridX * gridSize, 0, gridY * gridSize);

                GameObject tileInstance = Instantiate(gridPrefab, spawnPosition, Quaternion.identity);
                tileInstance.name = tileSO.tileName + instanceNumber;

                tileData[gridX, gridY] = new TileData(gridX, gridY, spawnPosition, true);

                tileScript[gridX, gridY] = tileInstance.GetComponent<TileScript>();

                if (tileScript[gridX, gridY] != null) {
                    tileScript[gridX, gridY].SetUp(tileData[gridX, gridY]);
                }

                instanceNumber++;
            }
        }
    }

    public TileData WorldPositionToGridTile(Vector3 worldPosition) {
        int gridX = Mathf.FloorToInt((worldPosition.x - (gridCenter.x - (tileSO.numberOfTilesToSpawnX * gridSize) / 2)) / gridSize);
        int gridY = Mathf.FloorToInt((worldPosition.z - (gridCenter.z - (tileSO.numberOfTilesToSpawnY * gridSize) / 2)) / gridSize);

        if (gridX < 0 || gridX >= tileSO.numberOfTilesToSpawnX || gridY < 0 || gridY >= tileSO.numberOfTilesToSpawnY) {
            return null; // Return null if the world position is outside the grid bounds
        }
        else {
            return tileData[gridX, gridY];
        }
    }

    public void SpawnManhattanDistanceGrid(Vector3 worldPosition, int range) {
        HashSet<TileData> visitedHash = new HashSet<TileData>();
        Queue<TileData> checkQueue = new Queue<TileData>();

        TileData startTile = WorldPositionToGridTile(worldPosition);

        checkQueue.Enqueue(startTile);
        visitedHash.Add(startTile);

        if (range == 0) {
            return;
        }

        for (int currentRange = 1; currentRange <= range; currentRange++) {
            int currentQueueCount = checkQueue.Count;
            for (int queueCount = 0; queueCount < currentQueueCount; queueCount++) {
                TileData currentTile = checkQueue.Dequeue();

                FindTileNeighbours(visitedHash, checkQueue, currentTile);

            }
        }

        foreach (TileData tile in visitedHash) {
            TileScript currentTileScript = tileScript[tile.gridX, tile.gridY];
            currentTileScript.SetHighlightedTile();
            highlightedTileHash.Add(tile);
        }
    }
    public void DeleteManhattanDistanceGrid() {
        if (highlightedTileHash.Count > 0) {
            foreach (TileData tile in highlightedTileHash) {
                TileScript currentTileScript = tileScript[tile.gridX, tile.gridY];
                currentTileScript.SetHighlightedTile();
            }
            highlightedTileHash.Clear();
        }
    }
    public int GetManhattanDistanceGridSize(int range) {
       return (2 * range * range) + (2 * range) + 1;
    }

    public void FindTileNeighbours(HashSet<TileData> hash, Queue<TileData> queue, TileData currentTile) {
        for (int gridX = -1; gridX <= 1; gridX++) {

            for (int gridY = -1; gridY <= 1; gridY++) {

                if (Mathf.Abs(gridX) + Mathf.Abs(gridY) > 1) {
                    continue;
                }

                if (gridX == 0 && gridY == 0) {
                    continue; // Skip the current tile
                }

                if (currentTile.gridX + gridX < 0 || currentTile.gridX + gridX >= tileSO.numberOfTilesToSpawnX || currentTile.gridY + gridY < 0 || currentTile.gridY + gridY >= tileSO.numberOfTilesToSpawnY) {
                    continue; // Skip out of bounds tiles
                }

                TileData neighbourTile = tileData[currentTile.gridX + gridX, currentTile.gridY + gridY];

                if (hash.Contains(neighbourTile)) {
                        continue; // Skip already visited tiles
                }

                if (neighbourTile.isWalkable) { 
                    queue.Enqueue(neighbourTile);
                    hash.Add(neighbourTile);
                }

            }
        }
    }
    public bool checkHighlightedTile(TileData checkTile) {
        if (checkTile == null) return false;

        return highlightedTileHash.Contains(checkTile);
    }
}
