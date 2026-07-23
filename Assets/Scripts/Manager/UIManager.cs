using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// 인게임 전반의 UI Toolkit 도큐먼트 바인딩 및 생명주기 관리 매니저
public class UIManager : MonoBehaviour {

    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset hpBarTempelete;
    [SerializeField] private VisualTreeAsset tooltipTemplate;
    [SerializeField] private VisualTreeAsset playerActionsTemplate;

    // [추가] 인스펙터에서 직접 타임라인 UXML을 연결하기 위한 에셋 슬롯 추가
    [SerializeField] private VisualTreeAsset timelineBarTemplate;

    private Camera mainCamera;
    private Dictionary<Unit, HealthBarController> registeredUnit;
    private TooltipController tooltipController;
    private PlayerActionsController playerActionsController;

    // [추가] 동적으로 제어될 타임라인 바 부모 컨테이너 노드 캐싱
    private VisualElement timelineContainer;

    public static UIManager Instance;

    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        mainCamera = Camera.main;
        registeredUnit = new Dictionary<Unit, HealthBarController>();
    }

    // 전장 게임 UI들을 동적으로 화면에 초기 배치 및 이벤트 수신 대기
    private void Start() {
        UnitManager.Instance.OnSpawnUnit += RegisterUnitUI;

        VisualElement canvas = uiDocument.rootVisualElement;

        tooltipController = new TooltipController(tooltipTemplate, canvas);
        playerActionsController = new PlayerActionsController(playerActionsTemplate, canvas);

        // [추가] 인스펙터에 할당된 타임라인 UXML을 복제하여 캔버스 하단에 자동 이식
        if (timelineBarTemplate != null) {
            timelineBarTemplate.CloneTree(canvas);
            timelineContainer = canvas.Q<VisualElement>("TimelineContainer");

            if (timelineContainer != null) {
                Debug.Log("<color=green>[UI 성공]</color> 하단 타임라인 바가 성공적으로 화면에 이식되었습니다.");
            }
        }
    }

    private void OnEnable() {
        EventBus<ShowTooltipEvent>.Subscribe(OnShowTooltip);
        EventBus<HideTooltipEvent>.Subscribe(OnHideTooltip);
        EventBus<DisableAttackButtonEvent>.Subscribe(DisableAttackButton);
        EventBus<DisableMoveButtonEvent>.Subscribe(DisableMoveButton);
        EventBus<ShowPlayerActionsEvent>.Subscribe(OnShowPlayerAction);
        EventBus<HidePlayerActionsEvent>.Subscribe(OnHidePlayerAction);
    }

    private void OnDisable() {
        EventBus<ShowTooltipEvent>.Unsubscribe(OnShowTooltip);
        EventBus<HideTooltipEvent>.Unsubscribe(OnHideTooltip);
        EventBus<DisableAttackButtonEvent>.Unsubscribe(DisableAttackButton);
        EventBus<DisableMoveButtonEvent>.Unsubscribe(DisableMoveButton);
        EventBus<ShowPlayerActionsEvent>.Unsubscribe(OnShowPlayerAction);
        EventBus<HidePlayerActionsEvent>.Unsubscribe(OnHidePlayerAction);
    }

    public void RegisterUnitUI(Unit unit) {
        if (unit.stats is IHealth health) {
            VisualElement canvas = uiDocument.rootVisualElement;

            HealthBarController currentHealthBarCtrl = new HealthBarController(hpBarTempelete, canvas, health, unit.unitFaction);
            health.OnHealthModified += currentHealthBarCtrl.UpdateUI;

            unit.OnUnitDie += UnregisterUnitUI;

            registeredUnit.Add(unit, currentHealthBarCtrl);
        }
    }

    public void UnregisterUnitUI(Unit unit) {
        if (registeredUnit.TryGetValue(unit, out HealthBarController currentHealthBarCtrl)) {
            registeredUnit.Remove(unit);
            if (unit.stats is IHealth health) {
                health.OnHealthModified -= currentHealthBarCtrl.UpdateUI;
            }
            unit.OnUnitDie -= UnregisterUnitUI;

            currentHealthBarCtrl.Cleanup();
        }
    }

    private void OnShowTooltip(ShowTooltipEvent evt) {
        tooltipController.Show(evt.targetUnit, evt.MousePosition);
    }

    private void OnHideTooltip(HideTooltipEvent evt) {
        tooltipController.Hide();
    }

    private void DisableAttackButton(DisableAttackButtonEvent evt) {
        playerActionsController.DisableAttackButton();
    }

    private void DisableMoveButton(DisableMoveButtonEvent evt) {
        playerActionsController.DisableMoveButton();
    }

    private void OnShowPlayerAction(ShowPlayerActionsEvent evt) {
        playerActionsController.ResetButtons();
        playerActionsController.Show();
    }

    private void OnHidePlayerAction(HidePlayerActionsEvent evt) {
        playerActionsController.Hide();
    }

    public void LateUpdate() {
        foreach (KeyValuePair<Unit, HealthBarController> it in registeredUnit) {
            Transform anchorTransform = it.Key.transform.Find("UI_Anchor");
            if (anchorTransform != null) {
                it.Value.UpdatePosition(mainCamera, anchorTransform);
            }
        }
    }
}