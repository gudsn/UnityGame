using UnityEngine;

public class PlayerFSM : MonoBehaviour {
    private ITurnState currentState;

    // FSMManager가 플레이어의 조작 완료 여부(null 여부)를 확인할 수 있도록 프로퍼티 개방
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
            UnitEnd(); // 오직 UI 상에서 Next 버튼을 눌렀을 때만 전체 예약을 닫고 마감합니다.
        }
    }

    public void ChangeState(ITurnState newState) {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    public void StartTurnfor(Unit currentUnit) {
        this.activeUnit = currentUnit;

        // 새로운 유닛 조작 시 행동 제어 플래그 초기화
        HasReservedMove = false;
        HasReservedAttack = false;

        EventBus<ShowPlayerActionsEvent>.Publish(new ShowPlayerActionsEvent());

        // [중요] 시작 시 null이 아닌 Idle 상태를 강제 부여하여 FSMManager가 즉시 패스하는 현상을 방어합니다.
        ChangeState(new PlayerIdleState(this));
    }

    public void UnitEnd() {
        // 모든 예약을 끝마쳤으므로 상태를 null로 전환하여 FSMManager의 코루틴 락을 풀어줍니다.
        ChangeState(null);
        EventBus<HidePlayerActionsEvent>.Publish(new HidePlayerActionsEvent());
    }
}