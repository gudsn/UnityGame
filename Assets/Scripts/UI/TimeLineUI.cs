using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class TimeLineUI : MonoBehaviour {
    public static TimeLineUI Instance { get; private set; }

    [Header("UI 아이콘 에셋 (Sprite Single 모드)")]
    [SerializeField] private Sprite moveIcon;
    [SerializeField] private Sprite attackIcon;

    [SerializeField] private UIDocument uiDocument;

    // 기존 플레이어 예약 박스가 생성될 컨테이너
    private VisualElement boxChainContainer;

    // [Step 2 추가] 하단 타임라인의 적군 트랙 컨테이너
    private VisualElement enemyTracksContainer;

    private void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitUI();
    }

    // [수정] 기존 UI와 하단 타임라인 UI를 모두 바인딩합니다.
    private void InitUI() {
        // 1. 기존 시스템용 컨테이너 바인딩
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (uiDocument != null && uiDocument.rootVisualElement != null) {
            boxChainContainer = uiDocument.rootVisualElement.Q<VisualElement>("box-chain-container");
        }

        // 2. [Step 2 추가] UIManager 관할의 하단 타임라인 바 바인딩
        if (UIManager.Instance != null) {
            UIDocument uiManagerDoc = UIManager.Instance.GetComponent<UIDocument>();
            if (uiManagerDoc != null && uiManagerDoc.rootVisualElement != null) {
                enemyTracksContainer = uiManagerDoc.rootVisualElement.Q<VisualElement>("enemy-tracks-container");
            }
        }
    }

    // ==========================================================
    // [Step 2 추가 로직] 적군 트랙 생성 (속도 내림차순 정렬)
    // ==========================================================
    public void BuildEnemyTracks(List<Unit> enemies) {
        if (enemyTracksContainer == null) InitUI();
        if (enemyTracksContainer == null) return;

        enemyTracksContainer.Clear(); // 이전 라운드 트랙 초기화

        // 속도(unitSpeed) 기준 내림차순 정렬
        var sortedEnemies = enemies.OrderByDescending(e => e.unitSpeed).ToList();

        foreach (var enemy in sortedEnemies) {
            VisualElement trackRow = CreateTrackRow(enemy.GetName());
            enemyTracksContainer.Add(trackRow);
        }
    }

    // 가로 한 줄(헤더 + 8틱 슬롯) UI 생성 헬퍼
    private VisualElement CreateTrackRow(string unitName) {
        VisualElement row = new VisualElement();
        row.AddToClassList("track-row");

        // 헤더 (이름)
        VisualElement header = new VisualElement();
        header.AddToClassList("track-header");
        Label nameLabel = new Label(unitName);
        nameLabel.AddToClassList("track-header-label");
        header.Add(nameLabel);
        row.Add(header);

        // 8개의 빈 슬롯
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

    // ==========================================================
    // [원본 유지] 아래는 기존 시스템 코드입니다. (변경 사항 없음)
    // ==========================================================

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

    // USS 클래스를 입혀 단일 박스 생성 (스타일은 USS에서 통제)
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

    // 모든 UI 박스 제거
    public void ClearAll() {
        boxChainContainer?.Clear();
    }
}