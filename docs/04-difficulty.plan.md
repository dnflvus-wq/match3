# Phase 4: 난이도 곡선 설계

> 대부분 **코드 작업 + LevelConfig SO Inspector 설정**.
> GDD(08)의 난이도 규격 참조. NotebookLM 리서치 완료.

## 핵심 개념 (NotebookLM 확인)

### 사인파 난이도 곡선
쉬운→어려운 레벨 교대 배치. 조절 파라미터:
- **색상 수**: 4개(쉬움)~7개(어려움). 색상 적을수록 매치 확률 급증
- **이동수**: 30~40(쉬움)~12~16(어려움)
- **장애물 수/종류**: 없음(쉬움)~복합 장애물(어려움)
- **보드 크기**: 6×6(쉬움)~8×8(어려움)

### DDA (Dynamic Difficulty Adjustment)
연속 실패 시 타일 스폰 확률 보정:
- 연속 3회 실패 → 인접 타일과 같은 색 스폰 확률 +10%
- 연속 5회 실패 → +20% + 매치 가능 배치 유도
- 클리어 시 → 보정 초기화

### Controlled Chaos (가중치 기반 스폰)
순수 랜덤 대신 보드 상태에 따라 동적 조절:
- 색상별 가중치 (LevelConfig SO의 `colorWeights[]`)
- DDA 보정값 (런타임 동적)

## Unity 에디터 작업

### LevelConfig SO Inspector 확장
**기존 LevelConfigEditor.cs에 추가할 필드:**
```
[Header("Difficulty")]
DifficultyTag difficulty    → 드롭다운 (Easy/Medium/Hard/VeryHard)
float spawnBias             → 슬라이더 (0.0~1.0)
```

**Custom Editor 표시:**
- 난이도 태그 옆에 목표 승률 라벨 ("Easy: 95%+", "Hard: 30~40%")
- spawnBias 슬라이더 옆에 설명 ("0=순수랜덤, 1=최대 보정")

### 각 레벨 SO 설정 (script-execute로 일괄 설정)
- Level01~20: difficulty=Easy, moves=30~40, colors=4~5, spawnBias=0
- Level21~50: difficulty=Medium, moves=22~28, colors=5~6
- Level51+: 사인파 패턴으로 Hard/Easy 교차

## 코드 작업 (간결)
- `DDAManager.cs`: 연속 실패 카운터 + spawnBias 보정
- `DropSimulator.cs` 확장: spawnBias 기반 가중치 스폰

## 검증 항목
- [ ] LevelConfig Inspector에서 difficulty/spawnBias 설정 가능
- [ ] DDA: 연속 3회 실패 시 스폰 보정 확인 (Play 모드)
- [ ] Easy 레벨 승률 90%+ / Hard 레벨 30~40% (AI 시뮬레이션으로 검증 — 향후)

## MCP 도구 호출 순서

```
Step 1: script-update-or-create
  → LevelConfig.cs에 difficulty, spawnBias 필드 추가
  → DDAManager.cs 생성
  → LevelConfigEditor.cs에 난이도 태그 UI 추가

Step 2: assets-refresh → 컴파일

Step 3: script-execute
  → 기존 LevelConfig SO 에셋 5개 로드 → difficulty/spawnBias 값 설정
  → Level01~20: Easy, spawnBias=0 / Level21~50: Medium 등

Step 4: gameobject-component-add
  → GameGrid 오브젝트에 DDAManager 컴포넌트 추가

Step 5: scene-save → 씬 저장

Step 6: editor-application-set-state → Play 모드
  → 연속 3회 실패 시뮬레이션으로 DDA 보정 확인

Step 7: screenshot-game-view → 확인 후 Play 모드 종료
```

## 진행률: 0%
