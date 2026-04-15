using UnityEngine;

[CreateAssetMenu(fileName = "TileSO", menuName = "Scriptable Objects/UnitState/StateSo")]
public class StateSO : ScriptableObject
{
    
    public string unitName;

    public float maxHealthPoint;
    public float maxMagicPoint;
    public float baseAttackPoint;
    public float baseDefensePoint;

    public int moveRange;
}
