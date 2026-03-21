# Match3 게임 고도화 계획서 (v2)

## 목표
상용화 수준의 매치3 게임 완성

## Phase 의존관계
```
Phase 1 (FSM) ← 필수 선행, 나머지 전부 이것에 의존
  ├→ Phase 2 (스왑) ← Phase 1 완료 후
  ├→ Phase 3 (힌트) ← Phase 1 완료 후
  └→ Phase 4 (Juice) ← Phase 2 완료 후
Phase 5 (폴리싱) ← Phase 1~4 전부 완료 후
Phase 6 (그래픽) ← 독립, 언제든 병행 가능 (이미지 생성은 코드 무관)
```
- Phase 6은 다른 Phase와 병행 가능 — 코드 작업 중 대기 시간에 이미지 생성
- Phase 2, 3은 Phase 1 완료 후 병행 가능
- Phase 4는 Phase 2의 애니메이션 기반이 필요

## 현재 상태 (GitHub: dnflvus-wq/match3, master 브랜치)
- 기본 매치3 동작 (스왑, 매치, 제거, 낙하)
- 모바일 Portrait 모드 (1080x1920)
- 캔디 스타일 타일 + 나무판 HUD
- 뒤로가기 버튼 (Android + UI)
- **문제:** 스왑 애니메이션 부자연스러움, 힌트 없음, Juice 없음, FSM 없음

## 핵심 원칙 (모든 세션/에이전트 필독)

### 작업 방식
1. **커밋은 Phase 단위로** — 푸시는 사용자 승인 후에만
2. **APK 빌드는 사용자 컨펌 후에만**
3. **진행상황은 반드시 이 파일에 기록** — 다음 세션이 이어받을 수 있도록
4. **이 계획서를 구현 전/후에 반드시 다시 읽을 것** (Read 도구로 파일 열기)

### 유니티 에디터 조작 (필수)
5. **코드만 수정하지 말 것** — 반드시 유니티 에디터를 직접 실행하고 Play 모드에서 눈으로 확인
6. **유니티 에디터 조작은 마우스 클릭으로** — screen_clicker.py / click_points.py 사용
7. **Inspector, Scene 뷰, Game 뷰 등 유니티 GUI를 직접 조작**하여 값 조정, 프리팹 수정 등 수행
8. **코드 수정 → 유니티 Play → 사용자에게 스크린샷 보여주기 → 컨펌 → 다음 단계**

### 도구 경로 (모든 세션 공통)
- 스크린샷: `mss` 라이브러리, 저장 위치: `C:/Users/comes/tools/screenshot.png` (덮어쓰기)
- 클릭: `python C:/Users/comes/tools/click_points.py "[[x,y]]"`
- 텍스트 찾아 클릭: `python C:/Users/comes/tools/screen_clicker.py click_text "텍스트" --monitor 1`
- 모니터: `--monitor 1` = 오른쪽(Unity), `--monitor 2` = 중앙(VSCode)
- Unity 에디터 경로: `C:/Program Files/Unity/Hub/Editor/6000.3.11f1/Editor/Unity.exe`
- 프로젝트 경로: `C:/Project/Match3`

### 절대 금지
- 코드만 수정하고 유니티로 확인 안 하기
- 사용자 컨펌 없이 APK 빌드
- 사용자 승인 없이 git push
- 계획서 진행상황 업데이트 안 하기
- `pytesseract` 사용 (설치 안 됨, EasyOCR만 가능)
- `pyautogui` 사용 (mss + ctypes SendInput 사용)
- 직접 SendInput 구현 (click_points.py / screen_clicker.py에 이미 있음)

---

## Phase 1: FSM + 데이터-시각 분리 (기반 작업)

### 1-1. GameState enum 추가
```csharp
enum GameState { PREGAME, READY, SWAPPING, EVALUATING, MATCHING, COLLAPSING, SHUFFLING, ENDGAME }
```
- GameGrid.cs에 `_currentState` 필드 추가
- Update()에서 `_currentState != GameState.READY`이면 터치 입력 무시

