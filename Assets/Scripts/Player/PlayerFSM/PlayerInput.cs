using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using System;

public class PlayerInput : MonoBehaviour {
    [SerializeField] HighlightTilePool tilePool;

    private PlayerControl control;

    public event Action<Vector2> OnMoveInputTriggered;
    public event Action OnEnterTriggered;
    public event Action<Vector2> OnLeftMouseClicked;

    private TileData currentHoverTile = null;

    private Vector2 lastMousePos;

    private Unit currentHoverUnit = null;
    private float tooltipTimer = 0f;
    private float tooltipDelay = 0.5f; 
    private bool isTooltipActive = false;

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

        if (currentHoverUnit != null && !isTooltipActive) {
            tooltipTimer += Time.deltaTime;
            if (tooltipTimer >= tooltipDelay) {

                isTooltipActive = true;

                EventBus<ShowTooltipEvent>.Publish(new ShowTooltipEvent {
                    targetUnit = currentHoverUnit,
                    MousePosition = currentMousePosition
                });
            }
        }

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

    }

    public void OnRightClickCanceled(InputAction.CallbackContext context) {

    }


    private void HanddleMouseHover(Vector2 mousePos) {
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        int layerMask = LayerMask.GetMask("Tile", "Unit");
        Unit newHoverUnit = null;
        TileData newHoverTile = null;

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask)) {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Unit")) {
                newHoverUnit = hit.collider.GetComponentInParent<Unit>();

                if (newHoverUnit != null) {
                    newHoverTile = GridSystem.Instance.GetTileData(newHoverUnit.currentPosition);
                }
            }
            else {
                // TileData is not MonoBehaviour
                newHoverTile = GridSystem.Instance.WorldPositionToGridTile(hit.point);
            }
        }

        UpdateTileHighlight(newHoverTile);
        UpdateUnitTooltipState(newHoverUnit);
    }

    private void ResetTooltip() {
        if (tooltipTimer >= tooltipDelay || isTooltipActive) {
            EventBus<HideTooltipEvent>.Publish(new HideTooltipEvent());
        }
        tooltipTimer = 0;
        isTooltipActive = false;
    }
    private void UpdateTileHighlight(TileData newHoverTile) {
        if (currentHoverTile == newHoverTile) return;

        if (currentHoverTile!= null) {
            tilePool.ReturnHighLightTiles(HighlightType.Hover);
        }

        currentHoverTile = newHoverTile;

        if (newHoverTile != null) {
            Vector3 hoverTilePos = currentHoverTile.worldPosition + new Vector3(0, 0.01f, 0);
            tilePool.GetHighLightTile(HighlightType.Hover ,hoverTilePos);
        }
    }
    private void UpdateUnitTooltipState(Unit newHoverUnit) {
        if (currentHoverUnit == newHoverUnit) return;

        if (currentHoverUnit != null) {
            ResetTooltip();
        }

        currentHoverUnit = newHoverUnit;
    }
}
