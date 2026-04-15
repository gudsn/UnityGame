using UnityEngine;
using UnityEngine.Rendering;
public class UnitStats
{

    //Health
    public float maxHealth { get; private set; }
    public float CurrentHealth {get; private set;}
    
    public float ModifyHealth(float amount) {
        CurrentHealth += amount;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);
        return CurrentHealth;
    }

    // Magic
    public float maxMagicPoint { get; private set; }
    public float CurrentMagicPoint { get; private set; }
    public float ModifyMagicPoint(float amount) {
        CurrentMagicPoint += amount;
        CurrentMagicPoint = Mathf.Clamp(CurrentMagicPoint, 0, maxMagicPoint);
        return CurrentMagicPoint;
    }

    // Attack
    private float baseAttack;
    public float CurrentAttack { get; private set; }

    // Defense
    private float baseDefense;
    public float CurrentDefense { get; private set; }

    public UnitStats(StateSO baseState) {
        this.maxHealth = baseState.maxHealthPoint;
        this.maxMagicPoint = baseState.maxMagicPoint;
        this.baseAttack = baseState.baseAttackPoint;
        this.baseDefense = baseState.baseDefensePoint;

        this.CurrentHealth = this.maxHealth;
        this.CurrentMagicPoint = this.maxMagicPoint;
        this.CurrentAttack = this.baseAttack;
        this.CurrentDefense = this.baseDefense;
    }
}
