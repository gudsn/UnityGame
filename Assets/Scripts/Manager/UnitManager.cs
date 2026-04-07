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
        Vector3 playerSpawnPosition = Vector3.zero;
        Vector3 enemySpawnPosition = new Vector3(3, 0, 4);

        GameObject playerInstance = Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
        TurnManager.Instance.playerController = playerInstance.GetComponent<PlayerController>();
        TurnManager.Instance.playerInput = playerInstance.GetComponent<PlayerInput>();

        GameObject enemyInstance = Instantiate(enemyPrefab, enemySpawnPosition, Quaternion.identity);
        TurnManager.Instance.enemyMove = enemyInstance.GetComponent<EnemyMove>();

        TurnManager.Instance.InitTurnSystem();
    }
    
}
