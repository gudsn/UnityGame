using System;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerActionsController {
    private VisualElement playerActionsRoot;
    private Button attack_btn;
    private Button move_btn;
    private Button orb_btn;
    private Button tools_btn;
    private Button next_btn;

    public PlayerActionsController(VisualTreeAsset template, VisualElement canvas) {
        template.CloneTree(canvas);

        playerActionsRoot = canvas.Q<VisualElement>("PlayerActionRoot");
        playerActionsRoot?.BringToFront();

        attack_btn = playerActionsRoot.Q<Button>("Attack_btn");
        move_btn = playerActionsRoot.Q<Button>("Move_btn");
        orb_btn = playerActionsRoot.Q<Button>("Orb_btn");
        tools_btn = playerActionsRoot.Q<Button>("Tools_btn");
        next_btn = playerActionsRoot.Q<Button>("Next_btn");

        // 클릭 이벤트
        attack_btn?.RegisterCallback<ClickEvent>(evt => {
            EventBus<UIActionAttackEvent>.Publish(new UIActionAttackEvent());
            evt.StopPropagation();
        });

        move_btn?.RegisterCallback<ClickEvent>(evt => {
            EventBus<UIActionMoveEvent>.Publish(new UIActionMoveEvent());
            evt.StopPropagation();
        });

        next_btn?.RegisterCallback<ClickEvent>(evt => {
            EventBus<UIActionNextEvent>.Publish(new UIActionNextEvent());
            evt.StopPropagation();
        });

        // 💡 [핵심] UI 표시 이벤트 수신 시 강제 전체 리셋
        EventBus<ShowPlayerActionsEvent>.Subscribe(evt => {
            Show();
            ResetButtons();
        });

        EventBus<HidePlayerActionsEvent>.Subscribe(evt => Hide());
        EventBus<DisableAttackButtonEvent>.Subscribe(evt => DisableAttackButton());
        EventBus<DisableMoveButtonEvent>.Subscribe(evt => DisableMoveButton());
    }

    public void Hide() {
        if (playerActionsRoot != null) {
            playerActionsRoot.style.display = DisplayStyle.None;
        }
    }

    public void Show() {
        if (playerActionsRoot != null) {
            playerActionsRoot.style.display = DisplayStyle.Flex;
            playerActionsRoot.SetEnabled(true); // 💡 루트 컨테이너 활성화 강제 복구
            playerActionsRoot.BringToFront();
        }
    }

    public void DisableAttackButton() {
        if (attack_btn != null) attack_btn.SetEnabled(false);
    }

    public void DisableMoveButton() {
        if (move_btn != null) move_btn.SetEnabled(false);
    }

    // 💡 [핵심] 모든 액션 버튼 상태를 활성화(true)로 리셋
    public void ResetButtons() {
        if (playerActionsRoot != null) playerActionsRoot.SetEnabled(true);
        if (attack_btn != null) attack_btn.SetEnabled(true);
        if (move_btn != null) move_btn.SetEnabled(true);
        if (orb_btn != null) orb_btn.SetEnabled(true);
        if (tools_btn != null) tools_btn.SetEnabled(true);
        if (next_btn != null) next_btn.SetEnabled(true);
    }
}