# Match3 게임 고도화 - 인수인계 문서

## 현재 상태 (2026-03-25)

### Phase 완료 현황

| Phase | 내용 | 상태 |
|-------|------|------|
| 1 | 에디터 에셋 기반 구축 | ✅ |
| 2 | 카메라 + Edit 모드 프리뷰 | ✅ |
| 3 | InputController 추출 | ✅ |
| 4 | BoardModel + MatchFinder 추출 | ✅ |
| 5 | Controller + FSM 분리 | ✅ |
| 5b | View 분리 + Prefab 연결 | 미착수 |
| 6 | UI Prefab 전환 | 미착수 |
| 7~ | Audio, 최적화 등 | 미착수 |

### 코드 구조

```
Controller/
  GameGrid.cs              153줄  위임만, 비즈니스 로직 0 (로직 126 + XML summary 27) — Init 시그니처 변경 (view 제거)
  InputController.cs       139줄  (로직 135 + XML summary 4)

Model/
  BoardModel.cs            438줄  EvaluateSwap, Action 이벤트 5개, _view 제거 (로직 418 + XML summary 20)
  MatchFinder.cs           265줄  순수 C# (기존 summary 포함)
  GameStateMachine.cs       26줄  순수 C# FSM
  IBoardState.cs, MatchResult.cs (SwapResult enum)

View/
  BoardView.cs             423줄  코루틴 + 시각효과, 이벤트 구독 5개 (로직 408 + XML summary 15)
  BoardPreview.cs           72줄  [ExecuteAlways]

Piece/
  GamePiece.cs              92줄  GameGridRef 제거 완료 (0개)
  MovablePiece.cs          136줄  const 4개
  ClearablePiece.cs         60줄  const 1개
  ClearLinePiece.cs, ClearColorPiece.cs, ColorPiece.cs

System/
  MatchParticles.cs         77줄  SerializeField Prefab (274줄→77줄)
  AudioManager.cs          113줄  SerializeField AudioClip + ProceduralAudio fallback
  ProceduralAudio.cs       163줄  fallback 전용

Data/
  LevelConfig.cs            32줄  ScriptableObject
  PiecePool.cs              82줄  미사용
```

### 핵심 지표

| 지표 | 값 |
|------|---|
| GameGrid.cs | 153줄 (로직 126 + XML summary 27, 로직 130 이하 ✅) |
| GameGridRef 사용처 | 0개 ✅ |
| BoardModel _view 직접 호출 | 0곳 ✅ (_view 필드 제거 완료) |
| BoardModel Action 이벤트 | 5개 (OnScorePopup, OnPieceCleared, OnMatchFound, OnBigMatch, OnHammerHit) ✅ |
| Magic number | const 전환 완료 (24개) |
| 컴파일 에러/경고 | 0/0 ✅ |
| Play 모드 Exception | 0개 ✅ |
| 버그 해결 | 7/10 |

### 이번 세션 수정 (2026-03-25 추가)

**버그 수정**
1. FillSequence 초기 호출 시 READY 상태 노출 방지 — `setReadyOnComplete` 파라미터 추가
2. BoardModel._pieces 배열 bounds check 추가 — InBounds 헬퍼 + IBoardState/ClearPiece/ClearAllValidMatches guard

**Model→View 이벤트 완성 (2026-03-25 세션 2)**
3. OnMatchFound(int) 이벤트 추가 — AudioManager.PlayMatch 위임
4. OnBigMatch(Vector3, Color) 이벤트 추가 — CameraShake + PlayBigAt + PlaySpecial 통합
5. OnHammerHit 이벤트 추가 — CameraShake + PlaySpecial 통합
6. BoardModel._view 필드 완전 제거 + Init(controller) 시그니처 변경
7. shake const 4개 BoardModel→BoardView 이동

**코드 품질 점검 (2026-03-25 세션 2)**
8. unused using 전수 점검 — 0개 ✅
9. unused field 전수 점검 — 0개 ✅
10. 접근 제한자 축소 4곳: GetWorldPosition→internal, ShowScorePopup/ShowComboText/CameraShake→private

