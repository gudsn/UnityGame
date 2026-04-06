using UnityEngine;
using System; 
using System.Collections;

using Random = UnityEngine.Random;

public class EnemyMove : MonoBehaviour
{
    Vector3 randomPosition;
    TileData targetTile;
    public IEnumerator Movement(int moveRange, Action onComplete) {
        int attemptCount = 0; 
        int maxAttempt = 50;

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

        transform.position = randomPosition;
        onComplete?.Invoke();
        yield return null;
    }
}
