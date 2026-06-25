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
    public void Enter(){
        moveRange = activeUnit.GetMoveRange();

        Debug.Log("Player Turn");
        PlayerInput.Instance.OnMoveInputTriggered += HandleIntendedMove;
        PlayerInput.Instance.OnEnterTriggered += HandleConfirmMove;

        SpawnGhost();
        validMoveTiles =  GridSystem.Instance.SpawnManhattanDistanceGrid(activeUnit.transform.position, moveRange, HighlightType.Move);
    }

    public void Execute() {
    
    }

    public void Exit() {
        PlayerInput.Instance.OnMoveInputTriggered -= HandleIntendedMove;
        PlayerInput.Instance.OnEnterTriggered -= HandleConfirmMove;

        Object.Destroy(ghostInstance);
        GridSystem.Instance.DeleteManhattanDistanceGrid();
    }

    public void HandleIntendedMove(Vector2 direction) {
        Vector3 movement = new Vector3(direction.x, 0f, direction.y);

        Vector3 intendedMovement = ghostPosition + movement;

        TileData ghostTile = GridSystem.Instance.WorldPositionToGridTile(intendedMovement);
        if (ghostTile == null) {
            return;
        }
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
        activeUnit.transform.position = ghostInstance.transform.position;
        activeUnit.transform.forward = ghostInstance.transform.forward;

        TileData currentTile = GridSystem.Instance.WorldPositionToGridTile(activeUnit.transform.position);

        Vector2Int newPosition = new Vector2Int(currentTile.gridX, currentTile.gridY);

        UnitManager.Instance.MoveUnit(newPosition, activeUnit);

        EventBus<DisableMoveButtonEvent>.Publish(new DisableMoveButtonEvent());

        machine.ChangeState(null);
        //machine.ChangeState(new PlayerMoveState(machine));
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

