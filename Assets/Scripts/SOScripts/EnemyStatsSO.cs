using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStatsSO", menuName = "Scriptable Objects/UnitStats/EnemyStatsSO")]
public class EnemyStatsSO : UnitStatsSO
{
    public override UnitStats CreateStats() {
        return new EnemyStats(this);
    }
}
    

