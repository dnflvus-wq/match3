# Match3 리팩토링 계획서 (Unity 에디터 활용 중심)

## 목표
God Object(GameGrid.cs 1400줄) 중심의 스파게티 코드를 MVC 아키텍처로 전환하되,
**Unity 에디터의 시각적 도구를 최대한 활용**하여 코드를 최소화하고,
에디터에서 직관적으로 수정/확장 가능한 구조로 만든다.

## 핵심 원칙
- **에디터에서 할 수 있는 것은 에디터에서** — 코드로 UI/파티클/애니메이션 생성 금지
- **Prefab 중심 개발** — new GameObject + AddComponent 금지, Prefab Instantiate
- **ScriptableObject로 데이터 관리** — 하드코딩/매직넘버 금지
- **Unity MCP 활용** — AI가 에디터를 직접 조작하여 Prefab/씬 작업

## 검증 프로세스
1. 각 Phase 구현 후 갭분석 (계획서 대비 99% 일치)
2. Unity Play 모드 E2E 테스트
3. 최종 갭분석 → 사용자 보고
4. **빌드는 사용자 승인 후에만**

---

## Phase 1: 에디터 에셋 기반 구축 (Prefab + ScriptableObject)
**위험도: 낮음** — 기존 코드 건드리지 않고 에셋만 추가

### 1-1. 타일 Prefab 시스템 (Unity MCP로 에디터에서 생성)
- **Base Tile Prefab** 1개 생성:
  - Transform (0,0,0), Scale (1,1,1)
  - SpriteRenderer (sortingLayer: "Tiles")
  - BoxCollider2D (isTrigger: true)
  - TileView.cs 스크립트 (스프라이트 교체, 이동 애니메이션, 파괴 이펙트)
- **Prefab Variant 8개** (Base 상속):
  - Tile_Red, Tile_Blue, Tile_Green, Tile_Yellow, Tile_Pink, Tile_Purple, Tile_Orange, Tile_Cyan
  - 각 배리언트에서 Sprite만 오버라이드

### 1-2. 파티클 이펙트 Prefab (에디터 Particle System)
- 코드(MatchParticles.cs 274줄) → **에디터에서 Particle System Prefab 4종** 제작
  - **MatchSparkle.prefab**: Duration 0.5, Burst 15~20, Looping OFF
  - **MatchStar.prefab**: Duration 0.8, Start Size 0.3~0.5
  - **MatchGlow.prefab**: Duration 0.3, Color over Lifetime(알파 페이드)
  - **MatchRing.prefab**: Duration 0.4, Shape Ring
- 각 Prefab의 Main/Emission/Shape/Color/Size 모듈을 **Inspector에서** 설정

### 1-3. UI Prefab (Canvas에서 직접 배치)
- **LevelButton.prefab**: Image(level_btn) + Button + Text(레벨번호) + Text(별점)
- **BoosterButton.prefab**: Image + Button + Text(수량) + 가격 표시
- **HudPanel.prefab**: 점수 Text, 이동수 Text, 코인 Text 배치
- **GameOverPanel.prefab**: 결과 텍스트, 별 3개, 다시하기/홈 버튼

### 1-4. ScriptableObject 데이터
- **GameConfig.asset** (SO): fillTime, swapTime, hintDelay, matchLength 등
- **LevelConfig.asset** × 5개: 보드 크기, 이동수, 목표점수, 타일 가중치
- **Custom Editor 스크립트**: Inspector에서 2D 그리드를 시각적으로 편집

### 1-5. Sprite Atlas
- **TileAtlas.spriteatlas**: 8색 타일 스프라이트 묶기
- **UIAtlas.spriteatlas**: UI 아이콘(부스터, 버튼, 배경) 묶기

### 검증 항목
- [ ] 타일 Prefab 8종이 Assets/Prefabs/Tiles/에 존재
- [ ] 파티클 Prefab 4종이 Assets/Prefabs/Effects/에 존재
- [ ] UI Prefab 4종이 Assets/Prefabs/UI/에 존재
- [ ] GameConfig, LevelConfig SO가 Assets/ScriptableObjects/에 존재
- [ ] Sprite Atlas 2종 생성 및 Pack Preview 확인
- [ ] 기존 게임 플레이에 영향 없음 (아직 연결 안 함)

