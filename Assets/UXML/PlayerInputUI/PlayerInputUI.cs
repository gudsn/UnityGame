using UnityEngine;
using UnityEngine.UIElements;

public class PlayerInputUI : MonoBehaviour {
    public static PlayerInputUI Instance { get; private set; }

    [SerializeField] private UIDocument uiDocument;
    private VisualElement root;

    private void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument != null) root = uiDocument.rootVisualElement;
    }

    // --- [1. 이동 체인 생성 및 드래거 부착] ---
    public VisualElement CreateMovePreviewGroup(int tickCount) {
        if (TimeLineUI.Instance == null) return null;

        VisualElement groupElement = TimeLineUI.Instance.CreateCommandGroupUI("MoveCommand");
        TimeLineUI.Instance.PopulateTickBoxes(groupElement, "MoveCommand", tickCount);

        VisualElement timelineRoot = TimeLineUI.Instance.GetComponent<UIDocument>()?.rootVisualElement;
        new TimeLineDragger(groupElement, timelineRoot);

        return groupElement;
    }

    // --- [2. 공격 체인(2틱) 생성 및 드래거 부착] ---
    public VisualElement CreateAttackGroup() {
        if (TimeLineUI.Instance == null) return null;

        VisualElement groupElement = TimeLineUI.Instance.CreateCommandGroupUI("AttackCommand");
        TimeLineUI.Instance.PopulateTickBoxes(groupElement, "AttackCommand", 2);

        VisualElement timelineRoot = TimeLineUI.Instance.GetComponent<UIDocument>()?.rootVisualElement;
        new TimeLineDragger(groupElement, timelineRoot);

        return groupElement;
    }

    // --- [3. 실제 경로 길이에 맞춘 체인 갱신] ---
    public void UpdateMoveGroupTicks(VisualElement groupElement, int newTickCount) {
        if (TimeLineUI.Instance != null && groupElement != null) {
            TimeLineUI.Instance.PopulateTickBoxes(groupElement, "MoveCommand", newTickCount);
        }
    }
}