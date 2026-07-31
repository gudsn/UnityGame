using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CommandPriority {
    Move = 1,
    Buff = 2,
    Attack = 3
}

public struct TickCommand {
    public int executeTick;
    public CommandPriority priority;
    public Unit owner;
    public IEnumerator actionLogic;
}

public interface IActionScheduler {
    List<TickCommand> Decompose(ICommand macroCommand, int startTick);
}

public class MoveScheduler : IActionScheduler {
    public List<TickCommand> Decompose(ICommand macroCommand, int startTick) {
        List<TickCommand> tickCommands = new List<TickCommand>();
        Unit owner = null;
        List<TileData> path = null;

        if (macroCommand is MoveCommand moveCmd) {
            owner = moveCmd.owner;
            path = moveCmd.path;
        }
        else if (macroCommand is PlayerMoveCommand playerMoveCmd) {
            owner = playerMoveCmd.owner;
            path = playerMoveCmd.path;
        }

        if (owner == null || path == null) return tickCommands;

        for (int i = 0; i < path.Count; i++) {
            tickCommands.Add(new TickCommand {
                executeTick = startTick + i,
                priority = CommandPriority.Move,
                owner = owner,
                actionLogic = MoveSingleTileLogic(owner, path[i])
            });
        }
        return tickCommands;
    }

