using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour {

    void Start() {
        StartCoroutine(GameSetup());
    }

    void Update() {

    }
    IEnumerator GameSetup() {
        yield return null;
        UnitManager.Instance.SetWorld();
    }
}
