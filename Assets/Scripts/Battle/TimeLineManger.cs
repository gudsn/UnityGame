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

    public void ScheduleAction(Unit unit, AIDecision decision) {
        if (decision == null || decision.intendedCommands == null) return;

        // [순서 보장 핵심] 이 유닛이 이전에 예약해 둔 모든 물리적/논리적 틱 명령 중 최댓값을 가져옵니다.
        var unitCmds = timelineQueue.Where(cmd => cmd.owner == unit).ToList();
        int currentTick = unitCmds.Count > 0 ? unitCmds.Max(cmd => cmd.executeTick) : 0;

        foreach (var macroCommand in decision.intendedCommands) {
            Type cmdType = macroCommand.GetType();

            if (schedulers.TryGetValue(cmdType, out IActionScheduler scheduler)) {
                List<TickCommand> microTicks = scheduler.Decompose(macroCommand, currentTick);

                if (microTicks.Count > 0) {
                    timelineQueue.AddRange(microTicks);
                    // 매크로 내 명령이 복수 개일 때를 대비해 마지막 틱을 갱신해 나갑니다.
                    currentTick = microTicks.Last().executeTick;
                }
            }
        }
    }

    public IEnumerator RunTickEngine() {
        timelineQueue = timelineQueue
            .OrderBy(cmd => cmd.executeTick)
            .ThenBy(cmd => cmd.priority)
            .ToList();

        int currentTick = timelineQueue.Count > 0 ? timelineQueue[0].executeTick : 0;
        List<Coroutine> activeCoroutines = new List<Coroutine>();

        while (timelineQueue.Count > 0) {
            TickCommand currentCmd = timelineQueue[0];

            if (currentCmd.executeTick > currentTick) {
                foreach (var coroutine in activeCoroutines) yield return coroutine;
                activeCoroutines.Clear();

                Debug.Log($"--- Tick {currentTick} 완료. 다음 Tick으로 넘어가기 전 0.5초 대기 ---");
                yield return new WaitForSeconds(0.5f);

                currentTick = currentCmd.executeTick;
            }

            timelineQueue.RemoveAt(0);

            string unitName = currentCmd.owner != null ? currentCmd.owner.gameObject.name : "System";
            Debug.Log($"[Tick {currentCmd.executeTick}] 실행: {unitName} | 로직: {currentCmd.actionLogic} | 우선순위: {currentCmd.priority}");

            if (currentCmd.actionLogic != null) {
                activeCoroutines.Add(StartCoroutine(currentCmd.actionLogic));
            }
        }

        foreach (var coroutine in activeCoroutines) yield return coroutine;

        // 타임라인 실행 연산이 완전히 끝나면 다음 턴 연산을 위해 큐를 비워줍니다.
        timelineQueue.Clear();
        Debug.Log("모든 타임라인 연산 완료.");
    }
}