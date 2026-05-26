using System;
using UnityEngine;
using UnityEngine.UIElements;
public class PlayerActionsController{
    private VisualElement playerActionsRoot;
    private Button attack_btn;
    private Button move_btn;
    private Button orb_btn;
    private Button tools_btn;
    private Button next_btn;


    public PlayerActionsController(VisualTreeAsset template, VisualElement canvas) {
        template.CloneTree(canvas);

        playerActionsRoot = canvas.Q<VisualElement>("PlayerActionRoot");

        attack_btn = playerActionsRoot.Q<Button>("Attack_btn");
        move_btn = playerActionsRoot.Q<Button>("Move_btn");
        orb_btn = playerActionsRoot.Q<Button>("Orb_btn");
        tools_btn = playerActionsRoot.Q<Button>("Tools_btn");
        next_btn = playerActionsRoot.Q<Button>("Next_btn");

        attack_btn.clicked += () => {
            EventBus<UIActionAttackEvent>.Publish(new UIActionAttackEvent());
            //attack_btn.SetEnabled(false);
        };

        move_btn.clicked += () => {
            EventBus<UIActionMoveEvent>.Publish(new UIActionMoveEvent());
        };

        next_btn.clicked += () => {
            EventBus<UIActionNextEvent>.Publish(new UIActionNextEvent());
        };

        //Hide();
    }
    public void Hide() {
        playerActionsRoot.style.display = DisplayStyle.None;
    }
    public void Show() {
        playerActionsRoot.style.display = DisplayStyle.Flex;
    }

    public void DisableAttackButton() {
        attack_btn.SetEnabled(false);
    }

    public void DisableMoveButton() {
        move_btn.SetEnabled(false);
    }


    public void ResetButtons() {
        attack_btn.SetEnabled(true);
        move_btn.SetEnabled(true);
    }

}
