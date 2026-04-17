using UnityEngine;

public class EnemyStats : UnitStats {
    public EnemyStats(UnitStatsSO baseState) : base(baseState) { }

    public override float GetAttackPower() {
        return base.GetAttackPower();
    }
}
