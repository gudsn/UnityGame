using UnityEditor;
using UnityEngine;
public class PlayerTurn : ITurnState
{
    private PlayerInput input;
    private PlayerController controller;
    private ITurnState enemyTurnState;
    public void Enter(TurnManager manager) {
        input = manager.playerInput;
        controller = manager.playerController;
        enemyTurnState = manager.enemyTurnState;

        input.OnMoveInputTriggered += controller.IntendedMove;
        input.OnSetMoveInputTriggered += ConfirmMove;
        input.OnEnable();
    }
    public void Execute() { 
    }

    private void ConfirmMove() {
        controller.SetMove();
        TurnManager.Instance.ChangeState(enemyTurnState);
    }
    public void Exit() {
        input.OnMoveInputTriggered -= controller.IntendedMove;
        input.OnSetMoveInputTriggered -= ConfirmMove;

        input.enabled = false;

        Debug.Log("플레이어 턴 종료!");
    }

}
