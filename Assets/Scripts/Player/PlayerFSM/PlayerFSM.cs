using UnityEngine;

public class PlayerFSM : MonoBehaviour {
    private ITurnState currentState;

    // FSMManager가 플레이어 조작 완료 여부를 확인할 수 있도록 프로퍼티 개방
    public ITurnState CurrentState => currentState;

    public Unit activeUnit { get; private set; }

    // 이번 턴 행동들의 중복 예약을 제어하기 위한 독립 플래그
    public bool HasReservedMove { get; set; } = false;
    public bool HasReservedAttack { get; set; } = false;

    private void OnEnable() {
        EventBus<UIActionAttackEvent>.Subscribe(OnUIActionAttack);
        EventBus<UIActionMoveEvent>.Subscribe(OnUIActionMove);
        EventBus<UIActionNextEvent>.Subscribe(OnUIActionNext);
    }

    private void OnDisable() {
        EventBus<UIActionAttackEvent>.Unsubscribe(OnUIActionAttack);
        EventBus<UIActionMoveEvent>.Unsubscribe(OnUIActionMove);
        EventBus<UIActionNextEvent>.Unsubscribe(OnUIActionNext);
    }

    void Update() {
        currentState?.Execute();
    }

    private void OnUIActionAttack(UIActionAttackEvent evt) {
        if (activeUnit != null && !HasReservedAttack) {
            ChangeState(new PlayerAttackState(this));
        }
    }

    private void OnUIActionMove(UIActionMoveEvent evt) {
        if (activeUnit != null && !HasReservedMove) {
            ChangeState(new PlayerMoveState(this));
        }
    }

    private void OnUIActionNext(UIActionNextEvent evt) {
        if (activeUnit != null) {
            UnitEnd(); // UI 상에서 Next 버튼을 눌렀을 때만 예약을 닫고 마감
        }
    }

    public void ChangeState(ITurnState newState) {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    public void StartTurnfor(Unit currentUnit) {
        this.activeUnit = currentUnit;

        // 턴 시작 시 가상 좌표를 실제 물리 좌표와 강제 동기화
        if (activeUnit != null) {
            activeUnit.virtualPosition = activeUnit.currentPosition;
        }

        HasReservedMove = false;
        HasReservedAttack = false;

        EventBus<ShowPlayerActionsEvent>.Publish(new ShowPlayerActionsEvent());

        // 시작 시 Idle 상태를 부여하여 FSMManager 코루틴 방어
        ChangeState(new PlayerIdleState(this));
    }

    public void UnitEnd() {
        // [핵심 보완] 타임라인 슬롯에 이동 박스를 최종 드롭하지 않은 채 턴을 넘긴 경우
        // 임시로 변경되었던 virtualPosition을 원래 물리 좌표로 완전 원복
        if (activeUnit != null && !HasReservedMove) {
            activeUnit.virtualPosition = activeUnit.currentPosition;
        }

        ChangeState(null);
        EventBus<HidePlayerActionsEvent>.Publish(new HidePlayerActionsEvent());
    }
}