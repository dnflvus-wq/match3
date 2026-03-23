# Phase 12: 그래픽/디자인 시스템

## 목표
상용 Match3 수준의 시각적 퀄리티 확보 + 일관된 디자인 시스템 구축

## 에셋 제작 파이프라인
- **AI 생성 (Antigravity + Gemini)**: 배경, 타일, UI 기본 소스
- **후처리**: rembg 누끼, 리사이즈, 색보정
- **Unity 에디터**: Sprite Atlas 묶기, Material/Shader 설정

---

## 12-1. 타일 스프라이트

| 항목 | 사양 |
|-----|------|
| 스타일 | 글로시 3D 느낌 (구슬/보석) |
| 해상도 | 128×128px (Atlas에서 관리) |
| 색상 | 8색 (빨/파/초/노/핑/보/주황/청록) |
| 포맷 | PNG 투명 배경 |
| 일관성 | 조명 방향, 그림자, 광택 위치 통일 |

### 특수 타일 비주얼
- **줄무늬**: 일반 타일 + 가로/세로 줄무늬 오버레이
- **폭탄**: 일반 타일 + 포장지 오버레이 + 글로우
- **레인보우**: 무지개 색상 순환 + Shader Graph 반짝임
- **로켓**: 추진체 모양 + 트레일 파티클

### 장애물 스프라이트
- **얼음**: 1~3단계 (투명도 변화, 금 갈수록 불투명)
- **사슬**: 타일 위 X자 사슬 오버레이
- **금속**: 파괴 불가, 회색 메탈릭
- **포털**: 입구/출구 색상 쌍

## 12-2. 배경 디자인

| 테마 | 분위기 | 용도 |
|-----|--------|------|
| 마법의 숲 | 크리스탈, 오로라, 반딧불 | 레벨 1~20 |
| 수중 궁전 | 산호, 물고기, 빛줄기 | 레벨 21~40 |
| 하늘 성 | 구름, 무지개, 날개 | 레벨 41~60 |
| 화산 동굴 | 용암, 보석, 횃불 | 레벨 61~80 |
| 별빛 우주 | 성운, 행성, 별똥별 | 레벨 81~100 |

### 배경 구성
- **3레이어 패럴랙스**: 원경(하늘) + 중경(건물/나무) + 근경(장식)
- 해상도: 1080×2160px (세로)
- 보드 뒤 영역만 보이므로 중앙 집중 디자인

## 12-3. UI 디자인 시스템

