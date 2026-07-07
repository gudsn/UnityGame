using System.Collections.Generic;
using UnityEngine;

// 전략 평가 인터페이스
public interface IUtilityStrategy {
    AIDecision Evaluate(Unit currentUnit, int chaseTurn);
}

// 탐색/공격 전략
public class AttackStrategy : IUtilityStrategy {
    public AIDecision Evaluate(Unit currentUnit, int chaseTurn) {
        if (chaseTurn <= 0) {
            return new AIDecision { utilityScore = 0f, intendedCommands = { new WaitCommand() } };
        }

        AIDecision bestDecision = new AIDecision { utilityScore = -1f };
        Unit finalTarget = null;
        TileData moveDestination = null;

        // 최적 타겟 산출
        foreach (var it in UnitManager.Instance.RegisteredUnit) {
            Unit targetUnit = it.Value;
            if (currentUnit.unitFaction == targetUnit.unitFaction || targetUnit.GetHealth() <= 0) continue;

            float currentTargetScore = 0;
            (TileData dest, Unit target) = GetEngageData(currentUnit, targetUnit);

            if (dest == null && target == null) continue;

            int dist = GridSystem.Instance.GetManhattanDistance(currentUnit.currentPosition, targetUnit.currentPosition);
            currentTargetScore += (dist <= 1) ? 60f : Mathf.Max(0, 60f - (dist * 5f));

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

        // 결정된 행동 캡슐화
        bestDecision.intendedCommands.Clear();
        if (moveDestination != null) {
            bestDecision.intendedCommands.Add(new MoveCommand(currentUnit, moveDestination));
        }
        if (finalTarget != null) {
            bestDecision.intendedCommands.Add(new AttackCommand(currentUnit, finalTarget));
        }

        return bestDecision;
    }

    // 타겟 접근 경로 산출
    private (TileData, Unit) GetEngageData(Unit currentUnit, Unit targetUnit) {
        TileData startTile = GridSystem.Instance.GetTileData(currentUnit.currentPosition);
        TileData endTile = GridSystem.Instance.GetTileData(targetUnit.currentPosition);
        int range = 1;

        if (GridSystem.Instance.GetManhattanDistance(currentUnit.currentPosition, targetUnit.currentPosition) <= range) {
            return (startTile, targetUnit);
        }

        List<TileData> path = GridSystem.Instance.AStarAlgorithm(startTile, endTile);
        if (path == null || path.Count == 0) return (null, null);

        if (path.Contains(endTile)) path.Remove(endTile);

        List<TileData> attackRangeTiles = GridSystem.Instance.FindTileNeighbours(endTile, range);
        int moveRange = currentUnit.GetMoveRange();

        if (path.Count > moveRange) path.RemoveRange(moveRange, path.Count - moveRange);

        TileData destinationTile = (path.Count > 0) ? path[path.Count - 1] : startTile;
        Unit finalTarget = null;

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

        // 도주 행동 생성
        if (bestFleeTile != null) {
            bestDecision.intendedCommands.Add(new MoveCommand(currentUnit, bestFleeTile));
        }
        else {
            bestDecision.intendedCommands.Add(new WaitCommand());
        }

        return bestDecision;
    }

    private TileData GetFleeTile(Unit currentUnit, Unit targetUnit) {
        TileData startTile = GridSystem.Instance.GetTileData(currentUnit.currentPosition);
        int moveRange = currentUnit.GetMoveRange();
        HashSet<TileData> walkableTiles = GridSystem.Instance.GetManhattanGrid(startTile, moveRange);

        int maxDistance = -1;
        TileData destinationTile = null;

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