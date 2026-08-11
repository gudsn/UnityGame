using UnityEngine;
using UnityEngine.UIElements;

public class TimelineScrollDragger {
    private readonly ScrollView scrollView;
    private bool isDragging;
    private Vector2 startMousePos;
    private Vector2 startScrollOffset;

    public TimelineScrollDragger(ScrollView targetScrollView) {
        this.scrollView = targetScrollView;

        scrollView.RegisterCallback<PointerDownEvent>(OnPointerDown);
        scrollView.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        scrollView.RegisterCallback<PointerUpEvent>(OnPointerUp);
        scrollView.RegisterCallback<PointerCaptureOutEvent>(evt => isDragging = false);
    }

    // 배경 클릭 시 드래그 시작
    private void OnPointerDown(PointerDownEvent evt) {
        if (evt.target is VisualElement el && el.ClassListContains("box-item")) return;

        isDragging = true;
        startMousePos = evt.position;
        startScrollOffset = scrollView.scrollOffset;
        scrollView.CapturePointer(evt.pointerId);
    }

    // 좌우 드래그 스크롤
    private void OnPointerMove(PointerMoveEvent evt) {
        if (!isDragging || !scrollView.HasPointerCapture(evt.pointerId)) return;

        Vector2 delta = (Vector2)evt.position - startMousePos;
        scrollView.scrollOffset = new Vector2(startScrollOffset.x - delta.x, startScrollOffset.y);
    }

    // 드래그 해제
    private void OnPointerUp(PointerUpEvent evt) {
        if (!isDragging) return;
        isDragging = false;
        if (scrollView.HasPointerCapture(evt.pointerId)) scrollView.ReleasePointer(evt.pointerId);
    }
}