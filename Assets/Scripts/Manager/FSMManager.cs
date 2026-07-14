using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FSMManager : MonoBehaviour {
    [SerializeField] private PlayerFSM playerFSM;
    [SerializeField] private EnemyController enemyController;

    public static FSMManager Instance { get; private set; }

    private PriorityQueue<Unit> unitQueue;
    private int currentTime = 0;
    private const int actionValue = 1000;

    private List<Unit> currentRoundPlayers = new List<Unit>();
    private List<Unit> currentRoundEnemies = new List<Unit>();

    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        unitQueue = new PriorityQueue<Unit>(PriorityQueue<Unit>.HeapType.min);
    }

    void Start() {
        UnitManager.Instance.OnSpawnUnit += EnqueueNewUnit;
    }

    public void StartState() {
        Debug.Log("Game Start - 아군 턴 기준 가변 라운드 시스템을 구동합니다.");
        StartNewRound();
    }

    public void EnqueueNewUnit(Unit unit) {
        int currentSpeed = Mathf.Max(1, unit.unitSpeed);
        int speed = actionValue / currentSpeed;
        int nextTurnTime = currentTime + speed;

        unitQueue.Enqueue(nextTurnTime, unit);
    }

    /// <summary>
    /// [문제 2 해결] 다음 아군 턴이 도달할 때까지 배치된 모든 적군을 한 라운드로 묶어 수집합니다.
    /// </summary>
    public void StartNewRound() {
        if (unitQueue.Count == 0) {
            Debug.Log("전장에 행동 가능한 유닛이 없습니다.");
            return;
        }

        currentRoundPlayers.Clear();
        currentRoundEnemies.Clear();

        // 1. 대기열을 스캔하여 미래에 가장 먼저 행동할 '아군의 턴 시간'을 데드라인으로 포착합니다.
        int nextPlayerTime = -1;
        List<KeyValuePair<int, Unit>> allItems = unitQueue.GetAllElements();

        foreach (var item in allItems) {
            if (item.Value != null && item.Value.GetHealth() > 0 && item.Value.unitFaction == Faction.Player) {
                nextPlayerTime = item.Key;
                break;
            }
        }

        // 대기열에 아군이 아예 존재하지 않는다면 첫 요소 시간대를 데드라인으로 둡니다.
        if (nextPlayerTime == -1) {
            nextPlayerTime = unitQueue.GetFirstPriority();
        }

        currentTime = unitQueue.GetFirstPriority();

        // 2. 데드라인(다음 아군 턴 시간) 이하에 존재하는 모든 적군과 해당 시간대의 아군을 수집합니다.
        while (unitQueue.Count > 0 && unitQueue.GetFirstPriority() <= nextPlayerTime) {
            int elementPriority = unitQueue.GetFirstPriority();
            Unit unit = unitQueue.Dequeue();

            if (unit == null || unit.GetHealth() <= 0) continue;

            if (unit.unitFaction == Faction.Player) {
                // 정확히 잡혀진 아군 타임라인 유닛만 수집
                if (elementPriority == nextPlayerTime) {
                    currentRoundPlayers.Add(unit);
                }
            }
            else if (unit.unitFaction == Faction.Enemy) {
                // 아군 턴이 오기 전 사이의 모든 적 유닛들을 스냅샷으로 통합 흡수
                currentRoundEnemies.Add(unit);
            }
        }

        if (currentRoundPlayers.Count == 0 && currentRoundEnemies.Count == 0) {
            StartNewRound();
            return;
        }

        StartCoroutine(ProcessRoundSequence());
    }

    private IEnumerator ProcessRoundSequence() {
        Debug.Log($"<color=cyan>====== [가변 라운드 오픈 (현재 시간: {currentTime})] ======</color>");

        // PHASE 1: 수집된 모든 적군의 의도를 수립하고 맵에 예고
        if (currentRoundEnemies.Count > 0) {
            enemyController.DisplayAllEnemyIntents();
            yield return new WaitForSeconds(1.0f);
        }

        // PHASE 2: 플레이어 행동 예약 및 누적 대기
        if (currentRoundPlayers.Count > 0) {
            foreach (Unit playerUnit in currentRoundPlayers) {
                if (playerUnit.GetHealth() <= 0) continue;

                playerFSM.StartTurnfor(playerUnit);

                while (playerFSM.CurrentState != null) {
                    yield return null;
                }
            }
        }

        // PHASE 3: [문제 1 해결] 캐싱된 적 의도를 타임라인 틱 엔진에 커밋하고 동시 연산
        Debug.Log("[Phase 3] 모든 예약 마감. 타임라인 동시 연산을 시작합니다.");

        enemyController.CommitEnemyActionsToTimeline(); // 🚨 이 시점에 적 행동이 주입되어 틱에 노출됩니다.
        enemyController.ClearAllEnemyIntents();

        yield return StartCoroutine(TimeLineManager.Instance.RunTickEngine());

        // ROUND END: 대기열 재진입
        foreach (Unit playerUnit in currentRoundPlayers) {
            if (playerUnit.GetHealth() > 0) EnqueueNewUnit(playerUnit);
        }
        foreach (Unit enemyUnit in currentRoundEnemies) {
            if (enemyUnit.GetHealth() > 0) EnqueueNewUnit(enemyUnit);
        }

        StartNewRound();
    }
}