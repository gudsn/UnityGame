using System.Collections.Generic;
using UnityEngine;

public class EnemyBrain : MonoBehaviour {
    private Unit currentUnit;

    private void Awake() {
        currentUnit = GetComponent<Unit>();
    }

    public AIDecision CalculateBestDecision() {
        // 기본값: 제자리 대기
        AIDecision currentDecision = new AIDecision {
            actionType = AIActionType.Wait,
            utilityScore = 0f, // 대기 점수를 최소 0점으로 설정
            destinationTile = GridSystem.Instance.GetTileData(currentUnit.currentPosition)
        };

        // 공격 옵션 평가
        AIDecision attackDecision = EvaluateAttackOptions();

        if (attackDecision != null && attackDecision.utilityScore > currentDecision.utilityScore) {
            currentDecision = attackDecision;
        }

        return currentDecision;
    }

    private AIDecision EvaluateAttackOptions() {
        AIDecision bestDecision = null;
        float highestScore = -1f;

        foreach (KeyValuePair<Vector2Int, Unit> it in UnitManager.Instance.registeredUnit) {
            Unit targetUnit = it.Value;

            if (currentUnit.unitFaction == targetUnit.unitFaction || targetUnit.GetHealth() <= 0) {
                continue;
            }

            // 루프마다 점수 초기화
            float currentTargetScore = 0;

            // 1. 이동 및 교전 데이터 가져오기
            AIDecision engageData = GetEngageData(targetUnit);

            // 타겟을 공격할 수 없는 위치거나 경로가 아예 없는 경우 스킵
            if (engageData == null || engageData.destinationTile == null) continue;

            // 2. 점수 계산 (거리 및 체력 상황 등)
            int dist = GridSystem.Instance.GetManhattanDistance(currentUnit.currentPosition,
                       new Vector2Int(engageData.destinationTile.gridX, engageData.destinationTile.gridY));
            currentTargetScore += (dist == 0) ? 50f : (1f / dist) * 100f;

            // 점수와 행동 타입을 결과에 명시적으로 주입
            engageData.utilityScore = currentTargetScore;
            // targetUnit이 있으면 Attack, 없으면 단순히 적 근처로 이동하는 것이므로 Move/Wait 처리
            engageData.actionType = (engageData.targetUnit != null) ? AIActionType.Attack : AIActionType.Wait;

            if (currentTargetScore > highestScore) {
                highestScore = currentTargetScore;
                bestDecision = engageData;
            }
        }

        return bestDecision;
    }

    private AIDecision GetEngageData(Unit targetUnit) {
        TileData startTile = GridSystem.Instance.GetTileData(currentUnit.currentPosition);
        TileData endTile = GridSystem.Instance.GetTileData(targetUnit.currentPosition);

        List<TileData> A_PathTile = GridSystem.Instance.A_Algorithm(startTile, endTile);

        // 경로가 아예 없으면 제자리 대기 반환
        if (A_PathTile == null || A_PathTile.Count == 0) {
            return new AIDecision {
                destinationTile = startTile,
                targetUnit = null,
                actionType = AIActionType.Wait
            };
        }

        // 적이 서 있는 타일은 경로에서 제거 (겹침 방지)
        if (A_PathTile.Contains(endTile)) A_PathTile.Remove(endTile);

        // 사거리 가져오기 (UnitStats 활용)
        // int range = currentUnit.stats.GetAttackRange();
        int range = 1;
        List<TileData> attackRangeTiles = GridSystem.Instance.FindTileNeighbours(endTile, range);

        int moveRange = currentUnit.GetMoveRange();
        if (A_PathTile.Count > moveRange) {
            A_PathTile.RemoveRange(moveRange, A_PathTile.Count - moveRange);
        }

        // 기본 목적지 설정 (이동 가능한 최대 지점)
        TileData destinationTile = (A_PathTile.Count > 0) ? A_PathTile[A_PathTile.Count - 1] : startTile;
        Unit finalTarget = null;

        // 역순 탐색을 통해 '공격 가능 타일'에 도착하는지 확인하고 targetUnit 할당
        for (int i = A_PathTile.Count - 1; i >= 0; i--) {
            if (attackRangeTiles.Contains(A_PathTile[i])) {
                destinationTile = A_PathTile[i];
                finalTarget = targetUnit; // 여기서 실제 타겟 할당
                break;
            }
        }

        // 제자리에서도 공격 가능한지 최종 확인
        if (finalTarget == null && attackRangeTiles.Contains(startTile)) {
            destinationTile = startTile;
            finalTarget = targetUnit;
        }

        return new AIDecision {
            targetUnit = finalTarget,
            destinationTile = destinationTile
            // actionType은 여기서 정해도 되지만, 평가 단계(Evaluate)에서 정하는 것이 더 유연합니다.
        };
    }
}