### 1-2. MovablePiece 데이터-시각 분리
**현재 문제:** `Move()`가 즉시 `_piece.X = newX` 설정 → 배열과 Transform 불일치
**해결:**
- `Move()` → 데이터(X,Y) 변경 + 시각 이동 (기존 용도, 매치 성공 시)
- `MoveVisual()` → 시각만 이동 (실패 시 핑퐁용, 이미 구현됨)
- 핵심: SwapPieces()에서 먼저 배열만 교환 → 매치 확인 → 성공이면 Move(), 실패면 배열 복원 + MoveVisual()

### 1-3. SwapPieces 흐름 FSM화
```
현재: SwapPieces() 안에서 즉시 매치 확인 + 처리
목표:
  READY → 입력 감지 → SWAPPING (애니메이션)
  → EVALUATING (매치 확인)
  → 매치 있음 → MATCHING (제거) → COLLAPSING (낙하) → 재검사 루프
  → 매치 없음 → 핑퐁 애니메이션 → READY
  → 유효 이동 없음 → SHUFFLING → READY
```

### 1-4. 검증 항목
- [ ] READY 상태에서만 터치 가능
- [ ] 스왑 애니메이션 중 입력 차단
- [ ] 매치 실패 시 핑퐁 → 블록 안 사라짐
- [ ] 연쇄 반응(Cascade) 정상 동작
- [ ] 데드 보드 시 셔플
- **커밋:** `feat: FSM 도입 + 데이터-시각 분리`

---

## Phase 2: 스왑 애니메이션 개선

### 2-1. Ease-Out 보간
```csharp
// MoveCoroutine 내부
float easedT = 1f - (1f - normalizedT) * (1f - normalizedT); // Quadratic Ease-Out
transform.position = Vector3.Lerp(startPos, endPos, easedT);
```

### 2-2. Z축 깊이 조절
- 스왑 중 한쪽 타일 z=-0.1 → 겹침 방지 (아크 모션)
- `Mathf.Sin(normalizedT * Mathf.PI) * -0.1f` 으로 곡선 깊이

### 2-3. 타이밍 조정
- 스왑: 0.25초 (현재 fillTime 사용 중)
- 핑퐁 복귀: 0.2초
- 파괴: 0.15초

### 2-4. 검증 항목
- [ ] 스왑 시 타일이 부드럽게 이동 (선형 아님)
- [ ] 두 타일이 교차할 때 겹치지 않음
- [ ] 실패 시 자연스럽게 갔다 돌아옴
- **커밋:** `feat: Ease-Out 스왑 애니메이션 + Z축 아크`

---

## Phase 3: 힌트 시스템

### 3-1. FindValidMove() 알고리즘
```
보드 전체 (x, y) 순회:
  상하좌우 4방향으로 가상 스왑
  → 스왑 후 GetMatch() 호출
  → 매치 있으면 (x, y, 방향) 반환
  → 없으면 다음 타일
전부 없으면 → 데드 보드 (셔플 필요)
```
- 8x8 보드 = 최대 112가지, 브루트 포스 OK

### 3-2. 힌트 시각 효과
- READY 상태에서 5초 무조작 → FindValidMove() 호출
- 결과 타일에 Sin 펄스 (scale 1.0 ↔ 1.15, 주기 0.5초)
- 유저 터치 시 즉시 중단 + 타이머 리셋

### 3-3. 데드 보드 셔플
- FindValidMove()가 null → Fisher-Yates 셔플
- 셔플 후 초기 매치 방지 재검사

### 3-4. 검증 항목
- [ ] 5초 대기 후 매치 가능 타일만 흔들림 (아무 타일 아님)
- [ ] 터치하면 흔들림 즉시 중단
- [ ] 데드 보드 시 자동 셔플
- [ ] 셔플 후 즉시 매치 없음
- **커밋:** `feat: 힌트 시스템 + 데드 보드 셔플`

---

