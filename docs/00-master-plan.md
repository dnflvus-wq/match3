# Match3 마스터 계획서

## 전체 진행 현황

| # | Phase | 세부 계획서 | 상태 | 진행률 |
|---|-------|-----------|------|--------|
| 1 | 아키텍처 리팩토링 (MVC + Prefab) | [01-architecture.plan.md](01-architecture.plan.md) | 진행중 | 15% |
| 2 | 특수 타일 시스템 | [02-special-tiles.plan.md](02-special-tiles.plan.md) | 미착수 | 0% |
| 3 | 튜토리얼 시스템 | [03-tutorial.plan.md](03-tutorial.plan.md) | 미착수 | 0% |
| 4 | 난이도 곡선 설계 | [04-difficulty.plan.md](04-difficulty.plan.md) | 미착수 | 0% |
| 5 | 수익화 (IAP + 광고) | [05-monetization.plan.md](05-monetization.plan.md) | 미착수 | 0% |
| 6 | 메타게임 | [06-metagame.plan.md](06-metagame.plan.md) | 미착수 | 0% |
| 7 | 레벨 에디터 도구 | [07-level-editor.plan.md](07-level-editor.plan.md) | 미착수 | 0% |
| 8 | GDD (게임 기획서) | [08-gdd.plan.md](08-gdd.plan.md) | 미착수 | 0% |
| 9 | 사운드/오디오 | [09-audio.plan.md](09-audio.plan.md) | 미착수 | 0% |
| 10 | 성능 최적화 | [10-optimization.plan.md](10-optimization.plan.md) | 미착수 | 0% |
| 11 | 데이터 저장 + DB 연동 | [11-data-storage.plan.md](11-data-storage.plan.md) | 미착수 | 0% |
| 12 | 그래픽/디자인 시스템 | [12-graphics-design.plan.md](12-graphics-design.plan.md) | 진행중 | 5% |
| 13 | 장애물 시스템 + 비정형 보드 | [13-obstacles.plan.md](13-obstacles.plan.md) | 미착수 | 0% |
| 14 | 라이브 이벤트 (LiveOps) | [14-liveops.plan.md](14-liveops.plan.md) | 미착수 | 0% |
| 15 | 출시 준비 (다국어/접근성/ASO/약관) | [15-launch.plan.md](15-launch.plan.md) | 미착수 | 0% |
| 16 | 운영 인프라 (Analytics/푸시/크래시) | [16-ops-infra.plan.md](16-ops-infra.plan.md) | 미착수 | 0% |

## 우선순위 (권장 순서)
1. **GDD** (8) — 전체 방향 정리
2. **아키텍처 리팩토링** (1) — 코드 기반 정리
3. **그래픽/디자인** (12) — 에셋 준비 (아키텍처와 병행)
4. **장애물 시스템** (13) — 게임플레이 다양성
5. **특수 타일** (2) — 핵심 게임플레이
6. **레벨 에디터** (7) — 레벨 대량 생산
7. **난이도 곡선** (4) — 레벨 밸런싱
8. **튜토리얼** (3) — 신규 유저 온보딩
9. **사운드** (9) — 게임 느낌 개선
10. **성능 최적화** (10) — 모바일 최적화
11. **데이터 저장** (11) — 서버 연동 준비
12. **수익화** (5) — 매출 구조
13. **메타게임** (6) — 장기 리텐션
14. **라이브 이벤트** (14) — 주간/시즌 이벤트
15. **출시 준비** (15) — 다국어, 접근성, 스토어 등록
16. **운영 인프라** (16) — Analytics, 푸시 알림, 크래시 리포팅

## 핵심 원칙
- Unity 에디터 기능 최대 활용 (Prefab, Inspector, Particle System, Timeline)
- 코드는 최소한, 에디터에서 할 수 있는 것은 에디터에서
- MVC 패턴: Model(순수C#) / View(Prefab+MonoBehaviour) / Controller(FSM)
- 각 Phase 완료 후 E2E 테스트 필수
- 빌드는 사용자 승인 후에만
- NotebookLM으로 각 Phase 시작 전 실무 지식 보충

## 황금 규칙 (NotebookLM 확인)
1. **Model-View 철저한 분리** — 코어 로직은 Unity 없이 순수 데이터만으로
2. **FSM으로 엄격한 턴 통제** — 보드 안정화 전까지 입력 차단
3. **오브젝트 풀링 필수** — Instantiate/Destroy 금지
4. **가중치 기반 타일 스폰** — 순수 무작위 금지
5. **데이터 주도 설계** — 하드코딩 금지, ScriptableObject/JSON으로

## 현재 프로젝트의 실수 (반드시 수정)
- [x] God Object (GameGrid.cs 1400줄) → Phase 1에서 해결
- [x] Unity Physics 미사용 (이미 트윈 기반) ← 유일하게 안 빠진 것
- [ ] 레벨 데이터 하드코딩 → Phase 7 레벨 에디터
- [ ] 오브젝트 풀링 미사용 → Phase 1-4단계
- [ ] Sprite Atlas 미사용 → Phase 1
- [ ] 순수 무작위 스폰 → Phase 4 난이도 곡선

## 현재까지 완료된 것
- 기본 Match3 게임플레이 (스왑, 매치, 낙하, 힌트, 콤보)
- 레벨 5개 (이동수/시간/장애물 모드)
- 부스터 3종 (망치, 셔플, +5)
- 코인/생명/일일보상 시스템
- 판타지 테마 배경
- 레벨 선택 UI (level_btn 통일)
- Android APK 빌드 (Match3_25)
