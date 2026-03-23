# Match3 게임 고도화 - 인수인계 문서

## 현재 상태 (2026-03-23)

### 프로젝트 실제 코드 구조 (변경 안 됨)
```
Assets/Scripts/           ← 폴더 분리 없음 (전부 플랫)
├── GameGrid.cs           ← 1393줄 God Object (터치, 매치, 낙하, 생성 전부)
├── MatchParticles.cs     ← 274줄 코드로 파티클 생성 (Prefab 미사용)
├── MobileHUDLayout.cs    ← 237줄 코드로 HUD UI 생성
├── BoosterUI.cs          ← 코드로 부스터 UI 생성
├── LevelSelect.cs        ← 코드로 레벨 버튼 동적 생성
├── ProceduralAudio.cs    ← 163줄 사인파 코드 생성
├── ColorPiece.cs         ← 타일 색상 매핑 (colorSprites 배열)
├── GamePiece.cs, MovablePiece.cs, ClearablePiece.cs ...
├── TileView.cs           ← Phase 1에서 만듦 (게임에서 아직 안 쓰임)
├── GameConfig.cs, LevelConfig.cs ← SO 정의 (게임에서 아직 안 읽음)
└── Editor/
    ├── LevelConfigEditor.cs  ← Custom Editor (그리드 프리뷰)
    └── Phase1Setup.cs        ← 에셋 셋업 유틸리티
```

### Phase 1에서 만든 에셋 (존재하지만 게임 코드가 아직 안 쓰는 것들)
- `Assets/Prefabs/Tiles/BaseTile.prefab` + 8 Variant
- `Assets/Prefabs/Effects/` 파티클 4종
- `Assets/Prefabs/UI/` UI Prefab 4종
- `Assets/ScriptableObjects/` GameConfig + LevelConfig 5개
- `Assets/SpriteAtlases/` TileAtlas + UIAtlas
- Tiles SortingLayer 추가됨

### Edit 모드 문제 (아직 해결 안 됨)
- 카메라 Clear Flags = Nothing → Edit 모드 Game View 깨짐
- 타일이 런타임에만 동적 생성 → Edit 모드에서 보드 안 보임

---

## 작업 방식 (매우 중요 — 반드시 따를 것)

### 스텝 바이 스텝 진행
1. 01-architecture.plan.md를 **Phase 1-1, 1-2, 1-3...** 으로 분할
2. 각 서브 Phase마다 **별도 상세 계획 파일** 작성 (`docs/01/phase-1-1.md` 등)
3. **한 번에 하나의 서브 Phase만 진행**
4. 완료할 때마다 **진행도를 메모리 + 파일에 즉시 저장**
5. 모든 서브 Phase 완료 → 최종 확인 → 다음 큰 Phase로

### Unity MCP 적극 활용 (필수)
- **모든 에디터 작업은 MCP(`npx unity-mcp-cli run-tool`)로 처리**
- MCP 직접 호출(`mcp__mcp-unity__`) 하면 타임아웃됨 — **반드시 CLI 방식**
- 코드 작성 → `script-update-or-create` → `assets-refresh` → 컴파일 확인
- Inspector 설정 → `gameobject-component-modify` 또는 `script-execute`
- 테스트 → `editor-application-set-state` {isPlaying: true}
- 확인 → `screenshot-game-view` 또는 `win32-inspector capture_screen`

### 클릭/조작이 필요할 때
- **Unity 에디터 UI 클릭**: `win32-inspector`의 `click`, `capture_screen` 사용
- **메뉴 항목 실행**: MCP `execute-menu-item` 사용
- **직접 조작이 안 되는 경우만** 사용자에게 요청 (최소화)

### 테스트 (매 작업 후 필수)
1. `editor-application-set-state` → Play 모드 진입
2. `screenshot-game-view` → 화면 캡처하여 직접 확인
3. `console-get-logs` → 에러 없는지 확인
4. 가능하면 `win32-inspector`로 클릭/조작하여 실제 플레이 테스트
5. **직접 조작이 안 되는 경우만** 사용자에게 "Play 모드에서 스왑 테스트 부탁드립니다" 요청
6. `editor-application-set-state` → Play 모드 종료

### 사용자 개입 최소화
- 사용자가 해야 하는 것: **최종 확인 + 승인만**
- AI가 해야 하는 것: **코드 작성, 에디터 설정, 테스트, 스크린샷 확인 전부**
- "할까요?" 묻지 말고 바로 실행 (CLAUDE.md 규칙)

---

## MCP 사용법

```bash
cd c:/Project/Match3 && npx unity-mcp-cli run-tool <도구명> --input-file - <<'EOF'
{파라미터 JSON}
EOF
```

### 주요 도구
| 도구 | 용도 |
|------|------|
| `script-update-or-create` | C# 스크립트 생성/수정 |
| `script-read` | 스크립트 읽기 |
| `script-execute` | C# 코드 즉시 실행 (class Script { static string Main() }) |
| `assets-refresh` | 컴파일 트리거 |
| `assets-find` | 에셋 검색 |
| `editor-application-get-state` | 에디터 상태 확인 |
| `editor-application-set-state` | Play 모드 시작/종료 |
| `screenshot-game-view` | Game View 스크린샷 |
| `console-get-logs` | 콘솔 에러 확인 |
| `scene-save` | 씬 저장 |

---

## 프로젝트 위치
- **Unity 프로젝트**: `C:/Project/Match3/`
- **Unity 버전**: 6.3 LTS (6000.3.11f1)
- **계획서**: `docs/README.md` (전체 요약), `docs/01-architecture.plan.md` (아키텍처)

## 계획서 문서 구조
- `docs/README.md` — 16개 Phase 한눈에 보기
- `docs/01-architecture.plan.md` — 아키텍처 리팩토링 마스터 계획
- `docs/01/` — Phase 1의 서브 Phase별 상세 계획 (이 폴더를 만들어서 진행)
- `docs/02~16-*.plan.md` — 나머지 Phase (로드맵 수준, 해당 Phase 시작 시 구체화)