## Phase 4: Juicing (게임 필)

### 4-1. 오브젝트 풀링 도입
- 타일 Instantiate/Destroy → 풀링으로 교체
- 색상별 풀 (각 20개 사전 할당)
- 파티클용 풀 별도

### 4-2. 매치 이펙트
- 타일 제거 시 스케일 축소 (0.15초) + 페이드 아웃
- 파티클 스파크 (풀에서 가져오기)
- 4매치 이상: 카메라 Screen Shake (진폭 0.05, 0.2초)

### 4-3. 낙하 이펙트
- Squash & Stretch: 낙하 완료 시 y축 0.85 → 1.0 복원 (0.1초)
- Bounce: 살짝 튕김 (Elastic Ease)

### 4-4. 오디오
- 매치 효과음 (기본)
- 콤보 시 피치 점진 상승 (pitch = 1.0 + combo * 0.1)
- 스왑 효과음

### 4-5. 콤보 카운터
- 연쇄 횟수 UI 표시 ("x2", "x3" ...)
- 콤보 텍스트 팝업 + 페이드 아웃

### 4-6. 검증 항목
- [ ] 타일 제거 시 이펙트 재생
- [ ] 4매치 이상 시 화면 흔들림
- [ ] 낙하 시 찌그러짐 → 복원
- [ ] 콤보 시 효과음 피치 상승
- [ ] GC 스파이크 없음 (풀링 동작 확인)
- **커밋:** `feat: Juicing (파티클, 쉐이크, 오디오, 풀링)`

---

## Phase 5: 폴리싱 + 상용화 준비

### 5-1. 레벨 데이터 ScriptableObject 분리
- LevelData.cs (ScriptableObject) 생성
- 그리드 크기, 목표, 이동 횟수, 스폰 가중치 포함

### 5-2. T자/L자 매치 감지 (재귀 플러드필)
- 현재: 가로/세로만 감지
- 목표: 인접한 같은 색상 전부 연결 → T, L, 십자 감지
- 4매치 → 줄무늬 타일, 5매치 → 레인보우, T/L → 폭탄

### 5-3. UI 개선
- 점수판 디자인 완성 (글씨 크기, 별 위치)
- 레벨 선택 화면 개선
- 게임 오버 / 클리어 연출 강화

### 5-4. 스폰 가중치 시스템
- 순수 랜덤 → 동적 가중치
- 보드에 많은 색상 확률 ↓

### 5-5. 검증 항목
- [ ] ScriptableObject로 새 레벨 추가 가능
- [ ] T/L 매치 정상 감지
- [ ] 특수 타일 생성 정상
- [ ] 전체 플레이 테스트 (레벨 1~3)
- **커밋:** `feat: ScriptableObject 레벨 + T/L 매치 + UI 폴리싱`

---

## Phase 6: 그래픽 고도화 (Antigravity + Gemini)

### 이미지 생성 방법
- **Antigravity IDE**에서 Match3 프로젝트(`C:/Project/Match3`) 열기
- Gemini 채팅에 프롬프트 입력 → 이미지가 프로젝트 폴더에 파일로 생성됨
- 저장 경로를 프롬프트에 명시해야 함 (예: `Assets/Textures/Sprites/fish_1.png`)
- Gemini API 쿼터 제한 있음 — 한 번에 7개 이상 연달아 생성하면 429 에러
- **쿼터 초과 시:** 잠시 대기 후 재시도, 한 번에 2~3개씩 나눠서 생성

### 6-1. 타일 이미지 리뉴얼
**현재 문제:** 통일성 없음 (빨간색만 눈 있음), 퀄리티 부족
**목표:** 캔디크러쉬 수준의 알록달록하고 아기자기한 타일
- 모든 타일 동일한 스타일 (눈 없는 광택 보석/캔디)
- 색상 6개: 빨강, 파랑, 초록, 노랑, 보라, 핑크 — 확실한 색 구분
- rainbow_fish: 무지개 특수 타일
- 크기: 110x110px, 투명 배경 PNG
- 프롬프트 예시:
```
Generate 6 candy/gem pieces for a match-3 game.
Style: glossy, colorful, round candy shape with shine effect. NO faces, NO eyes.
Consistent art style across all pieces. 110x110px, transparent background PNG.
Colors: 1=Yellow, 2=Purple, 3=Red, 4=Blue, 5=Green, 6=Pink
Save to Assets/Textures/Sprites/ as fish_1.png ~ fish_6.png
```

