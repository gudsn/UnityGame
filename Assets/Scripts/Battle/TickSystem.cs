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

    // 1칸 이동 로직 (실시간 동적 충돌 제어)
    private IEnumerator MoveSingleTileLogic(Unit owner, TileData targetTile) {
        Vector2Int targetPos = new Vector2Int(targetTile.gridX, targetTile.gridY);

        // [현장 충돌 감지] 예약 순서와 무관하게, 이 틱이 발동한 현 시점에 누군가 내 앞 타일에 도달했다면 중단
        if (UnitManager.Instance.RegisteredUnit.TryGetValue(targetPos, out Unit occupant)) {
            if (occupant != null && occupant != owner) {
                Debug.Log($"<color=orange>[이동 중단]</color> {owner.gameObject.name}의 앞길이 {occupant.gameObject.name}에 의해 막혀 제자리에 멈춥니다.");

                // 전방이 막혀 전진이 취소되었으므로 가상 위치를 현재 물리 위치로 돌려놓고 코루틴을 강제 종료합니다.
                owner.virtualPosition = owner.currentPosition;
                yield break;
            }
        }

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

        // 한 칸 진입에 도달한 시점에 즉시 실제 데이터를 갱신합니다.
        UnitManager.Instance.MoveUnit(targetPos, owner);
    }
}

// 공격 스케줄러 (사거리 2칸 내 실시간 모색 방식)
public class AttackScheduler : IActionScheduler {
    public List<TickCommand> Decompose(ICommand macroCommand, int startTick) {
        List<TickCommand> tickCommands = new List<TickCommand>();
        AttackCommand attackCmd = macroCommand as AttackCommand;

        if (attackCmd == null) return tickCommands;

        // 1틱: 공격 전 대기 선딜레이
        tickCommands.Add(new TickCommand {
            executeTick = startTick + 1,
            priority = CommandPriority.Move,
            owner = attackCmd.owner,
            actionLogic = WaitLogic(0.5f)
        });

        // 2틱: 실제 타격 연산 (실행되는 시점에 범위 내 플레이어를 타겟팅)
        tickCommands.Add(new TickCommand {
            executeTick = startTick + 2,
            priority = CommandPriority.Attack,
            owner = attackCmd.owner,
            actionLogic = AttackLogic(attackCmd.owner)
        });

        return tickCommands;
    }

    private IEnumerator WaitLogic(float waitTime) {
        yield return new WaitForSeconds(waitTime);
    }

    // [기획 반영] 실행 시점 기준, 내 주변 맨해튼 2칸 이내에 타겟 플레이어가 있는지 확인합니다.
    private IEnumerator AttackLogic(Unit owner) {
        int attackRange = 2; // 맨해튼 거리 2칸
        Unit targetPlayer = null;
        Vector2Int targetCoordinate = Vector2Int.zero;

        // 공격 애니메이션/연출 발동 직전, 범위 내에 서 있는 플레이어가 있는지 실시간 스캔
        foreach (var kvp in UnitManager.Instance.RegisteredUnit) {
            Unit candidate = kvp.Value;
            if (candidate == null || candidate.unitFaction != Faction.Player || candidate.GetHealth() <= 0) continue;

            int distance = GridSystem.Instance.GetManhattanDistance(owner.currentPosition, candidate.currentPosition);
            if (distance <= attackRange) {
                targetPlayer = candidate;
                targetCoordinate = candidate.currentPosition;
                break; // 유효한 플레이어를 탐색하면 타겟으로 실시간 잠금
            }
        }

        // 사거리 안에 플레이어가 전혀 도달하지 못했거나 도망쳤다면 공격이 불발(헛방) 처리됩니다.
        if (targetPlayer == null) {
            Debug.Log($"<color=yellow>[공격 실패]</color> {owner.gameObject.name}의 사거리(맨해튼 2칸) 내에 플레이어가 도달하지 못해 허공을 가릅니다.");
            yield break;
        }

        Vector3 lookTarget = targetPlayer.transform.position;
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