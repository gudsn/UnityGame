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

    public virtual float GetHealth() {
        return CurrentHealth;
    }

    // Attack
    private float baseAttack;
    public float CurrentAttack { get; private set; }

    public virtual float GetAttackPower() {
        return CurrentAttack; 
    }

    // Defense
    private float baseDefense;
    public float CurrentDefense { get; private set; }

    public virtual float GetDefensePower() {
        return CurrentDefense;
    }

    // Speed
    private int baseSpeed;
    public int CurrentSpeed { get; private set;}
    public virtual int GetUnitSpeed() {
        return CurrentSpeed;
    }

    // Faction
    public Faction CurrentFaction { get; private set;}

    public virtual Faction GetUnitFaction() {
        return CurrentFaction;
    }

    public virtual void OnTurnStarted() { 
    
    }
    public UnitStats(UnitStatsSO baseState) {
        this.maxHealth = baseState.maxHealthPoint;
        this.baseAttack = baseState.baseAttackPoint;
        this.baseDefense = baseState.baseDefensePoint;

        this.CurrentFaction = baseState.defaultFaction;

        this.baseSpeed = baseState.unitSpeed;

        this.CurrentHealth = this.maxHealth;
        this.CurrentAttack = this.baseAttack;
        this.CurrentDefense = this.baseDefense;

        this.CurrentSpeed = this.baseSpeed;
    }
}
