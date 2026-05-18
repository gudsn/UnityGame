using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyBrain : MonoBehaviour {
    private Unit currentUnit;
    private int chaseTurn = 0;

    private void Awake() {
        currentUnit = GetComponent<Unit>();
    }

    public AIDecision CalculateBestDecision() {
        // 기본값: 제자리 대기
        AIDecision currentDecision = new AIDecision {
            actionType = AIActionType.Wait,
            utilityScore = 20f,
        };

        int attackRange = 1;
        TileData currentTile = GridSystem.Instance.GetTileData(currentUnit.currentPosition);
        int currentUnitChaseRange = currentUnit.GetMoveRange() + attackRange;

        HashSet<TileData> currentUnitChase = GridSystem.Instance.GetManhattanGrid(currentTile, currentUnitChaseRange);

        bool isEnemyInSight = false;

        if (currentUnitChase != null) {
            foreach (KeyValuePair<Vector2Int, Unit> it in UnitManager.Instance.registeredUnit) {
                Unit checkUnit = it.Value;

                if (currentUnit.unitFaction == checkUnit.unitFaction || checkUnit.GetHealth() <= 0) continue;

                TileData checkTile = GridSystem.Instance.GetTileData(checkUnit.currentPosition);

                if (currentUnitChase.Contains(checkTile)) {
                    isEnemyInSight = true;
                    break;
                }
            }
        }

        if (isEnemyInSight) {
            chaseTurn = 3; // 시야에 적이 있으면 추격 의지 MAX
        }
        else if (chaseTurn > 0) {
            chaseTurn -= 1; // 시야에서 사라지면 매 턴마다 흥분이 가라앉음
        }

        // 공격 옵션 평가 (어그로가 남아있을 때만)
        if (chaseTurn > 0) {
            AIDecision attackDecision = EvaluateAttackOptions();

            if (attackDecision != null && attackDecision.utilityScore > currentDecision.utilityScore) {
                currentDecision = attackDecision;
            }
        }

        // 도망 옵션 평가 (언제나 생존은 최우선)
        AIDecision fleeDecision = EvaluateFleeDecision();
        if (fleeDecision != null && fleeDecision.utilityScore > currentDecision.utilityScore) {
            currentDecision = fleeDecision;
        }

        Debug.Log($"[{currentUnit.GetName()}의 턴] 최종 결정: {currentDecision.actionType}, " +
              $"이동할 목적지: ({currentDecision.destinationTile?.gridX}, {currentDecision.destinationTile?.gridY}), " +
              $"부여된 점수: {currentDecision.utilityScore}점 (남은 추격턴: {chaseTurn})");

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
            currentTargetScore += (dist == 0) ? 50f : (1f / dist) * 50f;

            float currentUnitAttackPower = currentUnit.stats.GetAttackPower();
            if (engageData.targetUnit != null) {
                float targetHp = engageData.targetUnit.GetHealth();
                float agressivePoint = (currentUnitAttackPower / targetHp) * 50;

                if (agressivePoint > 50) {
                    currentTargetScore += 50;
                }
                else {
                    currentTargetScore += agressivePoint;
                }
            }

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

    private AIDecision EvaluateFleeDecision() {
        float highestScore = 0f;

        // 범위 내에 적이 없거나 도망칠 필요가 없으면 기본적으로 Wait 상태를 유지합니다.
        AIActionType aiAction = AIActionType.Wait;
        TileData destinationTile = null;

        foreach (KeyValuePair<Vector2Int, Unit> it in UnitManager.Instance.registeredUnit) {
            Unit targetUnit = it.Value;
            float currentScore = 0;

            if (currentUnit.unitFaction == targetUnit.unitFaction || targetUnit.GetHealth() <= 0) {
                continue;
            }

            // 🛑 [거리 체크 필터링] 적이 시야 밖이고, 내가 추격 중(chaseTurn)도 아니라면 무시!
            int distanceToTarget = GridSystem.Instance.GetManhattanDistance(currentUnit.currentPosition, targetUnit.currentPosition);
            int safeDistance = currentUnit.GetMoveRange() + 1; // 내 시야 범위

            if (distanceToTarget > safeDistance && chaseTurn <= 0) {
                continue;
            }

            TileData currentTile = GetFleeTile(targetUnit);

            if (currentTile == null) {
                continue;
            }

            // --- 체력 점수 계산 로직 ---
            float currentUnitHealth = currentUnit.GetHealth();
            float targetUnitHealth = targetUnit.GetHealth();

            float currentUnitHealthRatio = currentUnitHealth / currentUnit.GetMaxHealth();
            float targetUnitHealthRatio = targetUnitHealth / targetUnit.GetMaxHealth();

            if (currentUnitHealthRatio < 0.3) {
                currentScore += (float)(0.3 - currentUnitHealthRatio) * 200;
            }

            if (currentUnitHealthRatio < targetUnitHealthRatio || currentUnitHealth < targetUnitHealth) {
                float ratioGap = Mathf.Max(0, targetUnitHealthRatio - currentUnitHealthRatio);
                currentScore += ratioGap * 40;

                if (targetUnitHealth > currentUnitHealth) {
                    float hpGap = (targetUnitHealth / Mathf.Max(1f, currentUnitHealth));
                    if (hpGap < 2) {
                        currentScore += hpGap * 20;
                    }
                    else {
                        currentScore += 40;
                    }
                }
            }

            // 최고 점수 갱신 시 Flee 액션으로 설정
            if (currentScore > highestScore) {
                highestScore = currentScore;

                // ✨ 새로 만드신 Flee 액션을 여기에 연결합니다!
                aiAction = AIActionType.Flee;
                destinationTile = currentTile;
            }
        }

        return new AIDecision {
            actionType = aiAction,
            destinationTile = destinationTile,
            utilityScore = highestScore
        };
    }

    private TileData GetFleeTile(Unit targetUnit) {
        TileData startTile = GridSystem.Instance.GetTileData(currentUnit.currentPosition);
        int moveRange = currentUnit.GetMoveRange();

        // 1. 이동 가능한 모든 타일 가져오기
        HashSet<TileData> walkableTiles = GridSystem.Instance.GetManhattanGrid(startTile, moveRange);

        int maxDistance = -1; // 최대 거리를 추적 (0보다 작게 시작)
        TileData destinationTile = null;

        // 2. 도달 가능한 타일 중 적과 가장 먼 타일 찾기
        foreach (TileData tile in walkableTiles) {
            Vector2Int currentTileCordinate = new Vector2Int(tile.gridX, tile.gridY);

            // 거리를 한 번만 계산하여 변수에 저장 (최적화)
            int currentDistance = GridSystem.Instance.GetManhattanDistance(currentTileCordinate, targetUnit.currentPosition);

            // 현재 계산한 거리가 지금까지 찾은 최대 거리보다 멀다면 갱신
            if (currentDistance > maxDistance) {
                maxDistance = currentDistance;
                destinationTile = tile;
            }
        }

        return destinationTile;
    }
}