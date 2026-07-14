using System.Collections.Generic;
using UnityEngine;

// 적의 턴 행동 기획(의도) 수립 및 맵 타일 시각화 예고 시스템
public class EnemyController : MonoBehaviour {

    [Header("하이라이트 설정")]
    [SerializeField] private HighlightType enemyMoveHighlightType = HighlightType.Move;
    [SerializeField] private HighlightType enemyAttackHighlightType = HighlightType.Attack;

    [Header("의존성 컴포넌트")]
    [SerializeField] private HighlightTilePool tilePool;

    private Dictionary<Unit, AIDecision> cachedEnemyDecisions = new Dictionary<Unit, AIDecision>();

    public Dictionary<Unit, AIDecision> CachedEnemyDecisions => cachedEnemyDecisions;

    // 특정 미래 틱 시점에 해당 적 유닛이 밟고 서 있을 타일을 경로 데이터 상에서 역산
    public TileData GetEnemyTileAtTick(Unit enemyUnit, int targetTick) {
        if (!cachedEnemyDecisions.TryGetValue(enemyUnit, out AIDecision decision)) {
            return GridSystem.Instance.GetTileData(enemyUnit.currentPosition);
        }

        MoveCommand moveCmd = null;
        foreach (ICommand cmd in decision.intendedCommands) {
            if (cmd is MoveCommand m) {
                moveCmd = m;
                break;
            }
        }

        if (moveCmd == null || moveCmd.path == null || moveCmd.path.Count == 0) {
            return GridSystem.Instance.GetTileData(enemyUnit.currentPosition);
        }

        int pathIndex = targetTick - 1;
        if (pathIndex < 0) {
            return GridSystem.Instance.GetTileData(enemyUnit.currentPosition);
        }
        else if (pathIndex >= moveCmd.path.Count) {
            return moveCmd.destination;
        }
        else {
            return moveCmd.path[pathIndex];
        }
    }

    // 모든 적의 행동 궤적 연산 후 위협 지역 및 화살표 가이드 장판을 맵에 시각화
    public void DisplayAllEnemyIntents() {
        GridSystem.Instance.ClearEnemyIntents(enemyMoveHighlightType, enemyAttackHighlightType);
        cachedEnemyDecisions.Clear();

        if (tilePool != null) {
            tilePool.ReturnArrowTiles(ArrowResourceType.Line);
            tilePool.ReturnArrowTiles(ArrowResourceType.Corner);
            tilePool.ReturnArrowTiles(ArrowResourceType.Head);
        }

        foreach (var kvp in UnitManager.Instance.RegisteredUnit) {
            Unit currentUnit = kvp.Value;
            if (currentUnit == null || currentUnit.unitFaction != Faction.Enemy || currentUnit.GetHealth() <= 0) continue;

            EnemyBrain brain = currentUnit.GetComponent<EnemyBrain>();
            if (brain == null) continue;

            AIDecision decision = brain.PlanAITurn();
            if (decision == null || decision.intendedCommands == null) continue;

            cachedEnemyDecisions[currentUnit] = decision;

            foreach (ICommand cmd in decision.intendedCommands) {
                if (cmd is MoveCommand moveCmd) {
                    GridSystem.Instance.SpawnEnemyMovePathIntent(moveCmd.path, enemyMoveHighlightType);

                    List<TileData> path = moveCmd.path;
                    if (path != null && path.Count > 0 && tilePool != null) {
                        TileData startTile = GridSystem.Instance.GetTileData(currentUnit.currentPosition);

                        for (int i = 0; i < path.Count; i++) {
                            TileData currentTile = path[i];

                            TileData prevTile = (i == 0) ? startTile : path[i - 1];
                            TileData nextTile = (i == path.Count - 1) ? null : path[i + 1];

                            Vector3 inDir = (currentTile.worldPosition - prevTile.worldPosition).normalized;
                            Quaternion arrowRotation = Quaternion.identity;
                            ArrowResourceType resourceType = ArrowResourceType.Line;

                            if (nextTile != null) {
                                Vector3 outDir = (nextTile.worldPosition - currentTile.worldPosition).normalized;

                                if (Vector3.Dot(inDir, outDir) > 0.9f) {
                                    resourceType = ArrowResourceType.Line;
                                    float angle = Mathf.Atan2(inDir.x, inDir.z) * Mathf.Rad2Deg;
                                    arrowRotation = Quaternion.Euler(0f, angle + 180f, 0f);
                                }
                                else {
                                    resourceType = ArrowResourceType.Corner;
                                    Vector3 cornerDir = (inDir + outDir).normalized;
                                    float angle = Mathf.Atan2(cornerDir.x, cornerDir.z) * Mathf.Rad2Deg;
                                    arrowRotation = Quaternion.Euler(0f, angle + 135f, 0f);
                                }
                            }
                            else {
                                resourceType = ArrowResourceType.Head;
                                float angle = Mathf.Atan2(inDir.x, inDir.z) * Mathf.Rad2Deg;
                                arrowRotation = Quaternion.Euler(0f, angle + 180f, 0f);
                            }

                            tilePool.SpawnArrowSprite(currentTile.worldPosition, arrowRotation, resourceType);
                        }
                    }
                }
                if (cmd is AttackCommand) {
                    Vector2Int expectedPos = currentUnit.currentPosition;
                    foreach (var c in decision.intendedCommands) {
                        if (c is MoveCommand m) {
                            expectedPos = new Vector2Int(m.destination.gridX, m.destination.gridY);
                        }
                    }
                    GridSystem.Instance.SpawnEnemyAttackIntent(expectedPos, enemyAttackHighlightType);
                }
            }
        }
    }

    // 라운드 준비가 마감되면 적의 연산된 예고 행동 목록을 타임라인 틱 엔진에 커밋 등록
    public void CommitEnemyActionsToTimeline() {
        foreach (var kvp in cachedEnemyDecisions) {
            Unit enemyUnit = kvp.Key;
            AIDecision decision = kvp.Value;

            if (enemyUnit != null && enemyUnit.GetHealth() > 0) {
                List<ICommand> finalizedCommands = new List<ICommand>();

                foreach (ICommand cmd in decision.intendedCommands) {
                    if (cmd is MoveCommand moveCmd) {
                        finalizedCommands.Add(moveCmd);
                        enemyUnit.virtualPosition = new Vector2Int(moveCmd.destination.gridX, moveCmd.destination.gridY);
                    }
                    else if (cmd is AttackCommand attackCmd) {
                        finalizedCommands.Add(attackCmd);
                    }
                    else if (cmd is WaitCommand waitCmd) {
                        finalizedCommands.Add(waitCmd);
                    }
                }

                AIDecision timelineReadyDecision = new AIDecision {
                    utilityScore = decision.utilityScore,
                    intendedCommands = finalizedCommands
                };

                TimeLineManager.Instance.ScheduleAction(enemyUnit, timelineReadyDecision, 0);
            }
        }
        cachedEnemyDecisions.Clear();
    }

    // 다음 라운드 진입 전 바닥 장판 가이드 및 화살표 시스템 일괄 수거
    public void ClearAllEnemyIntents() {
        GridSystem.Instance.ClearEnemyIntents(enemyMoveHighlightType, enemyAttackHighlightType);

        if (tilePool != null) {
            tilePool.ReturnArrowTiles(ArrowResourceType.Line);
            tilePool.ReturnArrowTiles(ArrowResourceType.Corner);
            tilePool.ReturnArrowTiles(ArrowResourceType.Head);
        }
    }
}