using System.Collections.Generic;
using UnityEngine;

public class EnemyBrain : MonoBehaviour {
    private Unit currentUnit;
    private int chaseTurn = 0;

    private List<IUtilityStrategy> strategies;

    private void Awake() {
        currentUnit = GetComponent<Unit>();

        // 전략 객체 초기화
        strategies = new List<IUtilityStrategy> {
            new AttackStrategy(),
            new FleeStrategy()
        };
    }

    // AI 행동 계획 수립 (외부 호출용)
    public AIDecision PlanAITurn() {
        UpdateVisionAndChaseState();

        AIDecision finalDecision = GetBestDecision();

        Debug.Log($"[{currentUnit.GetName()}] 계획 수립: {finalDecision.decisionName} (점수: {finalDecision.utilityScore})");

        return finalDecision;
    }

    // 거리 기반 시야 판별 및 추격 상태 갱신
    private void UpdateVisionAndChaseState() {
        int viewRange = currentUnit.GetMoveRange() + 1; // 시야: 이동 사거리 + 1
        bool isEnemyInSight = false;

        foreach (var it in UnitManager.Instance.RegisteredUnit) {
            Unit checkUnit = it.Value;

            // 아군 및 사망 유닛 무시
            if (currentUnit.unitFaction == checkUnit.unitFaction || checkUnit.GetHealth() <= 0) continue;

            // 맨해튼 거리로 타겟 발견 여부 확인
            int dist = GridSystem.Instance.GetManhattanDistance(currentUnit.currentPosition, checkUnit.currentPosition);

            if (dist <= viewRange) {
                isEnemyInSight = true;
                break;
            }
        }

        // 추격 턴 갱신
        if (isEnemyInSight) chaseTurn = 3;
        else if (chaseTurn > 0) chaseTurn--;
    }

    // 최고 점수의 의사결정 반환
    private AIDecision GetBestDecision() {
        // 1. 기본값: 대기 (20점)
        AIDecision bestDecision = new AIDecision { utilityScore = 20f, decisionName = "Wait" };
        bestDecision.actionQueue.Enqueue(new WaitCommand(0.5f));

        // 2. 전략 순회 및 최고 점수 갱신
        foreach (var strategy in strategies) {
            AIDecision candidate = strategy.Evaluate(currentUnit, chaseTurn);

            // 유효한 액션 및 더 높은 점수일 때 갱신
            if (candidate != null && candidate.utilityScore > bestDecision.utilityScore && candidate.actionQueue.Count > 0) {
                bestDecision = candidate;
            }
        }

        return bestDecision;
    }
}