### Model→View 이벤트 전환 현황 — 전체 완료 ✅

| Before (직접 호출) | After | 상태 |
|---|---|---|
| `_view.ShowScorePopup(pos, score)` | `OnScorePopup?.Invoke(pos, score)` | ✅ |
| `MatchParticles.Instance.PlayAt(pos, c)` | `OnPieceCleared?.Invoke(pos, c)` | ✅ |
| `_view.CameraShake(MatchShake...)` | `OnBigMatch?.Invoke(center, color)` | ✅ |
| `AudioManager.Instance.PlayMatch(combo)` | `OnMatchFound?.Invoke(combo)` | ✅ |
| `AudioManager.Instance.PlaySpecial()` | `OnBigMatch?.Invoke(center, color)` | ✅ |
| `MatchParticles.Instance.PlayBigAt(...)` | `OnBigMatch?.Invoke(center, color)` | ✅ |
| `AudioManager/CameraShake (망치)` | `OnHammerHit?.Invoke()` | ✅ |

### 이번 세션 전체 작업 (2026-03-24~25)

**아키텍처**
1. BoardModel IEnumerator 3개 제거 → BoardView 코루틴 이관
2. GameStateMachine.cs 추출
3. BoardPreview.cs 생성
4. LevelConfig SO 통합
5. BoardModel.EvaluateSwap 추출 (View→Model)
6. GameGridRef 디커플링 (CalcWorldPosition, BoardModelRef, OnCleared)
7. Magic number → const 24개
8. MatchParticles Prefab 전환 (274줄→77줄)
9. Model→View Action 이벤트 프로토타입 (OnScorePopup, OnPieceCleared)

**버그 수정**
10. Build Settings 정리 (11→6 씬)
11. GameOver UI stretch + EnsureCanvasScaler
12. ShuffleBoardSequence 데드보드 재시도 (3회)
13. Hud.cs null 가드
14. Level02~05 Missing script 제거 + BoardPreview 추가

**정리**
15. Data/ 폴더 삭제 (GUID 충돌)
16. FixArchitecture.cs deprecated API
17. unused using 전수 점검 (45파일, 0개)
18. 핵심 5파일 public 멤버 XML summary 추가 (GameGrid 27, BoardModel 14, BoardView 7, InputController 4, MatchFinder 기존)

---

## Phase 1~2 검증 (에셋 실제 존재 확인)

### Phase 1 — 100% ✅
| 항목 | 파일 수 | 확인 |
|------|--------|------|
| BaseTile.prefab + Variant 8종 | 9개 (BaseTile + 6색 + Orange + Cyan) | ✅ |
| 파티클 Prefab 4종 | MatchBurst/Star/Glow/Ring.prefab | ✅ |
| UI Prefab 4종 | HudPanel/GameOverPanel/LevelButton/BoosterButton.prefab | ✅ |
| LevelConfigEditor.cs | Assets/Scripts/Editor/ | ✅ |
| Sprite Atlas 2종 | TileAtlas + UIAtlas.spriteatlas | ✅ |
| LevelConfig SO 5개 | Assets/ScriptableObjects/Levels/Level01~05.asset | ✅ |

### Phase 2 — 100% ✅
| 항목 | 확인 |
|------|------|
| BoardPreview.cs [ExecuteAlways] | ✅ 72줄, Level01~05 전부 컴포넌트 추가 |
| OnDrawGizmos 그리드 표시 | ✅ GameGrid xDim/yDim 참조 |
| MobileCameraSetup.cs clearFlags | ✅ SolidColor 설정 |
| Play 모드 정상 동작 | ✅ MCP 테스트 통과 |

---

## Phase별 진행도

