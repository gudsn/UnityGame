using UnityEngine;
using UnityEngine.Rendering;
[System.Serializable]
public class Status
{
    //Health
    [SerializeField] private float maxHealth;
    private float mcurrentHealth = 0;
    public float CurrentHealth {
        get {
            return mcurrentHealth;
        }
    }
    public float ModifyHealth(float amount) {
        mcurrentHealth += amount;
        mcurrentHealth = Mathf.Clamp(mcurrentHealth, 0, maxHealth);
        return mcurrentHealth;
    }

    [SerializeField] private float baseAttack;
    public float CurrentAttack {
        get {
            return baseAttack;
        }
    }

    [SerializeField] private float baseDefense;
    public float CurrentDefense {
        get {
            return baseDefense;
        }
    }

    [SerializeField] private float maxMagicPoint;
    private float mcurrentMagicPoint = 0;
    public float CurrentMagicPoint {
        get {
            return mcurrentMagicPoint;
        }
    }
    public float ModifyMagicPoint(float amount) {
        mcurrentMagicPoint += amount;
        mcurrentMagicPoint = Mathf.Clamp(mcurrentMagicPoint, 0, maxMagicPoint);
        return mcurrentMagicPoint;
    }

    public void InitStatus() {
        mcurrentHealth = (mcurrentHealth == 0)? maxHealth : mcurrentHealth;
        mcurrentMagicPoint = (mcurrentMagicPoint == 0) ? maxMagicPoint : mcurrentMagicPoint;
    }

}
