using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class HealthBarController
{
    private ProgressBar hpBar;
    private VisualElement hpBarRoot;

    public HealthBarController(VisualTreeAsset template, VisualElement canvas, IHealth entity) {
        hpBarRoot = template.Instantiate();
        canvas.Add(hpBarRoot);

        hpBar = hpBarRoot.Q<ProgressBar>();

        Setup(entity.maxHealth, entity.CurrentHealth);
    }

    public void  Setup(float maxHealth, float currentHealth) {
        hpBar.highValue = maxHealth;
        hpBar.lowValue = 0;
        hpBar.value = currentHealth;
        hpBar.title = $"HP: {hpBar.value}/{hpBar.highValue}";
    }
    public void UpdateUI(float currentHealth) {
        hpBar.value = currentHealth;
        hpBar.title = $"HP: {hpBar.value}/{hpBar.highValue}";
    }
    public void UpdatePosition(Camera mainCam, Transform anchorTransform) {
        Vector2 newTransformPosition = RuntimePanelUtils.CameraTransformWorldToPanel(hpBarRoot.panel, anchorTransform.position, mainCam);

        hpBarRoot.style.left = newTransformPosition.x;
        hpBarRoot.style.top = newTransformPosition.y;
    }

    public void Cleanup() {
        hpBarRoot.RemoveFromHierarchy();
    }
}



