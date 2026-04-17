using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStatsSO", menuName = "Scriptable Objects/UnitStats/PlayerStatsSo")]
public class PlayerStatsSO : UnitStatsSO{
    public float maxMagicPoint;
    public override UnitStats CreateStats() {
        return new PlayerStats(this, maxMagicPoint);
    }
}
