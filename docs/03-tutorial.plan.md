# Phase 3: 튜토리얼 시스템

> **Unity 에디터 작업이 핵심인 Phase.**
> 마스킹/UI 배치/앵커 설정은 반드시 에디터에서 해야 함 (NotebookLM 확인).
> 손가락 애니메이션은 DOTween 사용 (동적 좌표 대응).

## Unity 에디터에서 반드시 해야 하는 작업 (NotebookLM 확인)

### 1. 마스킹 (반투명 오버레이 + 구멍) — 에디터 조작 순서

```
1. Canvas 하위에 UI > Image 생성 → 이름: "TutorialOverlay"
2. TutorialOverlay Inspector:
   - Color: 검정 (0, 0, 0, Alpha=150/255 ≈ 0.59)
   - RectTransform: Anchors Stretch (전체 화면 덮기)
   - Raycast Target: ON (입력 차단)
3. TutorialOverlay 자식으로 UI > Image 생성 → 이름: "Hole"
   - RectTransform: 하이라이트할 타일 크기 (약 120×120px)
   - Sprite: 둥근 사각형 (둥근 구멍 효과)
4. TutorialOverlay에 Mask 컴포넌트 추가
5. ⚠️ 핵심: Material 슬롯에 역마스크(Inverted Mask) 커스텀 매터리얼 할당
   - Stencil 버퍼로 구멍을 뚫는 매터리얼 필요
   - Assets > Create > Material > "InvertedMaskMat"
   - Shader: UI/Default → Stencil Comp: NotEqual, Stencil Ref: 1
```

### 2. UI 앵커/피벗 — 에디터에서 직접

```
- 튜토리얼 텍스트 창: Canvas 하단 중앙 앵커 (노치 대응)
- 손가락 이미지: Canvas에 배치, 코드에서 위치만 동적 변경
- 각 요소의 Raycast Target ON/OFF 설정
```

### 3. Prefab 할당 — Inspector에서 드래그앤드롭

```
TutorialManager Inspector:
- tutorialOverlay: TutorialOverlay Prefab
- fingerPointer: FingerPointer Prefab (손가락 UI Image)
- hintBubble: HintBubble Prefab (말풍선)
- successSound: AudioClip (칭찬 사운드)
- successParticle: Particle Prefab (폭죽)
- stepConfigs[]: TutorialStepConfig SO 3개 할당
```

## 코드 작업 (간결)

- `TutorialManager.cs`: FSM 연동, 단계 진행, 입력 필터링
- `TutorialStepConfig.cs` (SO): 고정 보드 배열, 허용 입력 좌표, 힌트 텍스트
- 손가락 애니메이션: `DOTween.DOMove(from→to, 0.8s, Ease.OutQuad).SetLoops(-1)`
- 구멍 위치 이동: `Hole.RectTransform.position = Camera.main.WorldToScreenPoint(targetTile.position)`

## 3단계 구성

| Step | 내용 | 에디터 작업 | 코드 작업 |
|------|------|-----------|----------|
| 1: 기본 스와이프 | 매치 가능한 곳 1개, 마스킹+손가락 | SO에 고정 보드 배열 입력, Hole 크기/위치 | 입력 필터, 성공 콜백 |
| 2: 특수 타일 | 4매치 유도, 스트라이프 생성 체험 | SO에 4매치 배열 입력, 힌트 텍스트 | 특수 타일 생성 감지 |
| 3: 부스터 | 교착 상태, 망치 사용 유도 | 부스터 UI 강조 애니메이션 | 부스터 사용 감지 |

## 검증 항목
- [ ] 마스킹: 타겟 셀만 밝고 나머지 어두움 (역마스크 정상)
- [ ] 손가락 DOTween 반복 애니메이션 자연스러움
- [ ] 지정 외 입력 완전 차단 (Raycast Target)
- [ ] Step 1→2→3 순차 진행
- [ ] 튜토리얼 완료 플래그 저장 (재실행 방지)

## MCP 도구 호출 순서 (NLM 검증 완료)

```
Step 1: script-update-or-create
  → TutorialStepConfig.cs (SO), TutorialManager.cs 코드 작성

Step 2: assets-refresh → 컴파일

Step 3: script-execute
  → ScriptableObject.CreateInstance + AssetDatabase.CreateAsset으로
    TutorialStepConfig SO 에셋 3개 (Step1, Step2, Step3) 생성
  → 각 SO에 고정 보드 배열, 하이라이트 좌표, 힌트 텍스트 설정

Step 4: script-execute
  → Canvas 하위에 마스킹 UI 구조 생성 (TutorialOverlay + Hole)
  → RectTransform 앵커 설정, Stencil 역마스크 매터리얼 할당
  → 프리팹으로 저장

Step 5: gameobject-component-add
  → 씬의 매니저 오브젝트에 TutorialManager 컴포넌트 부착

Step 6: gameobject-component-modify
  → TutorialManager Inspector에 SO 3개, 마스킹 UI 프리팹, 손가락 프리팹 할당

Step 7: scene-save → 씬 저장

Step 8: editor-application-set-state → Play 모드 테스트 + screenshot-game-view
```

**MCP로 처리:**
- 역마스크 매터리얼 Stencil 설정 → script-execute로 Material 생성 + Stencil 값 설정
- UI 앵커/피벗 → script-execute로 RectTransform 앵커 코드 설정
- 최종 확인만 사용자가 screenshot-game-view로 검토

## 진행률: 0%
