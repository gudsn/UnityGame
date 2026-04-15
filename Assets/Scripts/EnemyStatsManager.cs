using UnityEngine;

public class EnemyStatusManager:MonoBehaviour{
    [SerializeField] private StateSO baseState;
    private EnemyStats stats;

    void Awake() {
        stats = new EnemyStats(baseState);
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
        float amount = stats.CurrentAttack;
        if (stats.CurrentMagicPoint < 5) {
            Debug.Log("Enemy : Not enough MP");
            return 0;
        }

        float currentMagicPoint = stats.ModifyMagicPoint(-5);
        return amount;
    }

    public void HealthGeneration() {
        float currentHealth = stats.ModifyHealth(5);
    }

    public void MagicPointGeneration() {
        float currentMagicPoint = stats.ModifyMagicPoint(5);
    }

    public void Die() {
        Debug.Log("Enemy can't handle the damage.");
    }
}
