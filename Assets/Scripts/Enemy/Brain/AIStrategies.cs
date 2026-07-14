using System.Collections.Generic;
using UnityEngine;

public interface IUtilityStrategy {
    AIDecision Evaluate(Unit currentUnit, int chaseTurn);
}

// [수정 완료] 탐색/공격 전략
public class AttackStrategy : IUtilityStrategy {
    public AIDecision Evaluate(Unit currentUnit, int chaseTurn) {
        if (chaseTurn <= 0) {
            return new AIDecision { utilityScore = 0f, intendedCommands = new List<ICommand> { new WaitCommand() } };
        }

        AIDecision bestDecision = new AIDecision { utilityScore = -1f };
        Unit finalTarget = null;
        TileData moveDestination = null;

        Vector2Int basePosition = currentUnit.virtualPosition;

        foreach (var it in UnitManager.Instance.RegisteredUnit) {
            Unit targetUnit = it.Value;
            if (currentUnit.unitFaction == targetUnit.unitFaction || targetUnit.GetHealth() <= 0) continue;

            float currentTargetScore = 0;

            // 개선된 접근 데이터 연산 호출
            (TileData dest, Unit target) = GetEngageData(currentUnit, basePosition, targetUnit);

            if (dest == null) continue;

            // 목적지에 도달했을 때 타겟과의 실제 맨해튼 거리 계산
            int dist = GridSystem.Instance.GetManhattanDistance(new Vector2Int(dest.gridX, dest.gridY), targetUnit.currentPosition);

            currentTargetScore += (dist <= 2) ? 60f : Mathf.Max(0, 60f - (dist * 5f));

            if (target != null) {
                float attackPower = currentUnit.stats.GetAttackPower();
                float targetHp = target.GetHealth();
                currentTargetScore += (attackPower >= targetHp) ? 200f : Mathf.Min((attackPower / targetHp) * 50f, 50f);
            }

            if (currentTargetScore > bestDecision.utilityScore) {
                bestDecision.utilityScore = currentTargetScore;
                moveDestination = dest;
                finalTarget = target;
            }
        }

        bestDecision.intendedCommands.Clear();

        if (moveDestination != null) {
            TileData currentTile = GridSystem.Instance.GetTileData(currentUnit.currentPosition);
            if (moveDestination != currentTile) {
                bestDecision.intendedCommands.Add(new MoveCommand(currentUnit, moveDestination));
            }
        }

        Vector2Int finalVirtualPos = (moveDestination != null)
            ? new Vector2Int(moveDestination.gridX, moveDestination.gridY)
            : currentUnit.currentPosition;

        if (finalTarget != null) {
            int finalDist = GridSystem.Instance.GetManhattanDistance(finalVirtualPos, finalTarget.currentPosition);
            if (finalDist <= 2) {
                bestDecision.intendedCommands.Add(new AttackCommand(currentUnit, finalTarget));
            }
        }

        if (bestDecision.intendedCommands.Count == 0) {
            bestDecision.intendedCommands.Add(new WaitCommand());
        }

        return bestDecision;
    }

