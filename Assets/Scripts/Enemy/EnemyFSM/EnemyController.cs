using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour {
    public Unit activeUnit { get; private set; }


    public void StartTurnfor(Unit activeUnit) {
        this.activeUnit = activeUnit;
        EnemyBrain brain = activeUnit.GetComponent<EnemyBrain>();

        AIDecision currentDecision = brain.PlanAITurn();

        StartCoroutine(ExecuteActionQueue(currentDecision.actionQueue));
    }

    private IEnumerator ExecuteActionQueue(Queue<ICommand> actions) {
        while (actions.Count > 0) {
            ICommand nextCommand = actions.Dequeue();

            yield return StartCoroutine(nextCommand.ExecuteCoroutine());
        }

        UnitEnd();
    }

    public void UnitEnd() {
        FSMManager.Instance.EndFSM(activeUnit);
    }
}