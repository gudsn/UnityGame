using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour {

    [SerializeField]private UIDocument uiDocument;
    [SerializeField]private VisualTreeAsset hpBarTempelete;
    [SerializeField]private VisualTreeAsset tooltipTemplate;

    private Camera mainCamera;

    private Dictionary<Unit, HealthBarController> registeredUnit;

    private TooltipController tooltipController;

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
    }

    private void OnEnable() {
        EventBus<ShowTooltipEvent>.Subscribe(OnShowTooltip);
        EventBus<HideTooltipEvent>.Subscribe(OnHideTooltip);
    }

    private void OnDisable() {
        EventBus<ShowTooltipEvent>.Unsubscribe(OnShowTooltip);
        EventBus<HideTooltipEvent>.Unsubscribe(OnHideTooltip);
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

    public void LateUpdate() {
        foreach (KeyValuePair<Unit, HealthBarController> it in registeredUnit) {
            Transform anchorTransform = it.Key.transform.Find("UI_Anchor");
            if (anchorTransform != null) {
                it.Value.UpdatePosition(mainCamera, anchorTransform);
            }
        }
    }
}
