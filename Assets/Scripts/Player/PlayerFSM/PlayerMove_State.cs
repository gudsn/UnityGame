using UnityEngine;

public class PlayerMove_State : ITurnState {
    private PlayerFSM machine;
    private Unit activeUnit;

    private GameObject ghostInstance;
    private Vector3 ghostPosition;

    private int moveRange;

    public PlayerMove_State(PlayerFSM machine) {
        this.machine = machine;
        this.activeUnit = machine.activeUnit;
    }
    public void Enter(){
        moveRange = activeUnit.GetMoveRange();

        Debug.Log("Player Turn");
        PlayerInput.Instance.OnMoveInputTriggered += HandleIntendedMove;
        PlayerInput.Instance.OnEnterTriggered += HandleConfirmMove;

        SpwnGhost();
        GridSystem.Instance.SpawnManhattanDistanceGrid(activeUnit.transform.position, moveRange);
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
        if (!ghostTile.isWalkable) {
            return;
        }
        if (!GridSystem.Instance.checkHighlightedTile(ghostTile)) {
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

        //Next action after movement
        Debug.Log("Player Turn Over");
        machine.ChangeState(new PlayerAttack_State(machine));
    }

    private void SpwnGhost() {
        ghostInstance = Object.Instantiate(activeUnit.ghostPrefab, activeUnit.transform.position, Quaternion.identity);

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

