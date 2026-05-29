using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using System;

public class PlayerInput : MonoBehaviour {
    [SerializeField] HighlightedTilePool tilePool;

    private PlayerControl control;

    public event Action<Vector2> OnMoveInputTriggered;
    public event Action OnEnterTriggered;
    public event Action<Vector2> OnLeftMouseClicked;

    private TileData currentHoverTile = null;

    private Vector2 lastMousePos;

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
            OnLeftMouseClicked?.Invoke(mousePose);
        };

        control.Player.RightClick.performed += OnRightClickPerformed;
        control.Player.RightClick.canceled += OnRightClickCanceled;
    }

    // Update is called once per frame
    void Update() {
        Vector2 currentMousePosition = Mouse.current.position.ReadValue();

        if (currentMousePosition != lastMousePos) {
            lastMousePos = currentMousePosition;
            HanddleMouseHover(lastMousePos);
        }
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

    private void HanddleMouseHover(Vector2 mousePos) {
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        int layerMask = LayerMask.GetMask("Tile");

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask)) {

            // TileData is not MonoBehaviour
            TileData newHoverTile = GridSystem.Instance.WorldPositionToGridTile(hit.point);

            // GridSystem.WorldPositionToGridTile has an edge case
            if (newHoverTile == null) return;

            // No need to run more if mouse is in currentHoverTile area
            if (currentHoverTile == newHoverTile) return;

            if (currentHoverTile != null) {
                tilePool.ReturnHighLightTiles(HighlightType.Hover);
            }

            currentHoverTile = newHoverTile;
            Vector3 hoverTilePositon = new Vector3(currentHoverTile.worldPosition.x, 0.01f, currentHoverTile.worldPosition.z);

            tilePool.GetHighLightTile(HighlightType.Hover, hoverTilePositon);
        }
        else {
            if (currentHoverTile == null) return;

            tilePool.ReturnHighLightTiles(HighlightType.Hover);
            currentHoverTile = null;
        }
    }

    public void OnRightClickCanceled(InputAction.CallbackContext context) {
        EventBus<HideTooltipEvent>.Publish(new HideTooltipEvent());
    }
}
