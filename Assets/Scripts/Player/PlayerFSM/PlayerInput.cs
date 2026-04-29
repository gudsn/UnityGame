using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using System;

public class PlayerInput : MonoBehaviour {
    private PlayerControl control;

    public event Action<Vector2> OnMoveInputTriggered;
    public event Action OnSetMoveInputTriggered;
    public event Action<Vector2> OnMouseClicked;

    public static PlayerInput Instance { get; private set;}

    void Awake() {

        if (Instance!= null) {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        control = new PlayerControl();

        control.Player.Movement.performed += ctx => OnMoveInputTriggered?.Invoke(ctx.ReadValue<Vector2>());
        control.Player.SetMovement.performed += ctx => OnSetMoveInputTriggered?.Invoke();
        control.Player.Click.performed += ctx => {
            Vector2 mousePose = Mouse.current.position.ReadValue();
            OnMouseClicked?.Invoke(mousePose);
        };
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
}
