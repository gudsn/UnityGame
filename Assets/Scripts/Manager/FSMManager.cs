using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Rendering;

public class FSMManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField]private PlayerFSM playerFSM;
    [SerializeField]private EnemyFSM enemyFSM;

    public static FSMManager Instance { get; private set;}

    private PriorityQueue<Unit> unitQueue;

    private int currentTime = 0;

    private const int actionValue = 1000;
    void Start(){
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        unitQueue = new PriorityQueue<Unit>(PriorityQueue<Unit>.HeapType.min);

        UnitManager.Instance.OnSpawnUnit += EnqueueNewUnit;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartState() {
        Debug.Log("Game Start");
        NextState();
    }

    public void EnqueueNewUnit(Unit unit) {
        // Prevent speed 0
        int currentSpeed = Mathf.Max(1, unit.unitSpeed);

        // Calculating Priority
        int speed = actionValue / currentSpeed;
        int nextTurnTime = currentTime + speed;

        unitQueue.Enqueue(nextTurnTime, unit);
    }

    public void NextState() {
        if (unitQueue.Count() <= 1) {
            Debug.Log("No Unit for play");
            return;
        }

        currentTime = unitQueue.GetFirstPriority();
        Unit activeUnit = unitQueue.Dequeue();

        if (activeUnit == null) {
            return;
        }
        if (activeUnit.GetHealth() <= 0) {
            NextState();
            return;
        }

        switch (activeUnit.unitFaction) {
            case Faction.Player:
                playerFSM.StartTurnfor(activeUnit);
                break;
            case Faction.Enemy:
                enemyFSM.StartTurnfor(activeUnit);
                break;
        } 
    }

    public void EndFSM(Unit activeUnit) {
        if (activeUnit.GetHealth() > 0) {
            EnqueueNewUnit(activeUnit);
        }
        NextState();
    }




}
