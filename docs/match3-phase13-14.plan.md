# Match3 게임 고도화 추가 계획서 (Phase 13~14)

## Phase 13: 화면비 수정 (긴급)

### 문제
- **모든 스테이지**: 하단에 하늘색 빈 공간 발생
- **스테이지 2**: 오른쪽이 화면 밖으로 넘침
- 원인: 카메라 orthographic size / Y 위치가 그리드 크기에 적응 못 함

### 13-1. 카메라 자동 맞춤 개선
- MobileCameraSetup.cs 완전 재작성 ✅
- 그리드 중심 기준으로 카메라 위치 자동 계산
- HUD(상단 15%) + 부스터UI(하단 8%) 영역 제외한 중앙에 그리드 배치
- 가로/세로 중 더 큰 쪽에 맞춰 ortho size 결정 → 잘림 방지

### 13-2. 검증 항목
- [ ] 스테이지 1: 하단 빈 공간 없음
- [ ] 스테이지 2: 오른쪽 넘침 없음
- [ ] 스테이지 3: 정상 배치
- [ ] 9:16, 9:19.5, 9:21 등 다양한 비율에서 정상
- **커밋:** `fix: 모바일 화면비 자동 맞춤 개선`

---

## Phase 14: 그래픽 고도화 (배경 + UI)

### 14-1. 배경 이미지 리뉴얼
- 현재: Gemini가 만든 초원+과자집 (soso)
- 목표: 캔디크러쉬급 화려한 판타지 배경
- Antigravity + Gemini로 생성
- 1080x1920 Portrait, 상단 HUD + 중앙 그리드 + 하단 부스터 영역 구분
- 프롬프트:
```
Create a vibrant candy kingdom background for a match-3 mobile game.
Portrait orientation 1080x1920px.
Top 15%: sky area for HUD overlay.
Center 70%: lighter area with subtle grid pattern for game board.
Bottom 15%: ground area with candy decorations.
Style: colorful, fantasy, candy crush inspired, glossy.
Save to Assets/Textures/bg.png
```

### 14-2. HUD 패널 리뉴얼
- 현재: 나무판 (글씨가 테두리에 걸림)
- 목표: 라운드 사각형, 안쪽 여백 충분
- 프롬프트:
```
Create a glossy wooden panel UI element for a candy match-3 game HUD.
Rounded rectangle shape, thick candy-colored border.
Inner area should be flat and dark for text readability.
360x320px, transparent background PNG.
Save to Assets/Resources/hud_panel.png
```

### 14-3. 부스터 버튼 이미지
- 망치, 셔플, +5 아이콘 생성
- 프롬프트:
```
Create 3 booster icons for a candy match-3 game:
1. hammer_icon.png - cute hammer smashing a candy
2. shuffle_icon.png - two arrows in a circle (refresh/shuffle)
3. extra_moves_icon.png - "+5" text on a golden star
Each 128x128px, transparent PNG, glossy candy style.
Save to Assets/Textures/UI/
```

### 14-4. 특수 타일 이미지
- 줄무늬 캔디 (가로/세로): 기본 캔디 + 줄무늬 오버레이
- 폭탄: 기본 캔디 + 폭발 이펙트 마크
- 프롬프트:
```
Create special candy pieces for a match-3 game, same glossy round style:
1. striped_h.png - horizontal striped overlay on a candy (white lines)
2. striped_v.png - vertical striped overlay on a candy (white lines)
3. bomb_overlay.png - bomb/explosion mark overlay
Each 110x110px, transparent PNG.
Save to Assets/Textures/Sprites/
```

### 14-5. 검증 항목
- [ ] 배경이 화면에 맞게 표시
- [ ] HUD 글씨가 패널 안에 깔끔히 들어감
- [ ] 부스터 아이콘 정상 표시
- [ ] 특수 타일 이미지 게임에서 표시
- **커밋:** `art: 배경/HUD/부스터/특수타일 그래픽 고도화`

---

## 구현 검증 프로세스
1. 이 계획서를 반드시 Read 도구로 다시 읽기
2. 검증 항목 하나씩 체크
3. Unity Play 모드 + 모바일 APK 실제 테스트
4. Gap Analysis 99% 일치 시까지 반복
