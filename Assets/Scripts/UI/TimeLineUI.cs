using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class TimeLineUI : MonoBehaviour {
    public static TimeLineUI Instance { get; private set; }

    [Header("스프라이트 에셋")]
    [SerializeField] private Sprite emptyBoxSprite;
    [SerializeField] private Sprite moveBoxSprite;
    [SerializeField] private Sprite attackBoxSprite;

    public Sprite EmptyBoxSprite => emptyBoxSprite;
    public Sprite MoveBoxSprite => moveBoxSprite;
    public Sprite AttackBoxSprite => attackBoxSprite;

    [SerializeField] private UIDocument uiDocument;

    // UI 지연 탐색 프로퍼티
    private VisualElement _timelineRail;
    public VisualElement timelineRail {
        get {
            if (_timelineRail == null) {
                VisualElement root = GetRootVisualElement();
                if (root != null) _timelineRail = root.Q<VisualElement>("timeline-rail");
            }
            return _timelineRail;
        }
    }

    private ScrollView _timelineScrollView;
    public ScrollView timelineScrollView {
        get {
            if (_timelineScrollView == null) {
                VisualElement root = GetRootVisualElement();
                if (root != null) _timelineScrollView = root.Q<ScrollView>("timeline-scroll-view");
            }
            return _timelineScrollView;
        }
    }

    public VisualElement PlayerTrack => timelineRail;

    private Dictionary<(Unit, ICommand), List<VisualElement>> commandToBoxMap = new();
    private bool isScrollDraggerInitialized = false;

    private void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start() {
        CheckInitScrollDragger();
    }

    public VisualElement GetRootVisualElement() {
        if (uiDocument?.rootVisualElement != null) return uiDocument.rootVisualElement;
        if (UIManager.Instance != null) {
            var doc = UIManager.Instance.GetComponent<UIDocument>();
            if (doc != null) return doc.rootVisualElement;
        }
        return null;
    }

    private void CheckInitScrollDragger() {
        if (!isScrollDraggerInitialized && timelineScrollView != null) {
            new TimelineScrollDragger(timelineScrollView);
            isScrollDraggerInitialized = true;
        }
    }

    public void ResetAllEmptyNodes() {
        if (timelineRail == null) return;

        for (int i = 1; i <= 8; i++) {
            VisualElement emptyNode = timelineRail.Q<VisualElement>($"empty-node-{i}");
            if (emptyNode != null) {
                if (emptyBoxSprite != null) emptyNode.style.backgroundImage = new StyleBackground(emptyBoxSprite);
                emptyNode.style.display = DisplayStyle.Flex;
            }
        }
    }

    public void BuildEnemyTracks(List<Unit> enemies) => ResetAllEmptyNodes();

    // 적 행동 배치
    public void PlaceEnemyActionIntoSlot(string unitName, int tickIndex, string commandType) {
        if (tickIndex < 1 || tickIndex > 8 || timelineRail == null) return;

        VisualElement boxContainer = timelineRail.Q<VisualElement>($"box-container-{tickIndex}");
        if (boxContainer == null) return;

        VisualElement emptyNode = boxContainer.Q<VisualElement>($"empty-node-{tickIndex}");
        if (emptyNode != null) emptyNode.style.display = DisplayStyle.None;

        VisualElement actionBox = CreateActionBox(commandType, Faction.Enemy);
        boxContainer.Add(actionBox);
    }

    // 플레이어 행동 배치
    public VisualElement PlacePlayerActionIntoSlot(Unit owner, ICommand cmd, int tickIndex, string commandType) {
        if (tickIndex < 1 || tickIndex > 8 || timelineRail == null) return null;

        VisualElement boxContainer = timelineRail.Q<VisualElement>($"box-container-{tickIndex}");
        if (boxContainer == null) return null;

        VisualElement emptyNode = boxContainer.Q<VisualElement>($"empty-node-{tickIndex}");
        if (emptyNode != null) emptyNode.style.display = DisplayStyle.None;

        VisualElement actionBox = CreateActionBox(commandType, Faction.Player);
        actionBox.userData = cmd;

        actionBox.RegisterCallback<PointerDownEvent>(evt => {
            if (evt.button == 1) {
                CancelPlayerCommand(owner, cmd);
                evt.StopPropagation();
            }
        });

        boxContainer.Add(actionBox);

        var key = (owner, cmd);
        if (!commandToBoxMap.ContainsKey(key)) commandToBoxMap[key] = new List<VisualElement>();
        commandToBoxMap[key].Add(actionBox);

        return actionBox;
    }

    private VisualElement CreateActionBox(string commandType, Faction faction) {
        VisualElement box = new VisualElement();
        box.AddToClassList("box-item");
        box.AddToClassList(faction == Faction.Player ? "player-reserved" : "enemy-reserved");

        Sprite targetSprite = null;
        if (commandType == "Prepare") {
            targetSprite = emptyBoxSprite;
        }
        else if (commandType.Contains("Move")) {
            targetSprite = moveBoxSprite;
        }
        else {
            targetSprite = attackBoxSprite;
        }

        if (targetSprite != null) box.style.backgroundImage = new StyleBackground(targetSprite);

        return box;
    }

    // 예약 취소
    public void CancelPlayerCommand(Unit owner, ICommand cmd) {
        var key = (owner, cmd);
        if (!commandToBoxMap.TryGetValue(key, out var boxList)) return;

        foreach (var box in boxList) {
            VisualElement parent = box.parent;
            box.RemoveFromHierarchy();

            if (parent != null) {
                var remaining = parent.Query<VisualElement>(className: "box-item").ToList();
                if (remaining.Count == 0) {
                    var emptyNode = parent.Q<VisualElement>(className: "empty-node");
                    if (emptyNode != null) emptyNode.style.display = DisplayStyle.Flex;
                }
            }
        }
        commandToBoxMap.Remove(key);

        if (TimeLineManager.Instance != null) {
            TimeLineManager.Instance.CancelMacroCommand(owner, cmd);
        }

        PlayerFSM playerFSM = Object.FindFirstObjectByType<PlayerFSM>();
        if (playerFSM != null && playerFSM.activeUnit == owner) {
            if (cmd is PlayerMoveCommand || cmd is MoveCommand) {
                owner.virtualPosition = owner.currentPosition;
                playerFSM.HasReservedMove = false;
                EventBus<ShowPlayerActionsEvent>.Publish(new ShowPlayerActionsEvent());
            }
            else if (cmd is AttackCommand) {
                playerFSM.HasReservedAttack = false;
                EventBus<ShowPlayerActionsEvent>.Publish(new ShowPlayerActionsEvent());
            }
        }
    }

    public void ClearEnemyTrackSlots() {
        if (timelineRail == null) return;
        for (int i = 1; i <= 8; i++) {
            VisualElement container = timelineRail.Q<VisualElement>($"box-container-{i}");
            if (container == null) continue;

            var enemyBoxes = container.Query<VisualElement>(className: "enemy-reserved").ToList();
            foreach (var b in enemyBoxes) b.RemoveFromHierarchy();
            CheckAndRestoreEmptyNode(container, i);
        }
    }

    public void ClearPlayerTrackSlots() {
        if (timelineRail == null) return;
        for (int i = 1; i <= 8; i++) {
            VisualElement container = timelineRail.Q<VisualElement>($"box-container-{i}");
            if (container == null) continue;

            var playerBoxes = container.Query<VisualElement>(className: "player-reserved").ToList();
            foreach (var b in playerBoxes) b.RemoveFromHierarchy();
            CheckAndRestoreEmptyNode(container, i);
        }
        commandToBoxMap.Clear();
    }

    private void CheckAndRestoreEmptyNode(VisualElement container, int tickIndex) {
        var remaining = container.Query<VisualElement>(className: "box-item").ToList();
        if (remaining.Count == 0) {
            var emptyNode = container.Q<VisualElement>($"empty-node-{tickIndex}");
            if (emptyNode != null) emptyNode.style.display = DisplayStyle.Flex;
        }
    }

    public void ClearAll() {
        _timelineRail = null;
        _timelineScrollView = null;

        ClearEnemyTrackSlots();
        ClearPlayerTrackSlots();
        ResetAllEmptyNodes();
    }

    // 화면 좌측 상단 드래그 체인 루트 생성
    public VisualElement CreateCommandGroupUI(string commandType) {
        VisualElement rootCanvas = GetRootVisualElement();
        if (rootCanvas == null) return null;

        VisualElement groupRoot = new VisualElement();
        groupRoot.name = "drag-command-group";
        groupRoot.userData = commandType;

        groupRoot.style.position = Position.Absolute;
        groupRoot.style.top = 140;
        groupRoot.style.left = 30;
        groupRoot.style.flexDirection = FlexDirection.Row;
        groupRoot.style.alignItems = Align.Center;
        groupRoot.pickingMode = PickingMode.Position;

        VisualElement tickContainer = new VisualElement();
        tickContainer.name = "tick-container";
        tickContainer.AddToClassList("chain-container");

        groupRoot.Add(tickContainer);
        rootCanvas.Add(groupRoot);
        groupRoot.BringToFront();

        return groupRoot;
    }

    // 💡 [수정] 박스와 이음선(.box-divider)을 빈틈없이 맞물려 생성
    public void PopulateTickBoxes(VisualElement groupElement, string commandType, int tickCount) {
        if (groupElement == null) return;

        VisualElement tickContainer = groupElement.Q<VisualElement>("tick-container");
        if (tickContainer == null) return;
        tickContainer.Clear();

        for (int i = 0; i < tickCount; i++) {
            Sprite targetSprite = null;

            if (commandType.Contains("Attack")) {
                targetSprite = (i == 0) ? emptyBoxSprite : attackBoxSprite; // 1틱 대기(빈 박스), 2틱 타격(칼)
            }
            else if (commandType.Contains("Move")) {
                targetSprite = moveBoxSprite;
            }

            VisualElement box = new VisualElement();
            box.AddToClassList("box-item");
            box.pickingMode = PickingMode.Position;

            if (targetSprite != null) box.style.backgroundImage = new StyleBackground(targetSprite);

            tickContainer.Add(box);

            // 박스와 박스 사이에 딱 맞는 이음선 추가
            if (i < tickCount - 1) {
                VisualElement divider = new VisualElement();
                divider.AddToClassList("box-divider");
                tickContainer.Add(divider);
            }
        }
    }
}