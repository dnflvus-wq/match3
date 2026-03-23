# Phase 2: 특수 타일 시스템

> 08-gdd.plan.md의 특수 타일 규격을 구현하는 단계.
> **선행 조건**: Phase 4 (Model 분리) 완료 후 MatchFinder에 Shape Recognition 추가.
> NotebookLM 리서치 기반: Strategy 패턴 + ScriptableObject 데이터 분리.

## 목표
4매치/5매치/L자/T자 매치 시 특수 타일 생성 + 조합 시너지 구현

---

## Step 1 (코드): ScriptableObject 데이터 정의

### SpecialTileConfig.cs (ScriptableObject)
**파일 위치**: `Assets/Scripts/Data/SpecialTileConfig.cs`

```csharp
[CreateAssetMenu(menuName = "Match3/Special Tile Config")]
public class SpecialTileConfig : ScriptableObject
{
    [Header("식별")]
    public string tileId;              // "Striped_H", "Bomb", "ColorBomb" 등
    public SpecialTileType tileType;   // enum: Striped, Bomb, ColorBomb, Rocket

    [Header("비주얼")]
    public Sprite overlaySprite;       // 타일 위에 겹치는 특수 효과 스프라이트
    public GameObject effectPrefab;    // 기폭 시 스폰할 파티클 Prefab

    [Header("효과")]
    public int effectRange;            // 폭발 반경 (1=3x3, 2=5x5)
    public int scoreBonus;             // 기폭 시 추가 점수 (120/200/300)
    [Range(1, 10)]
    public int priority;               // 동시 기폭 시 우선순위 (높을수록 먼저)

    [Header("행동 플래그")]
    public bool canBeSwapped = true;
    public bool canBeMoved = true;     // 중력 낙하 여부
    public bool canBeTriggered = true; // 매치/터치로 기폭 가능 여부
}
```

### Step 2 (에디터): SO 에셋 생성

**Unity 에디터 조작:**
1. Assets > Create > Match3 > Special Tile Config
2. 4개 에셋 생성:

| 에셋 이름 | tileId | tileType | effectRange | scoreBonus | priority |
|-----------|--------|----------|-------------|------------|----------|
| **StripedH_Config** | Striped_H | Striped | 0 (줄 전체) | 120 | 3 |
| **StripedV_Config** | Striped_V | Striped | 0 (줄 전체) | 120 | 3 |
| **Bomb_Config** | Bomb | Bomb | 1 (3×3) | 200 | 5 |
| **ColorBomb_Config** | ColorBomb | ColorBomb | 0 (색 전체) | 300 | 8 |
| **Rocket_Config** | Rocket | Rocket | 0 (추적) | 120 | 2 |

3. 각 에셋 Inspector에서:
   - `overlaySprite`: 해당 특수 타일 오버레이 스프라이트 드래그앤드롭
   - `effectPrefab`: 전용 파티클 Prefab 드래그앤드롭

---

## Step 1 (코드): Strategy 패턴 — 폭발 로직

### ISpecialTileEffect 인터페이스
**파일 위치**: `Assets/Scripts/Model/SpecialTiles/`

```
추상 클래스: SpecialTileEffect : ScriptableObject
  - abstract List<int2> CalculateDestroyArea(int2 center, BoardModel model)
  - virtual int CalculateScore(int destroyedCount)

구현 클래스:
  - StripedEffect : SpecialTileEffect
    → center의 행(또는 열) 전체 좌표 반환
    → 방향(H/V)은 생성 시 스와이프 방향으로 결정

  - BombEffect : SpecialTileEffect
    → center 기준 이중루프(-range ~ +range) → effectRange=1이면 3×3
    → 경계 검사(IsValidPosition) 필수

  - ColorBombEffect : SpecialTileEffect
    → 타겟 색상 지정 → BoardModel 전체 스캔 → 해당 색 좌표 전부 반환

  - RocketEffect : SpecialTileEffect
    → 십자 4칸 + FindNearestTarget(목표물/장애물) 추적
```