| Phase | 검증 항목 | 완료 | 미완료 | 진행도 |
|-------|----------|------|--------|--------|
| 1 에셋 | 7 | 7 | 0 | **100%** |
| 2 프리뷰 | 4 | 4 | 0 | **100%** |
| 3 Input | 3 | 3 | 0 | **100%** |
| 4 Model | 7 | 7 | 0 | **100%** |
| 5 FSM | 4 | 4 | 0 | **100%** |
| 5b View Prefab | 5 | 0 | 5 | **0%** |
| 6 UI Prefab | 4 | 0 | 4 | **0%** |

### Phase 5 — 100% 완료 ✅
- [x] Model→View 이벤트 통신 완전 전환 (Action 5개) — `_view` 필드 제거 완료

### Phase 5b 미착수 항목
- [ ] 타일 Prefab 풀에서 인스턴스화
- [ ] 파티클 Prefab 매치 시 재생
- [ ] DOTween 애니메이션
- [ ] Edit 모드 보드 프리뷰 (BoardPreview는 있지만 TileView 미통합)
- [ ] 오브젝트 풀링 GC 스파이크 제거

### Phase 6 미착수 항목
- [ ] LevelSelect 에디터 배치 기반
- [ ] HUD 정상 (Edit 모드에서도 UI 보임)
- [ ] 부스터 동작
- [ ] 게임오버 연출

---

## 잔여 작업 총 정리

### 즉시 가능 (코드만)
1. Model→View 이벤트 완성 — OnBigMatch, OnMatchFound, OnHammerHit (3개 이벤트 → _view 제거)
2. Scripts 루트 17개 파일 폴더 이동 (참조 0, 안전)

### Phase 5b (중간 규모)
3. PiecePool 통합 (Instantiate/Destroy → 풀링)
4. DOTween 도입
5. TileView 확장 (SetSprite, MoveTo, PlayDestroyEffect)

### Phase 6 (대규모)
6. LevelSelect Prefab 전환
7. HUD Prefab 전환
8. Booster Prefab 전환
9. GameOver 정리

### 기타
10. 씬/프리팹 참조 17개 파일 폴더 이동 (MoveAsset + 씬 저장)
11. ProceduralAudio → 실제 AudioClip 에셋 교체

---

## TODO — 다음 세션

### Model→View 이벤트 완성 (잔여 5곳)
- `OnBigMatch(Vector3, Color)` — CameraShake + PlayBigAt + PlaySpecial 통합
- `OnMatchFound(int)` — PlayMatch
- `OnHammerHit` — CameraShake + PlaySpecial
- 완료 시 BoardModel에서 `_view` 필드 제거 가능

### Phase 5b: View 분리
- BoardView [ExecuteAlways] + TileView 확장
- PiecePool 통합
- DOTween 도입

### Phase 6: UI Prefab 전환
- LevelSelect → Prefab + LevelSelectUI.cs
- HUD → HudPanel Prefab + HudUI.cs
- Booster → Prefab 3개 + BoosterUI.cs 축소

### 폴더 구조 정리 (Scripts 루트 34개 분류)

AssetDatabase.MoveAsset으로 이동하면 GUID 유지됨 → 참조 안 깨짐.
단, **씬/프리팹에서 참조하는 파일은 이동 후 반드시 씬 저장 필요**.

#### 이동 안전도 분석 (GUID 참조 스캔 결과)

**씬/프리팹 참조 0개 — 안전하게 이동 가능 (17개)**
AudioManager.cs, BoosterSystem.cs, BoosterUI.cs, CoinSystem.cs, ColorType.cs,
GameConfig.cs, GameState.cs, Level.cs, LevelConfig.cs, LevelData.cs, LevelType.cs,
LifeSystem.cs, MatchParticles.cs, PiecePool.cs, PieceType.cs, ProceduralAudio.cs, SettingsUI.cs

