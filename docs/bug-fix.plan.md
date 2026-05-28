# 버그 수정 플랜

## 상태 요약 (2026-03-24 23:00 — MCP 검증 완료)

| # | 버그 | 상태 | 수정 방법 |
|---|------|------|-----------|
| 0 | 타일 그리드 불일치 | ⚠️ 확인 필요 | Play 모드에서 정상 동작 확인. 피벗 롤백된 상태 |
| 1 | 레벨 10개 표시 | ✅ 해결 | Build Settings에서 Level06~10 제거 (11→6씬). LevelSelect.cs는 이미 5개만 생성 |
| 2 | LevelSelect 옛날 UI | Phase 6 | Prefab 전환 시 해결 예정 |
| 3 | GameOver UI 크기 | ✅ 해결 | screenParent/bg RectTransform → stretch (0,0)-(1,1). GameOver.cs에 EnsureCanvasScaler 추가 (1080x2160, match 0.5) |
| 4 | 리플레이 버튼 | 유지 | GameOver.cs에 OnReplayClicked 존재. 의도된 기능 |
| 5 | Level02~05 입력 불가 | ✅ 해결 | GameGrid.Awake에서 InputController AddComponent 자동 생성 (57-59행). MCP로 전 레벨 확인 |
| 6 | 블록 전체 초기화 | ✅ 해결 | BoardView.FillSequence에 셔플 재시도 루프 추가 (최대 3회). BoardModel.ShuffleColors는 30회 시도 |
| 7 | 블록 흔들림 | ✅ 해결 | 현재 PiecePool 미사용 (모든 피스 Instantiate/Destroy). 향후 풀링 도입 시 Animator 리셋 필요 |
| 8 | Level02~05 프리뷰 | ✅ 해결 | MCP로 Level02~05에 BoardPreview 컴포넌트 추가 + 씬 저장 완료 |
| 9 | Level02~05 점수판 밖 | ⚠️ 확인 필요 | MobileHUDLayout.cs가 런타임에 HUD 재배치. GameOver.cs EnsureCanvasScaler가 Canvas 보정 |

## 해결 완료 (7/10)

### #1 Build Settings — MCP 검증
```
Before: 11 scenes (LevelSelect + Level01~10)
After:  6 scenes (LevelSelect + Level01~05)
```
script-execute로 EditorBuildSettings.scenes 직접 수정 후 확인.

### #3 GameOver UI — MCP 검증
- screenParent sizeDelta=(100,100) center anchor → stretch (0,0)-(1,1) offset=0
- CanvasScaler ref (1080,1920) → (1080,2160)
- GameOver.cs에 EnsureCanvasScaler() 런타임 보정 추가

### #5 Level02~05 입력 — MCP 검증
GameGrid.Awake (57-59행):
```csharp
if (_model == null) _model = gameObject.AddComponent<BoardModel>();
if (_view == null) _view = gameObject.AddComponent<BoardView>();
if (_inputController == null) _inputController = gameObject.AddComponent<InputController>();
```
모든 레벨에서 GameGrid 존재 확인 (MCP script-execute). AddComponent 폴백으로 동작 보장.

### #6 셔플 데드보드 — 코드 수정
BoardView.FillSequence:
```csharp
int shuffleAttempts = 0;
while (MatchFinder.FindValidMove(_model) == null && shuffleAttempts < 3)
{
    yield return StartCoroutine(ShuffleBoardSequence());
    shuffleAttempts++;
}
```

### #8 BoardPreview — MCP 검증
Level02~05에 BoardPreview 컴포넌트 추가 + Missing script 제거. scene-save 완료.

## 미해결 (3/10)

### #0 타일 그리드 불일치 — 확인 필요
Play 모드에서 정상 동작 확인됨. 피벗 수정이 이전 세션에서 롤백된 것으로 추정.
→ 사용자가 직접 플레이하여 최종 확인 필요.

### #2 LevelSelect 옛날 UI — Phase 6
현재 LevelSelect.cs가 코드로 5개 버튼 동적 생성. 동작에 문제 없음.
→ Phase 6에서 Prefab 기반으로 전환 시 해결.

### #9 Level02~05 점수판 — 확인 필요
MobileHUDLayout.cs가 Start()에서 HUD 패널 앵커를 재배치.
GameOver.cs EnsureCanvasScaler가 Canvas 보정.
→ 사용자가 Level02~05 플레이하여 최종 확인 필요.