    private IEnumerator MoveSingleTileLogic(Unit owner, TileData targetTile) {
        if (targetTile == null || owner == null) yield break;

        Vector2Int targetPos = new Vector2Int(targetTile.gridX, targetTile.gridY);
        TileData startTile = GridSystem.Instance.GetTileData(owner.currentPosition);

        // [이중 길막 검사] 
        // 1. 타일 데이터 자체의 점유 상태(isOccupied) 확인
        // 2. RegisteredUnit 사전에 나 이외의 살아있는 유닛이 존재하는지 확인
        bool isOccupiedInGrid = targetTile.isOccupied && targetPos != owner.currentPosition;
        bool hasOtherUnit = UnitManager.Instance.RegisteredUnit.TryGetValue(targetPos, out Unit occupant)
                            && occupant != null && occupant != owner && occupant.GetHealth() > 0;

        if (isOccupiedInGrid || hasOtherUnit) {
            Debug.Log($"<color=orange>[이동 완전 차단]</color> {owner.gameObject.name}의 앞길이 막혀 이동이 중단되었습니다.");

            // 1. 가상 좌표 및 물리 위치를 출발 타일 정중앙으로 완벽 스냅
            owner.virtualPosition = owner.currentPosition;

            if (startTile != null) {
                Vector3 exactTilePos = startTile.worldPosition;
                exactTilePos.y = owner.transform.position.y;
                owner.transform.position = exactTilePos;

                // 2. 시선을 직교 사방(동/서/남/북)으로 스냅
                Vector3 currentForward = owner.transform.forward;
                Vector3 cardinalForward = (Mathf.Abs(currentForward.x) > Mathf.Abs(currentForward.z))
                    ? new Vector3(Mathf.Sign(currentForward.x), 0, 0)
                    : new Vector3(0, 0, Mathf.Sign(currentForward.z));

                if (cardinalForward != Vector3.zero) {
                    owner.transform.rotation = Quaternion.LookRotation(cardinalForward);
                }
            }

            // 물리 보간 연산으로 진입하지 않고 즉시 종료
            yield break;
        }

        // 경로가 완전히 비어있을 때만 실제 3D 이동 처리
        float moveSpeed = 5f;
        Vector3 targetPosition = targetTile.worldPosition;
        targetPosition.y = owner.transform.position.y;
        Vector3 direction = (targetPosition - owner.transform.position).normalized;
        Quaternion targetRotation = owner.transform.rotation;

        if (direction != Vector3.zero) {
            Vector3 cardinalDir = (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
                ? new Vector3(Mathf.Sign(direction.x), 0, 0)
                : new Vector3(0, 0, Mathf.Sign(direction.z));

            targetRotation = Quaternion.LookRotation(cardinalDir);
        }

        while (Vector3.Distance(owner.transform.position, targetPosition) > 0.01f) {
            // 프레임 이동 도중 동시 이동으로 발생할 수 있는 실시간 충돌 체크
            if (targetTile.isOccupied || (UnitManager.Instance.RegisteredUnit.TryGetValue(targetPos, out Unit midCheck) && midCheck != null && midCheck != owner && midCheck.GetHealth() > 0)) {
                if (startTile != null) {
                    Vector3 resetPos = startTile.worldPosition;
                    resetPos.y = owner.transform.position.y;
                    owner.transform.position = resetPos;
                }
                owner.virtualPosition = owner.currentPosition;
                yield break;
            }

            owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, targetRotation, Time.deltaTime * 15f);
            owner.transform.position = Vector3.MoveTowards(owner.transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        owner.transform.position = targetPosition;
        owner.transform.rotation = targetRotation;

        // 이동 완료 후 데이터 및 타일 점유 정보 갱신
        UnitManager.Instance.MoveUnit(targetPos, owner);
    }
}

public class AttackScheduler : IActionScheduler {
    public List<TickCommand> Decompose(ICommand macroCommand, int startTick) {
        List<TickCommand> tickCommands = new List<TickCommand>();
        AttackCommand attackCmd = macroCommand as AttackCommand;

        if (attackCmd == null) return tickCommands;

        tickCommands.Add(new TickCommand {
            executeTick = startTick,
            priority = CommandPriority.Move,
            owner = attackCmd.owner,
            actionLogic = null
        });

        tickCommands.Add(new TickCommand {
            executeTick = startTick + 1,
            priority = CommandPriority.Attack,
            owner = attackCmd.owner,
            actionLogic = AttackLogic(attackCmd.owner, attackCmd.targetCoordinate)
        });

        return tickCommands;
    }

    private bool IsWithinCrossRange(Vector2Int origin, Vector2Int target, int maxRange) {
        int dx = Mathf.Abs(origin.x - target.x);
        int dy = Mathf.Abs(origin.y - target.y);

        return (dx == 0 && dy <= maxRange) || (dy == 0 && dx <= maxRange);
    }

    private IEnumerator AttackLogic(Unit owner, Vector2Int intendedTargetCoord) {
        int attackRange = 2;
        Unit finalTargetUnit = null;

        Faction targetFaction = (owner.unitFaction == Faction.Player) ? Faction.Enemy : Faction.Player;
        Vector2Int ownerPos = owner.currentPosition;

        if (UnitManager.Instance.RegisteredUnit.TryGetValue(intendedTargetCoord, out Unit originalTarget)) {
            if (originalTarget != null && originalTarget.unitFaction == targetFaction && originalTarget.GetHealth() > 0) {
                if (IsWithinCrossRange(ownerPos, originalTarget.currentPosition, attackRange)) {
                    finalTargetUnit = originalTarget;
                }
            }
        }

        if (finalTargetUnit == null) {
            foreach (var kvp in UnitManager.Instance.RegisteredUnit) {
                Unit candidate = kvp.Value;
                if (candidate == null || candidate.unitFaction != targetFaction || candidate.GetHealth() <= 0) continue;

                if (IsWithinCrossRange(ownerPos, candidate.currentPosition, attackRange)) {
                    finalTargetUnit = candidate;
                    break;
                }
            }
        }

        if (finalTargetUnit == null) {
            Debug.Log($"<color=yellow>[공격 실패]</color> {owner.gameObject.name}의 현재 위치 기준 사거리 내에 타깃이 존재하지 않습니다.");
            yield break;
        }

        Vector3 rawDir = (finalTargetUnit.transform.position - owner.transform.position);
        rawDir.y = 0;

        if (rawDir != Vector3.zero) {
            Vector3 cardinalDir;
            if (Mathf.Abs(rawDir.x) > Mathf.Abs(rawDir.z)) {
                cardinalDir = new Vector3(Mathf.Sign(rawDir.x), 0, 0);
            }
            else {
                cardinalDir = new Vector3(0, 0, Mathf.Sign(rawDir.z));
            }

            owner.transform.rotation = Quaternion.LookRotation(cardinalDir);
        }

        Vector2Int hitCoordinate = finalTargetUnit.currentPosition;
        owner.Attack(hitCoordinate);
        yield return new WaitForSeconds(0.5f);
    }
}

public class WaitScheduler : IActionScheduler {
    public List<TickCommand> Decompose(ICommand macroCommand, int startTick) {
        WaitCommand waitCmd = macroCommand as WaitCommand;
        if (waitCmd == null) return new List<TickCommand>();

        return new List<TickCommand> {
            new TickCommand {
                executeTick = startTick,
                priority = CommandPriority.Move,
                owner = null,
                actionLogic = WaitLogic(waitCmd.waitTime)
            }
        };
    }

    private IEnumerator WaitLogic(float waitTime) {
        yield return new WaitForSeconds(waitTime);
    }
}