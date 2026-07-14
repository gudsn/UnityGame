using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어 공격 범위 설정 및 조준 호버링 기반 동적 적중 예측 시뮬레이션 상태
public class PlayerAttackState : ITurnState {
    private PlayerFSM machine;
    private Unit activeUnit;
    private int attackRange = 2;

    private List<TileData> validAttackTile;
    private int predictedAttackTick;
    private TileData lastHoveredTile = null;

    private Dictionary<Unit, GameObject> previewGhostPool = new Dictionary<Unit, GameObject>();

    public PlayerAttackState(PlayerFSM machine) {
        this.machine = machine;
        this.activeUnit = machine.activeUnit;
    }

    // 공격 페이즈 이벤트를 바인딩하고 예측 적중 계산용 비주얼 고스트 풀 사전 확보
    public void Enter() {
        Debug.Log("[공격 페이즈] 사거리 내의 적을 클릭하여 공격을 예약하세요.");

        Vector3 virtualWorldPos = GridSystem.Instance.GetTileData(activeUnit.virtualPosition).worldPosition;
        validAttackTile = GridSystem.Instance.SpawnAttackRange(virtualWorldPos, attackRange);

        int moveTicks = 0;
        if (machine.HasReservedMove) {
            moveTicks = activeUnit.GetMoveRange();
        }
        predictedAttackTick = moveTicks + 2;

        InitializeGhostPool();

        PlayerInput.Instance.OnLeftMouseClicked += AttackTarget;
        PlayerInput.Instance.OnEnterTriggered += SkipTurn;
    }

    // 매 프레임 마우스 포인터 감지를 통해 호버링 타일 업데이트 분석
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

    // 상태 탈출 시 맵 하이라이트를 반환하고 소집된 적 고스트 풀링 데이터 일괄 해제
    public void Exit() {
        CleanupGhostPool();
        GridSystem.Instance.DeleteAttackRange();
        PlayerInput.Instance.OnLeftMouseClicked -= AttackTarget;
        PlayerInput.Instance.OnEnterTriggered -= SkipTurn;
    }

    // 씬 내 적 유닛들의 비주얼 고스트들을 풀에 비활성 상태로 선행 생성 및 붉은색 반투명 가공
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

    // 조준 중인 타일에 지정 공격 도달 틱 시점 적이 밟게 될 경우 해당 고스트 위치를 0.5f 보정하여 활성화
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

    // 타겟 지점을 클릭하여 캡슐화된 매크로 명령(AttackCommand)을 타임라인 스케줄러에 등록
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
            AIDecision playerDecision = new AIDecision {
                utilityScore = 100f,
                intendedCommands = new List<ICommand> { attackCmd }
            };
            TimeLineManager.Instance.ScheduleAction(activeUnit, playerDecision);

            EventBus<DisableAttackButtonEvent>.Publish(new DisableAttackButtonEvent());
            machine.HasReservedAttack = true;

            machine.ChangeState(new PlayerIdleState(machine));
        }
    }

    public void SkipTurn() {
        machine.ChangeState(new PlayerIdleState(machine));
    }
}