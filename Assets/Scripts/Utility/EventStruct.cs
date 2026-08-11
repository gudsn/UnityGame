using UnityEngine;

// 툴팁 활성화 이벤트
public struct ShowTooltipEvent {
    public Unit targetUnit;
    public Vector2 MousePosition;
}

// 툴팁 비활성화 이벤트
public struct HideTooltipEvent { }

// 플레이어 액션 UI 표시 이벤트
public struct ShowPlayerActionsEvent { }

// 플레이어 액션 UI 숨김 이벤트
public struct HidePlayerActionsEvent { }

// UI 공격 버튼 클릭 이벤트
public struct UIActionAttackEvent { }

// UI 이동 버튼 클릭 이벤트
public struct UIActionMoveEvent { }

// UI 다음 턴 버튼 클릭 이벤트
public struct UIActionNextEvent { }

// UI 공격 버튼 비활성화 이벤트
public struct DisableAttackButtonEvent { }

// UI 이동 버튼 비활성화 이벤트
public struct DisableMoveButtonEvent { }

// 라운드 UI 갱신 이벤트
public struct UpdateRoundEvent {
    public int currentRound;
    public UpdateRoundEvent(int round) {
        this.currentRound = round;
    }
}