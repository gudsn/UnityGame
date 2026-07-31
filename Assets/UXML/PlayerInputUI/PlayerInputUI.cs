using UnityEngine;
using UnityEngine.UIElements;

public class PlayerInputUI : MonoBehaviour {
    public static PlayerInputUI Instance { get; private set; }

    private void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public VisualElement CreateMovePreviewGroup(int maxMoveRange) {
        if (TimeLineUI.Instance == null) return null;

        VisualElement group = TimeLineUI.Instance.CreateCommandGroupUI("MoveCommand");
        TimeLineUI.Instance.PopulateTickBoxes(group, "MoveCommand", maxMoveRange);

        // [수정됨] UIElementDragger 대신 TimeLineDragger 사용 및 PlayerTrack 전달
        new TimeLineDragger(group, TimeLineUI.Instance.PlayerTrack);

        return group;
    }

    public void UpdateMoveGroupTicks(VisualElement groupElement, int actualTickCount) {
        if (TimeLineUI.Instance == null || groupElement == null) return;
        TimeLineUI.Instance.PopulateTickBoxes(groupElement, "MoveCommand", actualTickCount);
    }

    public VisualElement CreateAttackGroup() {
        if (TimeLineUI.Instance == null) return null;

        VisualElement group = TimeLineUI.Instance.CreateCommandGroupUI("AttackCommand");
        TimeLineUI.Instance.PopulateTickBoxes(group, "AttackCommand", 2);

        // [수정됨] UIElementDragger 대신 TimeLineDragger 사용 및 PlayerTrack 전달
        new TimeLineDragger(group, TimeLineUI.Instance.PlayerTrack);

        return group;
    }

    public void ClearAllUI() {
        TimeLineUI.Instance?.ClearAll();
    }
}