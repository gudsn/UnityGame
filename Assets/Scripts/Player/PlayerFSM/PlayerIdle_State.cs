using UnityEngine;

public class PlayerIdleState : ITurnState {
    private PlayerFSM machine;

    public PlayerIdleState(PlayerFSM machine) {
        this.machine = machine;
    }

    public void Enter() {
        Debug.Log($"[인풋 대기] {machine.activeUnit.gameObject.name}의 행동 예약 대기 중... (이동/공격 선택 가능)");
    }

    public void Execute() {
        // 마우스 호버 타일 툴팁 연산 등의 공통 루틴 수행
    }

    public void Exit() {
        // 이동이나 공격 상태로 화면이 전환될 때 정리 로직 수행
    }
}