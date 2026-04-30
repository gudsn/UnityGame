using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour {

    [SerializeField]private UIDocument uiDocument;
    [SerializeField]private VisualTreeAsset hpBarTempelete;

    private Camera mainCamera;

    private Dictionary<Unit, HealthBarController> registeredUnit;

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

    public void LateUpdate() {
        foreach (KeyValuePair<Unit, HealthBarController> it in registeredUnit) {
            Transform anchorTransform = it.Key.transform.Find("UI_Anchor");
            if (anchorTransform != null) {
                it.Value.UpdatePosition(mainCamera, anchorTransform);
            }
        }
    }
}
