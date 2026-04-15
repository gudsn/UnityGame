using System; 
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemyMove : MonoBehaviour {
    Vector3 randomPosition;
    TileData targetTile;
    TileData startTile;

    List<TileData> routeTiles = new List<TileData>();

    float moveSpeed = 1f;

    public IEnumerator Movement(int moveRange, Action onComplete) {
        int attemptCount = 0;
        int maxAttempt = 50;
        startTile = GridSystem.Instance.WorldPositionToGridTile(transform.position);

        do {
            int randomX = Random.Range(-moveRange, moveRange + 1);
            int randomY = Random.Range(-moveRange + Mathf.Abs(randomX), moveRange - Mathf.Abs(randomX) + 1);
            randomPosition = transform.position + new Vector3(randomX, 0, randomY);

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
                while (Vector3.Distance(transform.position, targetPosition) > 0.01f) {

                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

                    yield return null;
                }
                transform.position = targetPosition;
            }
        }
        onComplete?.Invoke();
    }
}
