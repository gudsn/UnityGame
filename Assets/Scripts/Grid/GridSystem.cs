using System.Collections.Generic;
using UnityEngine;

public class GridSystem : MonoBehaviour {

    public static GridSystem Instance { get; private set; }

    [SerializeField] private GameObject gridPrefab;
    [SerializeField] private TileSO tileSO;
    [SerializeField] private HighlightTilePool tilePool;

    private TileData[,] tileData;
    private TileScript[,] tileScript;

    private Vector3 gridCenter = Vector3.zero;
    private int gridSize = 1;
    private int maxSpwanX;
    private int maxSpwanY;

    private HashSet<TileData> highlightedTileHash;
    private int instanceNumber = 0;

    void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        gridCenter = transform.position;
        maxSpwanX = tileSO.numberOfTilesToSpawnX;
        maxSpwanY = tileSO.numberOfTilesToSpawnY;

        tileData = new TileData[maxSpwanX, maxSpwanY];
        tileScript = new TileScript[maxSpwanX, maxSpwanY];
        highlightedTileHash = new HashSet<TileData>();
    }

    // 타일의 점유 상태를 설정합니다.
    public void SetTileOccupide(Vector2Int cordinate, bool status) {
        if (cordinate.x < maxSpwanX && cordinate.x >= 0 && cordinate.y < maxSpwanY && cordinate.y >= 0) {
            tileData[cordinate.x, cordinate.y].isOccupied = status;
        }
    }

    // 전장 사각형 기본 그리드 프리팹들을 생성 배치합니다.
    public void SpawnSquareGrid() {
        Vector3 gridLeftBottom = gridCenter - new Vector3((maxSpwanX * gridSize) / 2, 0f, (maxSpwanY * gridSize) / 2);

        for (int gridX = 0; gridX < maxSpwanX; gridX++) {
            for (int gridY = 0; gridY < maxSpwanY; gridY++) {
                Vector3 spawnPosition = gridLeftBottom + new Vector3(gridX * gridSize, 0, gridY * gridSize);
                GameObject tileInstance = Instantiate(gridPrefab, spawnPosition, Quaternion.identity);
                tileInstance.name = tileSO.tileName + instanceNumber;

                tileData[gridX, gridY] = new TileData(gridX, gridY, spawnPosition, true, false);
                tileScript[gridX, gridY] = tileInstance.GetComponent<TileScript>();

                if (tileScript[gridX, gridY] != null) {
                    tileScript[gridX, gridY].SetUp(tileData[gridX, gridY]);
                }
                instanceNumber++;
            }
        }
    }

    // 월드 공간 좌표를 그리드 배열의 인덱스 일치 타일 데이터로 반환합니다.
    public TileData WorldPositionToGridTile(Vector3 worldPosition) {
        Vector3 leftBottom = gridCenter - new Vector3((maxSpwanX * gridSize) / 2, 0f, (maxSpwanY * gridSize) / 2);
        Vector3 currentGridPosition = worldPosition - leftBottom;

        int gridX = Mathf.RoundToInt(currentGridPosition.x / (float)gridSize);
        int gridY = Mathf.RoundToInt(currentGridPosition.z / (float)gridSize);

        if (gridX >= 0 && gridX < maxSpwanX && gridY >= 0 && gridY < maxSpwanY) {
            return tileData[gridX, gridY];
        }
        return null;
    }

    // 맨해튼 기동력 반경 그리드를 산출하고 바닥 하이라이트를 표시합니다.
    public HashSet<TileData> SpawnManhattanDistanceGrid(Vector3 worldPosition, int range, HighlightType highlightType) {
        TileData startTile = WorldPositionToGridTile(worldPosition);
        if (startTile == null) return new HashSet<TileData>();

        HashSet<TileData> visitedHash = GetManhattanGrid(startTile, range);
        foreach (TileData tile in visitedHash) {
            Vector3 highlightPosition = new Vector3(tile.worldPosition.x, 0.01f, tile.worldPosition.z);
            tilePool.GetHighLightTile(highlightType, highlightPosition);
        }
        return visitedHash ?? new HashSet<TileData>();
    }

    public void DeleteManhattanDistanceGrid() {
        tilePool.ReturnHighLightTiles();
    }

    // BFS 기반 순수 맨해튼 매트릭스 타일 노드 컬렉션을 마스킹 수집합니다.
    public HashSet<TileData> GetManhattanGrid(TileData startTile, int range) {
        HashSet<TileData> visitedHash = new HashSet<TileData>();
        if (range < 0) return visitedHash;

        Queue<TileData> checkQueue = new Queue<TileData>();
        checkQueue.Enqueue(startTile);
        visitedHash.Add(startTile);

        for (int currentRange = 1; currentRange <= range; currentRange++) {
            int currentQueueCount = checkQueue.Count;
            for (int queueCount = 0; queueCount < currentQueueCount; queueCount++) {
                TileData currentTile = checkQueue.Dequeue();
                List<TileData> neighbourList = FindTileNeighbours(currentTile, 1);

                foreach (TileData n in neighbourList) {
                    if (visitedHash.Contains(n) || !n.isWalkable || n.isOccupied) continue;
                    checkQueue.Enqueue(n);
                    visitedHash.Add(n);
                }
            }
        }
        return visitedHash;
    }

    // 공격 사거리를 연산하여 타격 가능 구역 하이라이트를 표시합니다.
    public List<TileData> SpawnAttackRange(Vector3 currentPosition, int attackRange) {
        TileData currentTile = WorldPositionToGridTile(currentPosition);
        if (currentTile == null) return new List<TileData>();

        List<TileData> attakRangeTile = FindTileNeighbours(currentTile, attackRange);
        foreach (TileData tile in attakRangeTile) {
            Vector3 highlightPosition = new Vector3(tile.worldPosition.x, 0.01f, tile.worldPosition.z);
            tilePool.GetHighLightTile(HighlightType.Attack, highlightPosition);
        }
        return attakRangeTile;
    }

    public int GetManhattanDistance(Vector2Int startCordinate, Vector2Int endcordinate) {
        return Mathf.Abs(startCordinate.x - endcordinate.x) + Mathf.Abs(startCordinate.y - endcordinate.y);
    }

    public int GetManhattanDistance(Vector3 start, Vector3 end) {
        TileData startTile = WorldPositionToGridTile(start);
        TileData endTile = WorldPositionToGridTile(end);
        if (startTile == null || endTile == null) return 9999;

        return GetManhattanDistance(new Vector2Int(startTile.gridX, startTile.gridY), new Vector2Int(endTile.gridX, endTile.gridY));
    }

    public void DeleteAttackRange() {
        tilePool.ReturnHighLightTiles(HighlightType.Attack);
    }

    private static readonly Vector2Int[] Directions = {
        new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0)
    };

    // 사방 십자 방향으로 지정 사거리 범위 안의 이웃 타일들을 구합니다.
    public List<TileData> FindTileNeighbours(TileData currentTile, int range) {
        List<TileData> neighbourTile = new List<TileData>();

        foreach (Vector2Int dir in Directions) {
            for (int currentRange = 1; currentRange <= range; currentRange++) {
                Vector2Int currentDir = dir * currentRange;
                int gridX = currentTile.gridX + currentDir.x;
                int gridY = currentTile.gridY + currentDir.y;

                if (gridX < 0 || gridX >= maxSpwanX || gridY < 0 || gridY >= maxSpwanY) continue;
                neighbourTile.Add(tileData[gridX, gridY]);
            }
        }
        return neighbourTile;
    }

    public bool CheckHighlightedTile(TileData checkTile) {
        if (checkTile == null) return false;
        return highlightedTileHash.Contains(checkTile);
    }

    public class Node {
        public TileData tile;
        public Node parent;
        public int gCost;
        public int hCost;
        public int fCost => gCost + hCost;
        public Node(TileData tile, Node parent, int gCost, int hCost) {
            this.tile = tile; this.parent = parent; this.gCost = gCost; this.hCost = hCost;
        }
    }

    PriorityQueue<Node> openQueue = new PriorityQueue<Node>(PriorityQueue<Node>.HeapType.min);
    Dictionary<TileData, Node> openSet = new Dictionary<TileData, Node>();
    HashSet<TileData> closedHash = new HashSet<TileData>();

    // 목적지 우회 최단 링크를 구축하는 A* 알고리즘입니다.
    public List<TileData> AStarAlgorithm(TileData start, TileData end) {
        if (start == null || end == null || start == end) return new List<TileData>();

        openQueue.Clear(); openSet.Clear(); closedHash.Clear();

        Node startNode = new Node(start, null, 0, 0);
        openSet[start] = startNode;
        openQueue.Enqueue(startNode.fCost, startNode);

        Node currentNode = null;
        List<TileData> neighbourList = new List<TileData>();

        while (openQueue.Count > 0) {
            currentNode = openQueue.Dequeue();
            if (currentNode == null) break;

            openSet.Remove(currentNode.tile);
            closedHash.Add(currentNode.tile);

            if (currentNode.tile == end) break;
            neighbourList = FindTileNeighbours(currentNode.tile, 1);

            foreach (TileData tile in neighbourList) {
                if ((!tile.isWalkable && tile != end) || (tile.isOccupied && tile != end) || closedHash.Contains(tile)) continue;

                int newGCost = currentNode.gCost + 10;
                if (openSet.TryGetValue(tile, out Node checkNode)) {
                    if (checkNode.gCost > newGCost) {
                        checkNode.gCost = newGCost; checkNode.parent = currentNode;
                        openQueue.Update(checkNode, checkNode.fCost, checkNode);
                    }
                    continue;
                }

                Node newNode = InsertNode(tile, end, currentNode);
                openSet[tile] = newNode;
                openQueue.Enqueue(newNode.fCost, newNode);
            }
        }

        List<TileData> visitedTile = new List<TileData>();
        if (currentNode != null && currentNode.tile == end) {
            while (currentNode != null && currentNode.tile != start) {
                visitedTile.Add(currentNode.tile);
                currentNode = currentNode.parent;
            }
            visitedTile.Reverse();
        }
        return visitedTile;
    }

    public Node InsertNode(TileData tile, TileData target, Node parent) {
        int gCost = parent.gCost + 10;
        int hCost = GetManhattanDistance(new Vector2Int(tile.gridX, tile.gridY), new Vector2Int(target.gridX, target.gridY)) * 10;
        return new Node(tile, parent, gCost, hCost);
    }

    public TileData GetTileData(Vector2Int cordinate) {
        if ((cordinate.x < 0 || cordinate.x >= maxSpwanX) || (cordinate.y < 0 || cordinate.y >= maxSpwanY)) return null;
        return tileData[cordinate.x, cordinate.y];
    }

    // 적의 기본 이동 경로 베이스 바닥 타일 프리뷰 하이라이트를 생성합니다.
    public void SpawnEnemyMovePathIntent(List<TileData> path, HighlightType highlightType) {
        if (path == null || path.Count == 0) return;
        foreach (TileData tile in path) {
            Vector3 highlightPosition = new Vector3(tile.worldPosition.x, 0.01f, tile.worldPosition.z);
            tilePool.GetHighLightTile(highlightType, highlightPosition);
        }
    }

    // 적의 사거리 기반 타격 위협 지역 인텐트 그리드를 활성화합니다.
    public void SpawnEnemyAttackIntent(Vector2Int virtualPosition, HighlightType highlightType) {
        TileData centerTile = GetTileData(virtualPosition);
        if (centerTile == null) return;

        List<TileData> attackTiles = FindTileNeighbours(centerTile, 2);
        foreach (TileData tile in attackTiles) {
            Vector3 highlightPosition = new Vector3(tile.worldPosition.x, 0.01f, tile.worldPosition.z);
            tilePool.GetHighLightTile(highlightType, highlightPosition);
        }
    }

    public void ClearEnemyIntents(HighlightType moveType, HighlightType attackType) {
        tilePool.ReturnHighLightTiles(moveType);
        tilePool.ReturnHighLightTiles(attackType);
    }
}