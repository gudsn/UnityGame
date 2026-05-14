using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class EnemyController : MonoBehaviour{
    private ITurnState currentState;

    private Queue<ITurnState> Actions;

    public Unit activeUnit { get; private set;}

    private void Awake() {
        Actions = new Queue<ITurnState>();
    }
    void Update(){
        currentState?.Execute();    
    }

    public void StartTurnfor(Unit activeUnit) {
        this.activeUnit = activeUnit;

        EnemyBrain brain = activeUnit.GetComponent<EnemyBrain>();

        AIDecision currentDecision = brain.CalculateBestDecision();

        switch (currentDecision.actionType) {
            case AIActionType.Wait:
                Actions.Enqueue(new EnemyMove_State(this, currentDecision.destinationTile));
                break;
            case AIActionType.Attack:
                Actions.Enqueue(new EnemyMove_State(this, currentDecision.destinationTile));
                Actions.Enqueue(new EnemyAttack_State(this, currentDecision.targetUnit));
                break;
            case AIActionType.Flee:
                Actions.Enqueue(new EnemyMove_State(this, currentDecision.destinationTile));
                break;
        }
        ExecuteNextAction();
    }

    public void ExecuteNextAction() {
        if (Actions.Count > 0) {
            ITurnState nextState = Actions.Dequeue();
            ChangeState(nextState);
        }
        else {
            UnitEnd();
        }
    }
    public void ChangeState(ITurnState newState) {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }
    public void UnitEnd() {
        ChangeState(null);
        FSMManager.Instance.EndFSM(activeUnit);
    }
}
