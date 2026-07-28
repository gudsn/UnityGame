using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class TimeLineUI : MonoBehaviour {
    public static TimeLineUI Instance { get; private set; }

    [Header("UI 아이콘 에셋 (Sprite Single 모드)")]
    [SerializeField] private Sprite moveIcon;
    [SerializeField] private Sprite attackIcon;

    [SerializeField] private UIDocument uiDocument;
    private VisualElement boxChainContainer;

    private void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitUI();
    }

    private void InitUI() {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        VisualElement root = uiDocument.rootVisualElement;
        if (root != null) {
            boxChainContainer = root.Q<VisualElement>("box-chain-container");
        }
    }

    // 명령 그룹 루프 틀 생성
    public VisualElement CreateCommandGroupUI(string commandType) {
        if (boxChainContainer == null) InitUI();

        VisualElement groupRoot = new VisualElement();
        groupRoot.AddToClassList("command-group");
        groupRoot.userData = commandType;

        VisualElement tickContainer = new VisualElement();
        tickContainer.name = "tick-container";
        tickContainer.AddToClassList("chain-container");

        groupRoot.Add(tickContainer);

        boxChainContainer?.Add(groupRoot);
        return groupRoot;
    }

    // 틱 단위 박스 생성 및 아이콘/빈 박스 데이터 세팅
    public void PopulateTickBoxes(VisualElement groupElement, string commandType, int tickCount) {
        if (groupElement == null) return;

        VisualElement tickContainer = groupElement.Q<VisualElement>("tick-container");
        if (tickContainer == null) return;

        tickContainer.Clear();

        for (int i = 0; i < tickCount; i++) {
            Sprite targetSprite = null;

            // 이동 명령: 모든 틱에 이동 아이콘 배치
            if (commandType == "MoveCommand") {
                targetSprite = moveIcon;
            }
            // 공격 명령: 1틱(대기)은 아이콘 없음(null), 2틱(실제 타격)에만 공격 아이콘 배치
            else if (commandType == "AttackCommand") {
                if (i > 0) {
                    targetSprite = attackIcon;
                }
            }

            VisualElement box = CreateSingleTickBox(targetSprite);
            tickContainer.Add(box);

            if (i < tickCount - 1) {
                tickContainer.Add(CreateDivider());
            }
        }
    }

    // USS 클래스를 입혀 단일 박스 생성 (스타일은 USS에서 통제)
    private VisualElement CreateSingleTickBox(Sprite iconSprite) {
        VisualElement box = new VisualElement();
        box.AddToClassList("box-item");

        // 아이콘 스프라이트가 존재하는 경우에만 Image 요소 추가
        if (iconSprite != null) {
            Image iconImage = new Image();
            iconImage.sprite = iconSprite;
            iconImage.AddToClassList("box-icon");

            box.Add(iconImage);
        }

        return box;
    }

    private VisualElement CreateDivider() {
        VisualElement divider = new VisualElement();
        divider.AddToClassList("box-divider");
        return divider;
    }

    // 모든 UI 박스 제거
    public void ClearAll() {
        boxChainContainer?.Clear();
    }
}