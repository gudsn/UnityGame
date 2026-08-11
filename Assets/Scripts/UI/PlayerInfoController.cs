using UnityEngine;
using UnityEngine.UIElements;

public class PlayerInfoController {
    private VisualElement rootContainer;
    private Image portraitImage;

    private VisualElement hpBarFill;
    private VisualElement mpBarFill;
    private Label hpText;
    private Label mpText;

    private Label atkValueText;
    private Label defValueText;
    private Label intValueText;
    private Label spdValueText;

    public PlayerInfoController(VisualTreeAsset template, VisualElement canvas, Sprite portraitSprite, Sprite hpBarSprite, Sprite mpBarSprite, Sprite bgSprite = null) {
        if (template == null || canvas == null) return;

        // 1. UXML 인스턴스화 및 Canvas 바인딩
        VisualElement instance = template.Instantiate();
        canvas.Add(instance);

        rootContainer = instance.Q<VisualElement>("player-info-container");
        if (rootContainer == null) return;

        // 2. 초상화 설정
        portraitImage = instance.Q<Image>("portrait-image");
        if (portraitImage != null && portraitSprite != null) {
            portraitImage.sprite = portraitSprite;
        }

        // 3. 게이지 배경/채움 요소 찾기
        VisualElement hpBarBg = instance.Q<VisualElement>("hp-bar-bg");
        VisualElement mpBarBg = instance.Q<VisualElement>("mp-bar-bg");
        hpBarFill = instance.Q<VisualElement>("hp-bar-fill");
        mpBarFill = instance.Q<VisualElement>("mp-bar-fill");

        hpText = instance.Q<Label>("hp-text");
        mpText = instance.Q<Label>("mp-text");

        // 4. 이미지 스프라이트 적용
        if (hpBarBg != null && bgSprite != null) hpBarBg.style.backgroundImage = new StyleBackground(bgSprite);
        if (mpBarBg != null && bgSprite != null) mpBarBg.style.backgroundImage = new StyleBackground(bgSprite);

        if (hpBarFill != null && hpBarSprite != null) hpBarFill.style.backgroundImage = new StyleBackground(hpBarSprite);
        if (mpBarFill != null && mpBarSprite != null) mpBarFill.style.backgroundImage = new StyleBackground(mpBarSprite);

        // 5. 스텟 라벨 바인딩
        atkValueText = instance.Q<Label>("atk-value");
        defValueText = instance.Q<Label>("def-value");
        intValueText = instance.Q<Label>("int-value");
        spdValueText = instance.Q<Label>("spd-value");

        UpdateHP(10, 10);
        UpdateMP(5, 5);
    }

    // 유닛 데이터를 UI 요소에 동기화
    public void BindUnit(Unit unit) {
        if (unit == null || unit.stats == null) return;

        UnitStats stats = unit.stats;

        UpdateHP(stats.GetHealth(), stats.GetMaxHealth());

        if (stats is PlayerStats playerStats) {
            UpdateMP(playerStats.CurrentMagicPoint, playerStats.maxMagicPoint);
        }
        else {
            UpdateMP(0, 0);
        }

        if (atkValueText != null) atkValueText.text = stats.GetAttackPower().ToString();
        if (defValueText != null) defValueText.text = stats.GetDefensePower().ToString();
        if (intValueText != null) intValueText.text = "~";
        if (spdValueText != null) spdValueText.text = unit.unitSpeed.ToString();
    }

    public void UpdateHP(float currentHp, float maxHp) {
        if (hpBarFill != null && maxHp > 0) {
            float pct = Mathf.Clamp01(currentHp / maxHp) * 100f;
            hpBarFill.style.width = Length.Percent(pct);
        }
        if (hpText != null) {
            hpText.text = $"{currentHp}/{maxHp}";
        }
    }

    public void UpdateMP(float currentMp, float maxMp) {
        if (mpBarFill != null) {
            if (maxMp > 0) {
                float pct = Mathf.Clamp01(currentMp / maxMp) * 100f;
                mpBarFill.style.width = Length.Percent(pct);
            }
            else {
                mpBarFill.style.width = Length.Percent(0);
            }
        }
        if (mpText != null) {
            mpText.text = maxMp > 0 ? $"{currentMp}/{maxMp}" : "-/-";
        }
    }

    public void Show() {
        if (rootContainer != null) {
            rootContainer.style.display = DisplayStyle.Flex;
        }
    }

    public void Hide() {
        if (rootContainer != null) {
            rootContainer.style.display = DisplayStyle.None;
        }
    }
}