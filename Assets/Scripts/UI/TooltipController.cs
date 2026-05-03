using UnityEngine;
using UnityEngine.UIElements;

public class TooltipController{
    
    private VisualElement tooltipRoot;

    private Label unitNameLabel;
    private Label factionLabel;
    private Label healthDataLabel;
    private Label attackDataLabel;
    private Label defenseDataLabel;
    private Label moveRangeDataLabel;

    public TooltipController(VisualTreeAsset template, VisualElement canvas) {
        tooltipRoot = template.Instantiate();
        canvas.Add(tooltipRoot);

        unitNameLabel = tooltipRoot.Q<Label>("UnitName");
        factionLabel = tooltipRoot.Q<Label>("Faction");
        healthDataLabel = tooltipRoot.Q<Label>("HealthData");
        attackDataLabel = tooltipRoot.Q<Label>("AttackData");
        defenseDataLabel = tooltipRoot.Q<Label>("DefenseData");
        moveRangeDataLabel = tooltipRoot.Q<Label>("MoveRangeData");

        Hide();
    }

    public void Show(Unit targetUnit, Vector2 mousePosition) {
        UpdateUI(targetUnit);
        UpdateUIPosition(mousePosition);
        tooltipRoot.style.display = DisplayStyle.Flex;
    }

    public void Hide() {
        tooltipRoot.style.display = DisplayStyle.None;
    }

    public void UpdateUI(Unit targetUnit) {
        unitNameLabel.text = targetUnit.GetName();
        factionLabel.text = targetUnit.unitFaction.ToString();

        healthDataLabel.text = $"{targetUnit.GetHealth()}/{targetUnit.GetMaxHealth()}";
        attackDataLabel.text = targetUnit.stats.GetAttackPower().ToString();
        defenseDataLabel.text = targetUnit.stats.GetDefensePower().ToString();
        moveRangeDataLabel.text = targetUnit.GetMoveRange().ToString();
    }

    private void UpdateUIPosition(Vector2 mousePosition) {
        Vector2 uiLocalPos = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
        float offsetX = 15f;
        float offsetY = 15f;

        float targetX = uiLocalPos.x + offsetX;
        float targetY = uiLocalPos.y + offsetY;

        float tooltipWidth = 180f;
        float tooltipHeight = 100f;

        if (targetX + tooltipWidth > Screen.width) {
            targetX = uiLocalPos.x - tooltipWidth - offsetX;
        }

        if (targetY + tooltipHeight > Screen.height) {
            targetY = uiLocalPos.y - tooltipHeight - offsetY;
        }

        tooltipRoot.style.left = targetX;
        tooltipRoot.style.top = targetY;
    }

    public void Cleanup() {
        tooltipRoot.RemoveFromHierarchy();
    }

}
