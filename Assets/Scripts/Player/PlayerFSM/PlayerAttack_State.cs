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
        PlayerInput.Instance.OnMouseClicked += AttackTarget;
    }

    public void Execute() { }

    public void Exit() {
        GridSystem.Instance.DeleteAttackRange();
        PlayerInput.Instance.OnMouseClicked -= AttackTarget;
    }

    public void AttackTarget(Vector2 cordinate) {
        Ray ray = Camera.main.ScreenPointToRay(cordinate);

        if (Physics.Raycast(ray, out RaycastHit hit)) {
            TileData currentTile = GridSystem.Instance.WorldPositionToGridTile(hit.point);

            if (currentTile != null) {
                Vector2Int currentCordinate = new Vector2Int(currentTile.gridX, currentTile.gridY);

                if (GridSystem.Instance.checkHighlightedTile(currentTile)) {
                    if (UnitManager.Instance.registeredUnit.TryGetValue(currentCordinate, out Unit targetUnit)) {
                        if (targetUnit.unitFaction != Faction.Enemy) {
                            activeUnit.transform.LookAt(hit.point);
                            activeUnit.Attack();
                        }
                    }

                }
            }

            machine.UnitEnd();
        }
    }
}
