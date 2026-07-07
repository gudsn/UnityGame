using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerAttackState : ITurnState {
    private PlayerFSM machine;
    private Unit activeUnit;
    private int attackRange = 2;

    private List<TileData> validAttackTile;
    public PlayerAttackState(PlayerFSM machine) {
        this.machine = machine;
        this.activeUnit = machine.activeUnit;
    }
    public void Enter() {
        Debug.Log("Attack phase.");

        // [변경] 유닛의 현재 고정 위치가 아닌, 예약 상태가 반영된 가상 위치를 기준으로 범위를 뿌립니다.
        Vector3 virtualWorldPos = GridSystem.Instance.GetTileData(activeUnit.virtualPosition).worldPosition;
        validAttackTile = GridSystem.Instance.SpawnAttackRange(virtualWorldPos, attackRange);

        PlayerInput.Instance.OnLeftMouseClicked += AttackTarget;
        PlayerInput.Instance.OnEnterTriggered += SkipTurn;
    }

    public void Execute() { }

    public void Exit() {
        GridSystem.Instance.DeleteAttackRange();
        PlayerInput.Instance.OnLeftMouseClicked -= AttackTarget;
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

            if (!validAttackTile.Contains(currentTile)) {
                Debug.Log("Out of attack range!");
                return;
            }

            UnitManager.Instance.RegisteredUnit.TryGetValue(currentCordinate, out Unit targetUnit);

            if (targetUnit != null && targetUnit.unitFaction != Faction.Enemy) {
                Debug.Log("Can't attack this unit!");
                return;
            }

            AttackCommand attackCmd = new AttackCommand(activeUnit, targetUnit, currentCordinate);
            AIDecision playerDecision = new AIDecision {
                utilityScore = 100f,
                intendedCommands = new List<ICommand> { attackCmd }
            };

            TimeLineManager.Instance.ScheduleAction(activeUnit, playerDecision);

            EventBus<DisableAttackButtonEvent>.Publish(new DisableAttackButtonEvent());
            machine.ChangeState(null);
        }
    }
    public void SkipTurn() {
        EventBus<DisableAttackButtonEvent>.Publish(new DisableAttackButtonEvent());
        machine.ChangeState(null);
    }
}