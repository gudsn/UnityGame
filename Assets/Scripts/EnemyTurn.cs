using System.Diagnostics;
using UnityEngine;

using Debug = UnityEngine.Debug;
public class EnemyTurn : ITurnState
{
    private ITurnState playerTurnState;

    private EnemyMove move;
    private int moveRange = 2;

    public void Enter(TurnManager manager) {
        move = manager.enemyMove;
        playerTurnState = manager.playerTurnState;

        TurnManager.Instance.StartCoroutine(move.Movement(moveRange, ()=> {
            TurnManager.Instance.ChangeState(playerTurnState);
        }));
    }
    public void Execute() { }
    public void Exit() {

        Debug.Log("적 턴 종료.");
    }
}
