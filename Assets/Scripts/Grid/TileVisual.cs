using System.Collections.Generic;
using UnityEngine;

public class TileVisual : MonoBehaviour {
    [SerializeField] private Material originalMaterial; // 인스펙터에서 기본 메티리얼 할당
    [SerializeField] private Material blockMaterial;    // 인스펙터에서 빨간색/금간 메티리얼 할당
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material attackRangeMaterial;
    private Renderer tileRenderer;
    public bool isHighlighted { get; private set; } = false; 
    
    void Awake() {
        tileRenderer = GetComponent<Renderer>();
        // 게임 시작 시 현재 메티리얼을 기본값으로 저장
        originalMaterial = tileRenderer.material;
    }

    public void SetVisual(bool isWalkable) {
        // 메티리얼 자체를 갈아끼웁니다. 텍스처와 쉐이더 설정이 그대로 유지됩니다.
        tileRenderer.material = isWalkable ? originalMaterial : blockMaterial;
    }

    public void SetHighlighted() {
        if (isHighlighted) {
            tileRenderer.material = originalMaterial;
            isHighlighted = false;
        }
        else {
            tileRenderer.material = highlightMaterial;
            isHighlighted = true;
        }
    }

    public void SetAttackRange() {
        if (isHighlighted) {
            tileRenderer.material = originalMaterial;
            isHighlighted = false;
        }
        else {
            tileRenderer.material = attackRangeMaterial;
            isHighlighted = true;
        }
    }

}