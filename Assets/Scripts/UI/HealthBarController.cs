using UnityEngine;
using UnityEngine.UIElements;

public class HealthBarController {

    private ProgressBar hpBar;
    private VisualElement hpBarRoot;
    private VisualElement fillElement;
    private Faction targetFaction;

    public HealthBarController(VisualTreeAsset template, VisualElement canvas, IHealth entity, Faction targetFaction) {
        hpBarRoot = template.Instantiate();
        canvas.Add(hpBarRoot);

        hpBar = hpBarRoot.Q<ProgressBar>();
        this.targetFaction = targetFaction;

        if (hpBar != null) {
            fillElement = hpBar.Q(className: "unity-progress-bar__progress");
            Setup(entity.maxHealth, entity.CurrentHealth);
        }
    }

    private void Setup(float maxHealth, float currentHealth) {
        if (hpBar == null) return;
        hpBar.highValue = maxHealth;
        hpBar.lowValue = 0;
        hpBar.value = currentHealth;
        hpBar.title = $"HP: {hpBar.value}/{hpBar.highValue}";

        UpdateColor(currentHealth, maxHealth);
    }

    public void UpdateUI(float currentHealth) {
        if (hpBar == null) return;
        hpBar.value = currentHealth;
        hpBar.title = $"HP: {hpBar.value}/{hpBar.highValue}";

        UpdateColor(currentHealth, hpBar.highValue);
    }

    // 💡 [핵심 해결] Panel, Root, 카메라, Anchor 유효성을 철저히 검사합니다.
    public void UpdatePosition(Camera mainCam, Transform anchorTransform) {
        if (hpBarRoot == null || hpBarRoot.panel == null || mainCam == null || anchorTransform == null) {
            return;
        }

        // 월드 3D 좌표 -> UI Toolkit 패널 2D 좌표 변환
        Vector2 newTransformPosition = RuntimePanelUtils.CameraTransformWorldToPanel(hpBarRoot.panel, anchorTransform.position, mainCam);

        hpBarRoot.style.left = newTransformPosition.x;
        hpBarRoot.style.top = newTransformPosition.y;
    }

    private void UpdateColor(float currentHealth, float maxHealth) {
        if (fillElement == null || maxHealth <= 0) return;

        float healthRatio = currentHealth / maxHealth;
        Color barColor;

        if (healthRatio <= 0.3f) {
            // 체력이 30% 이하일 때 경고 색상 (붉은색)
            barColor = new Color(150f / 255f, 30f / 255f, 30f / 255f);
        }
        else {
            // 소속(Faction)에 따른 기본 색상
            if (targetFaction == Faction.Player) {
                barColor = new Color(118f / 255f, 218f / 255f, 46f / 255f);
            }
            else if (targetFaction == Faction.Enemy) {
                barColor = new Color(217f / 255f, 48f / 255f, 48f / 255f);
            }
            else {
                barColor = new Color(128f / 255f, 128f / 255f, 128f / 255f);
            }
        }

        fillElement.style.backgroundColor = barColor;
    }

    public void Cleanup() {
        hpBarRoot?.RemoveFromHierarchy();
    }
}