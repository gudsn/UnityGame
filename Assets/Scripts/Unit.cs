using System;
using UnityEngine;

public class Unit : MonoBehaviour {

    [SerializeField] private UnitStatsSO baseState;
    [SerializeField] public GameObject ghostPrefab;

    public UnitStats stats { get; private set; }
    public Vector2Int currentPosition { get; private set; }

    // [추가] 예약(조준) 단계에서 유닛의 미래 위치를 추적하기 위한 가상 좌표 프로퍼티
    public Vector2Int virtualPosition { get; set; }

    public int unitSpeed { get; private set; }

    public Faction unitFaction { get; private set; }

    public Action<Unit> OnUnitDie;
    public Action<Vector2Int, Unit> OnAttack;

    void Awake() {
        stats = baseState.CreateStats();

        unitSpeed = stats.GetUnitSpeed();
        unitFaction = stats.GetUnitFaction();

        // 초기화 시 가상 위치도 현재 물리적 위치와 동기화
        virtualPosition = currentPosition;
    }

    public void SetPosition(Vector2Int currentPosition) {
        this.currentPosition = currentPosition;
        // 실제 이동이 완료되면 가상 위치도 함께 맞추어 줍니다.
        this.virtualPosition = currentPosition;
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

    public void Attack(Vector2Int attackPosition) {

        OnAttack?.Invoke(attackPosition, this);
    }

    public void Heal(float amount) {
        stats.ModifyHealth(amount);
    }

    public float GetHealth() {
        return stats.GetHealth();
    }

    public float GetMaxHealth() {
        return stats.GetMaxHealth();
    }

    public int GetMoveRange() {
        return stats.GetMoveRnage();
    }
    public void Die() {

        OnUnitDie?.Invoke(this);

        Destroy(gameObject, 2f);

        Debug.Log("Player can't handle the damage.");
    }

    public string GetName() {
        return stats.GetName();
    }

}