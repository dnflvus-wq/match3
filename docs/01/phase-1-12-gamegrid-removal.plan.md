# Phase 1-12: GameGrid.cs 완전 삭제 계획서

> **목표**: GameGrid.cs(1453줄) 완전 삭제. GameController.cs로 대체.
> **핵심 원칙**: 매 단계마다 게임이 정상 동작해야 함.
> **위험도**: 높음 — 12개 파일이 GameGrid를 참조

## 현재 상태

- GameGrid.cs (1453줄) = God Object. 보드 관리 + 매칭 + 낙하 + FSM + UI 이펙트 전부 포함
- BoardModel (100줄) ✅ 완성 — 순수 C# 보드 상태
- MatchFinder (221줄) ✅ 완성 — 순수 C# 매치 탐지
- DropSimulator (108줄) ✅ 완성 — 순수 C# 낙하 계산
- GameStateMachine (75줄) ✅ 완성 — 순수 C# 상태 전환
- BoardView (136줄) ⚠️ 껍데기 — 이벤트 핸들러 미구현
- GameController ❌ 미존재
- InputController (120줄) — GameGrid 강한 의존

## GameGrid를 참조하는 파일 (10개)

1. **GamePiece.cs** — `_gameGrid` 필드, `Init(GameGrid)`, `OnMouse*` 콜백
2. **MovablePiece.cs** — `_piece.GameGridRef.GetWorldPosition()` (3곳)
3. **ClearablePiece.cs** — `piece.GameGridRef.level.OnPieceCleared()`
4. **ClearLinePiece.cs** — `piece.GameGridRef.ClearRow/ClearColumn()`
5. **ClearColorPiece.cs** — `piece.GameGridRef.ClearColor()`
6. **InputController.cs** — `_grid` 필드, `GetComponent<GameGrid>()`
7. **BoosterUI.cs** — `_grid` 필드, `Init(GameGrid)`
8. **Level.cs** — `gameGrid` 필드, `.GameOver()`, `.IsFilling`
9. **MobileCameraSetup.cs** — `FindFirstObjectByType<GameGrid>()`
10. **BoardView.cs** — 주석만 (수정 불필요)

## 작업 단계

### Step 1: GameController.cs 생성
- GameGrid.cs 로직을 GameController.cs로 이관
- **변경점**:
  - `_currentState` → `GameStateMachine _stateMachine` 사용
  - `GetMatch()` 메서드 제거 (~185줄) → MatchFinder 사용
  - `FindValidMove()` 메서드 제거 (~45줄) → MatchFinder.FindValidMove() 사용
  - `ClearAllValidMatches()`에서 fallback GetMatch 제거, MatchFinder만 사용
  - `ShuffleBoard()`에서 GetMatch 호출 → MatchFinder 사용
  - 나머지 로직(FillStep, SpawnNewPiece, SwapPiecesCoroutine 등)은 그대로 이관
- **유지점**: `_pieces[,]`, `FillStep()`, 이펙트 코루틴들

### Step 2: GamePiece.cs 수정
- `GameGrid _gameGrid` → `GameController _controller`
- `GameGridRef` → `Controller`
- `Init(x, y, GameGrid, PieceType)` → `Init(x, y, GameController, PieceType)`
- `OnMouse*` 콜백 → `_controller.EnterPiece/PressPiece/ReleasePiece`

### Step 3: MovablePiece.cs 수정
- `_piece.GameGridRef.GetWorldPosition()` → `_piece.Controller.GetWorldPosition()` (3곳)

### Step 4: ClearablePiece.cs 수정
- `piece.GameGridRef.level` → `piece.Controller.level`

### Step 5: ClearLinePiece.cs 수정
- `piece.GameGridRef.ClearRow/ClearColumn` → `piece.Controller.ClearRow/ClearColumn`

### Step 6: ClearColorPiece.cs 수정
- `piece.GameGridRef.ClearColor` → `piece.Controller.ClearColor`

### Step 7: InputController.cs 수정
- `GameGrid _grid` → `GameController _controller`
- `GetComponent<GameGrid>()` → `GetComponent<GameController>()`
- `.CurrentState` → `._stateMachine.Current` 또는 `_controller.CurrentState` 프로퍼티 유지

### Step 8: BoosterUI.cs 수정
- `GameGrid _grid` → `GameController _controller`
- `Init(GameGrid)` → `Init(GameController)`

### Step 9: Level.cs 수정
- `GameGrid gameGrid` → `GameController gameController`

### Step 10: MobileCameraSetup.cs 수정
- `FindFirstObjectByType<GameGrid>()` → `FindFirstObjectByType<GameController>()`

### Step 11: GameGrid.cs 삭제
- 파일 삭제
- assets-refresh로 Unity 인식

### Step 12: Unity 씬 업데이트
- GameGrid 컴포넌트 제거, GameController 컴포넌트 추가
- Inspector 값 복사 (piecePrefabs, backgroundPrefab 등)
- MCP 도구로 수행 (gameobject-component-add, object-modify)

### Step 13: 컴파일 확인 + 에러 수정

### Step 14: Unity Play 모드 E2E 테스트
- Level 1~5 플레이 테스트
- 매치, 스왑, 낙하, 특수타일, 부스터, 게임오버 확인

### Step 15: 갭분석 99%

## 검증 항목 체크리스트
- [ ] GameGrid.cs 파일 완전 삭제
- [ ] 프로젝트에 "GameGrid" 문자열 참조 0개 (주석 제외)
- [ ] GameController가 GameStateMachine 사용
- [ ] GameController가 MatchFinder 사용 (GetMatch 메서드 없음)
- [ ] 모든 스크립트 컴파일 성공
- [ ] Unity 콘솔 에러 0개
- [ ] Level 1 정상 플레이 (매치→클리어→낙하→리필)
- [ ] 스왑 성공/실패 애니메이션 정상
- [ ] 특수타일 (RowClear, ColumnClear, Rainbow) 정상
- [ ] 부스터 (Hammer, Shuffle, +5) 정상
- [ ] 힌트 시스템 정상
- [ ] 게임오버 정상

## 검증 프로세스
1. 구현 완료 → 계획서 대비 갭분석 (99% 이상)
2. Unity Play 모드 E2E 테스트
3. 최종 갭분석 → 통과 시 Phase 1-12 완료
