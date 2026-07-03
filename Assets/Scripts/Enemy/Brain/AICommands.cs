using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 공통 규격
public interface ICommand {
    IEnumerator ExecuteCoroutine();
}

// 의사결정 데이터
public class AIDecision {
    public float utilityScore;
    public string decisionName;
    public Queue<ICommand> actionQueue = new Queue<ICommand>();
}

// 대기 커맨드
public class WaitCommand : ICommand {
    private float waitTime;

    public WaitCommand(float _waitTime = 0.5f) {
        this.waitTime = _waitTime;
    }

    public IEnumerator ExecuteCoroutine() {
        yield return new WaitForSeconds(waitTime);
    }
}

// 이동 커맨드
public class MoveCommand : ICommand {
    private Unit owner;
    private TileData destinationTile;
    private List<TileData> tileList;
    private float moveSpeed = 5f;

    public MoveCommand(Unit _owner, TileData _destinationTile) {
        this.owner = _owner;
        this.destinationTile = _destinationTile;

        TileData currentTile = GridSystem.Instance.GetTileData(owner.currentPosition);
        tileList = GridSystem.Instance.AStarAlgorithm(currentTile, destinationTile);
    }

    public IEnumerator ExecuteCoroutine() {
        if (tileList == null || tileList.Count == 0) yield break;

        foreach (var tile in tileList) {
            Vector3 targetPosition = tile.worldPosition;
            targetPosition.y = owner.transform.position.y;

            // 목표 방향 사전 계산
            Vector3 direction = (targetPosition - owner.transform.position).normalized;
            Quaternion targetRotation = owner.transform.rotation;
            if (direction != Vector3.zero) {
                targetRotation = Quaternion.LookRotation(direction);
            }

            // 부드러운 회전 및 위치 이동
            while (Vector3.Distance(owner.transform.position, targetPosition) > 0.05f) {
                owner.transform.rotation = Quaternion.Slerp(owner.transform.rotation, targetRotation, Time.deltaTime * 15f);
                owner.transform.position = Vector3.MoveTowards(owner.transform.position, targetPosition, moveSpeed * Time.deltaTime);

                yield return null;
            }

            // 위치 및 회전 오차 강제 보정 (어긋남 방지)
            owner.transform.position = targetPosition;
            owner.transform.rotation = targetRotation;

            // 논리적 데이터 동기화
            Vector2Int newPos = new Vector2Int(tile.gridX, tile.gridY);
            UnitManager.Instance.MoveUnit(newPos, owner);
        }
    }
}

// 공격 커맨드
public class AttackCommand : ICommand {
    private Unit owner;
    private int attackRange;
    private Faction ownerFaction;

    public AttackCommand(Unit _owner, int _attackRange) {
        this.owner = _owner;
        this.attackRange = _attackRange;
        this.ownerFaction = owner.unitFaction;
    }

    public IEnumerator ExecuteCoroutine() {
        // 등록된 전체 유닛 순회 탐색 (그리드 길막 버그 우회)
        foreach (var it in UnitManager.Instance.RegisteredUnit) {
            Unit targetUnit = it.Value;

            // 적군 및 생존 여부 확인
            if (targetUnit.unitFaction != ownerFaction && targetUnit.GetHealth() > 0) {

                // 맨해튼 거리 측정
                int dist = GridSystem.Instance.GetManhattanDistance(owner.currentPosition, targetUnit.currentPosition);

                // 사거리 내 타겟 확인
                if (dist <= attackRange) {
                    // 타겟 방향 회전
                    Vector3 lookTarget = targetUnit.transform.position;
                    lookTarget.y = owner.transform.position.y;
                    owner.transform.LookAt(lookTarget);

                    // 공격 실행
                    owner.Attack(targetUnit.currentPosition);

                    // 연출 대기
                    yield return new WaitForSeconds(0.5f);

                    // 단일 타겟 타격 후 종료
                    break;
                }
            }
        }
    }
}