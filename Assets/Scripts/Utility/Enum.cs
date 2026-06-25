using UnityEngine;

public enum Faction{
    Player,     // 아군 (플레이어 조작)
    Enemy,      // 적군 (AI 조작)
    Neutral,    // 중립 (마을 주민 등)
    Obstacle    // 파괴 가능한 장애물 (나무통, 바위 등)
}

public enum HighlightType {
    Move,       // 이동 가능 지역
    Attack,     // 공격 가능 지역
    Hover,       // 마우스 오버
    EnemyPath,   // 적 이동 예정 경로 (화살표 하이라이트)
    EnemyAttack  // 적 공격 예정 범위 (주황색)
}