### 6-2. 배경 이미지
**현재:** Gemini가 만든 초원+과자집 배경 (soso)
**목표:** 더 화려하고 판타지 느낌, 타일 그리드 영역과 맞게
- 그리드 영역(체크무늬)이 배경 이미지 안에 이미 그려져 있음 → 그리드 위치를 거기에 맞춰야 함
- 1080x1920 Portrait, 상단에 HUD 공간 확보
- 프롬프트에 "그리드 배치 영역을 중앙에 밝은 색으로" 명시

### 6-3. HUD 점수판 이미지
**현재:** 나무판 (hud_panel.png) — 글씨가 테두리에 걸림
**목표:** 글씨가 잘 보이는 디자인, 테두리 안쪽에 충분한 여백
- 나무판 유지하되 안쪽 평평한 영역을 넓게
- 또는 라운드 사각형 스타일로 변경

### 6-4. 특수 타일 이미지
- 줄무늬 캔디 (가로/세로): 기본 캔디 + 줄무늬 오버레이
- 폭탄: 기본 캔디 + 폭발 이펙트
- 레인보우: 무지개 반짝이

### 6-5. 이펙트/파티클 이미지
- 별 반짝이 스파크 (매치 시)
- 폭발 이펙트 (특수 타일)

### 6-6. 검증 항목
- [ ] 모든 타일 통일된 스타일
- [ ] 색 구분 확실 (빨파초노보핑)
- [ ] 타일 크기가 그리드에 맞음
- [ ] 배경과 그리드 영역 일치
- [ ] HUD 글씨가 테두리 안에 들어감
- [ ] 특수 타일 이미지 정상 표시
- **커밋:** `art: 타일/배경/HUD 이미지 리뉴얼`

### Antigravity 조작 방법
1. Antigravity IDE 실행 (별도 앱, VS Code 기반)
2. File → Open Folder → `C:/Project/Match3` 열기
3. 오른쪽 Gemini 채팅에 프롬프트 입력
4. 생성 완료 후 `Assets/Textures/` 폴더에 파일 확인
5. Unity 에디터에서 이미지 확인 → 적용

---

## 핵심 함정 & 방지법

| # | 함정 | 방지법 |
|---|---|---|
| 1 | Unity 물리 엔진으로 타일 낙하 | 직접 좌표 계산 + 커스텀 Tween |
| 2 | Instantiate/Destroy 반복 | 오브젝트 풀링 |
| 3 | 초기 보드에서 매치 발생 | 배치 시 좌측2+하단2 검사 |
| 4 | 데드 보드 (이동 불가) | FindValidMove() + Fisher-Yates 셔플 |
| 5 | 순수 랜덤 스폰 → 연쇄 재앙 | 동적 가중치 확률 테이블 |
| 6 | 애니메이션 중 입력 → 데이터 꼬임 | FSM + Processing 플래그 |
| 7 | Move()에서 X,Y 즉시 변경 | 데이터-시각 분리 (MoveVisual) |

## Git 전략
- **브랜치:** master (현재 안정 버전)
- **커밋:** Phase 단위, 메시지 명확히
- **푸시:** 사용자 승인 후에만
- **태그:** 각 Phase 완료 시 `v0.1`, `v0.2` 등

## 진행상황

