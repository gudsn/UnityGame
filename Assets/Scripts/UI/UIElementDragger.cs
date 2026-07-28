using UnityEngine;
using UnityEngine.UIElements;

public class UIElementDragger {
    private readonly VisualElement targetElement;
    private bool isDragging;
    private Vector2 dragStartPosition;
    private Vector2 elementStartPosition;

    public UIElementDragger(VisualElement target) {
        targetElement = target;

        targetElement.RegisterCallback<PointerDownEvent>(OnPointerDown);
        targetElement.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        targetElement.RegisterCallback<PointerUpEvent>(OnPointerUp);
    }

    private void OnPointerDown(PointerDownEvent evt) {
        isDragging = true;
        dragStartPosition = evt.position;

        // resolvedStyle 대신 layout 위치값을 참조하여 두 번째 이상 UI의 좌표 점프 현상 방지
        elementStartPosition = new Vector2(
            float.IsNaN(targetElement.style.left.value.value) ? targetElement.layout.x : targetElement.style.left.value.value,
            float.IsNaN(targetElement.style.top.value.value) ? targetElement.layout.y : targetElement.style.top.value.value
        );

        targetElement.CapturePointer(evt.pointerId);
        targetElement.AddToClassList("is-dragging");
        evt.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent evt) {
        if (!isDragging || !targetElement.HasPointerCapture(evt.pointerId)) return;

        Vector2 delta = (Vector2)evt.position - dragStartPosition;

        // 드래그 마우스 이동량 반영
        targetElement.style.left = elementStartPosition.x + delta.x;
        targetElement.style.top = elementStartPosition.y + delta.y;

        evt.StopPropagation();
    }

    private void OnPointerUp(PointerUpEvent evt) {
        if (!isDragging) return;

        isDragging = false;
        targetElement.ReleasePointer(evt.pointerId);
        targetElement.RemoveFromClassList("is-dragging");
        evt.StopPropagation();
    }
}