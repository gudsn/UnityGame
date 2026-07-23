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
    /// 단일 ICommand와 소유 유닛을 받아 유닛의 이전 틱 작업에 이어 틱 커맨드로 분해 등록합니다.
    /// </summary>
    public void ScheduleAction(Unit unit, ICommand macroCommand) {
        if (unit == null || macroCommand == null) return;

        Type cmdType = macroCommand.GetType();
        if (!schedulers.TryGetValue(cmdType, out IActionScheduler scheduler)) {
            Debug.LogWarning($"[TimeLineManager] 등록되지 않은 명령어 스케줄러입니다: {cmdType.Name}");
            return;
        }

        // 1. 해당 유닛이 등록한 틱 커맨드들 중 가장 마지막 틱 검색
        int startTick = 0;
        var ownerCommands = timelineQueue.Where(cmd => cmd.owner == unit).ToList();

        if (ownerCommands.Count > 0) {
            // 소유주의 기록이 있으면: 가장 마지막 틱 + 1부터 시작
            int lastTick = ownerCommands.Max(cmd => cmd.executeTick);
            startTick = lastTick + 1;
        }
        else {
            // 소유주의 기록이 없으면: 0틱부터 시작
            startTick = 0;
        }

        // 2. 스케줄러를 통해 ICommand -> List<TickCommand> 분해
        List<TickCommand> microTicks = scheduler.Decompose(macroCommand, startTick);

        // 3. 분해된 틱 커맨드들을 전체 Queue에 연쇄 삽입
        if (microTicks != null && microTicks.Count > 0) {
            timelineQueue.AddRange(microTicks);
            Debug.Log($"<color=cyan>[스케줄링 완료]</color> 유닛: {unit.gameObject.name} | 명령: {cmdType.Name} | 틱 범위: {microTicks.First().executeTick} ~ {microTicks.Last().executeTick}");
        }
    }

    /// <summary>
    /// AIDecision 내부의 여러 명령 목록을 유닛 타임라인에 순차적으로 바인딩합니다.
    /// </summary>
    public void ScheduleAction(Unit unit, AIDecision decision) {
        if (decision == null || decision.intendedCommands == null) return;

        foreach (var command in decision.intendedCommands) {
            ScheduleAction(unit, command);
        }
    }

    public IEnumerator RunTickEngine() {
        // 실행 틱 오름차순 -> 우선순위(Move -> Buff -> Attack) 순 정렬
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

            // 틱 번호가 전진할 경우 이전 틱의 모든 병렬 연산 완료 대기
            if (currentCmd.executeTick > currentTick) {
                foreach (var coroutine in activeCoroutines) yield return coroutine;
                activeCoroutines.Clear();

                Debug.Log($"--- Tick {currentTick} 완료. 다음 Tick으로 전진 ---");
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