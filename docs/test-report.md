# Match3 기능 테스트 리포트 (2026-03-22)

## 테스트 환경
- 에뮬레이터: Galaxy S10 (1440x3040, Android)
- 웹 확인: https://emulator.comes.co.kr/
- APK: Match3_17.apk

## 테스트 결과

### 시각적 확인 (에뮬레이터 스크린샷)

| # | 항목 | 상태 | 비고 |
|---|---|---|---|
| 1 | 타일 6색 표시 | ✅ 정상 | 빨파초노보핑 구분 확실 |
| 2 | 타일 광택 구슬 스타일 통일 | ✅ 정상 | 7종 전부 동일 스타일 |
| 3 | 게임 배경 (캔디 킹덤) | ✅ 적용됨 | 새 bg.png 정상 표시 |
| 4 | HUD (점수/이동수/타겟) | ✅ 정상 | 나무판 스타일 |
| 5 | 부스터 UI (하단) | ✅ 정상 | 아이콘 3종 + 수량 표시 |
| 6 | 코인 표시 | ✅ 정상 | "Coins: 300" 표시 |
| 7 | 뒤로가기 버튼 (<) | ✅ 정상 | 좌상단 |
| 8 | 화면비 - 스테이지 1 (6x6) | ✅ 개선됨 | 빈 공간 최소화 |
| 9 | 화면비 - 스테이지 2 (9x9) | ✅ 해결됨 | 오른쪽 넘침 없음 |
| 10 | 레벨 선택 화면 | ⚠️ 부분적 | 버튼 3개만 표시 (4~10 없음) |
| 11 | 레벨 선택 배경 | ❌ 미적용 | 아직 바다 테마 (새 캔디 배경 미연결) |

### 코드 레벨 확인 (구현 완료, 동작 테스트 필요)

| # | 항목 | 코드 | 실제 동작 | 비고 |
|---|---|---|---|---|
| 12 | FSM 상태 머신 | ✅ | ⬜ 미확인 | 사용자 이전 테스트에서 "버그 없음" |
| 13 | Ease-Out 스왑 애니메이션 | ✅ | ⬜ 미확인 | MovablePiece에 구현 |
| 14 | Z축 아크 (겹침 방지) | ✅ | ⬜ 미확인 | useArc 파라미터 |
| 15 | 매치 실패 핑퐁 | ✅ | ⬜ 미확인 | SwapPiecesCoroutine |
| 16 | 힌트 펄스 (5초 대기) | ✅ | ⬜ 미확인 | HintPulseCoroutine |
| 17 | 데드 보드 셔플 | ✅ | ⬜ 미확인 | ShuffleBoard |
| 18 | 매치 시 스케일축소+페이드 | ✅ | ⬜ 미확인 | ClearablePiece |
| 19 | 낙하 Squash & Stretch | ✅ | ⬜ 미확인 | MoveDrop |
| 20 | 카메라 쉐이크 (4매치+) | ✅ | ⬜ 미확인 | CameraShake |
| 21 | 콤보 텍스트 (x2, x3...) | ✅ | ⬜ 미확인 | ShowComboText |
| 22 | 점수 팝업 (+100) | ✅ | ⬜ 미확인 | ShowScorePopup |
| 23 | 3-2-1 카운트다운 | ✅ | ⬜ 미확인 | StartSequence |
| 24 | 이동수 경고 (5이하 빨간) | ✅ | ⬜ 미확인 | LevelMoves.PulseText |
| 25 | 효과음 7종 | ✅ | ⬜ 미확인 | ProceduralAudio |
| 26 | 콤보 피치 상승 | ✅ | ⬜ 미확인 | AudioManager.PlayMatch |
| 27 | 파티클 스파크 | ✅ | ⬜ 미확인 | MatchParticles |
| 28 | 스폰 가중치 | ✅ | ⬜ 미확인 | GetWeightedRandomColor |
| 29 | 코인 시스템 | ✅ | ⬜ 미확인 | CoinSystem |
| 30 | 부스터 - 망치 | ✅ | ⬜ 미확인 | BoosterUI.OnHammerClick |
| 31 | 부스터 - 셔플 | ✅ | ⬜ 미확인 | BoosterUI.OnShuffleClick |
| 32 | 부스터 - +5이동 | ✅ | ⬜ 미확인 | BoosterUI.OnExtraMovesClick |
| 33 | 일일 보상 | ✅ | ⬜ 미확인 | CoinSystem.ClaimDailyReward |
| 34 | 하트/라이프 | ✅ | ⬜ 미확인 | LifeSystem (코드만, UI 미연결) |
| 35 | 설정 UI (BGM/SFX) | ✅ | ⬜ 미확인 | SettingsUI (코드만, 버튼 미연결) |
| 36 | 레인보우+레인보우 전체클리어 | ✅ | ⬜ 미확인 | SwapPiecesCoroutine |
| 37 | 게임 클리어 연출 (별 팝) | ✅ | ⬜ 미확인 | GameOver.StarPop |
| 38 | 게임 오버 연출 | ✅ | ⬜ 미확인 | GameOver.ShowLose |
| 39 | 점수 카운트업 | ✅ | ⬜ 미확인 | GameOver.ScoreCountUp |

### 알려진 문제 (수정 필요)

| # | 문제 | 심각도 | 원인 | 해결 방법 |
|---|---|---|---|---|
| P1 | 레벨 선택 배경 미적용 | 중 | 씬의 SpriteRenderer가 기존 바다 배경 참조 | LevelSelect.cs에서 Resources.Load로 교체 |
| P2 | 레벨 4~10 버튼 없음 | 높 | LevelSelect 씬 UI에 3개 버튼만 있음 | 코드에서 동적 생성 필요 |
| P3 | 하트/라이프 UI 미연결 | 중 | LifeSystem 코드는 있지만 UI 없음 | 레벨 선택 화면에 하트 표시 추가 |
| P4 | 설정 버튼 없음 | 중 | SettingsUI 코드는 있지만 호출하는 버튼 없음 | 게임 화면에 설정 기어 아이콘 추가 |
| P5 | 타일 약간 우측 치우침 | 낮 | gridCenterX 미세 오프셋 | 미세 조정 필요 |
| P6 | 레벨 선택 배경 바다→캔디 교체 | 중 | Resources.Load 실패 추정 | Unity 에디터에서 직접 교체 필요 |
| P7 | 레벨 4~10 버튼 미표시 | 높 | LevelSelect 씬에 Canvas 없음 추정 | 씬 에디터에서 Canvas 추가 or 기존 버튼 복제 |

### 테스트 불가 항목 (에뮬레이터 한계)
- adb swipe가 Unity 터치 입력으로 전달 안 됨
- 효과음은 에뮬레이터 noVNC에서 확인 불가
- 실시간 애니메이션(펄스, 파티클)은 스크린샷으로 확인 불가
- **→ 사용자 실제 폰 테스트 필요**
