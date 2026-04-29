using System; 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Random = UnityEngine.Random;

public class EnemyMove_State : ITurnState{
    private Unit activeUnit;
    private EnemyFSM machine;

    Vector3 randomPosition;
    TileData targetTile;
    TileData startTile;

    List<TileData> routeTiles = new List<TileData>();

    float moveSpeed = 1f;

    public EnemyMove_State(EnemyFSM machine) {       
        this.machine = machine;
        this.activeUnit = machine.activeUnit;
    }
    private int moveRange; 

    public void Enter() {
        moveRange = activeUnit.GetMoveRange();

        Debug.Log("Enemy Turn");
        machine.StartCoroutine(Movement(moveRange, () => {
            machine.UnitEnd();
        }));
    }
    public void Execute() { }
    public void Exit() {

        Debug.Log("Enemy Trun End.");
    }

    public IEnumerator Movement(int moveRange, Action onComplete) {
        int attemptCount = 0;
        int maxAttempt = 50;
        startTile = GridSystem.Instance.WorldPositionToGridTile(activeUnit.transform.position);

        do {
            int randomX = Random.Range(-moveRange, moveRange + 1);
            int randomY = Random.Range(-moveRange + Mathf.Abs(randomX), moveRange - Mathf.Abs(randomX) + 1);
            randomPosition = activeUnit.transform.position + new Vector3(randomX, 0, randomY);

            targetTile = GridSystem.Instance.WorldPositionToGridTile(randomPosition);

            attemptCount++;
            if (attemptCount == maxAttempt) {
                onComplete?.Invoke();
                yield break;
            }
        }
        while (targetTile == null || !targetTile.isWalkable);

        routeTiles = GridSystem.Instance.A_Algorithm(startTile, targetTile);

        // Control move range
        if (routeTiles.Count > moveRange) {
            routeTiles.RemoveRange(moveRange, routeTiles.Count - moveRange);
        }

        if (routeTiles.Count > 0) {
            foreach (TileData tile in routeTiles) {
                Vector3 targetPosition = tile.worldPosition;
                targetPosition.y = 0f;
                while (Vector3.Distance(activeUnit.transform.position, targetPosition) > 0.01f) {

                    activeUnit.transform.LookAt(targetPosition);
                    activeUnit.transform.position = Vector3.MoveTowards(activeUnit.transform.position, targetPosition, moveSpeed * Time.deltaTime);

                    yield return null;
                }
                Vector2Int newPosition = new Vector2Int((int)targetPosition.x, (int)targetPosition.z);

                UnitManager.Instance.MoveUnit(newPosition, activeUnit);

                activeUnit.transform.position = targetPosition;
            }
        }
        onComplete?.Invoke();
    }
}
