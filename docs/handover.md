# Match3 게임 고도화 - 인수인계 문서

## ⚠️ 다음 세션 작업 (2026-03-23 업데이트)

### 완료된 작업 (이번 세션)
1. **판타지 테마 전환 완료** — Gemini로 생성한 배경 이미지 적용
   - `Assets/Resources/level_select_bg.png` — 마법의 숲 + 크리스탈 + 오로라 밤하늘
   - `Assets/Textures/UI/bg.png` — 마법의 숲 인게임 배경
   - `Assets/Textures/UI/bg2.png` — LevelSelect 씬 실제 참조 파일 (level_select_bg와 동일)
2. **레벨 버튼 이미지 통일** — LevelSelect.cs 전면 리팩토링
   - 레벨 1~5 전부 코드로 동적 생성, 3열 그리드 레이아웃
   - `level_btn.png` (해금) / `level_btn_locked.png` (잠금) 사용
   - 레벨 1~3 항상 해금, 4~5는 이전 레벨 클리어 필요
3. **파티클 이펙트 고도화** — 4레이어 (스파크+별+글로우+링)
   - 스파크에 트레일 이펙트 추가
   - 파티클 수/속도/크기 전반적 강화
4. **Resources 이미지 meta 수정** — 모든 Resources/*.png.meta: isReadable=1, spriteMode=1
5. **뒤로가기 아이콘** — Sprite 직접 로드 방식으로 개선
6. **힌트 시스템** — 동작 확인 (프레임 밝기 변화 감지)

### 남은 작업
1. **Unity MCP 연결 성공!** — `.mcp.json`에서 `type: "stdio"` + `env` 방식 사용. `npx unity-mcp-cli run-tool` Skill을 통해 HTTP API(`localhost:26785/api/tools/`)로 Unity와 통신. **실제 도구 호출 성공 확인 (editor-application-get-state)**. 설정:
   ```json
   {
     "mcp-unity": {
       "type": "stdio",
       "command": "C:\\Project\\Match3\\Library\\mcp-server\\win-x64\\unity-mcp-server.exe",
       "env": {
         "MCP_PLUGIN_PORT": "26785",
         "MCP_PLUGIN_CLIENT_TRANSPORT": "stdio"
       }
     }
   }
   ```
2. **빌드 + APK 배포 완료** — Match3_25.apk (Google Drive)
3. **bg2.png 재교체** — 숫자 그려진 배경 → 깨끗한 판타지 배경으로 교체 완료
4. **LevelSelect.cs 재수정** — 기존 씬 버튼 1~3 유지 + 4~5만 동적 추가 (사용자 요청)

### 향후 작업
1. **파티클 이펙트** — 사용자가 허접하다고 피드백
2. **인게임 배경** — bg.png도 필요시 교체
3. **힌트 시스템** — 실동작 확인 필요

### 미커밋 변경 파일
- `Assets/Scripts/LevelSelect.cs` — 전면 리팩토링 (버튼 통일)
- `Assets/Scripts/MatchParticles.cs` — 4레이어 파티클
- `Assets/Scripts/MobileHUDLayout.cs` — back_icon Sprite 로드 개선
- `Assets/Resources/level_select_bg.png` — Gemini 판타지 배경
- `Assets/Textures/UI/bg.png` — Gemini 인게임 배경
- `Assets/Textures/UI/bg2.png` — 판타지 배경 (씬 참조용)
- 여러 `.meta` 파일 — isReadable/spriteMode 수정

### 작업 프로세스 (모든 구현에 반드시 적용):
1. 코드 수정/이미지 적용
2. **Unity Play 모드 진입 → 스크린샷으로 직접 확인**
3. 오류 발견 → 즉시 수정
4. **다시 확인 → 문제 없을 때까지 반복**
5. 모든 항목 확인 완료 → 그때서야 빌드

**⛔ 빌드는 모든 것이 완료되었을 때만 한다.**

### 절대 규칙:
- **빌드 전 Unity Play 모드에서 충분히 테스트/수정 반복 필수**
- **코드 수정 → Unity 확인 → 수정 → 확인 루프를 반드시 거칠 것**
- **좌표 추정 금지** — find_text 또는 OCR 사용
- **Play 모드 확인** — 반드시 픽셀 색상으로 확인
- **해상도 변경 금지** — 2160x1080 Portrait 유지

---

## 프로젝트 위치
- **Unity 프로젝트**: `C:/Project/Match3/`
- **Unity 버전**: 6.3 LTS (6000.3.11f1)
- **APK 빌드 경로**: `Build/Match3.apk`
- **APK 배포**: `G:\내 드라이브\apk\Match3_{번호}.apk` (현재 최신: **Match3_25**)
- **다음 APK 번호**: **Match3_26**

## 핵심 스크립트 파일

| 파일 | 역할 |
|---|---|
| `Assets/Scripts/GameGrid.cs` | **메인 게임 로직** — 보드, 매치, 스왑, 힌트, 부스터, 파티클 |
| `Assets/Scripts/LevelSelect.cs` | 레벨 선택 화면 — 5개 버튼 동적 생성, 배경 교체 |
| `Assets/Scripts/MatchParticles.cs` | 파티클 — 4레이어 (스파크+별+글로우+링) |
| `Assets/Scripts/MobileHUDLayout.cs` | HUD 레이아웃 + 뒤로가기 버튼 |
| `Assets/Scripts/BoosterUI.cs` | 부스터 버튼 UI + 구매 팝업 |

## 배경 이미지 구조 (중요!)

| 파일 | 용도 | 참조 위치 |
|---|---|---|
| `Assets/Resources/level_select_bg.png` | 코드에서 Resources.Load | LevelSelect.cs |
| `Assets/Textures/UI/bg2.png` | **LevelSelect 씬 Background 스프라이트** | LevelSelect.unity (guid) |
| `Assets/Textures/UI/bg.png` | **인게임 씬 Background 스프라이트** | Level01~05.unity (guid) |

**주의: 씬의 배경은 guid로 참조하므로 파일 자체를 교체해야 함. Resources.Load로 코드 교체해도 씬 스프라이트가 우선 표시됨.**
