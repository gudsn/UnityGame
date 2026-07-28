using UnityEngine;
using UnityEngine.UIElements;

public class PlayerInputUI : MonoBehaviour {
    public static PlayerInputUI Instance { get; private set; }

    private void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 이동 프리뷰 생성
    public VisualElement CreateMovePreviewGroup(int maxMoveRange) {
        if (TimeLineUI.Instance == null) return null;

        VisualElement group = TimeLineUI.Instance.CreateCommandGroupUI("MoveCommand");
        TimeLineUI.Instance.PopulateTickBoxes(group, "MoveCommand", maxMoveRange);
        new UIElementDragger(group);

        return group;
    }

    // 이동 확정 시 경로 길이만큼 박스 갱신
    public void UpdateMoveGroupTicks(VisualElement groupElement, int actualTickCount) {
        if (TimeLineUI.Instance == null || groupElement == null) return;

        TimeLineUI.Instance.PopulateTickBoxes(groupElement, "MoveCommand", actualTickCount);
    }

    // 공격 그룹 생성 (선딜1+타격1 총 2박스)
    public VisualElement CreateAttackGroup() {
        if (TimeLineUI.Instance == null) return null;

        VisualElement group = TimeLineUI.Instance.CreateCommandGroupUI("AttackCommand");
        TimeLineUI.Instance.PopulateTickBoxes(group, "AttackCommand", 2);
        new UIElementDragger(group);

        return group;
    }

    // 라운드 종료 시 박스 전체 삭제
    public void ClearAllUI() {
        TimeLineUI.Instance?.ClearAll();
    }
}