    /// <summary>
    /// [버그 수정] 기동력 범위 잘라내기와 사거리 매칭 논리 순서 정상화
    /// </summary>
    private (TileData, Unit) GetEngageData(Unit currentUnit, Vector2Int basePosition, Unit targetUnit) {
        TileData startTile = GridSystem.Instance.GetTileData(basePosition);
        TileData endTile = GridSystem.Instance.GetTileData(targetUnit.currentPosition);
        int range = 2;

        if (GridSystem.Instance.GetManhattanDistance(basePosition, targetUnit.currentPosition) <= range) {
            return (startTile, targetUnit);
        }

        List<TileData> path = GridSystem.Instance.AStarAlgorithm(startTile, endTile);
        if (path == null || path.Count == 0) return (null, null);

        if (path.Contains(endTile)) path.Remove(endTile);

        List<TileData> attackRangeTiles = GridSystem.Instance.FindTileNeighbours(endTile, range);
        int moveRange = currentUnit.GetMoveRange();

        // [핵심 변경] 경로를 먼저 무조건 자르지 않고, 이동 한계치 내에서 사거리가 충족되는지 검사
        int maxReachableIndex = Mathf.Min(path.Count - 1, moveRange - 1);

        TileData destinationTile = null;
        Unit finalTarget = null;

        // 이동 가능 최대 반경 내에서 사거리가 닿는 최선의 타일을 역순 탐색
        for (int i = maxReachableIndex; i >= 0; i--) {
            if (attackRangeTiles.Contains(path[i])) {
                destinationTile = path[i];
                finalTarget = targetUnit;
                break;
            }
        }

        // 사거리가 닿는 타일이 없다면, 공격은 못 하더라도 내 기동력이 허락하는 최전방 타일까지 전진 추격
        if (destinationTile == null && maxReachableIndex >= 0) {
            destinationTile = path[maxReachableIndex];
            finalTarget = null; // 이동만 하고 공격은 불가
        }

        return (destinationTile, finalTarget);
    }
}

// [수정 완료] 도주 전략
public class FleeStrategy : IUtilityStrategy {
    public AIDecision Evaluate(Unit currentUnit, int chaseTurn) {
        AIDecision bestDecision = new AIDecision { utilityScore = 0f };
        float currentHealthRatio = currentUnit.GetHealth() / currentUnit.GetMaxHealth();

        if (currentHealthRatio >= 0.3f) {
            bestDecision.intendedCommands.Add(new WaitCommand());
            return bestDecision;
        }

        TileData bestFleeTile = null;

        foreach (var it in UnitManager.Instance.RegisteredUnit) {
            Unit targetUnit = it.Value;
            if (currentUnit.unitFaction == targetUnit.unitFaction || targetUnit.GetHealth() <= 0) continue;

            TileData fleeTile = GetFleeTile(currentUnit, targetUnit);
            if (fleeTile == null) continue;

            float currentScore = 50f + ((0.3f - currentHealthRatio) * 200f);

            if (currentScore > bestDecision.utilityScore) {
                bestDecision.utilityScore = currentScore;
                bestFleeTile = fleeTile;
            }
        }

        if (bestFleeTile != null) {
            bestDecision.intendedCommands.Add(new MoveCommand(currentUnit, bestFleeTile));
        }
        else {
            bestDecision.intendedCommands.Add(new WaitCommand());
        }

        return bestDecision;
    }

    /// <summary>
    /// [버그 수정] 막다른 골목에 갇히지 않도록 실제 A* 이동 효율을 가미한 도주 검증
    /// </summary>
    private TileData GetFleeTile(Unit currentUnit, Unit targetUnit) {
        TileData startTile = GridSystem.Instance.GetTileData(currentUnit.virtualPosition);
        int moveRange = currentUnit.GetMoveRange();
        HashSet<TileData> walkableTiles = GridSystem.Instance.GetManhattanGrid(startTile, moveRange);

        int maxDistance = -1;
        TileData destinationTile = null;

        foreach (TileData tile in walkableTiles) {
            if (!tile.isWalkable || tile.isOccupied) continue;

            Vector2Int currentTileCoord = new Vector2Int(tile.gridX, tile.gridY);

            // 1차 필터: 플레이어와의 맨해튼 거리 산출
            int currentDistance = GridSystem.Instance.GetManhattanDistance(currentTileCoord, targetUnit.currentPosition);

            // 2차 검증: 단순히 수치적 거리가 멀다고 선택하는 것이 아니라, 실제 그 타일로 가는 유효한 경로가 뚫려있는지 검증
            List<TileData> checkPath = GridSystem.Instance.AStarAlgorithm(startTile, tile);
            if (checkPath == null || checkPath.Count == 0) continue;

            if (currentDistance > maxDistance) {
                maxDistance = currentDistance;
                destinationTile = tile;
            }
        }
        return destinationTile;
    }
}
