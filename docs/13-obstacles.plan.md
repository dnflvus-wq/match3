# Phase 13: 장애물 시스템 + 비정형 보드

> **선행 조건**: Phase 4 (Model 분리) 완료 후 BoardModel에 CellState 확장.
> 08-gdd.plan.md의 장애물 규격 기준.

## 목표
다양한 장애물로 레벨 변화와 전략적 깊이 추가 + 비정방형 맵 지원

---

## Step 1 (코드): BoardModel 확장

### CellState enum
**파일 위치**: `Assets/Scripts/Model/CellState.cs`

```csharp
public enum CellType
{
    Normal,     // 일반 타일 배치 가능
    Empty,      // 빈 공간 (타일 불가, 비정형 보드용)
    Stone,      // 금속/돌 (파괴 불가, 이동 불가)
}

[System.Serializable]
public struct CellState
{
    public CellType cellType;
    public ObstacleType obstacleType;  // None, Ice, Chain, Jelly, TimerBomb
    public int obstacleHP;             // 장애물 남은 단계 (얼음 1~3, 사슬 1~2)
    public int timerCount;             // 타이머 폭탄 전용: 남은 턴 수
}
```

### BoardModel 확장
```
Grid2D<CellState> cellStates  // 기존 Grid2D<TileState>와 별도 관리
메서드:
- GetCellState(int x, int y) → CellState
- DamageObstacle(int x, int y) → bool (파괴 완료 여부)
- IsPassable(int x, int y) → bool (타일이 통과할 수 있는 셀인지)
- TickTimerBombs() → List<int2> (타이머 0 도달한 폭탄 위치)
```

---

## Step 1 (코드): 장애물별 ScriptableObject

### ObstacleConfig.cs
**파일 위치**: `Assets/Scripts/Data/ObstacleConfig.cs`

```csharp
[CreateAssetMenu(menuName = "Match3/Obstacle Config")]
public class ObstacleConfig : ScriptableObject
{
    [Header("식별")]
    public ObstacleType obstacleType;

    [Header("규칙")]
    public int maxHP;                  // 최대 단계 (얼음=3, 사슬=2)
    public bool blocksTileMovement;    // 타일 이동 차단 여부
    public bool blocksMatch;           // 매치 차단 여부
    public DestroyCondition destroyCondition; // Adjacent(인접매치), OnTop(위에서매치), Indestructible

    [Header("비주얼")]
    public Sprite[] hpSprites;         // HP별 스프라이트 (3단→2단→1단)
    public GameObject breakEffectPrefab; // 파괴 시 파티클

    [Header("점수")]
    public int scorePerHP;             // HP 1단계 파괴 시 점수
}
```

### Step 2 (에디터): SO 에셋 생성

| 에셋 이름 | obstacleType | maxHP | blocksTileMovement | destroyCondition | scorePerHP |
|-----------|-------------|-------|-------------------|-----------------|-----------|
| **Ice_Config** | Ice | 3 | false | Adjacent | 30 |
| **Chain_Config** | Chain | 2 | true (스왑 불가) | Adjacent | 50 |
| **Stone_Config** | Stone | ∞ | true | Indestructible | 0 |
| **Jelly_Config** | Jelly | 2 | false | OnTop | 40 |
| **TimerBomb_Config** | TimerBomb | 1 | false | Adjacent | 100 |

**Unity 에디터 조작:**
1. Assets > Create > Match3 > Obstacle Config → 5개 생성
2. Inspector에서 위 표의 값 입력
3. `hpSprites[]` 배열에 HP별 스프라이트 드래그앤드롭 (예: ice_3.png, ice_2.png, ice_1.png)
4. `breakEffectPrefab`에 파괴 파티클 할당

---

## Step 2 (에디터): 장애물 Prefab

### 추가 Prefab (Assets/Prefabs/Obstacles/)

| Prefab | 구조 | Sorting Order |
|--------|------|---------------|
| **IceOverlay.prefab** | SpriteRenderer (ice_3 sprite), 타일 위에 오버레이 | Tiles +2 |
| **ChainOverlay.prefab** | SpriteRenderer (chain sprite) | Tiles +2 |
| **JellyUnderlay.prefab** | SpriteRenderer (jelly sprite), 타일 아래 | Tiles -1 |
| **TimerBombOverlay.prefab** | SpriteRenderer + TextMeshPro (남은 턴 숫자) | Tiles +3 |

### 파괴 파티클 (Assets/Prefabs/Effects/)

