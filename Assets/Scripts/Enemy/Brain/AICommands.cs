using System.Collections.Generic;
using UnityEngine;

// 의도 캡슐화용 마커 인터페이스
public interface ICommand { }

// 최종 의사결정 데이터
public class AIDecision {
    public float utilityScore;
    public List<ICommand> intendedCommands = new List<ICommand>();
}

// 대기 데이터
public class WaitCommand : ICommand {
    public float waitTime = 0.5f;
}

// 이동 데이터 (경로 포함)
public class MoveCommand : ICommand {
    public Unit owner { get; private set; }
    public TileData destination { get; private set; }
    public List<TileData> path { get; private set; }

    public MoveCommand(Unit _owner, TileData _destinationTile) {
        owner = _owner;
        destination = _destinationTile;
        TileData currentTile = GridSystem.Instance.GetTileData(owner.currentPosition);
        path = GridSystem.Instance.AStarAlgorithm(currentTile, destination);
    }
}

// 플레이어 이동 데이터
public class PlayerMoveCommand : ICommand {
    public Unit owner { get; private set; }
    public TileData destination { get; private set; }
    public List<TileData> path { get; private set; }

    public PlayerMoveCommand(Unit _owner, TileData _destinationTile) {
        owner = _owner;
        destination = _destinationTile;
        TileData currentTile = GridSystem.Instance.GetTileData(owner.currentPosition);
        path = GridSystem.Instance.AStarAlgorithm(currentTile, destination);
    }
}

// 공격 데이터 (목표 좌표 정보 추가됨)
public class AttackCommand : ICommand {
    public Unit owner { get; private set; }
    public Unit target { get; private set; }
    public Vector2Int targetCoordinate { get; private set; } // 조준한 타일 좌표 추가

    public AttackCommand(Unit _owner, Unit _target) {
        owner = _owner;
        target = _target;
        targetCoordinate = _target.currentPosition;
    }

    // 명시적인 타일 타격을 위한 생성자 오버로딩 추가
    public AttackCommand(Unit _owner, Unit _target, Vector2Int _targetCoordinate) {
        owner = _owner;
        target = _target;
        targetCoordinate = _targetCoordinate;
    }
}