using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class PlayerAttackState : ITurnState {
    private PlayerFSM machine;
    private Unit activeUnit;
    private int attackRange = 2;

    private List<TileData> validAttackTile;
    private int predictedAttackTick;
    private TileData lastHoveredTile = null;

    private GameObject playerGhostInstance;

    private Dictionary<Unit, GameObject> previewGhostPool = new Dictionary<Unit, GameObject>();

    // UI 박스 그룹 참조 보관용
    private VisualElement attackGroupPreview;
    private bool isAttackConfirmed = false; // 공격 확정 여부 플래그

    public PlayerAttackState(PlayerFSM machine) {
        this.machine = machine;
        this.activeUnit = machine.activeUnit;
    }

    public void Enter() {
        Debug.Log("[공격 페이즈] 사거리 내의 적을 클릭하여 공격을 예약하세요.");

        // 1. Attack 상태 진입 시 2틱(준비+공격) UI 박스 그룹 생성
        if (PlayerInputUI.Instance != null) {
            attackGroupPreview = PlayerInputUI.Instance.CreateAttackGroup();
        }

        Vector3 virtualWorldPos = GridSystem.Instance.GetTileData(activeUnit.virtualPosition).worldPosition;
        validAttackTile = GridSystem.Instance.SpawnAttackRange(virtualWorldPos, attackRange);

        SpawnPlayerGhost(virtualWorldPos);

        int moveTicks = 0;
        if (machine.HasReservedMove) {
            moveTicks = activeUnit.GetMoveRange();
        }
        predictedAttackTick = moveTicks + 2;

        InitializeGhostPool();

        PlayerInput.Instance.OnLeftMouseClicked += AttackTarget;
        PlayerInput.Instance.OnEnterTriggered += SkipTurn;
    }

    public void Execute() {
        if (Mouse.current != null) {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Tile"))) {
                TileData hoveredTile = GridSystem.Instance.WorldPositionToGridTile(hit.point);
                if (hoveredTile != null && hoveredTile != lastHoveredTile) {
                    lastHoveredTile = hoveredTile;
                    UpdateAttackPredictionGhosts(hoveredTile);
                }
            }
        }
    }

    public void Exit() {
        // 공격을 확정하지 않고 이탈 시 프리뷰 UI 박스 제거
        if (!isAttackConfirmed && attackGroupPreview != null) {
            attackGroupPreview.RemoveFromHierarchy();
            attackGroupPreview = null;
        }

        if (playerGhostInstance != null) {
            Object.Destroy(playerGhostInstance);
        }

        CleanupGhostPool();
        GridSystem.Instance.DeleteAttackRange();
        PlayerInput.Instance.OnLeftMouseClicked -= AttackTarget;
        PlayerInput.Instance.OnEnterTriggered -= SkipTurn;

        EnemyController enemyController = Object.FindFirstObjectByType<EnemyController>();
        if (enemyController != null) {
            enemyController.RedrawCurrentEnemyIntents();
        }
    }

    private void SpawnPlayerGhost(Vector3 position) {
        if (activeUnit.ghostPrefab == null) return;

        playerGhostInstance = Object.Instantiate(activeUnit.ghostPrefab, position, activeUnit.transform.rotation);

        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        playerGhostInstance.layer = ignoreRaycastLayer;

        foreach (Transform child in playerGhostInstance.GetComponentsInChildren<Transform>()) {
            child.gameObject.layer = ignoreRaycastLayer;
        }

        Renderer ghostRenderer = playerGhostInstance.GetComponentInChildren<Renderer>();
        if (ghostRenderer != null) {
            Color newColor = ghostRenderer.material.color;
            newColor.a = 0.5f;
            ghostRenderer.material.color = newColor;
        }

        playerGhostInstance.SetActive(true);
    }

    private void InitializeGhostPool() {
        EnemyController enemyController = Object.FindFirstObjectByType<EnemyController>();
        if (enemyController == null) return;

        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");

        foreach (var kvp in enemyController.CachedEnemyDecisions) {
            Unit enemy = kvp.Key;
            if (enemy == null || enemy.GetHealth() <= 0) continue;

            GameObject ghost = Object.Instantiate(enemy.ghostPrefab, Vector3.zero, Quaternion.identity);
            ghost.SetActive(false);

            ghost.layer = ignoreRaycastLayer;
            foreach (Transform child in ghost.GetComponentsInChildren<Transform>()) {
                child.gameObject.layer = ignoreRaycastLayer;
            }

            Renderer ghostRenderer = ghost.GetComponent<Renderer>();
            if (ghostRenderer == null) {
                ghostRenderer = ghost.GetComponentInChildren<Renderer>();
            }

            if (ghostRenderer != null) {
                ghostRenderer.material.color = new Color(1f, 0.3f, 0.3f, 0.6f);
            }

            previewGhostPool.Add(enemy, ghost);
        }
    }

    private void UpdateAttackPredictionGhosts(TileData targetedTile) {
        DeactivateAllGhosts();

        if (!validAttackTile.Contains(targetedTile)) return;

        EnemyController enemyController = Object.FindFirstObjectByType<EnemyController>();
        if (enemyController == null) return;

        Vector2Int targetCoord = new Vector2Int(targetedTile.gridX, targetedTile.gridY);

        foreach (var kvp in enemyController.CachedEnemyDecisions) {
            Unit enemy = kvp.Key;
            if (enemy == null || enemy.GetHealth() <= 0) continue;

            TileData enemyTileAtAttackMoment = enemyController.GetEnemyTileAtTick(enemy, predictedAttackTick);
            if (enemyTileAtAttackMoment == null) continue;

            if (enemyTileAtAttackMoment.gridX == targetCoord.x && enemyTileAtAttackMoment.gridY == targetCoord.y) {
                if (previewGhostPool.TryGetValue(enemy, out GameObject ghost)) {
                    Vector3 spawnPos = enemyTileAtAttackMoment.worldPosition;
                    spawnPos.y += 0.5f;

                    ghost.transform.position = spawnPos;
                    ghost.SetActive(true);
                }
            }
        }
    }

    private void DeactivateAllGhosts() {
        foreach (var ghost in previewGhostPool.Values) {
            if (ghost != null) {
                ghost.SetActive(false);
            }
        }
    }

    private void CleanupGhostPool() {
        foreach (var ghost in previewGhostPool.Values) {
            if (ghost != null) {
                Object.Destroy(ghost);
            }
        }
        previewGhostPool.Clear();
    }

    public void AttackTarget(Vector2 cordinate) {
        Ray ray = Camera.main.ScreenPointToRay(cordinate);

        if (Physics.Raycast(ray, out RaycastHit hit)) {
            Vector3 targetPosition = hit.point;
            Unit clickedUnit = hit.collider.GetComponentInParent<Unit>();

            if (clickedUnit != null) {
                targetPosition = clickedUnit.transform.position;
            }

            TileData currentTile = GridSystem.Instance.WorldPositionToGridTile(targetPosition);
            if (currentTile == null) {
                Debug.Log("Out of boundary!");
                return;
            }

            Vector2Int currentCordinate = new Vector2Int(currentTile.gridX, currentTile.gridY);

            if (!validAttackTile.Contains(currentTile)) {
                Debug.Log("Out of attack range!");
                return;
            }

            UnitManager.Instance.RegisteredUnit.TryGetValue(currentCordinate, out Unit targetUnit);

            if (targetUnit != null && targetUnit.unitFaction != Faction.Enemy) {
                Debug.Log("Can't attack this unit!");
                return;
            }

            AttackCommand attackCmd = new AttackCommand(activeUnit, targetUnit, currentCordinate);

            // [핵심 변경] 만들어진 상자에게 어떤 명령인지 데이터를 주입합니다. 
            if (attackGroupPreview != null) attackGroupPreview.userData = attackCmd;

            isAttackConfirmed = true; 
            EventBus<DisableAttackButtonEvent>.Publish(new DisableAttackButtonEvent());
            machine.HasReservedAttack = true;
            machine.ChangeState(new PlayerIdleState(machine));
        }
    }

    public void SkipTurn() {
        machine.ChangeState(new PlayerIdleState(machine));
    }
}