# Match3 리팩토링 상세 계획서 (Unity 에디터 활용 중심)

> **이 문서만 보면 누구나 처음부터 끝까지 개발 가능**할 수준으로 작성.
> 각 단계마다 "코드로 할 것"과 "Unity 에디터에서 할 것"을 명확히 분리.
> "에디터에서 해야 함" 같은 추상적 지시 대신, **구체적 Inspector 설정값과 조작 순서** 명시.

## 핵심 원칙
- **에디터에서 할 수 있는 것은 에디터에서** — 코드로 생성 금지 (Prefab Instantiate만 허용)
- **Edit 모드에서도 보드가 보여야 함** — `[ExecuteAlways]` + Custom Inspector "Generate Board" 버튼
- **Logic / View 완전 분리** — Logic은 순수 C# (Unity API 의존 없음), View는 MonoBehaviour
- **이벤트 통신**: Model→View는 C# `Action`, View→Controller는 콜백, Controller→Model은 직접 호출
- **오브젝트 풀링 필수** — Instantiate/Destroy 금지, 풀 사이즈 72~90개 (6×6 보드 기준)
- **애니메이션 설정값은 `[SerializeField, Range()]`** — 하드코딩 절대 금지

## AI(MCP)가 전부 처리 (사람은 최종 확인만)

| 작업 | MCP 도구 |
|------|---------|
| **스크립트** | `script-update-or-create` |
| **컴포넌트 부착** | `gameobject-component-add` |
| **Inspector 값 설정** | `gameobject-component-modify` |
| **파티클 설정** | `script-execute` (ParticleSystem 모듈 코드 설정) |
| **카메라/UI 배치** | `script-execute` (RectTransform, Camera 속성 코드 설정) |
| **Prefab 생성** | `assets-prefab-create` 또는 `script-execute` |
| **에셋 검색** | `assets-find` |
| **컴파일** | `assets-refresh` |
| **Play 테스트** | `editor-application-set-state` |
| **스크린샷** | `screenshot-game-view` |
| **에러 확인** | `console-get-logs` |
| **씬 저장** | `scene-save` |
| **UI 클릭 (MCP 한계 시)** | `win32-inspector` (capture_screen, click, type_text) |

> **사람은 최종 스크린샷 확인 + 승인만.** 나머지 전부 AI가 MCP로 처리.

## 계획서 각 단계 작성 규칙 (NotebookLM 피드백)
> **Step 1** (코드): 스크립트 생성 → **Step 2** (에디터): 씬에 GameObject 생성 + 컴포넌트 부착 → **Step 3** (에디터): Inspector에서 Prefab/값 할당
> 이 3단계를 빠짐없이 명시해야 AI가 누락하지 않음

