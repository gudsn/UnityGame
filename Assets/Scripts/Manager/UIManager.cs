using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour {

    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

    [Header("UXML Templates")]
    [SerializeField] private VisualTreeAsset hpBarTempelete;
    [SerializeField] private VisualTreeAsset tooltipTemplate;
    [SerializeField] private VisualTreeAsset playerActionsTemplate;
    [SerializeField] private VisualTreeAsset timelineBarTemplate;
    [SerializeField] private VisualTreeAsset roundUITemplate;

    [Header("플레이어 정보 UI 리소스")]
    [SerializeField] private VisualTreeAsset playerInfoTemplate;
    [SerializeField] private Sprite playerPortraitSprite;
    [SerializeField] private Sprite hpBarSprite;
    [SerializeField] private Sprite mpBarSprite;
    [SerializeField] private Sprite barBgSprite;

    private Camera mainCamera;
    private Dictionary<Unit, HealthBarController> registeredUnit;

    private TooltipController tooltipController;
    private PlayerActionsController playerActionsController;
    private PlayerInfoController playerInfoController;

    private VisualElement roundRoot;
    private Label roundNumberLabel;
    private VisualElement timelineContainer;

    public static UIManager Instance { get; private set; }

    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();

        mainCamera = Camera.main;
        registeredUnit = new Dictionary<Unit, HealthBarController>();
    }

    private void Start() {
        UnitManager.Instance.OnSpawnUnit += RegisterUnitUI;

        VisualElement canvas = uiDocument.rootVisualElement;
        if (canvas == null) return;

        // 서브 UI 컨트롤러 초기화
        tooltipController = new TooltipController(tooltipTemplate, canvas);
        playerActionsController = new PlayerActionsController(playerActionsTemplate, canvas);

        // 플레이어 정보 UI 초기화
        if (playerInfoTemplate != null) {
            playerInfoController = new PlayerInfoController(
                playerInfoTemplate,
                canvas,
                playerPortraitSprite,
                hpBarSprite,
                mpBarSprite,
                barBgSprite
            );
        }

        // 타임라인 UI 이식
        if (timelineBarTemplate != null) {
            timelineBarTemplate.CloneTree(canvas);
            timelineContainer = canvas.Q<VisualElement>("TimelineContainer");
        }

        InitRoundUI(canvas);
    }

    private void OnEnable() {
        EventBus<ShowTooltipEvent>.Subscribe(OnShowTooltip);
        EventBus<HideTooltipEvent>.Subscribe(OnHideTooltip);
        EventBus<DisableAttackButtonEvent>.Subscribe(DisableAttackButton);
        EventBus<DisableMoveButtonEvent>.Subscribe(DisableMoveButton);
        EventBus<ShowPlayerActionsEvent>.Subscribe(OnShowPlayerAction);
        EventBus<HidePlayerActionsEvent>.Subscribe(OnHidePlayerAction);
        EventBus<UpdateRoundEvent>.Subscribe(OnUpdateRound);
    }

    private void OnDisable() {
        EventBus<ShowTooltipEvent>.Unsubscribe(OnShowTooltip);
        EventBus<HideTooltipEvent>.Unsubscribe(OnHideTooltip);
        EventBus<DisableAttackButtonEvent>.Unsubscribe(DisableAttackButton);
        EventBus<DisableMoveButtonEvent>.Unsubscribe(DisableMoveButton);
        EventBus<ShowPlayerActionsEvent>.Unsubscribe(OnShowPlayerAction);
        EventBus<HidePlayerActionsEvent>.Unsubscribe(OnHidePlayerAction);
        EventBus<UpdateRoundEvent>.Unsubscribe(OnUpdateRound);
    }

    #region 라운드 UI
    private void InitRoundUI(VisualElement canvas) {
        if (roundUITemplate != null) {
            roundRoot = roundUITemplate.Instantiate();
            canvas.Add(roundRoot);
        }
        else {
            roundRoot = canvas.Q<VisualElement>("round-container");
        }

        if (roundRoot != null) {
            roundNumberLabel = roundRoot.Q<Label>("round-number-label");
        }

        if (FSMManager.Instance != null) {
            SetRoundText(FSMManager.Instance.CurrentRound);
        }
    }

    private void OnUpdateRound(UpdateRoundEvent evt) => SetRoundText(evt.currentRound);

    public void SetRoundText(int round) {
        if (roundNumberLabel != null) {
            roundNumberLabel.text = round > 0 ? round.ToString() : "1";
        }
    }
    #endregion

    #region 유닛 및 플레이어 UI 등록/해제
    public void RegisterUnitUI(Unit unit) {
        if (unit == null) return;

        if (registeredUnit.ContainsKey(unit)) {
            UnregisterUnitUI(unit);
        }

        if (unit.stats is IHealth health) {
            VisualElement canvas = uiDocument.rootVisualElement;
            if (canvas == null) return;

            // 1. 머리 위 체력바 바인딩
            HealthBarController headHealthBarCtrl = new HealthBarController(hpBarTempelete, canvas, health, unit.unitFaction);
            health.OnHealthModified += headHealthBarCtrl.UpdateUI;

            unit.OnUnitDie += UnregisterUnitUI;
            registeredUnit.Add(unit, headHealthBarCtrl);

            // 2. 플레이어 유닛일 경우 메인 UI 바인딩 및 이벤트 연결
            if (unit.unitFaction == Faction.Player && playerInfoController != null) {
                playerInfoController.BindUnit(unit);
                health.OnHealthModified += (currentHp) => playerInfoController.UpdateHP(currentHp, health.maxHealth);
            }
        }
    }

    public void UnregisterUnitUI(Unit unit) {
        if (unit == null) return;

        if (registeredUnit.TryGetValue(unit, out HealthBarController headHealthBarCtrl)) {
            registeredUnit.Remove(unit);

            if (unit.stats is IHealth health) {
                health.OnHealthModified -= headHealthBarCtrl.UpdateUI;
            }
            unit.OnUnitDie -= UnregisterUnitUI;

            headHealthBarCtrl?.Cleanup();
        }
    }

    private void OnShowTooltip(ShowTooltipEvent evt) => tooltipController?.Show(evt.targetUnit, evt.MousePosition);
    private void OnHideTooltip(HideTooltipEvent evt) => tooltipController?.Hide();
    private void DisableAttackButton(DisableAttackButtonEvent evt) => playerActionsController?.DisableAttackButton();
    private void DisableMoveButton(DisableMoveButtonEvent evt) => playerActionsController?.DisableMoveButton();

    private void OnShowPlayerAction(ShowPlayerActionsEvent evt) {
        playerActionsController?.ResetButtons();
        playerActionsController?.Show();
    }

    private void OnHidePlayerAction(HidePlayerActionsEvent evt) => playerActionsController?.Hide();

    // 머리 위 체력바 위치 갱신
    public void LateUpdate() {
        if (mainCamera == null) {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        var units = new List<Unit>(registeredUnit.Keys);

        foreach (Unit unit in units) {
            if (unit == null) {
                registeredUnit.Remove(unit);
                continue;
            }

            Transform anchorTransform = unit.transform.Find("UI_Anchor");
            if (anchorTransform != null && registeredUnit.TryGetValue(unit, out HealthBarController headCtrl)) {
                headCtrl?.UpdatePosition(mainCamera, anchorTransform);
            }
        }
    }
    #endregion
}