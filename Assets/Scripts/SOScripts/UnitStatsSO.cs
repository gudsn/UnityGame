using UnityEngine;

[CreateAssetMenu(fileName = "UnitStatsSO", menuName = "Scriptable Objects/UnitStatsSo")]
public abstract class UnitStatsSO : ScriptableObject
{
    
    public string unitName;
    public Faction defaultFaction;

    public float maxHealthPoint;
    public float baseAttackPoint;
    public float baseDefensePoint;

    public int moveRange;

    public int unitSpeed;

    public abstract UnitStats CreateStats();
}
