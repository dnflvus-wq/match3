# Phase 7: 레벨 에디터 도구

> **선행 조건**: Phase 4 (Model) + Phase 13 (장애물) 완료 후.
> 코드 없이 레벨을 대량 생산할 수 있는 Unity 에디터 도구.
> NotebookLM 권장: Custom Inspector 시각 에디터 + CSV 하이브리드.

## 목표
기획자가 코드를 열지 않고 Inspector에서 마우스 클릭만으로 레벨을 제작

---

## 7-1. Custom Inspector 그리드 에디터

### Step 1 (코드): LevelConfigEditor.cs 확장

**파일 위치**: `Assets/Scripts/Editor/LevelConfigEditor.cs` (기존 파일 확장)

**기능 추가:**
```
1. 2D 그리드 시각 표시
   - boardLayout[] 배열을 width×height 그리드로 Inspector에 렌더링
   - 각 셀은 30x30px 버튼으로 표시
   - 셀 색상: Normal=연두, Empty=검정, Stone=회색

2. 셀 타입 편집
   - 좌클릭: CellType 순환 (Normal → Empty → Stone → Normal)
   - Shift+좌클릭: ObstacleType 순환 (None → Ice → Chain → Jelly → TimerBomb → None)
   - Ctrl+좌클릭: HP 1 증가 (최대 maxHP)
   - 우클릭: 초기화 (Normal, None, HP=0)

3. 팔레트 UI (그리드 상단)
   - 현재 선택된 브러시 표시
   - 버튼: [Normal] [Empty] [Stone] | [Ice] [Chain] [Jelly] [TimerBomb] [Portal]
   - 브러시 선택 후 그리드 클릭 → 해당 타입 배치

4. 레벨 메타데이터 영역
   - 보드 크기 (width/height) 슬라이더 → 변경 시 그리드 자동 리사이즈
   - 이동수/시간/목표점수 입력
   - levelType 드롭다운 (Moves/Time/Obstacle)
   - 색상 가중치 슬라이더 (기존 기능)

5. 테스트 버튼
   - [▶ Play Test] 버튼: 현재 LevelConfig로 즉시 Play 모드 진입
   - [📋 Copy JSON] 버튼: 레벨 데이터를 JSON으로 클립보드 복사
```

**Inspector 레이아웃:**
```
┌─────────────────────────────────────────┐
│ Level Config: Level_042                  │
├─────────────────────────────────────────┤
│ Board: [6] x [6]   Type: [Moves ▼]     │
│ Moves: [25]  Target: [15000]            │
├─────────────────────────────────────────┤
│ Brush: [Normal] [Empty] [Stone]         │
│        [Ice] [Chain] [Jelly] [Bomb]     │
├─────────────────────────────────────────┤
│  ┌──┬──┬──┬──┬──┬──┐                   │
│  │🟢│🟢│⬛│🟢│🟢│🟢│  (그리드)         │
│  ├──┼──┼──┼──┼──┼──┤                   │
│  │🟢│🧊│🟢│🟢│⛓│🟢│  🧊=얼음 ⛓=사슬  │
│  ├──┼──┼──┼──┼──┼──┤                   │
│  │🟢│🟢│🟢│⬜│🟢│🟢│  ⬜=금속 ⬛=빈칸  │
│  ├──┼──┼──┼──┼──┼──┤                   │
│  │🟢│🟢│🟢│🟢│🟢│🟢│                   │
│  ├──┼──┼──┼──┼──┼──┤                   │
│  │🟢│🟢│💣│🟢│🟢│🟢│  💣=타이머(5턴)   │
│  ├──┼──┼──┼──┼──┼──┤                   │
│  │🟢│🟢│🟢│🟢│🟢│🟢│                   │
│  └──┴──┴──┴──┴──┴──┘                   │
├─────────────────────────────────────────┤
│ Color Weights: ■Yellow [===] 16%        │
│                ■Purple [====] 20%       │
│                ...                      │
├─────────────────────────────────────────┤
│ [▶ Play Test]  [📋 Copy JSON]          │
└─────────────────────────────────────────┘
```

### Step 2 (에디터): LevelConfig SO 데이터 구조

**LevelConfig.cs 확장 필드:**
```csharp
[Header("Board Layout")]
public int[] boardLayout;        // CellType (0=Normal, 1=Empty, 2=Stone)
public int[] obstacleLayout;     // ObstacleType (0=None, 1=Ice, 2=Chain, ...)
public int[] obstacleHPLayout;   // 각 셀 장애물 HP

[Header("Mission")]
public MissionType missionType;  // Score, CollectTiles, DestroyObstacles
public MissionTarget[] targets;  // [{type=Ice, count=12}, {type=Apple, count=15}]
```

---

## 7-2. CSV/Excel → SO 파이프라인

### Step 1 (코드): LevelImporter.cs (Editor 스크립트)

**파일 위치**: `Assets/Scripts/Editor/LevelImporter.cs`

**CSV 형식:**
```csv
level_id,width,height,moves,target_score,num_colors,type,layout,obstacles
42,6,6,25,15000,6,Moves,"0,0,1,0,0,0,0,0,0,0,0,0,...","0,1,0,0,2,0,..."
43,7,7,30,20000,7,Obstacle,"...","..."
```

**기능:**
```
MenuItem("Match3/Import Levels from CSV")
1. FilePanel으로 CSV 파일 선택
2. 각 행 파싱 → LevelConfig SO 자동 생성
3. Assets/ScriptableObjects/Levels/ 폴더에 Level{id}.asset 저장
4. 기존 에셋 있으면 덮어쓰기 확인 다이얼로그
```

### 역할 분담
- **AI(MCP)**: LevelImporter.cs 스크립트 작성 + 컴파일
- **사람**: CSV 파일 작성, Import 실행, 결과 확인

---

## 7-3. AI 레벨 밸런싱 (향후)

### 헤드리스 자동 플레이
```
BoardModel이 순수 C#이므로 Unity 없이도 실행 가능
→ 1000번 자동 시뮬레이션 → 클리어율/평균 이동수 통계
→ 목표 승률에 맞게 이동수/장애물 자동 조정
```

**구현 시기**: Phase 4 (Model 분리) 완료 후 가능

---

## 검증 항목
- [ ] Custom Inspector에서 그리드 시각적 편집 가능 (셀 클릭으로 타입 변경)
- [ ] 팔레트 브러시로 장애물 배치 가능
- [ ] 보드 크기 변경 시 그리드 자동 리사이즈
- [ ] [▶ Play Test] 버튼으로 즉시 테스트 가능
- [ ] CSV Import → LevelConfig SO 자동 생성
- [ ] 생성된 레벨이 게임에서 정상 로드/플레이
- [ ] Undo/Redo 지원 (Undo.RecordObject 사용)

## MCP 도구 호출 순서

```
Step 1: script-update-or-create → LevelConfigEditor.cs 확장 (그리드 UI, 팔레트, Play Test 버튼)
Step 2: script-update-or-create → LevelImporter.cs (CSV 파서)
Step 3: assets-refresh → 컴파일
Step 4: script-execute → 테스트용 LevelConfig SO 에셋 생성 (다양한 보드 크기/장애물 조합)
Step 5: editor-application-set-state → Play 모드에서 생성된 레벨 테스트
Step 6: screenshot-game-view → 확인
```

## 진행률: 0%