| Prefab | 용도 | Duration | 특징 |
|--------|------|----------|------|
| **IceBreak.prefab** | 얼음 깨짐 | 0.4s | 파편 조각 6~8개, Gravity 2.0, 반투명 흰색 |
| **ChainBreak.prefab** | 사슬 해제 | 0.3s | 금속 파편 4개, 불꽃 스파크 |
| **JellyPop.prefab** | 젤리 제거 | 0.3s | 물방울 형태 Burst 10개, Bounce |

---

## Step 1 (코드): MatchFinder 장애물 연동

### 매치 시 장애물 처리 로직
```
ProcessMatch(matchResult) 내부:
1. 매치된 타일 좌표 수집
2. 각 매치 타일의 상하좌우 인접 셀 검사
3. 인접 셀에 장애물(Ice/Chain/TimerBomb) 있으면 → DamageObstacle() 호출
4. DamageObstacle: HP -= 1, HP == 0이면 장애물 완전 제거
5. Jelly: 매치된 타일 아래에 Jelly 있으면 → DamageObstacle()
6. TimerBomb: 매 턴 종료 시 TickTimerBombs() → 카운트 0이면 게임오버
```

### DropSimulator 장애물 대응
```
CalculateDrops에서:
- Stone/Empty 셀은 건너뜀 (타일이 통과 불가)
- 포털: 입구 좌표에서 사라진 타일이 출구 좌표에서 스폰
```

---

## 비정형 보드

### LevelConfig SO 확장
```csharp
[Header("Board Layout")]
public CellType[] boardLayout;  // 1D 배열 (width × height)
// boardLayout[y * width + x] = Normal/Empty/Stone
```

### Custom Editor (LevelConfigEditor.cs 확장)
**기존 LevelConfigEditor.cs에 추가:**
- 그리드 프리뷰에서 각 셀을 클릭하면 CellType 순환 (Normal → Empty → Stone → Normal)
- 색상 코드: Normal=연두, Empty=검정, Stone=회색
- 장애물 배치: Shift+클릭으로 ObstacleType 순환 (None → Ice → Chain → Jelly → None)
- HP 조정: Ctrl+클릭으로 HP 1씩 증가 (최대 maxHP)

---

## Step 3 (에디터): 씬 연결

1. BoardView Inspector에 `obstacleConfigs[]` 배열 추가 → 5종 SO 할당
2. BoardView Inspector에 `obstaclePrefabs[]` 배열 추가 → 4종 오버레이 Prefab 할당
3. LevelConfig SO에서 `boardLayout[]` 배열로 비정형 맵 정의 (Custom Editor UI 활용)
4. 각 레벨 SO에서 장애물 배치 (Custom Editor의 Shift+클릭)

---

## 검증 항목
- [ ] 얼음 3단계 → 인접 매치 3회로 완전 파괴
- [ ] 사슬 → 인접 매치로 해제 후 해당 타일 스왑 가능
- [ ] 금속 → 파괴 불가, 타일 낙하 우회 정상
- [ ] 젤리 → 위에서 매치 시 1단계씩 제거 (레벨 목표 카운트)
- [ ] 포털 → 타일 입구→출구 이동 정상
- [ ] 타이머 폭탄 → 카운트 0 도달 시 게임오버
- [ ] 비정형 보드 (Empty 셀) → 타일 배치 안 됨, 낙하 우회
- [ ] Custom Editor에서 장애물 시각적 배치 가능
- [ ] 장애물 파괴 파티클 재생
- [ ] 장애물 HP별 스프라이트 변경

## MCP 도구 호출 순서

```
Step 1: script-update-or-create → CellState.cs, ObstacleConfig.cs(SO), BoardModel 확장
Step 2: script-update-or-create → LevelConfigEditor.cs 확장 (장애물 팔레트 + Shift/Ctrl 클릭)
Step 3: assets-refresh → 컴파일
Step 4: script-execute → ObstacleConfig SO 에셋 5종 생성 (Ice, Chain, Stone, Jelly, TimerBomb)
Step 5: script-execute → 장애물 오버레이 Prefab 4종 생성 + 파괴 파티클 3종 설정
Step 6: script-execute → LevelConfig SO에 boardLayout/obstacleLayout 배열 설정
Step 7: gameobject-component-modify → BoardView Inspector에 obstacleConfigs[]/obstaclePrefabs[] 할당
Step 8: scene-save → 씬 저장
Step 9: editor-application-set-state → Play 모드 테스트
Step 10: screenshot-game-view → 장애물 렌더링/파괴 확인
```

## 진행률: 0%
