using System;
using System.Collections.Generic;
using UnityEngine;

public struct TickCommand {
    public int executeTick;
    public Unit owner;
    public Action actionLogic;
}
public class TimeLineManager : MonoBehaviour{
    public List<TickCommand> actionTimeLineList;
    public 
    void Awake() {
        actionTimeLineList = new List<TickCommand>();

    }



}