### 색상 팔레트
- Primary: 금색 (#FFD700) — 버튼, 강조
- Secondary: 보라색 (#6B3FA0) — 패널, 배경
- Accent: 하늘색 (#4FC3F7) — 링크, 활성 상태
- Danger: 빨간색 (#FF4444) — 경고, 이동수 부족
- Text: 흰색 (#FFFFFF), 어두운 갈색 (#3E2723)

### 폰트
- 제목: 굵은 라운드 산세리프 (LilitaOne 또는 유사)
- 본문: 깔끔한 산세리프
- 숫자: 고정폭, 굵은 글씨

### 버튼 스타일
- 기본: 금색 그라데이션 + 둥근 모서리 + 그림자
- 비활성: 회색 톤
- 클릭: 스케일 0.95 + 밝기 증가

### UI 화면 목록

| # | 화면 | 필요 에셋 |
|---|------|---------|
| 1 | 스플래시/로딩 | 로고, 프로그레스 바 |
| 2 | 메인 로비 | 배경, 플레이 버튼, 코인/하트 표시, 설정 아이콘 |
| 3 | 월드맵/레벨선택 | 맵 배경, 레벨 노드, 캐릭터 |
| 4 | 레벨 시작 팝업 | 목표 표시, 부스터 선택, 시작 버튼 |
| 5 | 인게임 HUD | 점수, 이동수, 목표 진행률, 일시정지 |
| 6 | 일시정지 팝업 | 계속/재시작/나가기 |
| 7 | 게임 클리어 팝업 | 별 3개 애니메이션, 점수, 보상, 다음 레벨 |
| 8 | 게임 실패 팝업 | +5이동수(광고/결제), 재시작, 나가기 |
| 9 | 부스터/상점 | 부스터 목록, 가격, 구매 버튼 |
| 10 | 일일 보상 팝업 | 연속 출석 보너스, 수령 버튼 |
| 11 | 설정 | BGM/SFX 슬라이더, 언어, 계정 연동 |
| 12 | 시즌패스 | 무료/유료 트랙, 진행률 |

## 12-4. 이펙트 & 연출

### 파티클 Prefab 목록
- MatchBurst (일반 매치 — 색상별 파편)
- SpecialCreate (특수 타일 생성 — 섬광)
- LineClear (줄무늬 발동 — 가로/세로 레이저)
- BombExplode (폭탄 — 3x3 폭발 + 쇼크웨이브)
- ColorBomb (레인보우 — 전체 화면 섬광)
- LevelClear (레벨 클리어 — 폭죽/컨페티)
- ComboFlash (콤보 — 화면 번쩍임)

### Shader Graph 이펙트
- **타일 반짝임 (Shimmer)**: Dot Product + 시간 기반 UV 이동
- **쇼크웨이브 (Shockwave)**: 폭탄 발동 시 배경 굴절
- **글로우 (Glow)**: 특수 타일/힌트 타일 외곽 발광
- **홀로그램**: 레인보우 타일 무지개 색상 순환

### 2D Lighting (URP)
- Global Light: 전체 분위기 조명
- Point Light: 특수 타일/폭발 시 동적 광원
- 배경 레이어에 Normal Map 적용 → 깊이감

### 화면 전환
- 씬 전환: Circle Wipe (원형 축소/확대)
- 팝업: 배경 어둡게 + 스케일 바운스 등장
- 레벨 시작: 3-2-1 카운트다운 + 줌인

## 12-5. 캐릭터/마스코트
- 메인 캐릭터 1종 (레벨선택/스토리에 등장)
- 감정 표현 3~5종 (기쁨/슬픔/놀람/응원/실패)
- 튜토리얼 가이드 역할

## 12-6. 앱스토어 에셋
- 앱 아이콘 (512×512, 1024×1024)
- 스토어 스크린샷 5~8장
- 피처 그래픽 (1024×500)
- 프로모션 영상 (30초)

## 12-7. NotebookLM 검토 결과 추가 필요 에셋

### 인게임 보드
- 유도형 특수 타일 스프라이트 (종이비행기/로켓)
- 특수 타일 시너지 이펙트 (폭탄+폭탄 거대폭발, 줄무늬+줄무늬 거대십자 등)
- 다층 장애물 단계별 상태 스프라이트 (얼음 금 가는 단계 등)
- 포털, 컨베이어 벨트, 생성기(대포) 기믹 스프라이트
- 타일 선택 하이라이트, 파괴 직전 떨림(Anticipation) 애니메이션

### VFX 추가
- 트레일/잔상 (특수 타일 이동 궤적)
- 콤보 타이포그래피 ("Great!", "Awesome!", "Amazing!")
- 거절(핑퐁) 애니메이션

### UI 추가
- **레벨 시작 팝업** (목표 표시 + 부스터 선택 + 시작 버튼)
- **보너스 타임 연출** (남은 턴 → 특수 타일로 변환 후 연쇄 폭발)
- 일일 보상/이벤트 배너
- 시즌패스 UI

## Unity 에디터 필수 설정 (NotebookLM 확인)

### 스프라이트 Import Settings
```
타일 스프라이트 (256×256 텍스처):
  - Texture Type: Sprite (2D and UI)
  - Pixels Per Unit: 256 (텍스처 크기와 동일 → 1 Unit = 1 타일)
  - Filter Mode: Bilinear (선명한 외곽선)
  - Max Size: 256
  - Compression: ASTC 4x4 (Android) / ASTC 4x4 (iOS)
  - Generate Mip Maps: OFF (2D 게임에서 불필요)

UI 스프라이트:
  - Texture Type: Sprite (2D and UI)
  - Filter Mode: Bilinear
  - Max Size: 1024 (배경은 2048)
  - Compression: ASTC 6x6 (UI는 약간 손실 허용)

배경 스프라이트:
  - Max Size: 2048
  - Compression: ASTC 6x6
```

### 텍스처 압축 (NLM 확인)
- **ASTC 4x4~6x6**: 최신 Android/iOS 표준. 타일은 4x4 (최소 손실)
- **ETC2 8-bit RGBA**: 구형 Android Fallback (Alpha 포함 시 필수)
- 8x8 이상 압축 시 타일 경계선 뭉개짐 → 사용 금지

### Sprite Atlas 최적 설정 (NLM 확인)
```
TileAtlas.spriteatlas Inspector:
  - Padding: 4 (블리딩 방지)
  - Allow Rotation: OFF (UI 요소 회전 깨짐 방지)
  - Max Texture Size: 2048
  - Compression: ASTC 4x4

UIAtlas.spriteatlas Inspector:
  - Padding: 2
  - Allow Rotation: OFF
  - Max Texture Size: 2048
  - Compression: ASTC 6x6
```

### Juicing 에디터 설정 (NLM 확인)
- **Post-Processing Volume**: Vignette (폭발 시 ON/OFF), Chromatic Aberration (절제 사용)
- **Shader Graph**: Sine/Cosine으로 타일 Wobble 효과 (스크립트 없이)
- **Screen Shake**: DOTween `DOShakePosition(0.1s, 0.1f)` (폭발 시)
- **Hitstop**: `Time.timeScale = 0` for 0.05s → 타격감 강화
- **Haptics**: 모바일 진동 `Handheld.Vibrate()` (폭발 시)

## 검증 항목
- [ ] 타일 8색 스프라이트 일관성
- [ ] 특수 타일 4종 비주얼 구분 명확
- [ ] 장애물 3종 이상 스프라이트
- [ ] 배경 최소 2종 (1종은 현재 판타지 테마)
- [ ] UI 12개 화면 디자인
- [ ] 파티클 Prefab 7종
- [ ] Shader Graph 이펙트 2종 이상
- [ ] 캐릭터 일러스트 1종
- [ ] 앱 아이콘

## MCP 도구 호출 순서

```
Step 1: script-execute → 텍스처 Import Settings 일괄 변경 (PPU, Filter Mode, ASTC 압축)
Step 2: script-execute → Sprite Atlas에 스프라이트 추가 + Padding/Rotation 설정
Step 3: assets-refresh → 리임포트
Step 4: screenshot-game-view → 압축 후 시각적 품질 확인
Step 5: script-execute → Shader Graph Material 생성 (Wobble, Glow 등)
```

**에셋 생성**: Antigravity+Gemini로 이미지 생성 → rembg 누끼 → Unity 에디터에 임포트
**MCP로 처리:**
- Shader Graph → script-execute로 ShaderGraph API 또는 Material 속성 설정
- Post-Processing Volume → script-execute로 Volume Profile 생성 + 값 설정
- 최종 시각 확인만 사용자가 screenshot-game-view로 검토

## 진행률: 5% (판타지 배경 1종 완료)
