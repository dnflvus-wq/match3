# Match3 비주얼 고도화 계획서

## 목표
- 타일 이미지를 알록달록하고 아기자기하게 교체 (빨강/파랑/초록/노랑 등 색 구분 확실)
- 뒷배경 교체
- 상단 HUD(전광판) 디자인 개선 — 글씨/이미지 삐져나감 수정
- 유니티 에디터 Play 모드로 확인 후 사용자 컨펌 시에만 APK 빌드

## 현재 상태
- 타일 이미지: `Assets/Textures/Sprites/fish_1~6.png` (물고기 테마, 6색)
- 색상 타입: Yellow, Purple, Red, Blue, Green, Pink (`ColorType.cs`)
- 배경: `Assets/Textures/UI/bg.png`, `bg2.png`
- HUD: `MobileHUDLayout.cs` — 패널 3등분 배치, 텍스트 삐져나감 문제 있음
- 특수 피스: `rainbow_fish.png`, 버블 8종

## 작업 항목

### Phase 1: 타일 이미지 교체
1. Antigravity IDE + Gemini로 새 타일 이미지 6개 생성
   - 스타일: 캔디크러쉬 스타일 알록달록, 둥글고 귀여운 형태
   - 색상: 빨강, 파랑, 초록, 노랑, 보라, 분홍 (기존 6색 매핑)
   - 크기: 128x128px, 투명 배경 (PNG)
   - rembg로 누끼 처리
2. rainbow 특수 피스 이미지도 새로 생성
3. `Assets/Textures/Sprites/` 교체
4. **유니티 에디터에서 Play → 타일 색 구분 확인**

### Phase 2: 배경 교체
1. 게임 배경 이미지 생성 (밝고 화사한 톤)
2. `Assets/Textures/UI/bg.png` 교체
3. **유니티 에디터에서 Play → 배경 확인**

### Phase 3: HUD 개선
1. HUD 배경 이미지 생성 (나무판/리본 스타일)
2. `MobileHUDLayout.cs` 수정:
   - subtext가 패널 밖으로 절대 안 나가게
   - 폰트 크기 자동 조절 강화
   - 패널 간 간격 확보
3. 숫자/라벨 가독성 확인
4. **유니티 에디터에서 Play → HUD 텍스트 잘림/삐져나감 없는지 확인**

### Phase 4: 최종 확인
1. 유니티 에디터 Play 모드 실행
2. 스크린샷 캡처하여 사용자에게 보여주기
3. **사용자 컨펌 받은 후에만 APK 빌드**

## 검증 항목 체크리스트
- [ ] 타일 6색이 확실히 구분되는가
- [ ] 타일이 알록달록하고 아기자기한가
- [ ] 배경이 밝고 화사한가
- [ ] HUD 글씨가 패널 안에 잘 들어가는가
- [ ] HUD subtext(target score, moves remaining)가 삐져나가지 않는가
- [ ] HUD 숫자가 읽기 좋은 크기인가
- [ ] 게임 플레이에 지장 없는가 (매칭, 애니메이션)
- [ ] 유니티 Play 모드에서 Portrait 모드 정상 동작

## 반복 검증 프로세스
1. 구현 완료 → 계획서 다시 읽기
2. 체크리스트 하나씩 유니티 에디터에서 검증
3. 실패 항목 수정 → 재검증
4. 모든 항목 통과 → 사용자에게 스크린샷 보여주기
5. 사용자 컨펌 → APK 빌드

## 이미지 생성 방법
- Antigravity IDE에서 Match3 프로젝트 열기
- Gemini 채팅으로 이미지 생성 요청
- 생성된 파일을 Assets/Textures/Sprites/에 배치
- rembg로 누끼 처리 (필요 시)
