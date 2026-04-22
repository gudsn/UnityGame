using UnityEngine;

public class Unit : MonoBehaviour {

    [SerializeField] private UnitStatsSO baseState;
    [SerializeField] public GameObject ghostPrefab;

    public UnitStats stats { get; private set;}
    public TileData currentTile { get; private set;}

    public int unitSpeed { get; private set; }

    public Faction unitFaction { get; private set; }

    void Awake() {
        stats = baseState.CreateStats();

        unitSpeed = stats.GetUnitSpeed();
        unitFaction = stats.GetUnitFaction();
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

    public float Attack() {

        float amount = stats.GetAttackPower();
        return amount;
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
        Debug.Log("Player can't handle the damage.");
    }

}
