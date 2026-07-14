using System.Collections.Generic;
using UnityEngine;

public class EnemyBrain : MonoBehaviour {
    private Unit currentUnit;
    private int chaseTurn = 0;
    private List<IUtilityStrategy> strategies;

    private void Awake() {
        currentUnit = GetComponent<Unit>();
        strategies = new List<IUtilityStrategy> {
            new AttackStrategy(),
            new FleeStrategy()
        };
    }

    /// <summary>
    /// 적 AI의 턴 전략을 수립합니다. (가상 위치 직접 오염 버그 수정)
    /// </summary>
    public AIDecision PlanAITurn() {
        UpdateVisionAndChaseState();

        // 의사결정 시점에는 오직 순수 데이터로만 계산합니다.
        AIDecision finalDecision = GetBestDecision();

        return finalDecision;
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

        if (bestDecision.utilityScore == -1f) {
            bestDecision.utilityScore = 0f;
            bestDecision.intendedCommands.Add(new WaitCommand());
        }

        return bestDecision;
    }
}