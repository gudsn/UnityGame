using System.Collections.Generic;
using UnityEngine;

public class PlayerMoveState : ITurnState {
    private PlayerFSM machine;
    private Unit activeUnit;

    private GameObject ghostInstance;
    private Vector3 ghostPosition;

    private int moveRange;

    private HashSet<TileData> validMoveTiles;

    public PlayerMoveState(PlayerFSM machine) {
        this.machine = machine;
        this.activeUnit = machine.activeUnit;
    }
    public void Enter() {
        moveRange = activeUnit.GetMoveRange();

        Debug.Log("Player Turn");
        PlayerInput.Instance.OnEnterTriggered += HandleConfirmMove;

        PlayerInput.Instance.OnLeftMouseClicked += HandleIntendedMove;

        SpawnGhost();

        // [변경] 가상 위치(virtualPosition) 세계 좌표를 기준으로 이동 그리드 생성
        Vector3 virtualWorldPos = GridSystem.Instance.GetTileData(activeUnit.virtualPosition).worldPosition;
        validMoveTiles = GridSystem.Instance.SpawnManhattanDistanceGrid(virtualWorldPos, moveRange, HighlightType.Move);
    }

    public void Execute() {

    }

    public void Exit() {
        PlayerInput.Instance.OnEnterTriggered -= HandleConfirmMove;

        PlayerInput.Instance.OnLeftMouseClicked -= HandleIntendedMove;
        Object.Destroy(ghostInstance);
        GridSystem.Instance.DeleteManhattanDistanceGrid();
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

        if (ghostTile == null) {
            return;
        }

        Vector3 intendedMovement = ghostTile.worldPosition;

        if (!ghostTile.isWalkable || ghostTile.isOccupied) {
            return;
        }
        if (!validMoveTiles.Contains(ghostTile)) {
            return;
        }

        ghostInstance.transform.LookAt(intendedMovement);
        ghostPosition = intendedMovement;
        ghostInstance.transform.position = ghostPosition;
    }

    public void HandleConfirmMove() {
        TileData targetTile = GridSystem.Instance.WorldPositionToGridTile(ghostInstance.transform.position);
        if (targetTile == null) return;

        PlayerMoveCommand playerMoveCmd = new PlayerMoveCommand(activeUnit, targetTile);

        AIDecision playerDecision = new AIDecision {
            utilityScore = 100f,
            intendedCommands = new List<ICommand> { playerMoveCmd }
        };

        TimeLineManager.Instance.ScheduleAction(activeUnit, playerDecision);

        // [컴파일 에러 해결] 추가된 virtualPosition 프로퍼티의 값을 변경하여 다음 공격 단계에 전달합니다.
        activeUnit.virtualPosition = new Vector2Int(targetTile.gridX, targetTile.gridY);

        EventBus<DisableMoveButtonEvent>.Publish(new DisableMoveButtonEvent());

        machine.ChangeState(null);
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