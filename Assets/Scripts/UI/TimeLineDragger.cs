using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class TimeLineDragger {
    private readonly VisualElement targetElement;
    private readonly VisualElement playerTrack;

    private bool isDragging;
    private Vector2 dragStartPosition;
    private Vector2 elementStartPosition;

    public TimeLineDragger(VisualElement target, VisualElement track) {
        targetElement = target;
        playerTrack = track;

        targetElement.RegisterCallback<PointerDownEvent>(OnPointerDown);
        targetElement.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        targetElement.RegisterCallback<PointerUpEvent>(OnPointerUp);
    }

    private void OnPointerDown(PointerDownEvent evt) {
        isDragging = true;
        dragStartPosition = evt.position;

        float startLeft = float.IsNaN(targetElement.style.left.value.value) ? targetElement.layout.x : targetElement.style.left.value.value;
        float startTop = float.IsNaN(targetElement.style.top.value.value) ? targetElement.layout.y : targetElement.style.top.value.value;
        elementStartPosition = new Vector2(startLeft, startTop);

        targetElement.BringToFront();
        targetElement.CapturePointer(evt.pointerId);
        evt.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent evt) {
        if (!isDragging || !targetElement.HasPointerCapture(evt.pointerId)) return;

        Vector2 delta = (Vector2)evt.position - dragStartPosition;
        targetElement.style.left = elementStartPosition.x + delta.x;
        targetElement.style.top = elementStartPosition.y + delta.y;

        evt.StopPropagation();
    }

    private void OnPointerUp(PointerUpEvent evt) {
        if (!isDragging) return;

        isDragging = false;
        targetElement.ReleasePointer(evt.pointerId);

        bool droppedInTimeline = false;
        ICommand cmd = targetElement.userData as ICommand;

        if (cmd != null && playerTrack != null) {
            int boxTickCount = 1;
            if (cmd is AttackCommand) boxTickCount = 2;
            else if (cmd is MoveCommand m && m.path != null) boxTickCount = m.path.Count;
            else if (cmd is PlayerMoveCommand pm && pm.path != null) boxTickCount = pm.path.Count;

            for (int i = 1; i <= 8; i++) {
                VisualElement slot = playerTrack.Q<VisualElement>($"slot-{i}");
                if (slot != null && slot.worldBound.Contains(evt.position)) {

                    // 1. 틱 범위 초과 검사
                    if (i + boxTickCount - 1 > 8) {
                        Debug.Log("<color=yellow>[알림]</color> 타임라인 범위를 초과하여 배치할 수 없습니다!");
                        break;
                    }

                    // 2. 해당 슬롯들에 이미 예약된 박스가 있는지 중복 검사
                    bool hasOverlap = false;
                    for (int offset = 0; offset < boxTickCount; offset++) {
                        int targetSlotIndex = i + offset;
                        VisualElement checkSlot = playerTrack.Q<VisualElement>($"slot-{targetSlotIndex}");

                        // 슬롯 내부에 'command-group' 클래스를 가진 자식 요소가 존재하는지 확인
                        if (checkSlot != null) {
                            bool hasGroup = checkSlot.Children().Any(e => e.ClassListContains("command-group") && e != targetElement);
                            if (hasGroup) {
                                hasOverlap = true;
                                break;
                            }
                        }
                    }

                    if (hasOverlap) {
                        Debug.Log("<color=yellow>[알림]</color> 이미 해당 틱 범위에 다른 행동이 예약되어 있습니다!");
                        break;
                    }

                    droppedInTimeline = true;

                    // 첫 번째 슬롯 안으로 자식 편입 및 절대 좌표 정렬
                    slot.Add(targetElement);
                    targetElement.style.position = Position.Absolute;
                    targetElement.style.left = 0;
                    targetElement.style.top = 0;

                    // 드래그 이벤트 해제
                    targetElement.UnregisterCallback<PointerDownEvent>(OnPointerDown);
                    targetElement.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                    targetElement.UnregisterCallback<PointerUpEvent>(OnPointerUp);

                    // 스케줄 등록
                    Unit owner = GetOwnerFromCommand(cmd);
                    if (owner != null) {
                        TimeLineManager.Instance.ScheduleActionAtTick(owner, cmd, i);
                    }
                    break;
                }
            }
        }

        if (!droppedInTimeline) {
            targetElement.style.left = elementStartPosition.x;
            targetElement.style.top = elementStartPosition.y;

            if (cmd == null) {
                Debug.Log("<color=yellow>[안내]</color> 먼저 맵을 클릭하여 행동을 확정해주세요!");
            }
        }

        evt.StopPropagation();
    }

    private Unit GetOwnerFromCommand(ICommand cmd) {
        if (cmd is MoveCommand m) return m.owner;
        if (cmd is PlayerMoveCommand pm) return pm.owner;
        if (cmd is AttackCommand a) return a.owner;
        return null;
    }
}