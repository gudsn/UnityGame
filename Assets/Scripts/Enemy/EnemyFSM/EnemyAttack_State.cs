using UnityEngine;

public class EnemyAttack_State : ITurnState{
    private Unit activeUnit;
    private EnemyController controller;
    private Unit targetUnit;
    public EnemyAttack_State(EnemyController controller, Unit targetUnit) {
        this.controller = controller;
        this.activeUnit = controller.activeUnit;
        this.targetUnit = targetUnit;
    }
    public void Enter() {
        TryAttack(targetUnit);
    }

    public void Execute() { }

    public void Exit() { }

    private void TryAttack(Unit targetUnit) {
        if (targetUnit == null) {
            controller.ExecuteNextAction();
            return;
        }

        Vector3 lookTarget = targetUnit.transform.position;
        lookTarget.y = activeUnit.transform.position.y;
        activeUnit.transform.LookAt(lookTarget);

        activeUnit.Attack(targetUnit.currentPosition);
        controller.ExecuteNextAction();
        return;
    }
}
