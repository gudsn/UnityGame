using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class TimeLineDragger {
    private readonly VisualElement targetElement;
    private readonly VisualElement timelineRoot;

    private bool isDragging;
    private Vector2 dragStartPointerPos;
    private Vector2 elementStartPos;

    public TimeLineDragger(VisualElement target, VisualElement timeline) {
        if (target == null) return;

        targetElement = target;
        timelineRoot = timeline;
        targetElement.pickingMode = PickingMode.Position;

        targetElement.RegisterCallback<PointerDownEvent>(OnPointerDown);
        targetElement.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        targetElement.RegisterCallback<PointerUpEvent>(OnPointerUp);
    }

    // 드래그 시작
    private void OnPointerDown(PointerDownEvent evt) {
        if (targetElement == null || evt.button != 0) return;

        isDragging = true;
        dragStartPointerPos = evt.position;

        float left = targetElement.resolvedStyle.left;
        float top = targetElement.resolvedStyle.top;
        if (float.IsNaN(left) || left == 0) left = targetElement.layout.x;
        if (float.IsNaN(top) || top == 0) top = targetElement.layout.y;
        if (left == 0 && top == 0) {
            left = 30f;
            top = 140f;
        }

        elementStartPos = new Vector2(left, top);
        targetElement.BringToFront();
        targetElement.CapturePointer(evt.pointerId);
        evt.StopPropagation();
    }

    // 드래그 이동
    private void OnPointerMove(PointerMoveEvent evt) {
        if (!isDragging || targetElement == null || !targetElement.HasPointerCapture(evt.pointerId)) return;

        Vector2 delta = (Vector2)evt.position - dragStartPointerPos;
        targetElement.style.left = elementStartPos.x + delta.x;
        targetElement.style.top = elementStartPos.y + delta.y;
        evt.StopPropagation();
    }

    // 드롭
    private void OnPointerUp(PointerUpEvent evt) {
        if (!isDragging || targetElement == null) return;

        isDragging = false;
        if (targetElement.HasPointerCapture(evt.pointerId)) {
            targetElement.ReleasePointer(evt.pointerId);
        }

        bool droppedInTimeline = false;
        ICommand cmd = targetElement.userData as ICommand;

        if (cmd != null && TimeLineUI.Instance != null && TimeLineManager.Instance != null) {
            int boxTickCount = 1;
            string commandTypeName = cmd.GetType().Name;

            if (cmd is AttackCommand) boxTickCount = 2;
            else if (cmd is MoveCommand m && m.path != null) boxTickCount = m.path.Count;
            else if (cmd is PlayerMoveCommand pm && pm.path != null) boxTickCount = pm.path.Count;

            // 💡 [수정] 우측 보정이 반영된 마우스 기준 틱 컬럼 탐색
            int targetSlotIndexFound = FindHoveredTickColumn(evt.position, targetElement);

            if (targetSlotIndexFound != -1) {
                int startTick = targetSlotIndexFound;
                Unit owner = GetOwnerFromCommand(cmd);

                // 8틱 초과 검증
                if (startTick + boxTickCount - 1 > 8) {
                    Debug.Log("<color=yellow>[알림]</color> 타임라인(8틱) 범위를 초과하여 배치할 수 없습니다!");
                }
                // 동일 유닛 중복 예약 검증
                else if (TimeLineManager.Instance.HasUnitReservedInTickRange(owner, startTick, boxTickCount)) {
                    Debug.Log($"<color=yellow>[알림]</color> {owner.GetName()}은(는) 해당 틱에 이미 예약된 행동이 있습니다.");
                }
                else {
                    droppedInTimeline = true;
                    targetElement.RemoveFromHierarchy();

                    // 타임라인 슬롯에 틱별 박스 배치
                    for (int offset = 0; offset < boxTickCount; offset++) {
                        int currentTick = startTick + offset;
                        string subType = commandTypeName;
                        if (cmd is AttackCommand && offset == 0) subType = "Prepare";

                        TimeLineUI.Instance.PlacePlayerActionIntoSlot(owner, cmd, currentTick, subType);
                    }

                    // 스케줄러 등록 및 FSM 버튼 비활성화
                    if (owner != null) {
                        TimeLineManager.Instance.ScheduleActionAtTick(owner, cmd, startTick);

                        PlayerFSM playerFSM = Object.FindFirstObjectByType<PlayerFSM>();
                        if (playerFSM != null && playerFSM.activeUnit == owner) {
                            if (cmd is PlayerMoveCommand pmCmd) {
                                if (pmCmd.destination != null) {
                                    owner.virtualPosition = new Vector2Int(pmCmd.destination.gridX, pmCmd.destination.gridY);
                                }
                                playerFSM.HasReservedMove = true;
                                EventBus<DisableMoveButtonEvent>.Publish(new DisableMoveButtonEvent());
                            }
                            else if (cmd is AttackCommand) {
                                playerFSM.HasReservedAttack = true;
                                EventBus<DisableAttackButtonEvent>.Publish(new DisableAttackButtonEvent());
                            }
                        }
                    }
                }
            }
        }

        // 실패 시 복귀
        if (!droppedInTimeline) {
            targetElement.style.left = elementStartPos.x;
            targetElement.style.top = elementStartPos.y;
        }

        evt.StopPropagation();
    }

    // 💡 [수정] 우측 보정 오프셋 적용 틱 탐색 함수
    private int FindHoveredTickColumn(Vector2 pointerPos, VisualElement groupRoot) {
        if (TimeLineUI.Instance == null) return -1;

        VisualElement docRoot = TimeLineUI.Instance.GetRootVisualElement();
        if (docRoot == null) return -1;

        VisualElement timelineContainer = docRoot.Q<VisualElement>("TimelineContainer");
        if (timelineContainer == null) return -1;

        Rect timelineArea = timelineContainer.worldBound;
        Rect groupBound = groupRoot.worldBound;

        // Y축 높이 검사
        bool isNearTimelineY = (pointerPos.y >= timelineArea.yMin - 80f && pointerPos.y <= timelineArea.yMax + 80f)
                            || (groupBound.yMax >= timelineArea.yMin - 50f && groupBound.yMin <= timelineArea.yMax + 50f);

        if (!isNearTimelineY) return -1;

        // 💡 [우측 보정 적용] 첫 번째 박스 중심점 또는 마우스 포인터에 우측 오프셋을 보정
        VisualElement firstBox = groupRoot.Q<VisualElement>(className: "box-item");
        float checkX = (firstBox != null) ? firstBox.worldBound.center.x : pointerPos.x;

        // 1~8번 컬럼 순회
        for (int i = 1; i <= 8; i++) {
            VisualElement col = docRoot.Q<VisualElement>($"tick-col-{i}");
            if (col != null) {
                Rect colBound = col.worldBound;
                if (checkX >= colBound.xMin - 15f && checkX <= colBound.xMax + 15f) {
                    return i;
                }
            }
        }

        // 가장 가까운 컬럼으로 스냅
        int closestCol = -1;
        float minDistance = float.MaxValue;
        for (int i = 1; i <= 8; i++) {
            VisualElement col = docRoot.Q<VisualElement>($"tick-col-{i}");
            if (col != null) {
                float dist = Mathf.Abs(checkX - col.worldBound.center.x);
                if (dist < minDistance && dist < 65f) {
                    minDistance = dist;
                    closestCol = i;
                }
            }
        }

        return closestCol;
    }

    private Unit GetOwnerFromCommand(ICommand cmd) {
        if (cmd is MoveCommand m) return m.owner;
        if (cmd is PlayerMoveCommand pm) return pm.owner;
        if (cmd is AttackCommand a) return a.owner;
        return null;
    }
}