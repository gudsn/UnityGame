using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

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

    // 특정 유닛의 해당 틱 구간 중복 예약 검증
    public bool HasUnitReservedInTickRange(Unit unit, int startTick, int tickCount) {
        if (unit == null || timelineQueue.Count == 0) return false;

        int endTick = startTick + tickCount - 1;
        return timelineQueue.Any(cmd => cmd.owner == unit && cmd.executeTick >= startTick && cmd.executeTick <= endTick);
    }

    public void ScheduleAction(Unit unit, ICommand macroCommand) {
        if (unit == null || macroCommand == null) return;

        Type cmdType = macroCommand.GetType();
        if (!schedulers.TryGetValue(cmdType, out IActionScheduler scheduler)) return;

        int startTick = 1;
        var ownerCommands = timelineQueue.Where(cmd => cmd.owner == unit).ToList();
        if (ownerCommands.Count > 0) {
            startTick = ownerCommands.Max(cmd => cmd.executeTick) + 1;
        }

        List<TickCommand> microTicks = scheduler.Decompose(macroCommand, startTick);
        if (microTicks != null && microTicks.Count > 0) {
            timelineQueue.AddRange(microTicks);
        }
    }

    public void ScheduleActionAtTick(Unit unit, ICommand macroCommand, int startTick) {
        if (unit == null || macroCommand == null) return;

        Type cmdType = macroCommand.GetType();
        if (!schedulers.TryGetValue(cmdType, out IActionScheduler scheduler)) return;

        List<TickCommand> microTicks = scheduler.Decompose(macroCommand, startTick);
        if (microTicks != null && microTicks.Count > 0) {
            timelineQueue.AddRange(microTicks);
        }
    }

    public void CancelMacroCommand(Unit unit, ICommand macroCommand) {
        if (unit == null || macroCommand == null || timelineQueue.Count == 0) return;

        CommandPriority priority = (macroCommand is AttackCommand) ? CommandPriority.Attack : CommandPriority.Move;
        int removed = timelineQueue.RemoveAll(cmd => cmd.owner == unit && cmd.priority == priority);
        Debug.Log($"<color=orange>[타임라인 취소]</color> {unit.GetName()}의 틱 명령 {removed}개 삭제됨");
    }

    public void CancelRemainingCommands(Unit unit, CommandPriority priorityToCancel) {
        if (unit == null || timelineQueue.Count == 0) return;
        timelineQueue.RemoveAll(cmd => cmd.owner == unit && cmd.priority == priorityToCancel);
    }

    // 💡 [핵심] 새 라운드 시작 시 대기열 완벽 초기화
    public void ClearQueue() {
        timelineQueue.Clear();
    }

    public IEnumerator RunTickEngine() {
        timelineQueue = timelineQueue
            .OrderBy(cmd => cmd.executeTick)
            .ThenBy(cmd => cmd.priority)
            .ToList();

        if (timelineQueue.Count == 0) yield break;

        int currentTick = timelineQueue[0].executeTick;
        List<Coroutine> activeCoroutines = new List<Coroutine>();

        while (timelineQueue.Count > 0) {
            TickCommand currentCmd = timelineQueue[0];

            if (currentCmd.executeTick > currentTick) {
                foreach (var coroutine in activeCoroutines) yield return coroutine;
                activeCoroutines.Clear();

                yield return new WaitForSeconds(0.4f);
                currentTick = currentCmd.executeTick;
            }

            timelineQueue.RemoveAt(0);
            yield return StartCoroutine(TriggerTickVisualEffectRoutine(currentCmd.executeTick));

            if (currentCmd.actionLogic != null) {
                activeCoroutines.Add(StartCoroutine(currentCmd.actionLogic));
            }
        }

        foreach (var coroutine in activeCoroutines) yield return coroutine;

        // 💡 틱 실행 완료 후 잔여 큐 확실히 비우기
        timelineQueue.Clear();
    }

    private IEnumerator TriggerTickVisualEffectRoutine(int tickIndex) {
        if (TimeLineUI.Instance == null) yield break;

        UIDocument doc = TimeLineUI.Instance.GetComponent<UIDocument>();
        if (doc?.rootVisualElement == null && UIManager.Instance != null) {
            doc = UIManager.Instance.GetComponent<UIDocument>();
        }
        if (doc?.rootVisualElement == null) yield break;

        VisualElement boxContainer = doc.rootVisualElement.Q<VisualElement>($"box-container-{tickIndex}");
        if (boxContainer != null) {
            var boxes = boxContainer.Query<VisualElement>(className: "box-item").ToList();
            foreach (var b in boxes) b.AddToClassList("is-executing");
            yield return new WaitForSeconds(0.4f);
            foreach (var b in boxes) b.RemoveFromClassList("is-executing");
        }
    }
}