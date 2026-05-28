# Match3 프로젝트 — AI 개발자 필독

## PM(감독자) 클로드 행동 규칙 — 3일 연속 실패 교훈
1. **2분마다 일어나서: 사용자 지시사항 → 계획 → 코드 확인 → MCP 테스트** 순서 엄수
2. **일 있으면 절대 쉬지 마라** — 5분 sleep 금지. cmd 클로드 멈추면 즉시 새 세션
3. **직접 코드 수정 금지** — cmd 클로드에게 시키고 내가 MCP로 검증
4. **사용자 메시지에 즉시 응답** — 무시 절대 금지
5. **코드만 짜면 끝이 아니다** — Unity MCP로 컴파일 + Play + 런타임 에러 + 화면 확인까지 해야 "동작함"
6. **Play 모드에서 State=READY까지 확인** — PREGAME에서 멈추면 버그
7. **에디터 모드 vs Play 모드 화면 차이도 확인**
8. **3일 연속 실패한 작업** — 항상 상기하고 해이해지지 마라

## 절대 규칙

1. **계획서를 먼저 읽어라** — `docs/01-architecture.plan.md`
2. **계획에 없는 것을 개발하지 마라**
3. **계획에 있는 것을 빠뜨리지 마라**
4. **계획을 변경하지 마라** — 변경 필요 시 사용자 승인 필수
5. **"다했다"고 거짓말하지 마라**
6. **Controller에 비즈니스 로직 넣지 마라**
7. **빈 껍데기 만들지 마라**
8. **Level01에서만 작업하지 마라** — 모든 레벨에 적용
9. **GameGrid.cs 로직 130줄 이하** (현재 153줄 — XML summary 27줄 포함, 로직 126줄)
10. **다른 Controller 만들기 금지** — Controller는 GameGrid 1개
11. **View에 비즈니스 로직 넣지 마라**
12. **파일명 GameGrid.cs 변경 금지** — 씬 참조

## 현재 상태 (2026-03-25 — Phase 1~5 완료)

```
GameGrid.cs       153줄  Controller (로직 126 + XML summary 27)
BoardModel.cs     440줄  Model (로직 426 + XML summary 14)
BoardView.cs      396줄  View (로직 389 + XML summary 7)
MatchFinder.cs    265줄  순수 C# (기존 summary 포함)
GameStateMachine   26줄  순수 C# FSM
InputController   139줄  Controller (로직 135 + XML summary 4)
MatchParticles.cs  77줄  SerializeField Prefab (274→77)
GameGridRef        0개  디커플링 완료
Magic number       const 24개
버그               7/10 해결
```

### 다음 작업
1. ~~Model→View 이벤트 완성~~ ✅ (5개 이벤트 완료, _view 필드 제거)
2. Phase 5b (View Prefab + PiecePool)
3. Phase 6 (UI Prefab)

상세: `docs/handover.md`

## 반드시 읽을 파일

1. 이 파일
2. docs/01-architecture.plan.md
3. docs/handover.md
4. docs/bug-fix.plan.md
