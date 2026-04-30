using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class HealthBarController
{
    private ProgressBar hpBar;
    private VisualElement hpBarRoot;
    private VisualElement fillElement;

    private Faction targetFaction;

    public HealthBarController(VisualTreeAsset template, VisualElement canvas, IHealth entity, Faction targetFaction) {
        hpBarRoot = template.Instantiate();
        canvas.Add(hpBarRoot);

        hpBar = hpBarRoot.Q<ProgressBar>();

        this.targetFaction = targetFaction;

        fillElement = hpBar.Q(className: "unity-progress-bar__progress");

        Setup(entity.maxHealth, entity.CurrentHealth);
    }

    public void  Setup(float maxHealth, float currentHealth) {
        hpBar.highValue = maxHealth;
        hpBar.lowValue = 0;
        hpBar.value = currentHealth;
        hpBar.title = $"HP: {hpBar.value}/{hpBar.highValue}";

        UpdateColor(currentHealth, maxHealth);
    }
    public void UpdateUI(float currentHealth) {
        hpBar.value = currentHealth;
        hpBar.title = $"HP: {hpBar.value}/{hpBar.highValue}";

        UpdateColor(currentHealth, hpBar.highValue);
    }
    public void UpdatePosition(Camera mainCam, Transform anchorTransform) {
        Vector2 newTransformPosition = RuntimePanelUtils.CameraTransformWorldToPanel(hpBarRoot.panel, anchorTransform.position, mainCam);

        hpBarRoot.style.left = newTransformPosition.x;
        hpBarRoot.style.top = newTransformPosition.y;
    }
    private void UpdateColor(float currentHealth, float maxHealth) {
        float healthRatio = currentHealth / maxHealth;
        Color barColor;

        if (healthRatio <= 0.3f) {
            // 체력이 30% 이하일 때 경고 색상 (붉은색)
            barColor = new Color(150f / 255f, 30f / 255f, 30f / 255f);
        }
        else {
            // 소속(Faction)에 따른 기본 색상
            if (targetFaction == Faction.Player) {
                barColor = new Color(118f / 255f, 218f / 255f, 46f / 255f); // 아군: 푸른색
            }
            else if (targetFaction == Faction.Enemy) {
                barColor = new Color(217f / 255f, 48f / 255f, 48f / 255f); // 적군: 주황/붉은색
            }
            else {
                barColor = new Color(128f / 255f, 128f / 255f, 128f / 255f); // 기타 중립
            }
        }

        // C#에서는 상황에 맞게 색상만 찔러 넣어줍니다! (애니메이션은 USS가 처리함)
        fillElement.style.backgroundColor = barColor;
    }

    public void Cleanup() {
        hpBarRoot.RemoveFromHierarchy();
    }
}



