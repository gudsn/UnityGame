using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour {

    [SerializeField]private UIDocument uiDocument;
    [SerializeField]private VisualTreeAsset hpBarTempelete;
    [SerializeField]private VisualTreeAsset tooltipTemplate;
    [SerializeField] private VisualTreeAsset playerActionsTemplate;

    private Camera mainCamera;

    private Dictionary<Unit, HealthBarController> registeredUnit;

    private TooltipController tooltipController;

    private PlayerActionsController playerActionsController;

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
    private void Start() {
        UnitManager.Instance.OnSpawnUnit += RegisterUnitUI;

        VisualElement canvas = uiDocument.rootVisualElement;

        tooltipController = new TooltipController(tooltipTemplate, canvas);

        playerActionsController = new PlayerActionsController(playerActionsTemplate, canvas);
    }

    private void OnEnable() {
        // Tooltip
        EventBus<ShowTooltipEvent>.Subscribe(OnShowTooltip);
        EventBus<HideTooltipEvent>.Subscribe(OnHideTooltip);
        // PlayerActionsController
        EventBus<DisableAttackButtonEvent>.Subscribe(DisableAttackButton);
        EventBus<DisableMoveButtonEvent>.Subscribe(DisableMoveButton);
        EventBus<ShowPlayerActionsEvent>.Subscribe(OnShowPlayerAction);
        EventBus<HidePlayerActionsEvent>.Subscribe(OnHidePlayerAction);
    }

    private void OnDisable() {
        // Tooltip
        EventBus<ShowTooltipEvent>.Unsubscribe(OnShowTooltip);
        EventBus<HideTooltipEvent>.Unsubscribe(OnHideTooltip);
        // PlayerActionsController
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

    // Tooltip
    private void OnShowTooltip(ShowTooltipEvent evt) { 
        tooltipController.Show(evt.targetUnit, evt.MousePosition);
    }

    private void OnHideTooltip(HideTooltipEvent evt) {
        tooltipController.Hide();
    }

    // PlayerActionsController
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
