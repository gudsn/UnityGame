using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class PlayerStats : UnitStats
{
    public PlayerStats(UnitStatsSO baseState, float magicPoint) : base(baseState) {
        this.maxMagicPoint = magicPoint;
        this.CurrentMagicPoint = this.maxMagicPoint;
    }

    // Magic
    public float maxMagicPoint { get; private set; }
    public float CurrentMagicPoint { get; private set; }
    public float ModifyMagicPoint(float amount) {
        CurrentMagicPoint += amount;
        CurrentMagicPoint = Mathf.Clamp(CurrentMagicPoint, 0, maxMagicPoint);
        return CurrentMagicPoint;
    }
    public override float GetAttackPower() {
        if (CurrentMagicPoint < 5) {
            Debug.Log("Not enough MP");
            return 0;
        }

        ModifyMagicPoint(-5);

        return base.CurrentAttack;
    }

    public override void OnTurnStarted() {
        ModifyMagicPoint(5);
        return;
    }
}
