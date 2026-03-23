# 다음 세션 프롬프트 — 그대로 복사-붙여넣기

---

```
Match3 게임 고도화 프로젝트를 이어서 진행해.

## 반드시 먼저 읽을 파일
1. `docs/handover.md` — 현재 상태, 작업 방식, MCP 사용법
2. `docs/01-architecture.plan.md` — 아키텍처 리팩토링 마스터 계획

## 너의 첫 번째 할 일

01-architecture.plan.md를 읽고, **처음부터** 서브 Phase로 분할해:

⚠️ Phase 1 에셋은 만들어져 있지만 **게임 코드에서 안 쓰이고 있음**. 에셋을 실제 게임에 연결하는 작업도 필요.

1. `docs/01/` 폴더 생성
2. 01-architecture.plan.md의 전체 Phase를 Phase 1-1, 1-2, 1-3... 으로 나눠서 각각 상세 계획 파일 작성 (카메라 수정, Input 분리, Model 분리, View 연결 등)
3. 각 서브 Phase 파일에는:
   - 정확히 무엇을 하는지 (코드 변경 내용, 에디터 설정 변경)
   - MCP 도구 호출 순서 (실제 파라미터 포함)
   - 완료 후 테스트 방법
   - 예상 결과 (스크린샷으로 뭐가 보여야 하는지)
4. 분할 완료 후, Phase 1-1부터 하나씩 실행

## 핵심 규칙

1. **스텝 바이 스텝** — 한 번에 하나의 서브 Phase만 진행. 완료→저장→다음.
2. **Unity MCP 필수** — `npx unity-mcp-cli run-tool` CLI 방식으로만 호출. mcp__mcp-unity__ 직접 호출 금지 (타임아웃됨).
3. **테스트 철저히** — 매 작업 후 Play 모드 + screenshot-game-view + console-get-logs. 가능하면 win32-inspector로 직접 클릭/조작하여 테스트. 직접 조작 안 되는 경우만 사용자에게 요청.
4. **사용자 개입 최소화** — AI가 전부 처리. 사용자는 최종 확인+승인만. "할까요?" 묻지 말고 실행.
5. **진행도 기록** — 서브 Phase 완료할 때마다 파일+메모리에 즉시 저장. 까먹지 않게.
6. **서브 에이전트에게 중요 작업 위임 금지** — 직접 해야 함.
7. **계획서 변경 시 사용자 보고** — 임의 변경 금지.

## 현재 문제점 (해결해야 함)

- GameGrid.cs 1393줄 God Object → MVC로 분리해야 함
- Edit 모드에서 Game View 안 보임 (카메라 clearFlags=Nothing)
- Phase 1에서 만든 Prefab/SO가 게임 코드에서 안 쓰이고 있음
- 코드가 전부 Scripts/ 루트에 플랫하게 있음 (폴더 분리 없음)

## 참고

- 16개 Phase 전체 요약: `docs/README.md`
- 16명 검증 결과 (계획서 완성도): `docs/00-plan-review-result.md`
- 계획서들은 "로드맵" 수준이므로, 각 Phase 시작 시 서브 Phase로 분할+구체화 먼저 하고 개발

handover.md부터 읽고 시작해.
```
