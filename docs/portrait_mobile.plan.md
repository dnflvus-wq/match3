# Match3 Android Portrait 모드 개발 계획서

## 목표
Unity Match3 게임을 Android Portrait 모드로 에뮬레이터에서 정상 동작하도록 구현

## 구현 항목 체크리스트

### 1. Portrait 모드 고정
- [x] ProjectSettings.asset: `defaultScreenOrientation: 0` (Portrait)
- [x] AutoRotation 비활성화

### 2. HUD 레이아웃 (MobileHUDLayout.cs)
- [x] GameUICanvas에 컴포넌트 부착
- [x] Target 패널 → 화면 상단 왼쪽 1/3
- [x] RemainingMoves/Time 패널 → 화면 상단 중앙 1/3
- [x] Score 패널 → 화면 상단 오른쪽 1/3
- [x] GameOver 패널 → 화면 전체 커버
- [x] StretchAllChildren: Text=stretch/BestFit, Image=중앙고정 크기제한
- [x] "/" 포함 오브젝트명 FindDirectChild로 처리

### 3. 카메라 조정 (MobileCameraSetup.cs)
- [x] 화면 비율에 따라 orthographicSize 자동 계산
- [x] hudHeightRatio로 상단 HUD 영역 확보
- [x] 카메라 Y 위치를 그리드 상단이 HUD 아래에 오도록 계산

### 4. SafeArea 처리 (SafeAreaHandler.cs)
- [x] 노치/상태바 SafeArea 반영

### 5. APK 빌드 (BuildScript.cs)
- [x] IL2CPP + ARM64|x86_64 설정
- [x] batchmode 빌드 성공

### 6. 에뮬레이터 배포 검증
- [x] SCP 전송 → adb install 성공
- [x] Portrait 방향 표시 확인
- [x] HUD 3패널 표시 확인
- [x] 그리드 화면 채움 확인
- [x] 에러 로그 없음 확인

## 검증 기준 (갭분석)

| 항목 | 기준 | 현재 상태 |
|------|------|----------|
| Portrait 모드 | 세로 화면 고정 | ✅ |
| HUD Target 패널 | 상단 좌 1/3에 target 점수 표시 | ✅ |
| HUD Remaining 패널 | 상단 중 1/3에 moves 표시 | ✅ |
| HUD Score 패널 | 상단 우 1/3에 점수 표시 | ✅ |
| 그리드 배치 | HUD 아래에서 화면 채움 | ✅ |
| GameOver 패널 | 전체 화면 커버 | 미검증 |
| 에러 없음 | logcat Error 없음 | ✅ |
| 게임 플레이 | 블록 스와이프/매치 동작 | 미검증 |

## 반복 검증 프로세스
1. 구현 → 빌드 → 배포 → 스크린샷 확인
2. 갭분석 99% 달성까지 반복
3. E2E 테스트 (실제 게임 플레이)
4. 최종 갭분석 99% 확인
