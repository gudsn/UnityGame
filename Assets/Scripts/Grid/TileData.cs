using UnityEngine;

public class TileData{
    public int gridX;
    public int gridY;
    public Vector3 worldPosition;
    public bool isWalkable;
    public TileData(int _gridX, int _gridY, Vector3 _worldPosition, bool _isWalkable){
        gridX = _gridX;
        gridY = _gridY;
        worldPosition = _worldPosition;
        isWalkable = _isWalkable;
    }
}