## 리서치 출처
- [Catlike Coding Match3](https://catlikecoding.com/unity/tutorials/prototypes/match-3/) — MVC 분리, 풀링, hot reload
- [Unity Gem Hunter Match](https://unity.com/blog/engine-platform/2d-puzzle-match-3-sample-gem-hunter-match) — URP, 2D 라이팅
- [ExecuteAlways 공식 문서](https://docs.unity3d.com/ScriptReference/ExecuteAlways.html)
- [adammyhre/Match3](https://github.com/adammyhre/Match3) — DOTween 기반 애니메이션

---

## 검증 프로세스 (매 Phase 완료 후)
1. 갭분석 (계획서 대비 99% 일치)
2. Unity Play 모드 E2E 테스트
3. 최종 갭분석 → 사용자 보고
4. 빌드는 사용자 승인 후에만

---

## Phase 1: 에디터 에셋 기반 구축 ✅ 완료 (2026-03-23)
> 상세 내용 생략 (이미 완료)

### 완료 항목
- [x] BaseTile.prefab (SpriteRenderer[Tiles layer] + BoxCollider2D[trigger] + TileView)
- [x] Tile Variant 8종 (6색 sprite 할당)
- [x] 파티클 Prefab 4종 Inspector 설정
- [x] UI Prefab 4종 이미지 할당
- [x] LevelConfigEditor.cs Custom Editor
- [x] Sprite Atlas 2종 packables 등록
- [x] 기존 게임 정상 동작 확인

---

## Phase 2: 카메라 + Edit 모드 프리뷰 수정 (신규)
**목표**: Edit 모드 Game View에서도 게임 화면이 제대로 보이게 수정
**위험도**: 낮음 — 기존 게임 로직 안 건드림

### 2-1. 카메라 설정 (Unity 에디터에서 수동 설정)

**조작 순서:**
1. Hierarchy > `MainCamera` 선택
2. Inspector > Camera 컴포넌트:
   - Clear Flags: `Solid Color`
   - Background: `#0D0D26` (어두운 남색)
   - Projection: `Orthographic` (이미 설정됨)
   - Size: `4.9` (보드 높이 6 + UI 여백 기준)
   - Clipping Planes Near: `-1`, Far: `20`
3. Transform:
   - Position: `(0, -4.9, -10)` — 보드 중앙이 화면 중앙-아래에 오도록
   - Rotation: `(0, 0, 0)`
4. **모든 레벨 씬(Level01~10)에 동일하게 적용** → Prefab으로 만들면 일괄 적용 가능

**코드 작업**: MobileCameraSetup.cs에서 clearFlags 강제 변경하는 코드가 있다면 제거

### 2-2. 보드 Edit 모드 프리뷰 (코드 작업)

**목적**: Edit 모드에서도 보드 그리드가 시각적으로 보이게 함

**작성할 스크립트**: `BoardPreview.cs`
```
[ExecuteAlways]를 사용
OnValidate()에서 보드 크기 변경 감지
Edit 모드에서 Gizmos로 보드 그리드 라인 표시
선택적으로 더미 타일 스프라이트 표시
```

**Unity 에디터에서 설정:**
1. GameGrid 오브젝트에 BoardPreview 컴포넌트 추가
2. Inspector에서:
   - `boardWidth`: 6
   - `boardHeight`: 6
   - `cellSize`: 1.0
   - `gridColor`: 반투명 흰색 (255, 255, 255, 50)
   - `showDummyTiles`: true (Edit 모드에서 더미 타일 표시)
   - `dummyTileSprites[]`: fish_1~6 스프라이트 할당

**BoardPreview.cs 핵심 코드 구조:**
```csharp
[ExecuteAlways]
public class BoardPreview : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] int boardWidth = 6;
    [SerializeField] int boardHeight = 6;
    [SerializeField] float cellSize = 1.0f;
    [SerializeField] Color gridColor = new Color(1, 1, 1, 0.2f);

    [Header("Preview")]
    [SerializeField] bool showDummyTiles = true;
    [SerializeField] Sprite[] dummyTileSprites; // fish_1~6 할당

    // Edit 모드에서 Scene View에 그리드 표시
    void OnDrawGizmos()
    {
        Vector3 origin = transform.position;
        Gizmos.color = gridColor;
        // 세로선
        for (int x = 0; x <= boardWidth; x++)
            Gizmos.DrawLine(
                origin + new Vector3(x * cellSize, 0, 0),
                origin + new Vector3(x * cellSize, boardHeight * cellSize, 0));
        // 가로선
        for (int y = 0; y <= boardHeight; y++)
            Gizmos.DrawLine(
                origin + new Vector3(0, y * cellSize, 0),
                origin + new Vector3(boardWidth * cellSize, y * cellSize, 0));
    }

    // Inspector 값 변경 시 호출 (Edit 모드에서도)
    void OnValidate()
    {
        if (showDummyTiles && !Application.isPlaying)
            SpawnDummyTiles();
    }
}
```

**주의사항:**
- `[ExecuteAlways]` 사용 (`[ExecuteInEditMode]`는 deprecated)
- Play 모드 진입 시 더미 타일 자동 파괴 (실제 게임 타일과 충돌 방지)
- OnDrawGizmos는 Scene View에서만 보임, Game View에서 보려면 Gizmos 토글 ON 필요

### 2-3. 배경 스프라이트 확인

**Unity 에디터에서 확인:**
1. Hierarchy > `Background` 선택
2. SpriteRenderer 확인:
   - Sprite: `bg` (또는 bg2) — 할당되어 있어야 함
   - Sorting Order: `-100`
   - Draw Mode: `Simple`
3. Transform:
   - Position: `(0, -4.9, 0)` — 카메라 Y와 동일
   - Scale: 배경이 화면을 완전히 덮도록 조정

### 검증 항목
- [ ] Edit 모드 Game View에서 배경이 보임
- [ ] Edit 모드 Game View에서 그리드 라인/더미 타일이 보임
- [ ] Play 모드에서 기존 게임 정상 동작
- [ ] 카메라 Clear Flags = SolidColor 확인

---

## Phase 3: InputController 추출 (Strangler Pattern 시작)
**위험도**: 낮음 — 가장 의존성 적은 입력 로직부터 분리
**참고**: NotebookLM 피드백 — "가장 핵심적이고 복잡한 데이터(Model)를 먼저 건드리면 에러 폭발"

> **Strangler Pattern 핵심**: 바깥쪽(의존성 적은 곳)부터 도려내기.
> InputController는 GameGrid의 나머지 로직에 의존하지 않으므로 가장 먼저 분리 가능.

### 3-1. InputController.cs (MonoBehaviour — 코드 + 에디터 작업)

**파일 위치**: `Assets/Scripts/Controller/InputController.cs`

```
역할: 터치/마우스 입력 → 그리드 좌표 변환 → GameGrid.TrySwap() 호출
메서드:
- HandleInput() — Update()에서 호출
- ScreenToTileSpace(Vector3 screenPos) → int2
- EvaluateDrag(Vector3 start, Vector3 end) → (int2 from, int2 to)?
```

**Unity 에디터 설정:**
1. GameGrid 오브젝트에 InputController 컴포넌트 추가
2. Inspector:
   - `dragThreshold`: `0.5` (Range 0.1~1.0)
   - `camera`: Main Camera 드래그앤드롭 할당
   - `gameGrid`: GameGrid 자동 연결 (GetComponent 또는 드래그앤드롭)

**MCP 도구 호출 순서:**
```
1. script-update-or-create → InputController.cs 생성
2. assets-refresh → 컴파일
3. gameobject-component-add → GameGrid에 InputController 추가
4. gameobject-component-modify → dragThreshold, camera 설정
5. editor-application-set-state → Play 모드 테스트
6. screenshot-game-view → 동작 확인
```

**Strangler 적용:**
- GameGrid.cs에서 입력 관련 코드(Update의 터치/드래그 처리)를 InputController로 이동
- GameGrid에는 `public void TrySwap(int2 from, int2 to)` 메서드만 남김
- InputController가 이 메서드를 호출

### 검증 항목
- [ ] InputController에서 터치 입력 → TrySwap 호출 정상 동작
- [ ] 기존 게임 플레이 동일하게 동작
- [ ] GameGrid.cs에서 입력 코드 완전 제거

---

## Phase 4: Model 분리 (순수 C# — Unity 의존 없음)
**위험도**: 중간 — 핵심 로직 분리, GameGrid에서 호출만 변경
**참고**: Catlike Coding의 Match3Game 패턴

### 4-1. BoardModel.cs (순수 C# — 코드 작업만)

**파일 위치**: `Assets/Scripts/Model/BoardModel.cs`

```
Grid2D<TileState> 구조 (직렬화 가능, hot reload 지원)
메서드:
- Initialize(int width, int height)
- GetTileAt(int x, int y) → TileState
- SetTileAt(int x, int y, TileState state)
- SwapTiles(int2 a, int2 b) → bool
- IsValidPosition(int x, int y) → bool
```

**Unity 에디터 작업**: 없음 (순수 C# 클래스)

### 4-2. MatchFinder.cs (순수 C# — 코드 작업만)

**파일 위치**: `Assets/Scripts/Model/MatchFinder.cs`

```
메서드:
- FindAllMatches(BoardModel board) → List<Match>
- HasValidMove(BoardModel board) → bool (데드보드 감지)
- GetHintMove(BoardModel board) → Move
- CheckSwapCreatesMatch(BoardModel board, int2 a, int2 b) → bool
```

**특수 타일 Shape Recognition 알고리즘** (NotebookLM 리서치):
```
1단계: 수평 스캔 → 연속 동일 타일 그룹 수집 (가로 매치 리스트)
2단계: 수직 스캔 → 연속 동일 타일 그룹 수집 (세로 매치 리스트)
3단계: 교차점 검사 → 가로+세로 양쪽에 포함된 타일 좌표 = L/T자 교차점
  - 교차점 발견 → 폭탄(Bomb) 생성 위치
4단계: 나머지 매치 길이 검사
  - 길이 5+ → 컬러밤(Color Bomb)
  - 길이 4 → 스트라이프(Striped) — 스와이프 방향에 따라 가로/세로
  - 길이 3 → 일반 매치 (특수 타일 없음)
```

**데드 보드 감지 + 셔플** (FindValidMove):
```
- 모든 인접 타일 쌍에 대해 가상 스왑 → 매치 가능 여부 확인
- 유효한 이동 없으면 → ShuffleBoard() 호출
- 셔플 후 재검사 → 최대 3회 반복
```

**초기 매치 방지 알고리즘** (Catlike Coding 패턴):
```
FillGrid 시 이전 2개 타일과 같은 색 금지
potentialMatchCount로 가능한 색 수 제한
```

### 4-3. DropSimulator.cs (순수 C# — 코드 작업만)

**파일 위치**: `Assets/Scripts/Model/DropSimulator.cs`

```
메서드:
- CalculateDrops(BoardModel board) → List<TileDrop>
- GenerateNewTiles(BoardModel board) → List<TileSpawn>
```

**Strangler Pattern 적용 순서:**
1. BoardModel 생성 → GameGrid에서 `_pieces[,]` 대신 BoardModel 읽도록 변경
2. MatchFinder 생성 → GameGrid의 매치 검사 로직을 MatchFinder 호출로 교체
3. DropSimulator 생성 → GameGrid의 낙하 로직을 DropSimulator 호출로 교체
4. 각 단계마다 Play 모드 테스트 → 기존 동작과 동일해야 함

### 4-4. 누락된 핵심 로직 (NotebookLM 피드백 — Phase 4에서 반드시 구현)

**특수 타일 Shape Recognition** (MatchFinder.cs에 포함):
```
- 직선 4매치 → 스트라이프 타일 (가로/세로 방향)
- L자/T자 5매치 → 폭탄 타일 (3x3 범위 폭발)
- 직선 5매치 → 컬러밤 타일 (같은 색 전체 제거)
- 특수 타일 + 특수 타일 조합 매트릭스 (BoardModel에 저장)
```

**데드 보드 감지 + 셔플** (MatchFinder.cs에 포함):
```
- FindValidMove(BoardModel board) → Move?
  : 보드에 유효한 이동이 1개라도 있는지 스캔
- 유효한 이동이 없으면 → 보드 자동 셔플
- 셔플 후에도 유효 이동이 없으면 → 재셔플 (최대 3회)
```

**장애물 + 비정방형 맵** (BoardModel 확장):
```
- CellState enum: Normal, Empty, Ice, Chain, Stone, Box
- Grid2D<CellState> cellStates — 각 셀의 상태 관리
- 장애물은 매치로 파괴 가능(Ice, Chain) / 불가(Stone)
- Empty 셀은 타일이 놓이지 않는 빈 공간 (비정방형 맵 지원)
```

### 검증 항목
- [ ] BoardModel이 GameGrid의 _pieces[,] 역할 대체
- [ ] MatchFinder가 매치 검사 정확히 수행 (가로/세로 3+ 매치)
- [ ] 특수 타일 Shape Recognition 동작 (4매치→스트라이프 등)
- [ ] 데드 보드 감지 시 자동 셔플
- [ ] DropSimulator가 낙하/스폰 정확히 계산
- [ ] 모든 레벨 정상 플레이 (Play 모드 E2E)
- [ ] 초기 보드에 매치 없음

---

## Phase 5: Controller + FSM 분리 (코드 작업)
**위험도**: 중간

### 5-1. GameStateMachine.cs (코드 작업만)

```
상태: PREGAME → READY → EVALUATE → MATCH → COLLAPSE → (재귀) → READY / ENDGAME
입력은 READY 상태에서만 허용
```

### 4-2. InputController.cs (MonoBehaviour — 에디터 설정 필요)

**코드 작업**: 터치/마우스 입력 → 그리드 좌표 변환

**Unity 에디터 설정:**
1. GameGrid 오브젝트에 InputController 컴포넌트 추가
2. Inspector:
   - `dragThreshold`: `0.5` (Range 0.1~1.0) — 스와이프 최소 거리
   - `camera`: Main Camera 드래그앤드롭 할당

### 4-3. GameController.cs (MonoBehaviour — 에디터 설정 필요)

**Unity 에디터 설정:**
1. GameGrid 오브젝트에 GameController 컴포넌트 추가
2. Inspector:
   - `boardModel`: 자동 연결 (GetComponent)
   - `boardView`: 자동 연결
   - `inputController`: 자동 연결
   - `stateMachine`: 자동 연결

### 이벤트 통신 패턴 (NotebookLM 피드백 — 매우 중요)

| 방향 | 방식 | 예시 |
|------|------|------|
| **Model → View** | C# `Action`/`event` (순수 C#, UnityEvent 금지) | `public event Action<int2, int2> OnTilesSwapped;` |
| **View → Controller** | 콜백/코루틴 | `OnAnimationComplete()` → FSM 상태 전이 |
| **Controller → Model** | 직접 참조 호출 | `boardModel.TrySwap(from, to)` |

> **핵심**: Model은 UnityEngine에 의존하면 안 됨. `System.Action`만 사용.
> BoardView가 BoardModel의 이벤트를 구독(Subscribe)하여 애니메이션 재생.

### 검증 항목
- [ ] FSM이 입력 차단/허용 올바르게 관리
- [ ] 연쇄 반응(Cascade) 무한 루프 없음
- [ ] 게임오버 조건 정상 판정
- [ ] Model→View 이벤트 통신 정상 (C# Action)

---

## Phase 5: View 분리 + Prefab 연결
**위험도**: 높음 — 시각적 변경, 씬 수정
**이 Phase에서 Edit 모드 프리뷰와 런타임 렌더링이 통합됨**

### 5-1. BoardView.cs (MonoBehaviour — 코드 + 에디터 작업)

**코드 작업:**
```
[ExecuteAlways] 적용
Phase 1의 타일 Prefab을 오브젝트 풀에서 가져와 배치
이벤트 구독: BoardModel.OnMatchFound → 파괴 애니메이션 + 파티클 스폰
```

**Unity 에디터 설정:**
1. GameGrid 오브젝트에 BoardView 컴포넌트 추가 (기존 GameGrid 대체)
2. Inspector:
   - `tilePrefabs[]`: 6개 배열에 Tile_Yellow, Tile_Purple, Tile_Red, Tile_Blue, Tile_Green, Tile_Pink 드래그앤드롭
   - `matchBurstPrefab`: Assets/Prefabs/Effects/MatchBurst 할당
   - `matchStarPrefab`: Assets/Prefabs/Effects/MatchStar 할당
   - `matchGlowPrefab`: Assets/Prefabs/Effects/MatchGlow 할당
   - `matchRingPrefab`: Assets/Prefabs/Effects/MatchRing 할당
   - `poolInitialSize`: `20` — 초기 풀 크기
   - `cellSize`: `1.0`
   - `boardOffset`: `(0, 0)` — 보드 좌측하단 월드 위치

### 5-2. 애니메이션 설정 (DOTween — Inspector에서 값 조정)

**BoardView Inspector:**
- `swapDuration`: `0.25` (Range 0.1~0.5) — 타일 교환
- `swapEase`: `Ease.InOutQuad` — 부드러운 교환
- `failSwapDuration`: `0.2` — 실패 시 복귀
- `dropSpeed`: `8.0` (Range 1~20) — 낙하 속도 (units/sec)
- `dropEase`: `Ease.OutBounce` — 바운스 착지
- `disappearDuration`: `0.15` (Range 0.05~0.5) — 매치 사라짐
- `disappearEase`: `Ease.InBack` — 안쪽으로 축소
- `newTileDropOffset`: `2.0` — 새 타일 스폰 높이 (보드 위)
- `comboTextDuration`: `0.8` — 콤보 텍스트 표시 시간

### 5-3. TileView.cs 확장 (코드 작업)

```
메서드:
- SetSprite(Sprite sprite)
- MoveTo(Vector3 target, float duration, Ease ease) → Tween
- PlayDestroyEffect() → float (소요 시간 반환)
- PlayHintPulse() — 힌트 깜빡임
- SetHighlight(bool on) — 선택 하이라이트
```

### 검증 항목
- [ ] 타일 Prefab이 정상 인스턴스화 (풀에서)
- [ ] 파티클 Prefab이 매치 시 재생
- [ ] DOTween 애니메이션 부드럽게 동작
- [ ] Edit 모드에서도 보드 프리뷰 보임
- [ ] 오브젝트 풀링으로 GC 스파이크 제거

---

## Phase 6: UI를 Prefab 기반으로 전환
**위험도**: 중간

### 6-1. LevelSelect 씬 — UI 에디터에서 직접 배치

**Unity 에디터 조작 순서:**
1. LevelSelect 씬 열기
2. 기존 LevelSelect.cs의 동적 UI 생성 코드 비활성화
3. Canvas에 직접 배치:
   - Canvas:
     - Render Mode: `Screen Space - Overlay`
     - Canvas Scaler: `Scale With Screen Size`
     - Reference Resolution: `1080 x 2160`
     - Match Width Or Height: `0.5`
   - 배경 Image:
     - Source Image: `level_select_bg`
     - Image Type: `Simple`
     - Preserve Aspect: `true`
   - 레벨 버튼 5개 (LevelButton Prefab 배치):
     - 3열 그리드: 1행(1,2,3) 2행(4,5)
     - 각 버튼 RectTransform:
       - Width: `220`, Height: `220`
       - 행 간격: `280px`
     - LevelButton Prefab 드래그앤드롭으로 배치
4. `LevelSelectUI.cs` 작성 (코드): 데이터 바인딩만 (별점, 잠금 상태)

### 6-2. 인게임 HUD — Canvas에서 직접 배치

**Unity 에디터 조작 순서:**
1. Level01 씬 > `GameUICanvas` 선택
2. HudPanel Prefab을 Canvas 자식으로 배치:
   - Anchor: `Top Stretch` (좌우 꽉 참)
   - Height: `320px`
   - Pivot: `(0.5, 1)` — 상단 기준
3. HudPanel 내부 레이아웃:
   - 3분할: 좌측(Target) / 중앙(Moves) / 우측(Score)
   - 각 영역: Anchor `1/3씩`, Text fontSize `48`, Color `White`
4. `HudUI.cs` 작성 (코드): 점수/이동수 Text 업데이트만

### 6-3. 부스터 UI

**Unity 에디터 조작:**
1. Canvas 하단에 BoosterButton Prefab 3개 배치:
   - Horizontal Layout Group: Spacing `30`, Alignment `Middle Center`
   - 각 버튼: Width `120`, Height `120`
   - 아이콘 할당: hammer_icon, shuffle_icon, extra_moves_icon
2. `BoosterUI.cs` 축소 (코드): 데이터 바인딩과 클릭 이벤트만

### 6-4. 게임오버

**Unity 에디터 조작:**
1. GameOverPanel Prefab을 Canvas 자식으로 배치 (비활성 상태)
2. 내부 구성:
   - ResultText: fontSize `64`, "Level Clear!" / "Game Over"
   - Stars 영역: 3개 Image (star_filled/star_empty 스프라이트)
   - ReplayButton: Width `180`, Height `60`
   - HomeButton: Width `180`, Height `60`
3. `GameOverUI.cs` (코드): 표시/숨김 + 별 애니메이션만

### 검증 항목
- [ ] 레벨 선택 화면이 에디터 배치 기반으로 정상 표시
- [ ] HUD 정상 (점수, 이동수, 코인) — Edit 모드에서도 UI 보임
- [ ] 부스터 동작 (망치, 셔플, +5)
- [ ] 게임오버 연출 정상

---

## Phase 7: 사운드/오디오 개선
**위험도**: 낮음

### Unity 에디터 작업:
1. **Audio Mixer 생성**: Assets > Create > Audio Mixer
   - Master Group
   - BGM Group: Volume 기본 -10dB
   - SFX Group: Volume 기본 0dB
2. **Audio Source 설정** (GameGrid 오브젝트):
   - Output: Audio Mixer의 SFX Group
   - Play On Awake: `false`
3. **Audio Clip 파일**: Assets/Audio/ 폴더에 배치
   - match.wav, swap.wav, combo.wav, clear.wav, gameover.wav, bgm.mp3

### 코드 작업:
- ProceduralAudio.cs 163줄 → **제거**
- AudioManager.cs 리팩토링: Mixer 기반으로 전환

---

## Phase 8: 데이터 저장 리팩토링
(코드 작업 중심 — Unity 에디터 작업 최소)

### 코드 작업:
- UserData.cs (순수 C# POCO)
- DataManager.cs (JSON → persistentDataPath)
- PlayerPrefs 완전 제거

---

## Phase 9: 최종 정리 + GameGrid.cs 제거
- GameGrid.cs → BoardModel + BoardView + GameController로 완전 대체 확인 후 삭제
- 미사용 코드/파일 정리
- 프로파일링 + 최적화
- 전체 레벨 E2E 테스트

---

## 파티클 이펙트 상세 설정 (Unity Inspector)

### MatchBurst.prefab (매치 시 스파클 폭발)
| 모듈 | 속성 | 값 |
|------|------|-----|
| **Main** | Duration | `0.5` |
| | Looping | `false` |
| | Start Lifetime | `0.3 ~ 0.5` (Random Between Two Constants) |
| | Start Speed | `3.0 ~ 5.0` |
| | Start Size | `0.1 ~ 0.2` |
| | Start Color | `White` (런타임에 타일 색으로 변경) |
| | Gravity Modifier | `1.2` |
| | Play On Awake | `false` |
| | Max Particles | `30` |
| | Simulation Space | `World` |
| **Emission** | Rate over Time | `0` |
| | Burst: Time 0 | Count `20`, Cycles `1` |
| **Shape** | Shape | `Sphere` |
| | Radius | `0.1` |
| **Color over Lifetime** | Gradient | Alpha: `1.0 → 0.0` (선형) |
| **Size over Lifetime** | Curve | `1.0 → 0.0` (선형 감소) |
| **Renderer** | Render Mode | `Billboard` |
| | Sorting Order | `100` |
| | Material | `Default-Particle` |

### MatchStar.prefab (별 모양 파티클)
| 모듈 | 속성 | 값 |
|------|------|-----|
| **Main** | Duration | `0.8` |
| | Start Lifetime | `0.5 ~ 0.8` |
| | Start Speed | `2.0` |
| | Start Size | `0.3 ~ 0.5` |
| | Start Rotation | `0 ~ 360` (Random Between) |
| | Gravity Modifier | `0.3` |
| | Play On Awake | `false` |
| | Max Particles | `20` |
| **Emission** | Burst: Time 0 | Count `15` |
| **Shape** | Shape | `Sphere`, Radius `0.2` |
| **Rotation over Lifetime** | Angular Velocity | `45` |
| **Color over Lifetime** | Gradient | Alpha: `1.0 → 0.0` |
| **Renderer** | Sorting Order | `101` |

### MatchGlow.prefab (중앙 글로우)
| 모듈 | 속성 | 값 |
|------|------|-----|
| **Main** | Duration | `0.3` |
| | Start Lifetime | `0.3` |
| | Start Speed | `0` |
| | Start Size | `1.5` |
| | Start Color | `White, Alpha 0.8` |
| | Play On Awake | `false` |
| | Max Particles | `3` |
| **Emission** | Burst: Time 0 | Count `1` |
| **Shape** | Shape | `Sphere`, Radius `0.01` |
| **Size over Lifetime** | Curve | `0.5 → 1.5 → 0.0` (팽창 후 소멸) |
| **Color over Lifetime** | Alpha | `0.8 → 0.0` |
| **Renderer** | Sorting Order | `99` |

### MatchRing.prefab (원형 파동)
| 모듈 | 속성 | 값 |
|------|------|-----|
| **Main** | Duration | `0.4` |
| | Start Lifetime | `0.3 ~ 0.4` |
| | Start Speed | `4.0` |
| | Start Size | `0.08 ~ 0.12` |
| | Gravity Modifier | `0` |
| | Play On Awake | `false` |
| | Max Particles | `36` |
| **Emission** | Burst: Time 0 | Count `30` |
| **Shape** | Shape | `Circle` (2D), Radius `0.05` |
| **Color over Lifetime** | Alpha | `1.0 → 0.0` |
| **Size over Lifetime** | Curve | `1.0 → 0.3` |
| **Renderer** | Sorting Order | `102` |

---

## DOTween 애니메이션 상세 설정

| 애니메이션 | Duration | Ease | 비고 |
|-----------|----------|------|------|
| 타일 스왑 | `0.25s` | `InOutQuad` | 두 타일 동시 이동 |
| 스왑 실패 복귀 | `0.2s` | `InOutQuad` | pingpong 원위치 |
| 타일 낙하 | 거리/`8.0` | `OutBounce` | 속도 기반 (8 units/sec) |
| 타일 사라짐 | `0.15s` | `InBack` | Scale 1→0 |
| 새 타일 스폰 낙하 | 거리/`8.0` | `OutBounce` | 보드 위 2유닛에서 시작 |
| 콤보 텍스트 | `0.8s` | `OutBack` | Scale 0→1.3→1.0, Y +50 이동 |
| 힌트 펄스 | `0.5s` 반복 | `InOutSine` | Scale 1.0↔1.15 무한 반복 |
| 게임오버 별 | `0.5s` × 3 | `OutBack` | 순차 표시, Scale 0→1.3→1.0 |

---

## 카메라 상세 설정 (모든 레벨 씬 공통)

| 속성 | 값 | 이유 |
|------|-----|------|
| Clear Flags | `Solid Color` | Edit 모드에서도 깨끗한 배경 |
| Background Color | `#0D0D26` | 어두운 남색, 판타지 테마와 어울림 |
| Projection | `Orthographic` | 2D 게임 |
| Size | `4.9` | 보드 6칸 + HUD 영역 커버 |
| Position | `(0, -4.9, -10)` | 보드+HUD가 화면에 맞도록 |
| Clipping Near | `-1` | 2D 스프라이트 Z 오프셋 허용 |
| Clipping Far | `20` | |

**Portrait 1080x2160 기준 계산:**
- orthographicSize = 4.9 → 화면 높이 = 9.8 world units
- aspect = 1080/2160 = 0.5 → 화면 너비 = 9.8 × 0.5 = 4.9 world units
- 보드 6×6 (각 셀 1 unit) → 너비 6 < 4.9? → 여유 없음, 보드 오프셋 필요
- **실제 적용**: MobileCameraSetup.cs에서 보드 크기에 맞게 동적 조정

---

## 폴더 구조 목표

```
Assets/
├── Prefabs/
│   ├── Tiles/
│   │   ├── BaseTile.prefab         ← 베이스 (SpriteRenderer + BoxCollider2D + TileView)
│   │   ├── Tile_Red.prefab         ← Variant (Sprite만 오버라이드)
│   │   └── ... (8색)
│   ├── Effects/
│   │   ├── MatchBurst.prefab       ← Inspector에서 Particle System 설정
│   │   ├── MatchStar.prefab
│   │   ├── MatchGlow.prefab
│   │   └── MatchRing.prefab
│   └── UI/
│       ├── LevelButton.prefab      ← Canvas에서 드래그앤드롭 배치
│       ├── BoosterButton.prefab
│       ├── HudPanel.prefab
│       └── GameOverPanel.prefab
├── ScriptableObjects/
│   ├── GameConfig.asset            ← Inspector에서 수치 편집
│   └── Levels/
│       ├── Level01.asset ~ Level05.asset
├── Scripts/
│   ├── Model/         ← 순수 C# (BoardModel, MatchFinder, DropSimulator)
│   ├── View/          ← MonoBehaviour (BoardView, TileView)
│   ├── Controller/    ← MonoBehaviour (GameController, InputController, FSM)
│   ├── UI/            ← 데이터 바인딩만 (HudUI, LevelSelectUI, GameOverUI, BoosterUI)
│   ├── Data/          ← UserData, DataManager
│   ├── System/        ← AudioManager, PrefabInstancePool, CoinSystem
│   └── Editor/        ← LevelConfigEditor, Phase1Setup, BoardPreview
├── Audio/
│   ├── SFX/           ← match.wav, swap.wav 등
│   └── BGM/           ← bgm.mp3
└── SpriteAtlases/
    ├── TileAtlas.spriteatlas
    └── UIAtlas.spriteatlas
```

---

## Phase별 "에디터 작업 vs 코드 작업" 요약

| Phase | 에디터에서 할 것 | 코드로 할 것 |
|-------|----------------|-------------|
| 1 (완료) | Prefab 생성, Inspector 설정, Sprite Atlas 구성 | TileView.cs, GameConfig.cs, LevelConfig.cs, Custom Editor |
| 2 (신규) | 카메라 Inspector 설정, 배경 위치 조정 | BoardPreview.cs ([ExecuteAlways] + Gizmos) |
| 3 | 없음 | BoardModel, MatchFinder, DropSimulator (순수 C#) |
| 4 | InputController Inspector 설정 | GameStateMachine, InputController, GameController |
| 5 | tilePrefabs[] Inspector 할당, Prefab 드래그앤드롭 | BoardView.cs ([ExecuteAlways]), TileView 확장, 풀링 |
| 6 | Canvas에 UI Prefab 배치 (드래그앤드롭), Anchor/Size 설정 | HudUI, LevelSelectUI, GameOverUI, BoosterUI (바인딩만) |
| 7 | Audio Mixer 생성, Audio Source 설정, Clip 할당 | AudioManager 리팩토링 |
| 8 | 없음 | UserData, DataManager, PlayerPrefs 제거 |
| 9 | 씬 정리, 미사용 에셋 제거 | GameGrid.cs 삭제, 프로파일링 |
