using UnityEngine;

[CreateAssetMenu(fileName = "TileSO", menuName = "Scriptable Objects/TileSO")]
public class TileSO : ScriptableObject{
    public string tileName;
    public int numberOfTilesToSpawnX;
    public int numberOfTilesToSpawnY;
}
