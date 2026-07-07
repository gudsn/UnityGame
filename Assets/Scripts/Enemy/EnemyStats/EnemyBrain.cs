using System.Collections.Generic;
using UnityEngine;

// AI 판단 주체
public class EnemyBrain : MonoBehaviour {
    private Unit currentUnit;
    private int chaseTurn = 0;

    private List<IUtilityStrategy> strategies;

    private void Awake() {
        currentUnit = GetComponent<Unit>();

        // 전략 풀 초기화
        strategies = new List<IUtilityStrategy> {
            new AttackStrategy(),
            new FleeStrategy()
        };
    }

    // 행동 계획 반환
    public AIDecision PlanAITurn() {
        UpdateVisionAndChaseState();
        return GetBestDecision();
    }

    private void UpdateVisionAndChaseState() {
        int viewRange = currentUnit.GetMoveRange() + 1;
        bool isEnemyInSight = false;

        foreach (var it in UnitManager.Instance.RegisteredUnit) {
            Unit checkUnit = it.Value;

            if (currentUnit.unitFaction == checkUnit.unitFaction || checkUnit.GetHealth() <= 0) continue;

            int dist = GridSystem.Instance.GetManhattanDistance(currentUnit.currentPosition, checkUnit.currentPosition);

            if (dist <= viewRange) {
                isEnemyInSight = true;
                break;
            }
        }

        if (isEnemyInSight) chaseTurn = 3;
        else if (chaseTurn > 0) chaseTurn--;
    }

    private AIDecision GetBestDecision() {
        AIDecision bestDecision = new AIDecision { utilityScore = -1f };

        foreach (var strategy in strategies) {
            AIDecision candidate = strategy.Evaluate(currentUnit, chaseTurn);

            if (candidate != null && candidate.utilityScore > bestDecision.utilityScore) {
                bestDecision = candidate;
            }
        }
        return bestDecision;
    }
}