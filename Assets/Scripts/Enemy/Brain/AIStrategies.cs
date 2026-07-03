using System.Collections.Generic;
using UnityEngine;

// 공통 전략 인터페이스
public interface IUtilityStrategy {
    AIDecision Evaluate(Unit currentUnit, int chaseTurn);
}

// 공격 전략
public class AttackStrategy : IUtilityStrategy {
    public AIDecision Evaluate(Unit currentUnit, int chaseTurn) {
        if (chaseTurn <= 0) return new AIDecision { utilityScore = 0f, decisionName = "Attack (Skip)" };

        AIDecision bestDecision = new AIDecision { utilityScore = -1f, decisionName = "Attack" };
        Unit finalTarget = null;
        TileData moveDestination = null;

        foreach (var it in UnitManager.Instance.RegisteredUnit) {
            Unit targetUnit = it.Value;
            if (currentUnit.unitFaction == targetUnit.unitFaction || targetUnit.GetHealth() <= 0) continue;

            float currentTargetScore = 0;
            (TileData dest, Unit target) = GetEngageData(currentUnit, targetUnit);

            if (dest == null && target == null) continue;

            // 1. 거리 점수
            int dist = GridSystem.Instance.GetManhattanDistance(currentUnit.currentPosition, targetUnit.currentPosition);
            currentTargetScore += (dist <= 1) ? 60f : Mathf.Max(0, 60f - (dist * 5f));

            // 2. 어그로 점수
            if (target != null) {
                float myAttackPower = currentUnit.stats.GetAttackPower();
                float targetHp = target.GetHealth();

                // 확정 처치 가능 시 최고 점수
                if (myAttackPower >= targetHp) {
                    currentTargetScore += 200f;
                }
                else {
                    // 딜 효율 비례 점수 (최대 50)
                    float aggressivePoint = (myAttackPower / targetHp) * 50f;
                    currentTargetScore += Mathf.Min(aggressivePoint, 50f);
                }
            }

            // 최고 점수 갱신
            if (currentTargetScore > bestDecision.utilityScore) {
                bestDecision.utilityScore = currentTargetScore;
                moveDestination = dest;
                finalTarget = target;
            }
        }

        // 결정된 커맨드 주입
        if (bestDecision.utilityScore > 0) {
            TileData currentTile = GridSystem.Instance.GetTileData(currentUnit.currentPosition);

            if (moveDestination != null && moveDestination != currentTile) {
                bestDecision.actionQueue.Enqueue(new MoveCommand(currentUnit, moveDestination));
            }
            if (finalTarget != null) {
                bestDecision.actionQueue.Enqueue(new AttackCommand(currentUnit, 1));
            }
        }

        return bestDecision;
    }

    // 타겟 교전 데이터(도착 타일, 타겟) 산출
    private (TileData, Unit) GetEngageData(Unit currentUnit, Unit targetUnit) {
        TileData startTile = GridSystem.Instance.GetTileData(currentUnit.currentPosition);
        TileData endTile = GridSystem.Instance.GetTileData(targetUnit.currentPosition);
        int range = 1;

        // 밀착 시 길찾기 생략
        if (GridSystem.Instance.GetManhattanDistance(currentUnit.currentPosition, targetUnit.currentPosition) <= range) {
            return (startTile, targetUnit);
        }

        List<TileData> path = GridSystem.Instance.AStarAlgorithm(startTile, endTile);
        if (path == null || path.Count == 0) return (null, null);

        if (path.Contains(endTile)) path.Remove(endTile);

        List<TileData> attackRangeTiles = GridSystem.Instance.FindTileNeighbours(endTile, range);
        int moveRange = currentUnit.GetMoveRange();

        // 이동 사거리 초과분 절삭
        if (path.Count > moveRange) {
            path.RemoveRange(moveRange, path.Count - moveRange);
        }

        TileData destinationTile = (path.Count > 0) ? path[path.Count - 1] : startTile;
        Unit finalTarget = null;

        // 공격 가능 타일 도출 (역순 탐색)
        for (int i = path.Count - 1; i >= 0; i--) {
            if (attackRangeTiles.Contains(path[i])) {
                destinationTile = path[i];
                finalTarget = targetUnit;
                break;
            }
        }

        if (finalTarget == null && attackRangeTiles.Contains(startTile)) {
            destinationTile = startTile;
            finalTarget = targetUnit;
        }

        return (destinationTile, finalTarget);
    }
}

// 도주 전략
public class FleeStrategy : IUtilityStrategy {
    public AIDecision Evaluate(Unit currentUnit, int chaseTurn) {
        AIDecision bestDecision = new AIDecision { utilityScore = 0f, decisionName = "Flee" };

        float myHealthRatio = currentUnit.GetHealth() / currentUnit.GetMaxHealth();

        // 체력 40% 이상 시 도주 스킵
        if (myHealthRatio >= 0.4f) {
            return bestDecision;
        }

        TileData bestFleeTile = null;

        foreach (var it in UnitManager.Instance.RegisteredUnit) {
            Unit targetUnit = it.Value;
            if (currentUnit.unitFaction == targetUnit.unitFaction || targetUnit.GetHealth() <= 0) continue;

            float targetHealthRatio = targetUnit.GetHealth() / targetUnit.GetMaxHealth();

            // 적 체력 비율이 더 낮으면 교전 지속
            if (targetHealthRatio < myHealthRatio) {
                continue;
            }

            TileData fleeTile = GetFleeTile(currentUnit, targetUnit);
            if (fleeTile == null) continue;

            // 남은 체력 비례 점수 산출
            float dangerDegree = 0.4f - myHealthRatio;
            float currentScore = 40f + (dangerDegree * 150f);

            // 최고 점수 갱신
            if (currentScore > bestDecision.utilityScore) {
                bestDecision.utilityScore = currentScore;
                bestFleeTile = fleeTile;
            }
        }

        // 결정된 커맨드 주입
        if (bestDecision.utilityScore > 0 && bestFleeTile != null) {
            bestDecision.actionQueue.Enqueue(new MoveCommand(currentUnit, bestFleeTile));
        }

        return bestDecision;
    }

    // 최적 도주 타일 도출
    private TileData GetFleeTile(Unit currentUnit, Unit targetUnit) {
        TileData startTile = GridSystem.Instance.GetTileData(currentUnit.currentPosition);
        int moveRange = currentUnit.GetMoveRange();
        HashSet<TileData> walkableTiles = GridSystem.Instance.GetManhattanGrid(startTile, moveRange);

        int maxDistance = -1;
        TileData destinationTile = null;

        // 적과 가장 먼 타일 탐색
        foreach (TileData tile in walkableTiles) {
            Vector2Int currentTileCoord = new Vector2Int(tile.gridX, tile.gridY);
            int currentDistance = GridSystem.Instance.GetManhattanDistance(currentTileCoord, targetUnit.currentPosition);

            if (currentDistance > maxDistance) {
                maxDistance = currentDistance;
                destinationTile = tile;
            }
        }
        return destinationTile;
    }
}