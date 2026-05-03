using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject enemyPrefab;

    public Dictionary<Vector2Int, Unit> registeredUnit;

    public Action<Unit> OnSpawnUnit;
    public Action<Dictionary<Vector2Int, Unit>> OnMoveUnit;

    public static UnitManager Instance;
    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        registeredUnit = new Dictionary<Vector2Int, Unit>();

    }
    void Start(){
    }

    public void SetWorld() {
        GridSystem.Instance.spawnSquareGrid();

        Vector3 playerSpawnPosition = Vector3.zero;
        Vector3 enemySpawnPosition = new Vector3(2, 0, 0);

        GameObject playerInstance = Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
        if (playerInstance.TryGetComponent(out Unit playerUnit)) {
            TileData playerTile = GridSystem.Instance.WorldPositionToGridTile(playerSpawnPosition);
            Vector2Int playerGridPos = new Vector2Int(playerTile.gridX, playerTile.gridY);

            playerUnit.SetPosition(playerGridPos);
            SpawnUnit(playerUnit);
            GridSystem.Instance.UnitOnTile(playerGridPos);
        }

        GameObject enemyInstance = Instantiate(enemyPrefab, enemySpawnPosition, Quaternion.identity);
        if (enemyInstance.TryGetComponent(out Unit enemyUnit)) {
            TileData enemyTile = GridSystem.Instance.WorldPositionToGridTile(enemySpawnPosition);
            Vector2Int enemyGridPos = new Vector2Int(enemyTile.gridX, enemyTile.gridY);

            enemyUnit.SetPosition(enemyGridPos);
            SpawnUnit(enemyUnit);
            GridSystem.Instance.UnitOnTile(enemyGridPos);
        }

        FSMManager.Instance.StartState();
    }

    public void SpawnUnit(Unit unit) {
        Vector2Int currentUnitPosition = unit.currentPosition;

        registeredUnit.TryAdd(currentUnitPosition, unit);

        unit.OnUnitDie += KillUnit;

        OnSpawnUnit?.Invoke(unit);
    }

    public void MoveUnit (Vector2Int newPosition, Unit unit) {
        Vector2Int oldPosition = unit.currentPosition;

        if (registeredUnit.ContainsKey(oldPosition) && registeredUnit[oldPosition] == unit) {
            registeredUnit.Remove(oldPosition); 
            GridSystem.Instance.UnitOnTile(oldPosition);
        }

        registeredUnit.TryAdd(newPosition, unit);

        unit.SetPosition(newPosition);

        GridSystem.Instance.UnitOnTile(newPosition);

        OnMoveUnit?.Invoke(registeredUnit);
    }

    public void KillUnit(Unit unit) {
        Vector2Int currentPosition = unit.currentPosition;

        GridSystem.Instance.UnitOnTile(currentPosition);

        if (registeredUnit.ContainsKey(currentPosition) && registeredUnit[currentPosition] == unit) {
            registeredUnit.Remove(unit.currentPosition);
        }

        unit.OnUnitDie -= KillUnit;
    }


}
