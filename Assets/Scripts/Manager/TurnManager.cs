using UnityEngine;

public class TurnManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static TurnManager Instance { get; private set ;}

    public PlayerController playerController;
    public PlayerInput playerInput;

    public ITurnState playerTurnState;
    public ITurnState enemyTurnState;

    public EnemyMove enemyMove;
    
    private ITurnState currentState;

    public void ChangeState(ITurnState newState) {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter(this);
    }
    void Awake()
    {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void InitTurnSystem() {
        playerTurnState = new PlayerTurn();
        enemyTurnState = new EnemyTurn();

        ChangeState(playerTurnState);
    }

    // Update is called once per frame
    void Update()
    {
        currentState.Execute();
    }

}
