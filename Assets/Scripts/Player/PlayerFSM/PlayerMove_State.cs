using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerMoveState : ITurnState {
    private PlayerFSM machine;
    private Unit activeUnit;

    private GameObject ghostInstance;
    private Vector3 ghostPosition;

    private int moveRange;
    private HashSet<TileData> validMoveTiles;

    // UI 박스 그룹 참조 보관용
    private VisualElement moveGroupPreview;
    private bool isMoveConfirmed = false; // 이동 확정 여부 플래그

    public PlayerMoveState(PlayerFSM machine) {
        this.machine = machine;
        this.activeUnit = machine.activeUnit;
    }

    public void Enter() {
        moveRange = activeUnit.GetMoveRange();
        Debug.Log("[이동 페이즈] 이동할 타일을 클릭한 후 Enter 키를 눌러 예약하세요.");

        // 1. Move 상태 진입 시 최대 이동 범위만큼 UI 틱 박스 그룹 프리뷰 생성
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

        // 행동을 확정하지 않고 이탈 시 프리뷰 UI 박스 제거
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

        // 2. 이동 확정 시 실제 A* 경로 타일 수만큼 UI 틱 박스 수 조정
        if (PlayerInputUI.Instance != null && playerMoveCmd.path != null) {
            PlayerInputUI.Instance.UpdateMoveGroupTicks(moveGroupPreview, playerMoveCmd.path.Count);
        }

        isMoveConfirmed = true; // 이동 확정 플래그 설정

        TimeLineManager.Instance.ScheduleAction(activeUnit, playerMoveCmd);

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