### 시너지 매트릭스 처리
**파일 위치**: `Assets/Scripts/Model/SpecialTiles/SynergyMatrix.cs`

```
Dictionary<(SpecialTileType, SpecialTileType), ISynergyHandler> 매핑

시너지 조합별 핸들러:
- StripedStripedSynergy → 가로+세로 십자 좌표 합집합
- StripedBombSynergy → 3줄 두께 십자 (center ± 1 범위의 행+열)
- BombBombSynergy → effectRange=2 (5×5) 적용
- ColorBombSpecialSynergy → 타겟 색 전체를 해당 특수 타일로 변환 → 일괄 기폭
- ColorBombColorBombSynergy → 보드 전체 좌표 반환
```

**기폭 우선순위** (동시 기폭 시):
1. ColorBomb (priority 8) — 가장 먼저 처리
2. Bomb (priority 5) — 범위 폭발
3. Striped (priority 3) — 라인 폭발
4. Rocket (priority 2) — 추적 타격
5. 일반 매치 (priority 1)

---

## Step 1 (코드): MatchFinder Shape Recognition 확장

### 알고리즘 (교차점 기반)

```
FindSpecialMatches(BoardModel board, int2 swapPos) → List<MatchResult>

1단계: 수평 스캔
  - 각 행을 좌→우 순회, 연속 동일 타일 그룹 수집
  - 결과: List<MatchGroup> horizontalMatches (각 그룹에 좌표 List 포함)

2단계: 수직 스캔
  - 각 열을 상→하 순회, 동일하게 수집
  - 결과: List<MatchGroup> verticalMatches

3단계: 교차점 검사 (L/T자 인식)
  - horizontalMatches와 verticalMatches를 이중 루프로 비교
  - HashSet<int2>.Intersect()로 양쪽에 공통된 좌표(교집합) 찾기
  - 교차점 발견 → 두 그룹 합치기 → MatchResult.type = Bomb
  - 교차점 위치 = 특수 타일 생성 위치

4단계: 나머지 길이 검사
  - 교차하지 않는 그룹 중:
    - 길이 5+ → MatchResult.type = ColorBomb
    - 길이 4 → MatchResult.type = Striped (방향은 스와이프 방향)
    - 길이 3 → MatchResult.type = Normal

5단계: 결과 반환
  - MatchResult { type, tiles[], spawnPosition, spawnDirection }
```

---

## Step 2 (에디터): 특수 타일 Prefab 생성

**Unity 에디터 조작:**
1. Assets/Prefabs/Tiles/ 에 특수 타일 Prefab 생성:
   - `Tile_Striped_H.prefab`: BaseTile Variant + 가로 줄무늬 오버레이 Sprite
   - `Tile_Striped_V.prefab`: BaseTile Variant + 세로 줄무늬 오버레이 Sprite
   - `Tile_Bomb.prefab`: BaseTile Variant + 폭탄 오버레이 Sprite
   - `Tile_ColorBomb.prefab`: 독립 Prefab (색상 없음, 무지개 스프라이트)
   - `Tile_Rocket.prefab`: BaseTile Variant + 로켓 오버레이 Sprite

2. 각 Prefab Inspector:
   - SpriteRenderer: 기본 타일 스프라이트 (색상별)
   - 자식 SpriteRenderer: 오버레이 스프라이트 (sortingOrder +1)

---

## Step 2 (에디터): 특수 타일 전용 파티클

### 추가 파티클 Prefab (Assets/Prefabs/Effects/)

| Prefab | 용도 | Main Duration | Shape | 특징 |
|--------|------|---------------|-------|------|
| **StripedLineEffect** | 스트라이프 기폭 라인 | 0.3s | Edge(Line) | 보드 너비만큼 늘어나는 라인 |
| **BombExplosion** | 폭탄 기폭 | 0.4s | Circle r=1.5 | 3×3 범위 시각화, 2회 연속 |
| **ColorBombRainbow** | 컬러밤 기폭 | 0.6s | Sphere | 무지개색 Gradient, 보드 전체 |
| **RocketTrail** | 로켓 비행 궤적 | 1.0s | Cone | Trail Renderer 포함 |

