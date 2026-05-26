using UnityEngine;

public struct ShowTooltipEvent {
    public Unit targetUnit;
    public Vector2 MousePosition;
}

public struct HideTooltipEvent { }

public struct ShowPlayerActionsEvent { }

public struct HidePlayerActionsEvent { }

public struct UIActionAttackEvent { }
public struct UIActionMoveEvent { }

public struct UIActionNextEvent { }

public struct DisableAttackButtonEvent { }

public struct DisableMoveButtonEvent { }

