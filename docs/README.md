# Match3 게임 개발 계획 — 한눈에 보기

## 현재 상태
- **16개 Phase 계획서 작성 완료** (단, 실행 스펙 수준은 아닌 로드맵 수준)
- **다음 작업**: 01-architecture를 서브 Phase(1-1, 1-2...)로 분할 → 처음부터 하나씩 진행

## 문서 구조

```
docs/
├── README.md              ← 지금 보고 있는 파일 (전체 요약)
├── handover.md            ← AI 인수인계 (다음 세션 시작 시 읽는 파일)
├── next-session-prompt.md ← 다음 세션 첫 메시지 (복사-붙여넣기용)
│
├── 00-master-plan.md           ← 16개 Phase 전체 현황 테이블
├── 00-phase0-plan-research.md  ← AI 작업 규칙 (절대 목표, MCP 활용 원칙)
├── 00-plan-review-result.md    ← 16명 검증 결과 (각 계획서 완성도 %)
│
├── 01~16 계획서들 (아래 표 참조)
├── asset-tracker.md       ← 이미지 에셋 생성 현황
└── notebooklm-*.md        ← NLM 응답 기록 (참고용, 수정 불필요)
```

## 16개 Phase 요약

| # | 파일 | 한줄 설명 | 완성도 | 개발 상태 |
|---|------|----------|--------|----------|
| **1** | 01-architecture | MVC 리팩토링 (카메라, Input, Model, View, FSM) | 30~40% | Phase 1만 완료 |
| **2** | 02-special-tiles | 4/5매치 특수 타일 + 시너지 조합 | 미달 | 미착수 |
| **3** | 03-tutorial | 3단계 인터랙티브 튜토리얼 | 미달 | 미착수 |
| **4** | 04-difficulty | 사인파 난이도 곡선 + DDA | 미달 | 미착수 |
| **5** | 05-monetization | IAP 7종 + 보상형 광고 + 시즌패스 | 미달 | 미착수 |
| **6** | 06-metagame | 월드맵 + 스토리/꾸미기 + 일일 보상 | 미달 | 미착수 |
| **7** | 07-level-editor | Custom Inspector 그리드 에디터 + CSV 임포트 | 미달 | 미착수 |
| **8** | 08-gdd | 게임 전체 규칙/숫자 (GDD) | **70%** | 규격 문서 |
| **9** | 09-audio | Audio Mixer + SFX 17종 + BGM 3종 | 미달 | 미착수 |
| **10** | 10-optimization | 풀링 128개 + ASTC 압축 + Profiler | **70%** | 미착수 |
| **11** | 11-data-storage | JSON 저장 + AES 암호화 + 클라우드 | **60%** | 미착수 |
| **12** | 12-graphics-design | 스프라이트 Import + Atlas + Juicing | **50%** | 5% (배경 1종) |
| **13** | 13-obstacles | 장애물 6종 + 비정형 보드 | 미달 | 미착수 |
| **14** | 14-liveops | Addressables 원격 배포 + 이벤트 | 미달 | 미착수 |
| **15** | 15-launch | 다국어 + SDF 폰트 + Safe Area | 미달 | 미착수 |
| **16** | 16-ops-infra | Firebase Analytics/Crashlytics/FCM/RC | 미달 | 미착수 |

## 개발 우선순위 (권장 순서)

```
Phase 1 ✅ 완료
  ↓
Phase 1-1~ (서브 Phase로 분할하여 진행)  ← 다음 작업
  ↓
Phase 3 (InputController 추출)
  ↓
Phase 4 (Model 분리: BoardModel, MatchFinder, DropSimulator)
  ↓
Phase 5 (Controller + FSM)
  ↓
Phase 6 (View + Prefab 연결 + DOTween)
  ↓
Phase 7 (UI를 Prefab 기반으로)
  ↓
이후: 08(GDD 확정) → 02(특수타일) → 13(장애물) → 07(레벨에디터) → 04(난이도) → ...
```

## 핵심 규칙 (AI가 지켜야 하는 것)

1. **Unity MCP 필수** — `npx unity-mcp-cli run-tool`로 에디터 조작. AI가 전부 처리, 사용자는 최종 확인만.
2. **계획서 순서대로** — 임의 변경 금지, 변경 시 사용자 보고
3. **개발 전 계획서 구체화** — 각 Phase 시작 시 해당 계획서를 실행 가능 수준으로 먼저 보강
4. **Play 모드 테스트 필수** — 코드 작성 후 무조건 확인
5. **작업 완료 즉시 문서 업데이트**

## 완성도 "미달"이란?
계획서에 방향은 적혀 있지만, 아래가 부족:
- MCP 도구 호출의 구체적 파라미터/코드
- 메서드 시그니처만 있고 구현 로직 없음
- 테스트 데이터/기대값 없음
- 에셋 파일 경로 불명확

→ **각 Phase 개발 시작 시 AI가 먼저 구체화한 후 개발 진행**
