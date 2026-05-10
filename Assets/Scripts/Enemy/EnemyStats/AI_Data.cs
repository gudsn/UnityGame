using UnityEngine;

public enum AIActionType {
    Attack,
    Wait
}

public class AIDecision {
    public AIActionType actionType;
    public TileData destinationTile;
    public Unit targetUnit;
    public float utilityScore;
}
