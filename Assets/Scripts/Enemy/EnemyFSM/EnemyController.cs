using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour {

    [Header("하이라이트 설정")]
    [SerializeField] private HighlightType enemyMoveHighlightType = HighlightType.Move;
    [SerializeField] private HighlightType enemyAttackHighlightType = HighlightType.Attack;

    [Header("오브젝트 풀")]
    [SerializeField] private HighlightTilePool tilePool;

    private Dictionary<Unit, AIDecision> cachedEnemyDecisions = new Dictionary<Unit, AIDecision>();

    public Dictionary<Unit, AIDecision> CachedEnemyDecisions => cachedEnemyDecisions;

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
        }

        RedrawCurrentEnemyIntents();
    }

    public void RedrawCurrentEnemyIntents() {
        GridSystem.Instance.ClearEnemyIntents(enemyMoveHighlightType, enemyAttackHighlightType);

        if (tilePool != null) {
            tilePool.ReturnArrowTiles(ArrowResourceType.Line);
            tilePool.ReturnArrowTiles(ArrowResourceType.Corner);
            tilePool.ReturnArrowTiles(ArrowResourceType.Head);
        }

        // [추가] 적 의도를 다시 그리기 전, 타임라인 UI의 적군 슬롯들에 기존에 들어있던 아이콘들을 먼저 클리어
        if (TimeLineUI.Instance != null) {
            TimeLineUI.Instance.ClearEnemyTrackSlots();
        }

        foreach (var kvp in cachedEnemyDecisions) {
            Unit currentUnit = kvp.Key;
            AIDecision decision = kvp.Value;

            if (currentUnit == null || currentUnit.GetHealth() <= 0 || decision == null) continue;

            string unitName = currentUnit.GetName();
            int tickCounter = 1;

            foreach (ICommand cmd in decision.intendedCommands) {
                if (cmd is MoveCommand moveCmd) {
                    List<TileData> path = moveCmd.path;
                    if (path != null && path.Count > 0) {
                        for (int i = 0; i < path.Count; i++) {
                            if (TimeLineUI.Instance != null) {
                                TimeLineUI.Instance.PlaceEnemyActionIntoSlot(unitName, tickCounter, "MoveCommand");
                            }

                            TileData currentTile = path[i];
                            TileData prevTile = (i == 0) ? GridSystem.Instance.GetTileData(currentUnit.currentPosition) : path[i - 1];
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
                                    float baseAngle = Mathf.Atan2(inDir.x, inDir.z) * Mathf.Rad2Deg;
                                    float crossY = Vector3.Cross(inDir, outDir).y;
                                    if (crossY > 0f) {
                                        arrowRotation = Quaternion.Euler(0f, baseAngle + 180f, 0f);
                                    }
                                    else {
                                        arrowRotation = Quaternion.Euler(0f, baseAngle + 270f, 0f);
                                    }
                                }
                            }
                            else {
                                resourceType = ArrowResourceType.Head;
                                float angle = Mathf.Atan2(inDir.x, inDir.z) * Mathf.Rad2Deg;
                                arrowRotation = Quaternion.Euler(0f, angle + 180f, 0f);
                            }

                            tilePool.SpawnArrowSprite(currentTile.worldPosition, arrowRotation, resourceType);
                            tickCounter++;
                        }
                    }
                }
                else if (cmd is AttackCommand) {
                    tickCounter++;
                    if (TimeLineUI.Instance != null) {
                        TimeLineUI.Instance.PlaceEnemyActionIntoSlot(unitName, tickCounter, "AttackCommand");
                    }
                    tickCounter++;

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
                foreach (ICommand cmd in decision.intendedCommands) {
                    if (cmd is MoveCommand moveCmd) {
                        enemyUnit.virtualPosition = new Vector2Int(moveCmd.destination.gridX, moveCmd.destination.gridY);
                    }
                    TimeLineManager.Instance.ScheduleAction(enemyUnit, cmd);
                }
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