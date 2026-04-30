using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour{

    private Dictionary<Vector2Int, Unit> currentUnitData;
    void Start(){
        currentUnitData = UnitManager.Instance.registeredUnit;

        foreach (KeyValuePair<Vector2Int,Unit> it in currentUnitData) {
            it.Value.OnAttack += TryAttack;
        }

        UnitManager.Instance.OnSpawnUnit += HaddleNewSpwanedUnit;
        UnitManager.Instance.OnMoveUnit += HanddleUnitPositionUpdate;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private void TryAttack(Vector2Int attackPosition, Unit attackUnit) {
        if (currentUnitData.TryGetValue(attackPosition, out Unit targetUnit)) {
            float amount = attackUnit.stats.GetAttackPower();
            targetUnit.TakeDamage(amount);
        }
    }

    private void HaddleNewSpwanedUnit(Unit unit) {
        unit.OnAttack += TryAttack;
        unit.OnUnitDie += HandleUnitDeath;
    }

    private void HandleUnitDeath(Unit unit) {
        unit.OnAttack -= TryAttack;        
        unit.OnUnitDie -= HandleUnitDeath; 
    }

    private void HanddleUnitPositionUpdate(Dictionary<Vector2Int, Unit>newRegisteredUnit) {
        currentUnitData = newRegisteredUnit;
    }
    private void OnDestroy() {
        if (UnitManager.Instance != null) {
            UnitManager.Instance.OnSpawnUnit -= HaddleNewSpwanedUnit;
            UnitManager.Instance.OnMoveUnit -= HanddleUnitPositionUpdate;
        } 
    }
}
