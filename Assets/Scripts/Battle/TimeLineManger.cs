using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TimeLineManager : MonoBehaviour {
    public static TimeLineManager Instance { get; private set; }

    private List<TickCommand> timelineQueue = new List<TickCommand>();
    private Dictionary<Type, IActionScheduler> schedulers;

    private void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        schedulers = new Dictionary<Type, IActionScheduler> {
            { typeof(WaitCommand), new WaitScheduler() },
            { typeof(MoveCommand), new MoveScheduler() },
            { typeof(PlayerMoveCommand), new MoveScheduler() },
            { typeof(AttackCommand), new AttackScheduler() }
        };
    }

    /// <summary>
    /// 플레이어용 기존 스케줄러 인터페이스 유지 (기존 시간 계산법 사용)
    /// </summary>
    public void ScheduleAction(Unit unit, AIDecision decision) {
        // 내부 연산의 안전을 위해 0틱 기준으로 동작하도록 오버로딩 호출로 토스합니다.
        ScheduleAction(unit, decision, 0);
    }

    /// <summary>
    /// [핵심 추가] 동시 턴 시스템을 위해 시작 틱 기준점(forcedStartTick)을 강제로 지정하는 스케줄링 메서드
    /// </summary>
    public void ScheduleAction(Unit unit, AIDecision decision, int forcedStartTick) {
        if (decision == null || decision.intendedCommands == null) return;

        // 이번 라운드 진영 동시 출발을 위해 강제 지정된 틱(0)에서 연산을 출발시킵니다.
        int currentTick = forcedStartTick;

        foreach (var macroCommand in decision.intendedCommands) {
            Type cmdType = macroCommand.GetType();

            if (schedulers.TryGetValue(cmdType, out IActionScheduler scheduler)) {
                List<TickCommand> microTicks = scheduler.Decompose(macroCommand, currentTick);

                if (microTicks.Count > 0) {
                    timelineQueue.AddRange(microTicks);
                    // 한 유닛의 연속된 하위 행동(이동 후 공격 등)은 누적해서 틱을 연결합니다.
                    currentTick = microTicks.Last().executeTick;
                }
            }
        }
    }

    public IEnumerator RunTickEngine() {
        // 1순위: 실행 틱 넘버 순, 2순위: 우선순위(Move -> Buff -> Attack) 순 정렬
        timelineQueue = timelineQueue
            .OrderBy(cmd => cmd.executeTick)
            .ThenBy(cmd => cmd.priority)
            .ToList();

        if (timelineQueue.Count == 0) {
            Debug.Log("타임라인 대기열이 비어있어 엔진을 구동하지 않습니다.");
            yield break;
        }

        int currentTick = timelineQueue[0].executeTick;
        List<Coroutine> activeCoroutines = new List<Coroutine>();

        while (timelineQueue.Count > 0) {
            TickCommand currentCmd = timelineQueue[0];

            // 틱 넘버가 전진했을 경우, 이전 틱의 모든 병렬 코루틴 완료를 기다린 후 넘어감
            if (currentCmd.executeTick > currentTick) {
                foreach (var coroutine in activeCoroutines) yield return coroutine;
                activeCoroutines.Clear();

                Debug.Log($"--- Tick {currentTick} 완료. 다음 Tick으로 넘어가기 전 0.5초 대기 ---");
                yield return new WaitForSeconds(0.5f);

                currentTick = currentCmd.executeTick;
            }

            timelineQueue.RemoveAt(0);

            string unitName = currentCmd.owner != null ? currentCmd.owner.gameObject.name : "System";
            Debug.Log($"[Tick {currentCmd.executeTick}] 실행: {unitName} | 우선순위: {currentCmd.priority}");

            if (currentCmd.actionLogic != null) {
                activeCoroutines.Add(StartCoroutine(currentCmd.actionLogic));
            }
        }

        foreach (var coroutine in activeCoroutines) yield return coroutine;

        timelineQueue.Clear();
        Debug.Log("모든 타임라인 연산 완료.");
    }
}