---

## Phase 2: Model 분리 (순수 C# — Unity 의존 없음)
**위험도: 중간** — 핵심 로직 분리, GameGrid에서 호출만 변경

### 2-1. BoardModel.cs (순수 C#)
- TileData[,] 배열로 보드 상태 관리
- Initialize(), GetTileAt(), SetTileAt(), SwapData()
- **Unity API 의존 없음** → 단위 테스트 가능

### 2-2. MatchFinder.cs (순수 C#)
- FindAllMatches(), HasValidMove(), GetHintMove()
- CheckSwapCreatesMatch()

### 2-3. DropSimulator.cs (순수 C#)
- CalculateDrops(), GenerateNewTiles()
- 가중치 기반 타일 색상 생성 (Controlled Chaos)

### 검증 항목
- [ ] BoardModel이 GameGrid의 _pieces[,] 역할 대체
- [ ] MatchFinder가 매치 검사 정확히 수행
- [ ] DropSimulator가 낙하/스폰 정확히 계산
- [ ] 모든 레벨 정상 플레이

---

## Phase 3: Controller + FSM 분리
**위험도: 중간**

### 3-1. GameStateMachine.cs
- PREGAME → READY → EVALUATE → MATCH → COLLAPSE → (재귀) → READY / ENDGAME
- 입력은 READY 상태에서만 허용

### 3-2. InputController.cs (MonoBehaviour)
- 터치/마우스 입력 → 그리드 좌표 변환
- 드래그 임계값 (타일 절반 길이 이상)
- 유효하지 않은 스왑 시 거절 애니메이션(핑퐁)

### 3-3. GameController.cs (메인 오케스트레이터)
- Model, View, FSM을 연결
- 스왑 → 매치검사 → 제거 → 낙하 → 재검사 루프 관리
- 코루틴으로 애니메이션 시퀀싱 (DOTween 활용)

### 검증 항목
- [ ] FSM이 입력 차단/허용 올바르게 관리
- [ ] 연쇄 반응(Cascade) 무한 루프 없음
- [ ] 카운트다운 중 입력 차단
- [ ] 게임오버 조건 정상 판정

---

## Phase 4: View 분리 + Prefab 연결
**위험도: 높음** — 시각적 변경, 씬 수정

### 4-1. BoardView.cs (MonoBehaviour)
- Phase 1에서 만든 타일 Prefab을 Instantiate
- 이벤트 구독: Model.OnMatchFound → 파괴 애니메이션 + 파티클 Prefab 스폰
- DOTween으로 스왑/낙하 애니메이션 (Ease.OutBounce 등)
- **오브젝트 풀링** 통합 (타일 + 파티클)

### 4-2. TileView.cs (각 타일 Prefab에 부착)
- SetSprite(), MoveTo(), PlayDestroyEffect(), PlayHintPulse()
- Shader Graph 기반 하이라이트 (글레어/글로우)

### 4-3. 통신 방식
- **Model → View**: C# event/Action (OnMatchFound, OnTilesSwapped 등)
- **Controller → Model**: 직접 참조
- **View → Controller**: 애니메이션 완료 콜백 (OnAnimationFinished)

### 검증 항목
- [ ] 타일 Prefab이 정상 인스턴스화
- [ ] 파티클 Prefab이 매치 시 재생
- [ ] 오브젝트 풀링으로 GC 스파이크 제거
- [ ] DOTween 애니메이션 부드럽게 동작
- [ ] Edit 모드 Game 뷰에서도 배경/UI가 보임

---

## Phase 5: UI를 Prefab 기반으로 전환
**위험도: 중간**

### 5-1. LevelSelect 씬
- Canvas에 LevelButton Prefab 5개 배치 (에디터에서)
- LevelSelectUI.cs: 데이터 바인딩만 (별점, 잠금 상태)
- 코드에서 new GameObject 생성 → **완전 제거**

