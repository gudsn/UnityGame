using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class PlayerMoveState : ITurnState {
    private PlayerFSM machine;
    private Unit activeUnit;

    private GameObject ghostInstance;
    private Vector3 ghostPosition;

    private int moveRange;
    private HashSet<TileData> validMoveTiles;

    private VisualElement moveGroupPreview;
    private bool isMoveConfirmed = false;

    public PlayerMoveState(PlayerFSM machine) {
        this.machine = machine;
        this.activeUnit = machine.activeUnit;
    }

    public void Enter() {
        moveRange = activeUnit.GetMoveRange();
        Debug.Log("[이동 페이즈] 이동할 타일을 클릭한 후 Enter 키를 눌러 예약하세요.");

        if (PlayerInputUI.Instance != null) {
            moveGroupPreview = PlayerInputUI.Instance.CreateMovePreviewGroup(moveRange);
        }

        PlayerInput.Instance.OnEnterTriggered += HandleConfirmMove;
        PlayerInput.Instance.OnLeftMouseClicked += HandleIntendedMove;

        SpawnGhost();

        Vector3 virtualWorldPos = GridSystem.Instance.GetTileData(activeUnit.virtualPosition).worldPosition;
        validMoveTiles = GridSystem.Instance.SpawnManhattanDistanceGrid(virtualWorldPos, moveRange, HighlightType.Move);
    }

    public void Execute() { }

    public void Exit() {
        PlayerInput.Instance.OnEnterTriggered -= HandleConfirmMove;
        PlayerInput.Instance.OnLeftMouseClicked -= HandleIntendedMove;

        if (!isMoveConfirmed && moveGroupPreview != null) {
            moveGroupPreview.RemoveFromHierarchy();
            moveGroupPreview = null;
        }

        if (ghostInstance != null) {
            Object.Destroy(ghostInstance);
        }

        GridSystem.Instance.DeleteManhattanDistanceGrid();

        EnemyController enemyController = Object.FindFirstObjectByType<EnemyController>();
        if (enemyController != null) {
            enemyController.RedrawCurrentEnemyIntents();
        }
    }

    public void HandleIntendedMove(Vector2 mousePos) {
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        int layerMask = LayerMask.GetMask("Tile", "Unit");
        TileData ghostTile = null;

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask)) {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Tile")) {
                ghostTile = GridSystem.Instance.WorldPositionToGridTile(hit.point);
            }
        }

        if (ghostTile == null) return;

        Vector3 intendedMovement = ghostTile.worldPosition;

        if (!ghostTile.isWalkable || ghostTile.isOccupied) return;
        if (!validMoveTiles.Contains(ghostTile)) return;

        ghostInstance.transform.LookAt(intendedMovement);
        ghostPosition = intendedMovement;
        ghostInstance.transform.position = ghostPosition;
    }

    public void HandleConfirmMove() {
        TileData targetTile = GridSystem.Instance.WorldPositionToGridTile(ghostInstance.transform.position);
        if (targetTile == null) return;

        PlayerMoveCommand playerMoveCmd = new PlayerMoveCommand(activeUnit, targetTile);

        if (PlayerInputUI.Instance != null && playerMoveCmd.path != null) {
            PlayerInputUI.Instance.UpdateMoveGroupTicks(moveGroupPreview, playerMoveCmd.path.Count);

            if (moveGroupPreview != null) moveGroupPreview.userData = playerMoveCmd;
        }

        isMoveConfirmed = true;

        activeUnit.virtualPosition = new Vector2Int(targetTile.gridX, targetTile.gridY);

        EventBus<DisableMoveButtonEvent>.Publish(new DisableMoveButtonEvent());
        machine.HasReservedMove = true;

        machine.ChangeState(new PlayerIdleState(machine));
    }

    private void SpawnGhost() {
        ghostInstance = Object.Instantiate(activeUnit.ghostPrefab, activeUnit.transform.position, Quaternion.identity);

        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        ghostInstance.layer = ignoreRaycastLayer;

        foreach (Transform child in ghostInstance.GetComponentsInChildren<Transform>()) {
            child.gameObject.layer = ignoreRaycastLayer;
        }

        Renderer ghostRenderer = ghostInstance.GetComponentInChildren<Renderer>();
        ghostPosition = activeUnit.transform.position;

        if (ghostRenderer != null) {
            Color newColor = ghostRenderer.material.color;
            newColor.a = 0.5f;
            ghostRenderer.material.color = newColor;
        }
        ghostInstance.SetActive(true);
    }
}