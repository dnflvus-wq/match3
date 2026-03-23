# Phase 10: 성능 최적화

## 목표
모바일(저사양 Android)에서 60fps 안정 유지

## 반복 검증 프로세스
1. 구현 완료 → 계획서 대비 갭분석 (99% 일치까지 반복)
2. E2E 테스트 (Playwright MCP, UI 조작)
3. 최종 갭분석 (99% 확인)
4. 모든 단계 통과 시에만 완료 보고

---

## 최적화 항목

### 10-1. 오브젝트 풀링
- 타일, 파티클, 점수 팝업 전부 풀링
- Instantiate/Destroy 호출 제거
- GC 스파이크 제거

**코드 상세:**
```csharp
public class ObjectPool<T> where T : Component
{
    private Queue<T> pool = new Queue<T>();
    private T prefab;
    private Transform parent;

    public ObjectPool(T prefab, int initialSize, Transform parent)
    {
        this.prefab = prefab;
        this.parent = parent;
        for (int i = 0; i < initialSize; i++)
            AddToPool(CreateInstance());
    }

    public T Get()
    {
        T obj = pool.Count > 0 ? pool.Dequeue() : CreateInstance();
        obj.gameObject.SetActive(true);
        return obj;
    }

    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }

    private T CreateInstance() { ... }
    private void AddToPool(T obj) { ... }
}
```

- `PoolManager` 싱글턴에서 타일/파티클/팝업 풀을 일괄 관리
- 씬 로드 시 Pre-warm (타일 **128~160개**, 파티클 **50~100개**, 팝업 10개)
  - *(NLM 피드백: 컬러밤+컬러밤 조합 시 64개 동시 파괴+64개 동시 스폰이 발생하므로 기존 64/20개로는 풀 부족)*

### 10-2. Sprite Atlas

**Unity 에디터 조작 순서:**
1. Project 창에서 우클릭 → `Create > 2D > Sprite Atlas` 생성
2. TileAtlas: Inspector에서 Objects for Packing에 타일 8색 스프라이트 전부 드래그
3. UIAtlas: 동일 방식으로 UI 아이콘 스프라이트 추가
4. Inspector에서 `Include in Build` 체크 확인
5. **Pack Preview 버튼 클릭**하여 아틀라스 패킹 결과 확인
6. 빌드 설정에서 `Edit > Project Settings > Editor > Sprite Packer > Mode`를 `Always Enabled`로 설정

- 타일 8색 → TileAtlas
- UI 아이콘 → UIAtlas
- 드로우 콜 감소

### 10-3. 텍스처 압축

**Unity 에디터 조작 순서:**
1. 텍스처 에셋 선택 → Inspector에서 Platform별 Override
2. Android 탭: Format 선택
   - **타일/UI 스프라이트**: `ASTC 4x4` ~ `ASTC 5x5` (외곽선 선명도 보장)
   - **배경 이미지**: `ASTC 6x6` (파일 크기 절약)
   - *(NLM 피드백: 타일/UI에 6x6 적용 시 외곽선이 뭉개지는 현상 발생)*
3. Max Size → 512 또는 1024로 제한
4. `Apply` 클릭

- ASTC 형식 우선 적용 (모바일 범용)
- 텍스처 최대 크기 제한

### 10-4. Unity Profiler

**Unity 에디터 조작 순서:**
1. **`Window > Analysis > Profiler`** 메뉴로 Profiler 창 열기
2. CPU Usage 모듈: 프레임당 ms 확인, 16.6ms(60fps) 초과 구간 식별
3. GPU Usage 모듈: 렌더링 병목 확인
4. Memory 모듈: GC Alloc 컬럼에서 매 프레임 할당 0 목표
5. Rendering 모듈: Batches, SetPass Calls, Draw Calls 확인
6. 실기기 프로파일링: Build Settings에서 `Development Build` + `Autoconnect Profiler` 체크 → 빌드 → USB 연결 → Profiler에서 디바이스 선택

### 10-5. VSync 및 Target Frame Rate 설정

**Unity 에디터 조작 순서:**
1. **`Edit > Project Settings > Quality`** 열기
2. 각 Quality Level 선택 → **VSync Count → `Don't Sync`** 설정
3. 모든 Quality Level에 대해 반복

**코드 설정:**
```csharp
// GameManager.Awake() 또는 초기화 시점
void Awake()
{
    QualitySettings.vSyncCount = 0; // 코드에서도 보장

    // NLM 피드백: 저사양/배터리 절약을 위한 30fps 분기 처리
    if (SystemInfo.systemMemorySize < 3000)
        Application.targetFrameRate = 30;
    else
        Application.targetFrameRate = 60;
}
```

- VSync 비활성화 후 `Application.targetFrameRate`로 직접 제어
- 에디터와 코드 양쪽에서 설정하여 확실히 보장
- **저사양 기기(RAM 3GB 미만) 자동 30fps 분기** — 발열/배터리 소모 감소 *(NLM 피드백)*

### 10-6. 배칭
- SRP Batcher 활용
- 동일 Material/Atlas 공유
- Draw Call 최소화

**에디터 확인:**
1. `Edit > Project Settings > Graphics` → SRP Batcher 활성화 확인
2. Frame Debugger (`Window > Analysis > Frame Debugger`)로 배칭 상태 확인

### 10-7. Audio Streaming 설정

**Unity 에디터 조작 순서:**
1. AudioClip 에셋 선택 → Inspector
2. 짧은 효과음 (< 200KB): `Load Type → Decompress On Load`
3. BGM 등 긴 오디오 (> 200KB): **`Load Type → Streaming`** 설정
4. `Compression Format → Vorbis`, Quality 슬라이더 70% 정도
5. `Apply` 클릭

- BGM은 Streaming으로 메모리 절약
- 효과음은 Decompress On Load로 지연 없이 재생

### 10-8. 물리 엔진 미사용
- Rigidbody/Physics 사용 금지
- 타일 이동은 직접 좌표 계산 + 트위닝

## 검증 항목
- [ ] Profiler 창(`Window > Analysis > Profiler`)에서 GC 스파이크 없음 (프레임당 GC Alloc 0B 목표)
- [ ] Frame Debugger에서 Draw Call 50 이하
- [ ] 저사양 기기에서 60fps 안정 (VSync 끈 상태에서 측정)
- [ ] 메모리 사용량 200MB 이하 (Profiler Memory 모듈 확인)
- [ ] `Application.targetFrameRate` = 60 확인 (저사양 기기에서는 30fps 분기 확인)
- [ ] Quality Settings에서 VSync Count = Don't Sync 확인
- [ ] Sprite Atlas Pack Preview 정상 (빈 아틀라스 없음)
- [ ] BGM AudioClip이 Streaming 모드인지 확인
- [ ] ASTC 텍스처 압축 적용 확인

## MCP 도구 호출 순서 (NLM 검증 완료)

```
Step 1: script-update-or-create → ObjectPool<T>.cs, targetFrameRate/vSync 초기화 코드 수정
Step 2: assets-refresh → 컴파일
Step 3: assets-find → 타일/UI 텍스처 에셋 경로 목록 검색
Step 4: script-execute → TextureImporter API로 검색된 텍스처 일괄 ASTC 4x4~6x6 변경 + SaveAndReimport()
Step 5: editor-application-set-state → Play 모드 (Profiler 백그라운드 수집)
Step 6: screenshot-game-view → ASTC 압축 후 타일 외곽선 깨짐 없는지 시각 검증
Step 7: editor-application-set-state → Play 모드 종료
Step 8: scene-save → 저장
```

## 진행률: 0%
