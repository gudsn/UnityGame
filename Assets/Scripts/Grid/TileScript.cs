using System.Collections.Generic;
using UnityEngine;

public class TileScript : MonoBehaviour {
    private TileData data;
    private TileVisual visual;

    private void Awake() {
        visual = GetComponent<TileVisual>();
    }
    public void SetUp(TileData tileData) {
        this.data = tileData;
    }
    /*
    private void OnMouseDown() {
        data.isWalkable = !data.isWalkable;
        visual.SetVisual(data.isWalkable);
    }
    */

    public void SetHighlightedTile() {
        visual.SetHighlighted();
    }
    public void SetAttackRange() {
        visual.SetAttackRange();
    }

    public bool GetIsHighlighted() {
        return visual.isHighlighted;
    }
}
