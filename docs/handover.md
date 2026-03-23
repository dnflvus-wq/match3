# Match3 게임 고도화 - 인수인계 문서

## ⚠️ 현재 상태 (2026-03-23)

### 이번 세션에서 한 것
1. **판타지 테마 배경 적용** — Antigravity+Gemini로 생성, bg2.png 교체 완료
2. **레벨 버튼 통일** — LevelSelect.cs에서 1~5 전부 level_btn.png로 동적 생성
3. **Unity MCP 연결 성공** — stdio 방식 (.mcp.json), npx unity-mcp-cli로 도구 호출
4. **16개 Phase 계획서 수립** — NotebookLM + Playwright로 리서치, docs/00-master-plan.md
5. **이미지 에셋 생성** — 타일 8색, 특수타일 4종, 장애물 4종, 앱 아이콘 (asset-tracker.md 참조)
6. **Phase 1 에셋 생성 (부실)** — Prefab/SO/Atlas 파일은 만들었으나 내용 미흡

### ⛔ 중요: Phase 1이 제대로 안 됨
Phase 1 (에디터 에셋 기반 구축)의 파일은 존재하지만 **계획서 요구 수준에 미달**:
- 타일 Prefab: Base + Variant 구조가 아닌 개별 Prefab
- TileView.cs: 코드만 있고 Prefab에 미부착
- 파티클 Prefab: 코드로 생성, 에디터에서 시각적 설정 안 함
- UI Prefab: 실제 이미지 미할당, 빈 껍데기
- Custom Editor: 미작성
- Sprite Atlas: Pack Preview 미확인

### ⛔ Phase 2-3 롤백됨
- 이전에 계획서 순서를 무시하고 Phase 2(Model), Phase 3(Controller)을 먼저 진행했다가 **전부 롤백**함
- GameGrid.cs는 원래 1393줄 상태로 복원됨
- **반드시 Phase 1을 제대로 완료한 후 Phase 2로 넘어갈 것**

---

## 계획서 체계
- **마스터 계획서**: `docs/00-master-plan.md` (16개 Phase 전체 현황)
- **Phase 1**: `docs/01-architecture.plan.md` (아키텍처 리팩토링)
- **Phase 2~16**: 각각 `docs/02~16-*.plan.md`
- **에셋 트래커**: `docs/asset-tracker.md` (이미지 생성 현황)

## 절대 규칙 (MEMORY.md에도 있음)
1. **계획서(.plan.md)를 반드시 Read로 읽고 그 순서대로 실행**
2. **계획서 변경 시 사용자 보고+승인 필수**
3. **작업 덩어리 완료 즉시 계획서+마스터+인수인계 업데이트**
4. **빌드는 사용자 승인 후에만**
5. **Unity 에디터 기능 최대 활용** (코드 생성 금지, Prefab/Inspector 사용)

## 다음 작업
1. **Phase 1을 제대로 다시 해야 함** — 01-architecture.plan.md의 Phase 1 항목 하나씩 확인
   - Base Tile Prefab + Variant 8개 (에디터에서)
   - TileView.cs를 Prefab에 부착
   - 파티클을 에디터 Particle System으로 설정
   - UI Prefab에 실제 이미지 할당
   - Custom Editor 작성
2. Phase 1 완료 후 Phase 2 (Model 분리) 진행

## 프로젝트 위치
- **Unity 프로젝트**: `C:/Project/Match3/`
- **Unity 버전**: 6.3 LTS (6000.3.11f1)
- **APK 최신**: Match3_25 (`G:\내 드라이브\apk\`)
- **다음 APK 번호**: Match3_26

## MCP 설정
- `.mcp.json`: mcp-unity (stdio) + win32-inspector
- Unity MCP: `npx unity-mcp-cli run-tool <도구> --input-file -`
- NotebookLM MCP: 반복 응답 문제 있음 → Playwright MCP로 직접 접근 가능

## NotebookLM 노트북
- match3-1: `eb5804c6` — 게임 아키텍처/알고리즘
- match3-2: `9621b87a` — 추가 리서치
- match3 (그래픽): `b0886bfe` — 디자인/그래픽
- unity-android-mcp: `40bb5215` — MCP 설정

## Antigravity
- 실행: `env -u ELECTRON_RUN_AS_NODE "C:/Users/comes/AppData/Local/Programs/Antigravity/Antigravity.exe"`
- ELECTRON_RUN_AS_NODE=1 환경변수 해제 필수
- Gemini Pro 일일 쿼터 제한 있음
- 가이드: `C:/Users/comes/.claude/projects/c--Project/memory/guide_antigravity.md`
