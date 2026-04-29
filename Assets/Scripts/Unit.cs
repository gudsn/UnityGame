using System;
using UnityEngine;

public class Unit : MonoBehaviour {

    [SerializeField] private UnitStatsSO baseState;
    [SerializeField] public GameObject ghostPrefab;

    public UnitStats stats { get; private set;}
    public Vector2Int currentPosition { get; private set;}

    public int unitSpeed { get; private set; }

    public Faction unitFaction { get; private set; }

    public Action<Unit> OnUnitDie;
    public Action<Vector2Int, Unit> OnAttack;

    void Awake() {
        stats = baseState.CreateStats();

        unitSpeed = stats.GetUnitSpeed();
        unitFaction = stats.GetUnitFaction();
    }

    public void SetPosition(Vector2Int currentPosition) {
        this.currentPosition = currentPosition;
    }

    public void TakeDamage(float amount) {
        amount -= stats.CurrentDefense;

        if (amount < 0) {
            return;
        }

        float currentHealth = stats.ModifyHealth(-amount);

        if (currentHealth <= 0) {
            Die();
        }
    }

    public void Attack() {
        Vector2Int attackDirection = new Vector2Int(
        Mathf.RoundToInt(this.transform.forward.x),
        Mathf.RoundToInt(this.transform.forward.z)
        );
        Vector2Int attackPosition = attackDirection + currentPosition;

        OnAttack?.Invoke(attackPosition, this);
    }

    public void Heal(float amount) {
        stats.ModifyHealth(amount);
    }

    public float GetHealth() {
        return stats.GetHealth();
    }

    public int GetMoveRange() {
        return stats.GetMoveRnage();
    }
    public void Die() {

        OnUnitDie?.Invoke(this);

        Destroy(gameObject, 2f);

        Debug.Log("Player can't handle the damage.");
    }

}