**씬/프리팹에서 참조 — 이동 가능하지만 씬 저장 필요 (17개)**
| 파일 | 참조 수 | 비고 |
|------|---------|------|
| GameGrid.cs | 10 | **이동 금지** (씬 참조 최다) |
| Hud.cs | 11 | 모든 레벨 씬 |
| MobileCameraSetup.cs | 10 | 모든 레벨 씬 |
| LevelMoves.cs | 8 | 레벨 씬 |
| GamePiece.cs | 6 | 타일 프리팹 |
| ColorPiece.cs | 4 | 타일 프리팹 |
| MovablePiece.cs | 4 | 타일 프리팹 |
| ClearLinePiece.cs | 2 | 프리팹 |
| ClearablePiece.cs | 2 | 프리팹 |
| GameOver.cs | 1 | 레벨 씬 |
| LevelObstacles.cs | 1 | 레벨 씬 |
| LevelTimer.cs | 1 | 레벨 씬 |
| LevelSelect.cs | 1 | LevelSelect 씬 |
| MobileHUDLayout.cs | 1 | 레벨 씬 |
| SafeAreaHandler.cs | 1 | 레벨 씬 |
| TileView.cs | 1 | 프리팹 |
| ClearColorPiece.cs | 1 | 프리팹 |

| 대상 폴더 | 파일 | 타입 |
|-----------|------|------|
| **Controller/** | GameGrid.cs (**이동 금지**) | MonoBehaviour |
| | InputController.cs (이미 여기) | MonoBehaviour |
| **Model/** | BoardModel.cs (이미 여기) | MonoBehaviour |
| | MatchFinder.cs (이미 여기) | static class |
| | GameStateMachine.cs (이미 여기) | class |
| | IBoardState.cs, MatchResult.cs (이미 여기) | interface/struct |
| **View/** | BoardView.cs (이미 여기) | MonoBehaviour |
| | BoardPreview.cs (이미 여기) | MonoBehaviour |
| | TileView.cs | MonoBehaviour |
| | MovablePiece.cs | MonoBehaviour |
| | ClearablePiece.cs | MonoBehaviour |
| | ClearLinePiece.cs | MonoBehaviour |
| | ClearColorPiece.cs | MonoBehaviour |
| | ColorPiece.cs | MonoBehaviour |
| **Data/** | GamePiece.cs | MonoBehaviour |
| | GameState.cs | enum |
| | ColorType.cs | enum |
| | PieceType.cs | enum |
| | LevelType.cs | enum |
| | LevelConfig.cs | ScriptableObject |
| | GameConfig.cs | ScriptableObject |
| | LevelData.cs | ScriptableObject |
| | Level.cs | MonoBehaviour |
| | LevelMoves.cs | Level 상속 |
| | LevelObstacles.cs | Level 상속 |
| | LevelTimer.cs | Level 상속 |
| **System/** | AudioManager.cs | MonoBehaviour (싱글톤) |
| | MatchParticles.cs | MonoBehaviour (싱글톤) |
| | ProceduralAudio.cs | static class |
| | PiecePool.cs | MonoBehaviour (싱글톤) |
| | CoinSystem.cs | MonoBehaviour (싱글톤) |
| | BoosterSystem.cs | enum + MonoBehaviour |
| | LifeSystem.cs | MonoBehaviour (싱글톤) |
| | MobileCameraSetup.cs | MonoBehaviour |
| | SafeAreaHandler.cs | MonoBehaviour |
| **UI/** | Hud.cs | MonoBehaviour |
| | GameOver.cs | MonoBehaviour |
| | BoosterUI.cs | MonoBehaviour |
| | LevelSelect.cs | MonoBehaviour |
| | MobileHUDLayout.cs | MonoBehaviour |
| | SettingsUI.cs | MonoBehaviour |

---

## MCP 사용법

```bash
npx unity-mcp-cli run-tool <도구명> --input '{JSON}'
```

Play 모드:
```bash
npx unity-mcp-cli run-tool script-execute --input '{"csharpCode": "using UnityEditor; public class Script { public static string Main() { EditorApplication.isPlaying = true; return \"OK\"; } }"}'
```

## 프로젝트
- **위치**: `C:/Project/Match3/`
- **Unity**: 6.3 LTS (6000.3.11f1)
- **SO**: `Assets/ScriptableObjects/Levels/Level01~05.asset`
