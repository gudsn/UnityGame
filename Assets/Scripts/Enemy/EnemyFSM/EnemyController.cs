using System.Collections;
using UnityEngine;

// 적 턴 진행 및 타임라인 실행 중계
public class EnemyController : MonoBehaviour {
    public Unit activeUnit { get; private set; }

    // 턴 시작 진입점
    public void StartTurnfor(Unit activeUnit) {
        this.activeUnit = activeUnit;
        StartCoroutine(ProcessTurn());
    }

    // 턴 처리 코루틴
    private IEnumerator ProcessTurn() {
        EnemyBrain brain = activeUnit.GetComponent<EnemyBrain>();

        // 1. 의사결정 도출
        AIDecision currentDecision = brain.PlanAITurn();

        // 2. 타임라인 대기열에 행동 예약
        TimeLineManager.Instance.ScheduleAction(activeUnit, currentDecision);

        // 3. 틱 엔진 가동 및 완료 대기
        yield return StartCoroutine(TimeLineManager.Instance.RunTickEngine());

        // 4. 턴 마감 처리
        UnitEnd();
    }

    // 턴 종료 및 상태 전환
    public void UnitEnd() {
        FSMManager.Instance.EndFSM(activeUnit);
    }
}