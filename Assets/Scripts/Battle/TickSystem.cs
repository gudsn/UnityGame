using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 틱 실행 우선순위
public enum CommandPriority {
    Move = 1,
    Buff = 2,
    Attack = 3
}

// 단일 틱 실행 구조체
public struct TickCommand {
    public int executeTick;
    public CommandPriority priority;
    public Unit owner;
    public IEnumerator actionLogic;
}

// 틱 분해 스케줄러 인터페이스
public interface IActionScheduler {
    List<TickCommand> Decompose(ICommand macroCommand, int startTick);
}

// 이동 스케줄러 (1타일 = 1틱)
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
                executeTick = startTick + i + 1,
                priority = CommandPriority.Move,
                owner = owner,
                actionLogic = MoveSingleTileLogic(owner, path[i])
            });
        }
        return tickCommands;
    }

    // 1칸 이동 로직
    private IEnumerator MoveSingleTileLogic(Unit owner, TileData targetTile) {
        float moveSpeed = 5f;
        Vector3 targetPosition = targetTile.worldPosition;
        targetPosition.y = owner.transform.position.y;
        Vector3 direction = (targetPosition - owner.transform.position).normalized;
        Quaternion targetRotation = owner.transform.rotation;

        if (direction != Vector3.zero) targetRotation = Quaternion.LookRotation(direction);

        while (Vector3.Distance(owner.transform.position, targetPosition) > 0.05f) {
            owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, targetRotation, Time.deltaTime * 15f);
            owner.transform.position = Vector3.MoveTowards(owner.transform.position, targetPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        owner.transform.position = targetPosition;
        owner.transform.rotation = targetRotation;

        Vector2Int newPos = new Vector2Int(targetTile.gridX, targetTile.gridY);
        UnitManager.Instance.MoveUnit(newPos, owner);
    }
}

// 공격 스케줄러
public class AttackScheduler : IActionScheduler {
    public List<TickCommand> Decompose(ICommand macroCommand, int startTick) {
        List<TickCommand> tickCommands = new List<TickCommand>();
        AttackCommand attackCmd = macroCommand as AttackCommand;

        if (attackCmd == null) return tickCommands;

        // 1틱: 공격 전 1틱 대기
        tickCommands.Add(new TickCommand {
            executeTick = startTick + 1,
            priority = CommandPriority.Move,
            owner = attackCmd.owner,
            actionLogic = WaitLogic(0.5f)
        });

        // 2틱: 실제 타격 실행 (타겟 유닛 뿐만 아니라 조준 타일 좌표 정보도 전달)
        tickCommands.Add(new TickCommand {
            executeTick = startTick + 2,
            priority = CommandPriority.Attack,
            owner = attackCmd.owner,
            actionLogic = AttackLogic(attackCmd.owner, attackCmd.target, attackCmd.targetCoordinate)
        });

        return tickCommands;
    }

    // 1틱을 소모하기 위한 대기 코루틴 로직
    private IEnumerator WaitLogic(float waitTime) {
        yield return new WaitForSeconds(waitTime);
    }

    // 타격 코루틴 로직 (요구사항 실현 시점)
    private IEnumerator AttackLogic(Unit owner, Unit target, Vector2Int targetCoordinate) {
        // [조건 체크] 타임라인 실행 시점에 조준한 타일에 적이 여전히 살아있는지 유효성 검사 수행
        if (!UnitManager.Instance.RegisteredUnit.TryGetValue(targetCoordinate, out Unit currentOccupant) || currentOccupant == null || currentOccupant.GetHealth() <= 0) {
            Debug.Log($"<color=red>[공격 실패]</color> {owner.gameObject.name}의 공격이 실패했습니다. (타겟 좌표 {targetCoordinate}에 적이 존재하지 않습니다.)");
            yield break;
        }

        Vector3 lookTarget = currentOccupant.transform.position;
        lookTarget.y = owner.transform.position.y;
        owner.transform.LookAt(lookTarget);

        owner.Attack(targetCoordinate);
        yield return new WaitForSeconds(0.5f);
    }
}

// 대기 스케줄러
public class WaitScheduler : IActionScheduler {
    public List<TickCommand> Decompose(ICommand macroCommand, int startTick) {
        WaitCommand waitCmd = macroCommand as WaitCommand;
        if (waitCmd == null) return new List<TickCommand>();

        return new List<TickCommand> {
            new TickCommand {
                executeTick = startTick + 1,
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