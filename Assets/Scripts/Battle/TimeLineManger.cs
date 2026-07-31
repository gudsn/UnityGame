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

        // 명령어별 스케줄러 등록
        schedulers = new Dictionary<Type, IActionScheduler> {
            { typeof(WaitCommand), new WaitScheduler() },
            { typeof(MoveCommand), new MoveScheduler() },
            { typeof(PlayerMoveCommand), new MoveScheduler() },
            { typeof(AttackCommand), new AttackScheduler() }
        };
    }

    public void ScheduleAction(Unit unit, ICommand macroCommand) {
        if (unit == null || macroCommand == null) return;

        Type cmdType = macroCommand.GetType();
        if (!schedulers.TryGetValue(cmdType, out IActionScheduler scheduler)) {
            Debug.LogWarning($"[TimeLineManager] 등록되지 않은 명령어 스케줄러입니다: {cmdType.Name}");
            return;
        }

        int startTick = 0;
        var ownerCommands = timelineQueue.Where(cmd => cmd.owner == unit).ToList();

        if (ownerCommands.Count > 0) {
            int lastTick = ownerCommands.Max(cmd => cmd.executeTick);
            startTick = lastTick + 1;
        }
        else {
            startTick = 1;
        }

        List<TickCommand> microTicks = scheduler.Decompose(macroCommand, startTick);

        if (microTicks != null && microTicks.Count > 0) {
            timelineQueue.AddRange(microTicks);
        }
    }

    public void ScheduleAction(Unit unit, AIDecision decision) {
        if (decision == null || decision.intendedCommands == null) return;

        foreach (var command in decision.intendedCommands) {
            ScheduleAction(unit, command);
        }
    }

    public void ScheduleActionAtTick(Unit unit, ICommand macroCommand, int startTick) {
        if (unit == null || macroCommand == null) return;

        Type cmdType = macroCommand.GetType();
        if (!schedulers.TryGetValue(cmdType, out IActionScheduler scheduler)) {
            Debug.LogWarning($"[TimeLineManager] 등록되지 않은 명령어 스케줄러입니다: {cmdType.Name}");
            return;
        }

        List<TickCommand> microTicks = scheduler.Decompose(macroCommand, startTick);

        if (microTicks != null && microTicks.Count > 0) {
            timelineQueue.AddRange(microTicks);
        }
    }

    // hitTick보다 '이전'에 완결되는 이동 명령만 추적하여 미래 위치 계산
    public MoveCommand GetScheduledMoveCommandAtTick(Unit unit, int hitTick) {
        if (unit == null || timelineQueue.Count == 0) return null;

        var lastMoveTick = timelineQueue
            .Where(cmd => cmd.owner == unit && cmd.executeTick < hitTick && cmd.priority == CommandPriority.Move)
            .OrderByDescending(cmd => cmd.executeTick)
            .FirstOrDefault();

        if (lastMoveTick.owner == null) return null;

        var scheduledMove = timelineQueue
            .Where(cmd => cmd.owner == unit && cmd.executeTick < hitTick)
            .Select(cmd => cmd.actionLogic)
            .OfType<MoveCommand>()
            .FirstOrDefault();

        return scheduledMove;
    }

    public IEnumerator RunTickEngine() {
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

            if (currentCmd.executeTick > currentTick) {
                foreach (var coroutine in activeCoroutines) yield return coroutine;
                activeCoroutines.Clear();

                yield return new WaitForSeconds(0.4f);
                currentTick = currentCmd.executeTick;
            }

            timelineQueue.RemoveAt(0);

            // 틱 UI 연출 동기화
            yield return StartCoroutine(TriggerTickVisualEffectRoutine(currentCmd.owner, currentCmd.executeTick));

            if (currentCmd.actionLogic != null) {
                activeCoroutines.Add(StartCoroutine(currentCmd.actionLogic));
            }
        }

        foreach (var coroutine in activeCoroutines) yield return coroutine;

        timelineQueue.Clear();
    }

    private IEnumerator TriggerTickVisualEffectRoutine(Unit owner, int tickIndex) {
        if (UIManager.Instance == null || owner == null) yield break;

        UIDocument uiManagerDoc = UIManager.Instance.GetComponent<UIDocument>();
        if (uiManagerDoc == null || uiManagerDoc.rootVisualElement == null) yield break;

        VisualElement root = uiManagerDoc.rootVisualElement;
        VisualElement targetSlot = null;

        if (owner.unitFaction == Faction.Player) {
            VisualElement playerTrack = root.Q<VisualElement>("player-track");
            if (playerTrack != null) {
                targetSlot = playerTrack.Q<VisualElement>($"slot-{tickIndex}");
            }
        }
        else {
            VisualElement enemyTracksContainer = root.Q<VisualElement>("enemy-tracks-container");
            if (enemyTracksContainer != null) {
                var rows = enemyTracksContainer.Query<VisualElement>(className: "track-row").ToList();
                foreach (var row in rows) {
                    Label nameLabel = row.Q<Label>(className: "track-header-label");
                    if (nameLabel != null && nameLabel.text == owner.GetName()) {
                        targetSlot = row.Q<VisualElement>($"slot-{tickIndex}");
                        break;
                    }
                }
            }
        }

        if (targetSlot != null) {
            var boxItem = targetSlot.Q<VisualElement>(className: "box-item");
            if (boxItem != null) {
                boxItem.AddToClassList("is-executing");
                yield return new WaitForSeconds(0.7f);
                boxItem.RemoveFromClassList("is-executing");
            }
        }
    }
}