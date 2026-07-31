using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class TimeLineDragger {
    private readonly VisualElement targetElement;
    private readonly VisualElement playerTrack;

    private bool isDragging;
    private Vector2 dragStartPosition;
    private Vector2 elementStartPosition;

    private static GameObject dragHoverGhostInstance;
    private static int lastHoveredSlotIndex = -1;

    public TimeLineDragger(VisualElement target, VisualElement track) {
        targetElement = target;
        playerTrack = track;

        targetElement.RegisterCallback<PointerDownEvent>(OnPointerDown);
        targetElement.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        targetElement.RegisterCallback<PointerUpEvent>(OnPointerUp);
    }

    private void OnPointerDown(PointerDownEvent evt) {
        isDragging = true;
        dragStartPosition = evt.position;

        float startLeft = float.IsNaN(targetElement.style.left.value.value) ? targetElement.layout.x : targetElement.style.left.value.value;
        float startTop = float.IsNaN(targetElement.style.top.value.value) ? targetElement.layout.y : targetElement.style.top.value.value;
        elementStartPosition = new Vector2(startLeft, startTop);

        targetElement.BringToFront();
        targetElement.CapturePointer(evt.pointerId);
        evt.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent evt) {
        if (!isDragging || !targetElement.HasPointerCapture(evt.pointerId)) return;

        Vector2 delta = (Vector2)evt.position - dragStartPosition;
        targetElement.style.left = elementStartPosition.x + delta.x;
        targetElement.style.top = elementStartPosition.y + delta.y;

        ICommand cmd = targetElement.userData as ICommand;
        if (cmd is AttackCommand && playerTrack != null) {
            CheckDragHoverSlot(evt.position);
        }

        evt.StopPropagation();
    }

    private void OnPointerUp(PointerUpEvent evt) {
        if (!isDragging) return;

        isDragging = false;
        if (targetElement.HasPointerCapture(evt.pointerId)) {
            targetElement.ReleasePointer(evt.pointerId);
        }

        ClearDragHoverGhost();

        bool droppedInTimeline = false;
        ICommand cmd = targetElement.userData as ICommand;

        if (cmd != null && playerTrack != null) {
            int boxTickCount = 1;
            string commandTypeName = cmd.GetType().Name;

            if (cmd is AttackCommand) boxTickCount = 2;
            else if (cmd is MoveCommand m && m.path != null) boxTickCount = m.path.Count;
            else if (cmd is PlayerMoveCommand pm && pm.path != null) boxTickCount = pm.path.Count;

            int targetSlotIndexFound = -1;
            Rect targetRect = targetElement.worldBound;
            float boxLeftX = targetRect.xMin;

            for (int i = 1; i <= 8; i++) {
                VisualElement slot = playerTrack.Q<VisualElement>($"slot-{i}");
                if (slot != null) {
                    Rect slotRect = slot.worldBound;
                    if (boxLeftX >= slotRect.xMin && boxLeftX <= slotRect.xMax) {
                        if (targetRect.yMax >= slotRect.yMin && targetRect.yMin <= slotRect.yMax) {
                            targetSlotIndexFound = i;
                            break;
                        }
                    }
                }
            }

            if (targetSlotIndexFound != -1) {
                int startTick = targetSlotIndexFound;

                if (startTick + boxTickCount - 1 > 8) {
                    Debug.Log("<color=yellow>[알림]</color> 타임라인 범위를 초과하여 배치할 수 없습니다!");
                }
                else {
                    bool hasOverlap = false;
                    for (int offset = 0; offset < boxTickCount; offset++) {
                        int checkIndex = startTick + offset;
                        VisualElement checkSlot = playerTrack.Q<VisualElement>($"slot-{checkIndex}");

                        if (checkSlot != null) {
                            bool hasGroup = checkSlot.Children().Any(e => e.ClassListContains("command-group") && e != targetElement);
                            if (hasGroup) {
                                hasOverlap = true;
                                break;
                            }
                        }
                    }

                    if (hasOverlap) {
                        Debug.Log("<color=yellow>[알림]</color> 이미 해당 틱 범위에 다른 행동이 예약되어 있습니다!");
                    }
                    else {
                        droppedInTimeline = true;
                        targetElement.RemoveFromHierarchy();

                        for (int offset = 0; offset < boxTickCount; offset++) {
                            int slotIndex = startTick + offset;
                            VisualElement slot = playerTrack.Q<VisualElement>($"slot-{slotIndex}");
                            if (slot != null) {
                                VisualElement timelineBox = new VisualElement();
                                timelineBox.AddToClassList("box-item");
                                timelineBox.AddToClassList("command-group");

                                Sprite iconSprite = null;
                                if (TimeLineUI.Instance != null) {
                                    if (commandTypeName.Contains("Move")) {
                                        iconSprite = TimeLineUI.Instance.MoveIcon;
                                    }
                                    else if (commandTypeName.Contains("Attack")) {
                                        if (offset > 0) iconSprite = TimeLineUI.Instance.AttackIcon;
                                    }
                                }

                                if (iconSprite != null) {
                                    Image iconImage = new Image();
                                    iconImage.sprite = iconSprite;
                                    iconImage.AddToClassList("box-icon");
                                    timelineBox.Add(iconImage);
                                }

                                slot.Add(timelineBox);
                            }
                        }

                        Unit owner = GetOwnerFromCommand(cmd);
                        if (owner != null) {
                            TimeLineManager.Instance.ScheduleActionAtTick(owner, cmd, startTick);

                            // [핵심 보완] 타임라인 슬롯에 배치가 완전히 확정되었을 때만 virtualPosition 반영
                            if (cmd is PlayerMoveCommand pmCmd && pmCmd.destination != null) {
                                owner.virtualPosition = new Vector2Int(pmCmd.destination.gridX, pmCmd.destination.gridY);
                            }
                            else if (cmd is MoveCommand mCmd && mCmd.destination != null) {
                                owner.virtualPosition = new Vector2Int(mCmd.destination.gridX, mCmd.destination.gridY);
                            }
                        }
                    }
                }
            }
        }

        if (!droppedInTimeline) {
            targetElement.style.left = elementStartPosition.x;
            targetElement.style.top = elementStartPosition.y;
        }

        evt.StopPropagation();
    }

    private void CheckDragHoverSlot(Vector2 pointerPos) {
        int currentHoverSlot = -1;
        Rect targetRect = targetElement.worldBound;
        float boxLeftX = targetRect.xMin;

        for (int i = 1; i <= 8; i++) {
            VisualElement slot = playerTrack.Q<VisualElement>($"slot-{i}");
            if (slot != null) {
                Rect slotRect = slot.worldBound;
                if (boxLeftX >= slotRect.xMin && boxLeftX <= slotRect.xMax) {
                    if (targetRect.yMax >= slotRect.yMin && targetRect.yMin <= slotRect.yMax) {
                        currentHoverSlot = i;
                        break;
                    }
                }
            }
        }

        if (currentHoverSlot != lastHoveredSlotIndex) {
            lastHoveredSlotIndex = currentHoverSlot;
            ClearDragHoverGhost();

            if (currentHoverSlot != -1) {
                int attackHitTick = currentHoverSlot + 1;
                ShowAttackValidationGhost(attackHitTick);
            }
        }
    }

    private bool IsWithinCrossRange(Vector3 originWorld, Vector3 targetWorld, int maxRange) {
        TileData originTile = GridSystem.Instance.WorldPositionToGridTile(originWorld);
        TileData targetTile = GridSystem.Instance.WorldPositionToGridTile(targetWorld);

        if (originTile == null || targetTile == null) return false;

        int dx = Mathf.Abs(originTile.gridX - targetTile.gridX);
        int dy = Mathf.Abs(originTile.gridY - targetTile.gridY);

        return (dx == 0 && dy <= maxRange) || (dy == 0 && dx <= maxRange);
    }

    private void ShowAttackValidationGhost(int hitTick) {
        EnemyController enemyController = Object.FindFirstObjectByType<EnemyController>();
        if (enemyController == null) return;

        ICommand cmd = targetElement.userData as ICommand;
        AttackCommand attackCmd = cmd as AttackCommand;
        if (attackCmd == null) return;

        Unit owner = attackCmd.owner;
        Unit targetUnit = attackCmd.target;
        if (owner == null) return;

        Vector3 playerEffectiveWorldPos = owner.transform.position;

        int lastPlacedMoveEndTick = -1;
        if (playerTrack != null) {
            for (int i = 8; i >= 1; i--) {
                VisualElement slot = playerTrack.Q<VisualElement>($"slot-{i}");
                if (slot != null && slot.Children().Any(e => e.ClassListContains("command-group"))) {
                    var icon = slot.Q<Image>();
                    if (icon != null && icon.sprite == TimeLineUI.Instance.MoveIcon) {
                        lastPlacedMoveEndTick = i;
                        break;
                    }
                }
            }
        }

        if (lastPlacedMoveEndTick != -1 && hitTick > lastPlacedMoveEndTick) {
            TileData virtualTile = GridSystem.Instance.GetTileData(owner.virtualPosition);
            if (virtualTile != null) {
                playerEffectiveWorldPos = virtualTile.worldPosition;
            }
        }

        TileData enemyTileAtTick = null;
        bool isValidTarget = false;

        if (targetUnit != null && targetUnit.GetHealth() > 0) {
            enemyTileAtTick = enemyController.GetEnemyTileAtTick(targetUnit, hitTick);
            if (enemyTileAtTick != null) {
                if (IsWithinCrossRange(playerEffectiveWorldPos, enemyTileAtTick.worldPosition, 2)) {
                    isValidTarget = true;
                }
            }
        }

        if (!isValidTarget) {
            foreach (var kvp in enemyController.CachedEnemyDecisions) {
                Unit enemy = kvp.Key;
                if (enemy == null || enemy.GetHealth() <= 0) continue;

                TileData tile = enemyController.GetEnemyTileAtTick(enemy, hitTick);
                if (tile != null) {
                    if (IsWithinCrossRange(playerEffectiveWorldPos, tile.worldPosition, 2)) {
                        targetUnit = enemy;
                        enemyTileAtTick = tile;
                        isValidTarget = true;
                        break;
                    }
                }
            }
        }

        if (!isValidTarget || enemyTileAtTick == null || targetUnit == null || targetUnit.ghostPrefab == null) {
            ClearDragHoverGhost();
            return;
        }

        Vector3 spawnPos = enemyTileAtTick.worldPosition;

        dragHoverGhostInstance = Object.Instantiate(targetUnit.ghostPrefab, spawnPos, Quaternion.identity);

        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        dragHoverGhostInstance.layer = ignoreRaycastLayer;
        foreach (Transform child in dragHoverGhostInstance.GetComponentsInChildren<Transform>()) {
            child.gameObject.layer = ignoreRaycastLayer;
        }

        Renderer ghostRenderer = dragHoverGhostInstance.GetComponentInChildren<Renderer>();
        if (ghostRenderer != null) {
            ghostRenderer.material.color = new Color(1f, 0.2f, 0.2f, 0.75f);
        }

        dragHoverGhostInstance.SetActive(true);
    }

    private void ClearDragHoverGhost() {
        if (dragHoverGhostInstance != null) {
            Object.Destroy(dragHoverGhostInstance);
            dragHoverGhostInstance = null;
        }
    }

    private Unit GetOwnerFromCommand(ICommand cmd) {
        if (cmd is MoveCommand m) return m.owner;
        if (cmd is PlayerMoveCommand pm) return pm.owner;
        if (cmd is AttackCommand a) return a.owner;
        return null;
    }
}