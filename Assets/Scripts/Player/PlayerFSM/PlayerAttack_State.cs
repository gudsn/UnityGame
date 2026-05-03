using Unity.VisualScripting;
using UnityEngine;

public class PlayerAttack_State:ITurnState{
    private PlayerFSM machine;
    private Unit activeUnit;
    private int attackRange = 2;
    public PlayerAttack_State(PlayerFSM machine) {
        this.machine = machine;
        this.activeUnit = machine.activeUnit;
    }
    public void Enter() {
        Debug.Log("Attack phase.");
        GridSystem.Instance.SpawnAttackRange(activeUnit.transform.position, attackRange);
        PlayerInput.Instance.OnRightMouseClicked += AttackTarget;
        PlayerInput.Instance.OnEnterTriggered += SkipTurn;
    }

    public void Execute() { }

    public void Exit() {
        GridSystem.Instance.DeleteAttackRange();
        PlayerInput.Instance.OnRightMouseClicked -= AttackTarget;
        PlayerInput.Instance.OnEnterTriggered -= SkipTurn;
    }

    public void AttackTarget(Vector2 cordinate) {
        Ray ray = Camera.main.ScreenPointToRay(cordinate);

        if (Physics.Raycast(ray, out RaycastHit hit)) {

            Vector3 targetPosition = hit.point;
            Unit clickedUnit = hit.collider.GetComponentInParent<Unit>();

            if (clickedUnit != null) {
                targetPosition = clickedUnit.transform.position;
            }

            TileData currentTile = GridSystem.Instance.WorldPositionToGridTile(targetPosition);

            if (currentTile == null) {
                Debug.Log("Out of boundary!");
                return;
            }

            Vector2Int currentCordinate = new Vector2Int(currentTile.gridX, currentTile.gridY);

            if (!GridSystem.Instance.checkHighlightedTile(currentTile)) {
                Debug.Log("Out of attack range!");
                return;
            }
            if (!UnitManager.Instance.registeredUnit.TryGetValue(currentCordinate, out Unit targetUnit)) {
                Debug.Log("Please click the unit!");
                return;
            }
            if (targetUnit.unitFaction != Faction.Enemy) {
                Debug.Log("Can't attack this unit!");
                return;
            }

            Vector3 lookTarget = targetUnit.transform.position;
            lookTarget.y = activeUnit.transform.position.y;
            activeUnit.transform.LookAt(lookTarget);

            activeUnit.Attack(currentCordinate);
                                       
            machine.UnitEnd();
        }
    }
    public void SkipTurn() {
        machine.UnitEnd();
    }
}
