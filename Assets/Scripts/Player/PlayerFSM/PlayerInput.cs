using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using System;

public class PlayerInput : MonoBehaviour {
    private PlayerControl control;

    public event Action<Vector2> OnMoveInputTriggered;
    public event Action OnEnterTriggered;
    public event Action<Vector2> OnRightMouseClicked;

    public static PlayerInput Instance { get; private set;}

    void Awake() {

        if (Instance!= null) {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        control = new PlayerControl();

        control.Player.Movement.performed += ctx => OnMoveInputTriggered?.Invoke(ctx.ReadValue<Vector2>());
        
        control.Player.Enter.performed += ctx => OnEnterTriggered?.Invoke();
        
        control.Player.LeftClick.performed += ctx => {
            Vector2 mousePose = Mouse.current.position.ReadValue();
            OnRightMouseClicked?.Invoke(mousePose);
        };

        control.Player.RightClick.performed += OnRightClickPerformed;
        control.Player.RightClick.canceled += OnRightClickCanceled;
    }

    // Update is called once per frame
    void Update() {
    }

    public void OnEnable() {
        control.Player.Enable();
    }
    public void OnDisable() {
        control.Player.Disable();
    }

    private void OnRightClickPerformed(InputAction.CallbackContext context) {
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Ray ray = Camera.main.ScreenPointToRay(mousePosition);

        int layerMask = LayerMask.GetMask("Unit");

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask)) {
            Unit checkUnit = hit.collider.GetComponentInParent<Unit>();

            if (checkUnit != null) {
                EventBus<ShowTooltipEvent>.Publish(new ShowTooltipEvent {
                    targetUnit = checkUnit,
                    MousePosition = mousePosition
                });
            }
        }
    }

    public void OnRightClickCanceled(InputAction.CallbackContext context) {
        EventBus<HideTooltipEvent>.Publish(new HideTooltipEvent());
    }
}
