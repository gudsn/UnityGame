using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSystem : MonoBehaviour {

    // GridSystem Singleton Instance
    public static GridSystem Instance { get; private set; }

    // Grid Prefab for Spawning 
    [SerializeField] private GameObject gridPrefab;
    [SerializeField] private TileSO tileSO;

    private TileData[,] tileData;
    private TileScript[,] tileScript;

    // grid init data
    private Vector3 gridCenter = Vector3.zero;
    private int gridSize = 1;

    private HashSet<TileData> highlightedTileHash;

    // Grid Instace number while it spawned
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
        List<TileData> neighbourList = new List<TileData>();

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

                neighbourList = FindTileNeighbours(currentTile);

                foreach (TileData n in neighbourList) {
                    if (visitedHash.Contains(n)) {
                        continue; // Skip already visited tiles
                    }

                    if (n.isWalkable) {
                        checkQueue.Enqueue(n);
                        visitedHash.Add(n);
                    }
                }

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
    public int GetManhattanDistance(Vector3 start, Vector3 end) {
        int distance = Mathf.Abs((int)start.x - (int)end.x);
        distance += Mathf.Abs((int)start.y - (int)end.y);
        return distance;
    }

    // Grid Neighbour direction Array
    private static readonly Vector2Int[] Directions = {
    new Vector2Int(0, 1),
    new Vector2Int(0, -1),
    new Vector2Int(-1, 0),
    new Vector2Int(1, 0)
    };

    public List<TileData> FindTileNeighbours(TileData currentTile) {
        List<TileData> neighbourTile = new List<TileData>();

        foreach (Vector2Int dir in Directions) {
            int gridX = currentTile.gridX + dir.x;
            int gridY = currentTile.gridY + dir.y;

            if (gridX < 0 || gridX >= tileSO.numberOfTilesToSpawnX || gridY < 0 || gridY >= tileSO.numberOfTilesToSpawnY) {
                continue; // Skip out of bounds tiles
            }

            neighbourTile.Add(tileData[gridX, gridY]);
        }
        return neighbourTile;
    }
    public bool checkHighlightedTile(TileData checkTile) {
        if (checkTile == null) return false;

        return highlightedTileHash.Contains(checkTile);
    }

    // Node class for A* alogorithm
    public class Node {
        public TileData tile;
        public Node parent;

        public int gCost;
        public int hCost;
        public int fCost => gCost + hCost;
        public Node(TileData tile, Node parent, int gCost, int hCost) {
            this.tile = tile;
            this.parent = parent;
            this.gCost = gCost;
            this.hCost = hCost;
        }
    }
    // OpenList for unsearched tiles
    List<Node> openList = new List<Node>();

    // ClosedList for searched tiles
    List<Node> closedList = new List<Node>();
    public List<TileData> A_Algorithm(TileData start, TileData end){
        openList.Add(new Node(start, null, 0, (GetManhattanDistance(start.worldPosition, end.worldPosition) * 10)));

        Node currentNode = new Node(null, null, 0, 0);

        List<TileData> neighbourTileList = new List<TileData>();

        while (openList.Count > 0) {
            currentNode = openList[0];

            closedList.Add(currentNode);
            openList.RemoveAt(0);

            if (currentNode.tile == end) {
                break;
            }

            neighbourTileList = FindTileNeighbours(currentNode.tile);

            foreach (Node node in openList) {
                if (!neighbourTileList.Contains(node.tile)) {
                    continue;
                }
                if (node.gCost >= currentNode.gCost + 10) {
                    continue;
                }
                node.parent = currentNode;
                node.gCost = currentNode.gCost + 10;
                neighbourTileList.Remove(node.tile);
            }

            foreach (Node node in closedList) {
                if (!neighbourTileList.Contains(node.tile)) {
                    continue;
                }
                neighbourTileList.Remove(node.tile);
            }

            foreach (TileData tile in neighbourTileList) {
                if (!tile.isWalkable) {
                    continue;
                }
                openList.Add(InsertNode(tile, end, currentNode));
            }

            openList.Sort((nodeA, nodeB) => {
                int value = nodeA.fCost.CompareTo(nodeB.fCost);

                if (value == 0) {
                    value = nodeA.hCost.CompareTo(nodeB.hCost);
                }

                return value;
            });
        }

        List<TileData> tileList = new List<TileData>();
        if (currentNode != null && currentNode.tile == end) {
            while (currentNode.tile != start) {
                tileList.Add(currentNode.tile);
                currentNode = currentNode.parent;
            }
        }

        tileList.Reverse();

        closedList.Clear();
        openList.Clear();

        return tileList;
        
    }
    public Node InsertNode(TileData tile, TileData target, Node parent) {
        int gCost = parent.gCost + 10;
        int hCost = GetManhattanDistance(tile.worldPosition, target.worldPosition) * 10;

        return new Node(tile, parent, gCost, hCost);
    }
}

