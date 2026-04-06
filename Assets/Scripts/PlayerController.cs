using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    private GameObject ghostInstance;

    private Vector3 movement = new Vector3();
    private Vector3 intendedMovement = new Vector3();
    private Vector3 ghostPosition = new Vector3();

    //private bool isHilighted = false;

    void Awake() {
    }

    // Update is called once per frame
    void Update() {
    }

 
    public void IntendedMove(Vector2 direction) {
        if (ghostInstance == null) {

            ghostInstance = Instantiate(playerPrefab, transform.position, Quaternion.identity);
            ghostPosition = transform.position;

            Renderer ghostRenderer = ghostInstance.GetComponent<Renderer>();
            if (ghostRenderer != null) {
                Color newColor = ghostRenderer.material.color;

                newColor.a = 0.5f;

                ghostRenderer.material.color = newColor;
            }
            ghostInstance.SetActive(false);
        }

        if (!ghostInstance.activeSelf) {
            ghostInstance.SetActive(true);
            ghostPosition = transform.position;
            GridSystem.Instance.SpawnManhattanDistanceGrid(transform.position, 2);
        }

        movement = new Vector3(direction.x, 0f, direction.y);

        intendedMovement = ghostPosition + movement;

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

        ghostPosition = intendedMovement;
        ghostInstance.transform.position = ghostPosition;
    }
    public void SetMove() {
        if (ghostInstance != null && ghostInstance.activeSelf) {
            ghostInstance.SetActive(false);
            transform.position = ghostInstance.transform.position;
            GridSystem.Instance.DeleteManhattanDistanceGrid();
        }
    }
}
