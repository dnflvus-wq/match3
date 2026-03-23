# Phase 0: 완벽한 개발 계획서 수립

## 절대 목표 (이것을 잃으면 안 됨 — 까먹으면 이 파일 다시 읽을 것)

### 0. Unity MCP 도구를 적극 활용해서 개발 (가장 중요)
- **모든 Phase에 Unity MCP 도구 활용 절차가 포함되어야 함**
- MCP로 할 수 있는 것: Prefab 생성, 컴포넌트 추가/수정, Inspector 값 설정, 씬 저장, 스크립트 생성, Play 모드 진입/종료, 스크린샷, 에셋 검색 등
- 각 Phase의 각 Step에서 **어떤 MCP 도구를 어떤 순서로 호출하는지** 명시
- 코드만 짜고 끝나는 계획은 의미 없음 — **MCP로 에디터에 반영하는 단계까지** 포함

### 1. 이 계획서만 보면 누구나 처음부터 끝까지 개발 가능
- 추상적 지시 금지 ("에디터에서 해야 함" ❌)
- 구체적 조작 단계 ("Main Camera > Clear Flags: Solid Color > Background #0D0D26" ✅)

### 2. Unity 에디터 도구가 필수인 작업은 구체적 조작 순서까지 명시
- Unity 에디터 Inspector/Scene View/Prefab 조작이 필요한 작업 → **구체적 값과 순서**
- 코드도 필요하면 상세히 (간결히가 아님)

### 3. 모든 계획서를 NotebookLM에 질문하고 검증
- 내 판단만으로 작성 금지
- 반드시 NotebookLM(Playwright)에 질문 → 응답 확인 → 반영
- 하나씩 순서대로, 동시에 여러 개 하지 않음

### 4. 의미 없는 계획은 과감히 삭제
- 계획서가 길다고 좋은 게 아님 — **정확해야 함**
- Unity 에디터 조작이 필요 없는 순수 코드 Phase는 핵심만 짧게
- 중복되는 내용은 GDD(08)에 한 번만 적고 다른 파일에서 참조

## 계획서 작성 규칙

### Unity 에디터 작업이 필요한 항목 (상세히 적어야 함)
- Particle System Inspector 설정 (모듈별 구체적 값)
- Camera Inspector 설정 (Clear Flags, Size, Position 등)
- Canvas/UI 배치 (Anchor, Size, Sprite 할당)
- Prefab 생성/구성 (컴포넌트 추가, Inspector 값)
- ScriptableObject 에셋 생성 (Inspector 필드값)
- Audio Mixer 설정 (Group, Volume, Exposed Parameter)
- Sprite Atlas 구성 (Packables 등록)
- Custom Editor Inspector UI (그리드 에디터 등)
- Scene 구성 (GameObject 배치, 컴포넌트 할당)

### 코드 작업 (필요하면 상세히, 불필요한 건 안 적기)
- 핵심 알고리즘 (Shape Recognition, 데드보드 감지 등) → **구체적 단계와 로직 상세히**
- 클래스 구조/메서드 → **필요한 메서드, 파라미터, 반환값 상세히**
- 이벤트 통신 → 패턴 + 구체적 시그니처 (`Action<int2, int2>` 등)
- ⚠️ "코드는 간결히" 가 아님 — **코드도 중요하면 상세히 계획해야 함**
- 불필요한 것: 이미 다른 Phase에서 적은 중복 내용, 자명한 구현 (getter/setter 등)

## 보강 순서 (남은 것)

| # | 파일 | 상태 | 비고 |
|---|------|------|------|
| ✅ | 01-architecture | 완료 | NotebookLM 3회 검증 |
| ✅ | 08-gdd | 완료 | NotebookLM 검증 |
| ✅ | 02-special-tiles | 완료 | NotebookLM 검증 |
| ✅ | 13-obstacles | 완료 | GDD+NLM 기반 |
| ✅ | 07-level-editor | 완료 | Custom Inspector 상세 |
| 🔄 | 04-difficulty | NLM 질문 완료, 보강 필요 | DDA/사인파/Controlled Chaos |
| ⬜ | 03-tutorial | 보강 필요 (NLM 질문 필요) | |
| ⬜ | 09-audio | 보강 필요 (NLM 질문 필요) | |
| ⬜ | 12-graphics-design | 보강 필요 (NLM 질문 필요) | |
| ⬜ | 10-optimization | 보강 필요 | |
| ⬜ | 11-data-storage | 보강 필요 | |
| ⬜ | 05-monetization | 보강 필요 | |
| ⬜ | 06-metagame | 보강 필요 (사용자 선택 필요) | |
| ⬜ | 14-liveops | 보강 필요 | |
| ⬜ | 15-launch | 보강 필요 | |
| ⬜ | 16-ops-infra | 보강 필요 | |

## 삭제/병합 검토 대상
- 순수 코드 작업만 있는 Phase → GDD에 병합하거나 대폭 축소 가능
- 예: 11-data-storage (PlayerPrefs→JSON 전환은 코드만 필요, Unity 에디터 작업 거의 없음)
- 예: 10-optimization (프로파일링은 Unity 도구지만 계획서에 상세히 적을 필요 없을 수 있음)