| Phase | 상태 | 커밋 | 날짜 |
|---|---|---|---|
| Phase 1: FSM + 데이터-시각 분리 | ✅ 완료 | 커밋 대기 | 2026-03-21 |
| Phase 2: 스왑 애니메이션 | ✅ 완료 | 커밋 대기 | 2026-03-21 |
| Phase 3: 힌트 시스템 | ✅ 완료 | 커밋 대기 | 2026-03-21 |
| Phase 4: Juicing | ✅ 완료 | 커밋 대기 | 2026-03-21 |
| Phase 5: 폴리싱 + 상용화 | ✅ 완료 | 커밋 대기 | 2026-03-21 |
| Phase 6: 그래픽 고도화 | ✅ 타일 완료 | - | 2026-03-21 |

### Phase 6 진행상황 (2026-03-21)
- ✅ fish_1.png (노랑), fish_2.png (보라), fish_3.png (빨강), fish_4.png (파랑), fish_5.png (초록), fish_6.png (핑크), rainbow_fish.png — 전부 생성 완료, 광택 구슬 스타일 통일
- **미해결 문제:** Antigravity Gemini 입력란에 프롬프트 붙여넣기 실패
  - 원인: VSCode 터미널에서 python 실행 시 포커스를 뺏어감 → keybd_event(Ctrl+V)가 VSCode로 감
  - 시도한 방법: screen_clicker.py click_text, 직접 좌표 클릭, 원스크립트 처리 — 전부 실패
  - **제안 해결책:** pythonw로 별도 스크립트 실행 (콘솔 없이) 또는 사용자가 직접 입력
- 배경(bg.png), HUD(hud_panel.png) — 미생성

## 리서치 출처
- NotebookLM #1: https://notebooklm.google.com/notebook/eb5804c6-b234-4c48-ac37-7d55041af807
  - 매치3 게임 개발 방법론 + 알고리즘 아키텍처 + 통합 구축 전략
- NotebookLM #2: https://notebooklm.google.com/notebook/9621b87a-cce7-4530-a218-38b24f7ff6b5
  - 유니티 매치3 개발 (MVC, FSM, 오브젝트 풀링, ScriptableObject)
- 구현 중 막히면 위 NotebookLM에 질문하여 자료 확인할 것

## 새 세션 인수인계 체크리스트
새 세션이 이 프로젝트를 이어받을 때:
1. [ ] 이 계획서 전체를 읽었는가
2. [ ] 진행상황 테이블에서 현재 Phase를 확인했는가
3. [ ] 글로벌 CLAUDE.md (`C:/Users/comes/CLAUDE.md`)를 읽었는가
4. [ ] 도구 경로(screen_clicker.py, click_points.py)를 확인했는가
5. [ ] Unity 에디터를 실행하고 Play 모드로 현재 상태를 확인했는가
6. [ ] NotebookLM 리서치 자료를 필요 시 참조할 수 있는가

## 이전 세션 미해결 문제 (2026-03-21)

### Antigravity Gemini 입력란 붙여넣기 실패
- **증상:** Ctrl+V가 Antigravity가 아니라 VSCode(Claude Code 터미널)로 들어감
- **원인:** python 스크립트가 VSCode 터미널에서 실행되므로, SendInput 클릭으로 Antigravity에 포커스를 줘도 keybd_event 시점에 VSCode가 포커스를 다시 가져감
- **시도한 것:** screen_clicker.py click_text, 직접 좌표 클릭, 원스크립트(클릭+붙여넣기+Enter), SetForegroundWindow — 전부 실패
- **해결 방향:**
  1. `pythonw gemini_input.py "프롬프트"` — 콘솔 없이 실행하면 VSCode가 포커스 안 뺏을 수 있음
  2. subprocess.Popen으로 별도 프로세스 실행 후 현재 프로세스 즉시 종료
  3. 사용자가 직접 입력 (가장 확실)

### screen_clicker.py --monitor 좌표 오프셋 문제
- `find_text "Queue another" --monitor 2` → x=-1035 반환 (음수)
- monitors[2]의 left=0인데 음수 좌표 반환 → offset이 이중 적용되는 버그 의심
- 실제 screen 좌표는 x≈885인데 -1035 반환
