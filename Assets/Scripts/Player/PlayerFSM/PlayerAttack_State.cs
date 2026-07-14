using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackState : ITurnState {
    private PlayerFSM machine;
    private Unit activeUnit;
    private int attackRange = 2; // 맨해튼 거리 2칸

    private List<TileData> validAttackTile;

    public PlayerAttackState(PlayerFSM machine) {
        this.machine = machine;
        this.activeUnit = machine.activeUnit;
    }

    public void Enter() {
        Debug.Log("[공격 페이즈] 사거리 내의 적을 클릭하여 공격을 예약하세요.");

        // 유닛의 물리적 위치가 아닌, 예약 정황이 반영된 '가상 위치'를 기준으로 사거리 2칸 생성
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

            // 1. 공격 명령 캡슐화 및 타임라인 큐 적재
            AttackCommand attackCmd = new AttackCommand(activeUnit, targetUnit, currentCordinate);
            AIDecision playerDecision = new AIDecision {
                utilityScore = 100f,
                intendedCommands = new List<ICommand> { attackCmd }
            };
            TimeLineManager.Instance.ScheduleAction(activeUnit, playerDecision);

            // 2. UI 버튼 비활성화 및 FSM 내 공격 완료 플래그 세팅
            EventBus<DisableAttackButtonEvent>.Publish(new DisableAttackButtonEvent());
            machine.HasReservedAttack = true;

            // [핵심 변경] 예약을 마친 후 턴을 강제 종료하지 않고 대기 상태(Idle)로 안전 복귀
            machine.ChangeState(new PlayerIdleState(machine));
        }
    }

    public void SkipTurn() {
        // 공격 입력을 취소(스킵)할 경우 다시 행동 선택 대기 상태(Idle)로 빠져나감
        machine.ChangeState(new PlayerIdleState(machine));
    }
}