### 5-2. 인게임 HUD
- Canvas에 HudPanel Prefab 배치 (에디터에서)
- HudUI.cs: 점수/이동수 Text 업데이트만
- MobileHUDLayout.cs 237줄 → **제거** (Prefab + CanvasScaler로 대체)

### 5-3. 부스터 UI
- Canvas에 BoosterButton Prefab 3개 배치 (에디터에서)
- BoosterUI.cs 384줄 → 100줄 이하로 축소 (데이터 바인딩만)

### 5-4. 게임오버
- GameOverPanel Prefab + Timeline 연출
- GameOver.cs 154줄 → Timeline PlayableDirector 제어만

### 검증 항목
- [ ] 레벨 선택 화면이 Prefab 기반으로 정상 표시
- [ ] HUD 정상 (점수, 이동수, 코인)
- [ ] 부스터 동작 (망치, 셔플, +5)
- [ ] 게임오버 연출 정상

---

## Phase 6: 사운드/오디오 개선
**위험도: 낮음**

### 6-1. 프로시저럴 오디오 → 실제 음원
- ProceduralAudio.cs 163줄 (사인파 생성) → **제거**
- 무료 SFX 에셋 또는 Antigravity로 생성
- Audio Mixer로 BGM/SFX 볼륨 분리 관리

### 검증 항목
- [ ] 매치/스왑/콤보/클리어/게임오버 사운드 재생
- [ ] BGM/SFX 개별 볼륨 조절

---

## Phase 7: 데이터 저장 계층 리팩토링
**위험도: 중간**

### 7-1. UserData.cs (순수 C# POCO)
- coins, lives, levelStars, boosterCounts
- JSON 직렬화 → Application.persistentDataPath 저장

### 7-2. DataManager.cs
- SaveLocal(), LoadLocal()
- (향후) SyncToServer(), SyncFromServer() — REST API 준비

### 7-3. PlayerPrefs 완전 제거
- CoinSystem, BoosterSystem, LifeSystem, LevelSelect에서 제거

### 검증 항목
- [ ] 데이터가 JSON 파일로 저장/로드
- [ ] 앱 재시작 시 데이터 유지
- [ ] PlayerPrefs 참조 완전 제거

---

## Phase 8: 최종 정리 + GameGrid.cs 제거
**위험도: 낮음**

- GameGrid.cs → 모든 기능이 분리된 클래스로 이관 확인 후 삭제
- 미사용 코드/파일 정리
- 프로파일링 + 최적화
- 전체 레벨 E2E 테스트

---

## Phase 9: 레벨 에디터 도구 (대량 레벨 제작)
**위험도: 낮음** — 에디터 전용 도구, 게임 코드 영향 없음

