using UnityEngine;

public enum Faction {
    Player,     // 아군
    Enemy,      // 적군
    Neutral,    // 중립
    Obstacle    // 장애물
}

public enum HighlightType {
    Move,       // 이동 가능 지역
    Attack,     // 공격 가능 지역
    Hover,      // 마우스 오버
    EnemyPath,  // 적 이동 예정 경로 (화살표)
    EnemyAttack // 적 공격 예정 범위 (주황색)
}

public enum ArrowResourceType {
    Line,   // 직진 구간 (수직 바)
    Corner, // 모퉁이 구간 (꺾임 모양)
    Head    // 종착지 촉 (화살표)
}