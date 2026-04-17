using System.Xml.Serialization;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject enemyPrefab;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spawnUnit();
    }

    private void spawnUnit() {
        GridSystem.Instance.spawnSquareGrid();

        Vector3 playerSpawnPosition = Vector3.zero;
        Vector3 enemySpawnPosition = new Vector3(3, 0, 4);

        GameObject playerInstance = Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
        if (playerInstance.TryGetComponent(out Unit playerUnit)) {
            FSMManager.Instance.EnqueueNewUnit(playerUnit);
        }

        GameObject enemyInstance = Instantiate(enemyPrefab, enemySpawnPosition, Quaternion.identity);
        if (enemyInstance.TryGetComponent(out Unit enemyUnit)) {
            FSMManager.Instance.EnqueueNewUnit(enemyUnit);
        }

        FSMManager.Instance.StartState();
    }
    
}