### 9-1. Custom Inspector 그리드 에디터
- LevelConfig SO를 Inspector에서 시각적 2D 그리드로 표시
- 마우스 클릭으로 타일 종류 배치 (일반/장애물/빈칸/얼음/사슬 등)
- 색상 팔레트로 직관적 편집
- 참고: [UnityGridLevelEditor](https://github.com/SinlessDevil/UnityGridLevelEditor)

### 9-2. CSV/Excel → ScriptableObject 파이프라인
- 기획자가 Excel/Google Sheets에서 레벨 데이터 작성
- 보드 크기, 이동수, 목표점수, 타일 가중치 등
- [BakingSheet](https://github.com/cathei/BakingSheet) 또는 자체 CSV 파서로 임포트
- 임포트 시 LevelConfig SO 자동 생성

### 9-3. AI 레벨 밸런싱 (향후)
- 헤드리스 모드로 AI가 레벨 자동 플레이
- 클리어율/평균 이동수 통계 수집
- 난이도 자동 조정

### 검증 항목
- [ ] Custom Inspector에서 그리드 시각적 편집 가능
- [ ] CSV에서 LevelConfig SO 자동 생성
- [ ] 생성된 레벨이 게임에서 정상 로드/플레이

---

## 목표 폴더 구조
```
Assets/
├── Prefabs/
│   ├── Tiles/
│   │   ├── BaseTile.prefab         ← 베이스
│   │   ├── Tile_Red.prefab         ← Variant
│   │   ├── Tile_Blue.prefab
│   │   └── ... (8색)
│   ├── Effects/
│   │   ├── MatchSparkle.prefab     ← 에디터에서 Particle System
│   │   ├── MatchStar.prefab
│   │   ├── MatchGlow.prefab
│   │   └── MatchRing.prefab
│   └── UI/
│       ├── LevelButton.prefab      ← 에디터에서 Canvas 배치
│       ├── BoosterButton.prefab
│       ├── HudPanel.prefab
│       └── GameOverPanel.prefab
├── ScriptableObjects/
│   ├── GameConfig.asset            ← Inspector에서 편집
│   └── Levels/
│       ├── Level01.asset
│       └── ... (5개)
├── Scripts/
│   ├── Model/        ← 순수 C# (Unity 의존 없음)
│   ├── View/         ← MonoBehaviour (Prefab에 부착)
│   ├── Controller/   ← MonoBehaviour (씬에 배치)
│   ├── UI/           ← 데이터 바인딩만
│   ├── Data/         ← UserData, DataManager
│   ├── System/       ← Audio, Pool, Coin, Booster
│   └── Events/       ← GameEvents
├── SpriteAtlases/
│   ├── TileAtlas.spriteatlas
│   └── UIAtlas.spriteatlas
└── Timeline/
    ├── GameStart.playable
    └── GameOver.playable
```

## Unity 에디터 활용 요약

| 기능 | 현재 (코드) | 목표 (에디터) |
|------|------------|-------------|
| 타일 생성 | Instantiate + 코드 설정 | Prefab Variant + Inspector |
| UI 배치 | new GameObject + AddComponent | Canvas에서 드래그앤드롭 |
| 파티클 | 코드로 274줄 프로시저럴 | Particle System Inspector |
| 애니메이션 | Coroutine + Lerp | DOTween + Timeline |
| 레벨 설정 | 하드코딩/매직넘버 | ScriptableObject + Custom Editor |
| 사운드 | 사인파 코드 생성 | Audio Clip + Audio Mixer |
| 타일 하이라이트 | 코드로 색상 변경 | Shader Graph Material |
| 연출 | 코루틴 하드코딩 | Timeline PlayableDirector |

## 리팩토링 순서 원칙 (NotebookLM Playwright 직접 확인)

### 핵심: MVC 코드 먼저 → Prefab 나중에
- Prefab은 View에 해당 → Model/Logic 분리 후에 적용해야 안전
- 현재 GameGrid.cs가 특정 GameObject 구조를 기대하므로, Prefab부터 만들면 게임이 망가짐

### Strangler Pattern (점진적 교살 패턴)
- 바깥쪽(의존성 적은 곳)부터 하나씩 추출
- 매 단계마다 게임 정상 작동 테스트

### 5단계 순서 (NotebookLM 권장)
1. **InputController 추출** — 가장 독립적, 내부 로직 안 건드림
2. **BoardModel 구축** — 순수 C# 데이터 모델, GameGrid가 BoardModel 읽도록 변경
3. **MatchFinder + DropSimulator 추출** — 알고리즘 분리, BoardModel만 참조
4. **BoardView + PiecePool 통합** — GameGrid → BoardView 이름 변경, 오브젝트 풀링 이 단계에서
5. **GameController + FSM** — 중앙 통제, 상태 머신 마지막에

### DOTween vs Animator (NotebookLM 확인)
- **보드 내 모든 타일 애니메이션: DOTween** (스왑, 낙하, 파괴, 팝업, 카메라쉐이크)
  - 스왑: OnComplete 콜백으로 로직 동기화
  - 낙하: Bounce/Elastic Easing (물리엔진 사용 금지)
  - 파괴: DOTween Sequence (스케일→페이드→파티클 체이닝)
  - 콤보: DOMoveY + DOFade
  - 카메라: DOShakePosition 또는 Cinemachine Impulse
- **캐릭터/UI 정적 연출만: Animator**

### 기타 원칙
- 한 Phase 완료 후 E2E 테스트 통과 확인 후 다음 Phase
- 빌드는 사용자 승인 후에만
