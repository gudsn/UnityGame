using System.ComponentModel.Design;
using UnityEngine;

public class PlayerFSM : MonoBehaviour
{
    private ITurnState currentState;

    public Unit activeUnit { get; private set;}

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

    // Update is called once per frame
    void Update() {
        currentState?.Execute();
    }

    private void OnUIActionAttack(UIActionAttackEvent evt) {
        if (activeUnit != null) {
            ChangeState(new PlayerAttack_State(this));
        }
    }
    private void OnUIActionMove(UIActionMoveEvent evt) {
        if (activeUnit != null) {
            ChangeState(new PlayerMove_State(this));
        }
    }
    private void OnUIActionNext(UIActionNextEvent evt) {
        if (activeUnit != null) {
            UnitEnd();
        }
    }
    public void ChangeState(ITurnState newState) {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }
    public void StartTurnfor(Unit currentUnit) {
        this.activeUnit = currentUnit;

        EventBus<ShowPlayerActionsEvent>.Publish(new ShowPlayerActionsEvent());
    }
    public void UnitEnd() {

        ChangeState(null);
        EventBus<HidePlayerActionsEvent>.Publish(new HidePlayerActionsEvent());
        FSMManager.Instance.EndFSM(activeUnit);
    }

}
