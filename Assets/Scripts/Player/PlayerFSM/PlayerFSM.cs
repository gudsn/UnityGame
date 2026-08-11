using UnityEngine;

public class PlayerFSM : MonoBehaviour {
    public Unit activeUnit { get; set; }

    public ITurnState currentState { get; private set; }
    public ITurnState CurrentState => currentState;

    public bool IsTurnCompleted { get; set; } = false;
    public bool HasReservedMove { get; set; } = false;
    public bool HasReservedAttack { get; set; } = false;

    private void Awake() {
        EventBus<UIActionMoveEvent>.Subscribe(OnUIActionMove);
        EventBus<UIActionAttackEvent>.Subscribe(OnUIActionAttack);
        EventBus<UIActionNextEvent>.Subscribe(OnUIActionNext);
    }

    private void OnDestroy() {
        EventBus<UIActionMoveEvent>.Unsubscribe(OnUIActionMove);
        EventBus<UIActionAttackEvent>.Unsubscribe(OnUIActionAttack);
        EventBus<UIActionNextEvent>.Unsubscribe(OnUIActionNext);
    }

    // 새 턴/라운드 시작 시 호출
    public void StartTurnfor(Unit unit) {
        activeUnit = unit;
        IsTurnCompleted = false;
        HasReservedMove = false;
        HasReservedAttack = false;

        // 💡 새 라운드 시작 시 가상 위치를 현재 위치로 동기화
        if (activeUnit != null) {
            activeUnit.virtualPosition = activeUnit.currentPosition;
        }

        // UI 버튼 전체 활성화
        EventBus<ShowPlayerActionsEvent>.Publish(new ShowPlayerActionsEvent());

        ChangeState(new PlayerIdleState(this));
    }

    private void OnUIActionMove(UIActionMoveEvent evt) {
        if (activeUnit != null && !HasReservedMove) {
            ChangeState(new PlayerMoveState(this));
        }
    }

    private void OnUIActionAttack(UIActionAttackEvent evt) {
        if (activeUnit != null && !HasReservedAttack) {
            ChangeState(new PlayerAttackState(this));
        }
    }

    private void OnUIActionNext(UIActionNextEvent evt) {
        if (activeUnit != null) {
            IsTurnCompleted = true;
            ChangeState(null);
            EventBus<HidePlayerActionsEvent>.Publish(new HidePlayerActionsEvent());
            Debug.Log($"<color=green>[턴 완료]</color> {activeUnit.GetName()}의 턴 종료 -> 틱 엔진 구동");
        }
    }

    public void ChangeState(ITurnState newState) {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    private void Update() {
        currentState?.Execute();
    }
}