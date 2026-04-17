using UnityEngine;

public class PlayerFSM : MonoBehaviour
{
    private ITurnState currentState;

    public Unit activeUnit { get; private set;}

    void Awake() {
    }

    // Update is called once per frame
    void Update() {
        currentState?.Execute();
    }

    public void ChangeState(ITurnState newState) {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }
    public void StartTurnfor(Unit currentUnit) {
        this.activeUnit = currentUnit;
       
        ChangeState(new PlayerMove_State(this));
    }
    public void UnitEnd() {
        ChangeState(null);
        FSMManager.Instance.EndFSM(activeUnit);
    }

}
