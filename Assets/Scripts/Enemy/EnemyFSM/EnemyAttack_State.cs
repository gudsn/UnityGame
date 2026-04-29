using UnityEngine;

public class EnemyAttack_State : ITurnState{
    private Unit activeUnit;
    private EnemyFSM machine;

    public EnemyAttack_State(EnemyFSM machine) {
        this.machine = machine;
        this.activeUnit = machine.activeUnit;
    }
    public void Enter() { }

    public void Execute() { }

    public void Exit() { }
}
