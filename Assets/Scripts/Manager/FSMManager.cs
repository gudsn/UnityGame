using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 가변 라운드 및 유닛 턴 순서 제어 클래스
public class FSMManager : MonoBehaviour {
    [SerializeField] private PlayerFSM playerFSM;
    [SerializeField] private EnemyController enemyController;

    public static FSMManager Instance { get; private set; }

    private PriorityQueue<Unit> unitQueue;
    private int currentTime = 0;
    private const int actionValue = 1000;

    private int currentRound = 0;
    public int CurrentRound => currentRound;

    private List<Unit> currentRoundPlayers = new List<Unit>();
    private List<Unit> currentRoundEnemies = new List<Unit>();

    // 싱글톤 및 대기열 초기화
    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        unitQueue = new PriorityQueue<Unit>(PriorityQueue<Unit>.HeapType.min);
    }

    // 유닛 생성 이벤트 구독
    void Start() {
        UnitManager.Instance.OnSpawnUnit += EnqueueNewUnit;
    }

    // 전투 시작 및 첫 라운드 구동
    public void StartState() {
        currentRound = 0;
        StartNewRound();
    }

    // 유닛 속도 기반 대기열 등록
    public void EnqueueNewUnit(Unit unit) {
        int currentSpeed = Mathf.Max(1, unit.unitSpeed);
        int speed = actionValue / currentSpeed;
        int nextTurnTime = currentTime + speed;

        unitQueue.Enqueue(nextTurnTime, unit);
    }

    // 다음 라운드 유닛 수집 및 라운드 이벤트 발행
    public void StartNewRound() {
        if (unitQueue.Count == 0) return;

        currentRound++;
        EventBus<UpdateRoundEvent>.Publish(new UpdateRoundEvent(currentRound));

        currentRoundPlayers.Clear();
        currentRoundEnemies.Clear();

        int nextPlayerTime = -1;
        List<KeyValuePair<int, Unit>> allItems = unitQueue.GetAllElements();

        foreach (var item in allItems) {
            if (item.Value != null && item.Value.GetHealth() > 0 && item.Value.unitFaction == Faction.Player) {
                nextPlayerTime = item.Key;
                break;
            }
        }

        if (nextPlayerTime == -1) {
            nextPlayerTime = unitQueue.GetFirstPriority();
        }

        currentTime = unitQueue.GetFirstPriority();

        while (unitQueue.Count > 0 && unitQueue.GetFirstPriority() <= nextPlayerTime) {
            int elementPriority = unitQueue.GetFirstPriority();
            Unit unit = unitQueue.Dequeue();

            if (unit == null || unit.GetHealth() <= 0) continue;

            if (unit.unitFaction == Faction.Player) {
                if (elementPriority == nextPlayerTime) {
                    currentRoundPlayers.Add(unit);
                }
            }
            else if (unit.unitFaction == Faction.Enemy) {
                currentRoundEnemies.Add(unit);
            }
        }

        if (currentRoundPlayers.Count == 0 && currentRoundEnemies.Count == 0) {
            StartNewRound();
            return;
        }

        StartCoroutine(ProcessRoundSequence());
    }

    // 적 의도 예고 -> 플레이어 예약 -> 타임라인 동시 연산 순차 시퀀스
    private IEnumerator ProcessRoundSequence() {
        // [수정] 단일 공유 타임라인 초기화
        if (TimeLineUI.Instance != null) {
            TimeLineUI.Instance.ClearAll();
        }

        if (currentRoundEnemies.Count > 0) {
            enemyController.DisplayAllEnemyIntents();
            yield return new WaitForSeconds(1.0f);
        }

        if (currentRoundPlayers.Count > 0) {
            foreach (Unit playerUnit in currentRoundPlayers) {
                if (playerUnit.GetHealth() <= 0) continue;

                playerFSM.StartTurnfor(playerUnit);

                while (playerFSM.CurrentState != null) {
                    yield return null;
                }
            }
        }

        enemyController.CommitEnemyActionsToTimeline();
        enemyController.ClearAllEnemyIntents();

        yield return StartCoroutine(TimeLineManager.Instance.RunTickEngine());

        if (TimeLineUI.Instance != null) {
            TimeLineUI.Instance.ClearAll();
        }

        foreach (Unit playerUnit in currentRoundPlayers) {
            if (playerUnit.GetHealth() > 0) EnqueueNewUnit(playerUnit);
        }
        foreach (Unit enemyUnit in currentRoundEnemies) {
            if (enemyUnit.GetHealth() > 0) EnqueueNewUnit(enemyUnit);
        }

        StartNewRound();
    }
}