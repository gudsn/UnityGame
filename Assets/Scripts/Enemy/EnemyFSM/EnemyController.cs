using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour {

    [Header("하이라이트 설정")]
    [SerializeField] private HighlightType enemyMoveHighlightType = HighlightType.Move;
    [SerializeField] private HighlightType enemyAttackHighlightType = HighlightType.Attack;

    [Header("의존성 컴포넌트")]
    [SerializeField] private HighlightTilePool tilePool;

    private Dictionary<Unit, AIDecision> cachedEnemyDecisions = new Dictionary<Unit, AIDecision>();

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

                                // 직진 구간 연산 (+180도 회전 보정)
                                if (Vector3.Dot(inDir, outDir) > 0.9f) {
                                    resourceType = ArrowResourceType.Line;
                                    float angle = Mathf.Atan2(inDir.x, inDir.z) * Mathf.Rad2Deg;
                                    arrowRotation = Quaternion.Euler(0f, angle + 180f, 0f);
                                }
                                // 모퉁이 꺾임 구간 연산 (+180도 회전 보정)
                                else {
                                    resourceType = ArrowResourceType.Corner;
                                    Vector3 cornerDir = (inDir + outDir).normalized;
                                    float angle = Mathf.Atan2(cornerDir.x, cornerDir.z) * Mathf.Rad2Deg;
                                    arrowRotation = Quaternion.Euler(0f, angle + 135f, 0f);
                                }
                            }
                            else {
                                // 경로 종착지 촉 연산 (+180도 회전 보정)
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

    public void ClearAllEnemyIntents() {
        GridSystem.Instance.ClearEnemyIntents(enemyMoveHighlightType, enemyAttackHighlightType);

        if (tilePool != null) {
            tilePool.ReturnArrowTiles(ArrowResourceType.Line);
            tilePool.ReturnArrowTiles(ArrowResourceType.Corner);
            tilePool.ReturnArrowTiles(ArrowResourceType.Head);
        }
    }
}