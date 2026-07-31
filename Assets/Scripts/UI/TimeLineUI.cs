using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class TimeLineUI : MonoBehaviour {
    public static TimeLineUI Instance { get; private set; }

    [Header("UI 아이콘 에셋 (인스펙터 직접 할당)")]
    [SerializeField] private Sprite moveIcon;
    [SerializeField] private Sprite attackIcon;

    public Sprite MoveIcon => moveIcon;
    public Sprite AttackIcon => attackIcon;

    [SerializeField] private UIDocument uiDocument;

    private VisualElement boxChainContainer;
    private VisualElement enemyTracksContainer;
    public VisualElement PlayerTrack { get; private set; }

    private void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitUI();
    }

    private void InitUI() {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument != null && uiDocument.rootVisualElement != null) {
            boxChainContainer = uiDocument.rootVisualElement.Q<VisualElement>("box-chain-container");
        }

        if (UIManager.Instance != null) {
            UIDocument uiManagerDoc = UIManager.Instance.GetComponent<UIDocument>();
            if (uiManagerDoc != null && uiManagerDoc.rootVisualElement != null) {
                enemyTracksContainer = uiManagerDoc.rootVisualElement.Q<VisualElement>("enemy-tracks-container");
                PlayerTrack = uiManagerDoc.rootVisualElement.Q<VisualElement>("player-track");
            }
        }
    }

    public void BuildEnemyTracks(List<Unit> enemies) {
        if (enemyTracksContainer == null) InitUI();
        if (enemyTracksContainer == null) return;

        enemyTracksContainer.Clear();

        var sortedEnemies = enemies.OrderByDescending(e => e.unitSpeed).ToList();

        foreach (var enemy in sortedEnemies) {
            VisualElement trackRow = CreateTrackRow(enemy.GetName());
            enemyTracksContainer.Add(trackRow);
        }
    }

    private VisualElement CreateTrackRow(string unitName) {
        VisualElement row = new VisualElement();
        row.AddToClassList("track-row");

        VisualElement header = new VisualElement();
        header.AddToClassList("track-header");
        Label nameLabel = new Label(unitName);
        nameLabel.AddToClassList("track-header-label");
        header.Add(nameLabel);
        row.Add(header);

        for (int i = 1; i <= 8; i++) {
            VisualElement slot = new VisualElement();
            slot.name = $"slot-{i}";
            slot.AddToClassList("track-slot");

            Label tickLabel = new Label(i.ToString());
            tickLabel.AddToClassList("tick-label");
            slot.Add(tickLabel);

            row.Add(slot);
        }

        return row;
    }

    public void ClearEnemyTrackSlots() {
        if (enemyTracksContainer == null) return;
        foreach (var row in enemyTracksContainer.Query<VisualElement>(className: "track-row").ToList()) {
            for (int i = 1; i <= 8; i++) {
                VisualElement slot = row.Q<VisualElement>($"slot-{i}");
                if (slot != null) {
                    var boxes = slot.Query<VisualElement>(className: "box-item").ToList();
                    foreach (var box in boxes) {
                        box.RemoveFromHierarchy();
                    }
                }
            }
        }
    }

    // [추가] 플레이어 타임라인 슬롯(1~8)에 남아있는 예약 박스들을 일괄 제거
    public void ClearPlayerTrackSlots() {
        if (PlayerTrack == null) InitUI();
        if (PlayerTrack == null) return;

        for (int i = 1; i <= 8; i++) {
            VisualElement slot = PlayerTrack.Q<VisualElement>($"slot-{i}");
            if (slot != null) {
                var boxes = slot.Query<VisualElement>(className: "box-item").ToList();
                foreach (var box in boxes) {
                    box.RemoveFromHierarchy();
                }
            }
        }
    }

    public void PlaceEnemyActionIntoSlot(string unitName, int tickIndex, string commandType) {
        if (enemyTracksContainer == null) InitUI();
        if (enemyTracksContainer == null) return;

        foreach (var row in enemyTracksContainer.Query<VisualElement>(className: "track-row").ToList()) {
            Label nameLabel = row.Q<Label>(className: "track-header-label");
            if (nameLabel != null && nameLabel.text == unitName) {
                VisualElement slot = row.Q<VisualElement>($"slot-{tickIndex}");
                if (slot != null) {
                    VisualElement actionBox = new VisualElement();
                    actionBox.AddToClassList("box-item");

                    Sprite targetSprite = (commandType == "MoveCommand") ? moveIcon : attackIcon;
                    if (targetSprite != null) {
                        Image iconImage = new Image();
                        iconImage.sprite = targetSprite;
                        iconImage.AddToClassList("box-icon");
                        actionBox.Add(iconImage);
                    }

                    slot.Add(actionBox);
                }
                break;
            }
        }
    }

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

    public void PopulateTickBoxes(VisualElement groupElement, string commandType, int tickCount) {
        if (groupElement == null) return;

        VisualElement tickContainer = groupElement.Q<VisualElement>("tick-container");
        if (tickContainer == null) return;

        tickContainer.Clear();

        for (int i = 0; i < tickCount; i++) {
            Sprite targetSprite = null;

            if (commandType == "MoveCommand") {
                targetSprite = moveIcon;
            }
            else if (commandType == "AttackCommand") {
                if (i > 0) targetSprite = attackIcon;
            }

            VisualElement box = CreateSingleTickBox(targetSprite);
            tickContainer.Add(box);

            if (i < tickCount - 1) {
                tickContainer.Add(CreateDivider());
            }
        }
    }

    private VisualElement CreateSingleTickBox(Sprite iconSprite) {
        VisualElement box = new VisualElement();
        box.AddToClassList("box-item");

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

    public void ClearAll() {
        boxChainContainer?.Clear();
        ClearPlayerTrackSlots(); // 라운드 종료 시 플레이어 타임라인 슬롯도 함께 초기화
        if (enemyTracksContainer != null) {
            enemyTracksContainer.Clear();
        }
    }
}