**AI(MCP) 역할**: 임시 에디터 스크립트로 기본 Shape/Emission 설정
**디테일 조정**: script-execute로 AnimationCurve/Gradient 코드 설정, 최종 확인만 사용자 screenshot 검토

---

## Step 3 (에디터): Inspector 할당

1. BoardView Inspector의 `specialTilePrefabs[]` 배열에 5종 Prefab 할당
2. BoardView의 `specialTileConfigs[]` 배열에 5종 SO 에셋 할당
3. BoardView의 `specialEffectPrefabs[]` 배열에 4종 파티클 Prefab 할당

---

## 보너스 타임 (레벨 클리어 시)
- 남은 이동수 1회당 → 보드 위 일반 타일 1개를 **무작위 특수 타일로 변환 → 기폭**
- 변환 1회당 **1,000점** + 콤보 연쇄 폭발 점수
- 0.3초 간격으로 순차 변환 (시각적 연출)
- DOTween Sequence: Scale 0→1.3→1.0 (OutBack) + 기폭

---

## 검증 항목
- [ ] 4매치 → 스트라이프(방향 정확) 생성
- [ ] 5매치 직선 → 컬러밤 생성
- [ ] L/T자 교차 → 폭탄 생성 (교차점 위치에)
- [ ] 2×2 → 로켓 생성
- [ ] 스트라이프 기폭 → 1줄 전체 파괴
- [ ] 폭탄 기폭 → 3×3 범위 2회 폭발
- [ ] 컬러밤+일반 → 해당 색 전체 제거
- [ ] 시너지 6종 정상 작동 (매트릭스 전체 테스트)
- [ ] 기폭 우선순위 정상 (ColorBomb > Bomb > Striped > Rocket)
- [ ] 보너스 타임 연출 정상
- [ ] 전용 파티클 이펙트 재생
- [ ] SO Inspector에서 값 변경 → 즉시 반영

## MCP 도구 호출 순서 (NLM 검증 완료)

```
Step 1: script-update-or-create
  → SpecialTileConfig.cs (SO), ISpecialTileEffect.cs, StripedEffect.cs, BombEffect.cs, ColorBombEffect.cs, RocketEffect.cs
  → MatchFinder.cs에 Shape Recognition 로직 추가

Step 2: assets-refresh
  → 컴파일 대기

Step 3: script-execute
  → ScriptableObject.CreateInstance + AssetDatabase.CreateAsset으로
    SpecialTileConfig SO 에셋 5개 (StripedH, StripedV, Bomb, ColorBomb, Rocket) 파일 생성

Step 4: assets-prefab-create
  → 특수 타일 Prefab 5종 + 파티클 Prefab 4종 빈 프리팹 생성

Step 5: script-execute
  → 파티클 프리팹 로드 → ParticleSystem 모듈 설정 (Emission Burst, Shape, Color over Lifetime 등)
  → PrefabUtility.SaveAsPrefabAsset으로 저장

Step 6: gameobject-component-modify
  → BoardView Inspector의 specialTileConfigs[] 배열에 SO 에셋 5개 할당
  → BoardView Inspector의 specialTilePrefabs[] 배열에 Prefab 5종 할당
  → BoardView Inspector의 specialEffectPrefabs[] 배열에 파티클 4종 할당

Step 7: scene-save
  → 씬 저장

Step 8: editor-application-set-state {isPlaying: true}
  → Play 모드 테스트

Step 9: screenshot-game-view
  → 스크린샷 확인

Step 10: editor-application-set-state {isPlaying: false}
  → Play 모드 종료
```

## 진행률: 0%
