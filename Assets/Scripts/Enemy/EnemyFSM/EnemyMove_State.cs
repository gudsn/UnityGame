using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Random = UnityEngine.Random;

public class EnemyMove_State : ITurnState {
    private Unit activeUnit;
    private EnemyController controller;

    private TileData targetTile;

    List<TileData> routeTiles = new List<TileData>();

    float moveSpeed = 1f;

    public EnemyMove_State(EnemyController controller, TileData targetTile = null) {
        this.controller = controller;
        this.activeUnit = controller.activeUnit;
        this.targetTile = targetTile;
    }
    private int moveRange;

    public void Enter() {
        moveRange = activeUnit.GetMoveRange();

        Debug.Log("Enemy Turn");
        controller.StartCoroutine(Movement(moveRange, targetTile, () => {
            controller.ExecuteNextAction();
        }));
    }
    public void Execute() { }
    public void Exit() {

        Debug.Log("Enemy Trun End.");
    }

    public IEnumerator Movement(int moveRange, TileData targetTile, Action onComplete) {

        TileData startTile = GridSystem.Instance.WorldPositionToGridTile(activeUnit.transform.position);

        if (targetTile == null) {
            targetTile = GetRandTile(moveRange);
        }

        routeTiles = GridSystem.Instance.A_Algorithm(startTile, targetTile);

        // Control move range
        if (routeTiles.Count > moveRange) {
            routeTiles.RemoveRange(moveRange, routeTiles.Count - moveRange);
        }

        if (routeTiles.Count > 0) {

            TileData finalTile = routeTiles[routeTiles.Count - 1];
            Vector2Int finalGridPos = new Vector2Int(finalTile.gridX, finalTile.gridY);
            UnitManager.Instance.MoveUnit(finalGridPos, activeUnit);

            foreach (TileData tile in routeTiles) {

                Vector3 targetPosition = tile.worldPosition;
                targetPosition.y = activeUnit.transform.position.y;

                while (Vector3.Distance(activeUnit.transform.position, targetPosition) > 0.05f) {

                    Vector3 direction = (targetPosition - activeUnit.transform.position).normalized;

                    if (direction != Vector3.zero) {
                        Quaternion lookRotation = Quaternion.LookRotation(direction);
                        activeUnit.transform.rotation = Quaternion.Slerp(activeUnit.transform.rotation, lookRotation, Time.deltaTime * 10f);
                    }

                    activeUnit.transform.position = Vector3.MoveTowards(activeUnit.transform.position, targetPosition, moveSpeed * Time.deltaTime);

                    yield return null;
                }

                activeUnit.transform.position = targetPosition;
            }
        }
        onComplete?.Invoke();
    }

    private TileData GetRandTile(int moveRange) {
        int attemptCount = 0;
        int maxAttempt = 50;
        Vector3 randomPosition;

        do {
            int randomX = Random.Range(-moveRange, moveRange + 1);
            int randomY = Random.Range(-moveRange + Mathf.Abs(randomX), moveRange - Mathf.Abs(randomX) + 1);
            randomPosition = activeUnit.transform.position + new Vector3(randomX, 0, randomY);

            targetTile = GridSystem.Instance.WorldPositionToGridTile(randomPosition);

            attemptCount++;
            if (attemptCount == maxAttempt) {
                break;
            }
        }
        while (targetTile == null || !targetTile.isWalkable);

        return targetTile;
    }
}
