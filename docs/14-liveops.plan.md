# Phase 14: 라이브 이벤트 (LiveOps)

## 목표
앱 업데이트 없이 서버에서 콘텐츠를 내려보내는 라이브 운영 체계 구축

## 반복 검증 프로세스
1. 구현 완료 → 계획서 대비 갭분석 (99% 일치까지 반복)
2. E2E 테스트 (Playwright MCP, UI 조작)
3. 최종 갭분석 (99% 확인)
4. 모든 단계 통과 시에만 완료 보고

---

## Addressables 패키지 설정

### 에디터 조작 순서 — 패키지 설치:
1. **`Window > Package Manager`** 열기
2. 좌측 상단 드롭다운 → `Unity Registry` 선택
3. 검색창에 `Addressables` 입력
4. **`Addressables`** 패키지 선택 → `Install` 클릭

### 에디터 조작 순서 — 에셋을 Addressable로 지정:
1. Project 창에서 Addressable로 만들 에셋 선택 (스프라이트, 프리팹, 오디오 등)
2. **Inspector 창 상단**에 **`Addressable` 체크박스** 가 표시됨
3. **체크박스를 활성화** → 자동으로 Addressable Name이 에셋 경로로 설정됨
4. Addressable Name을 원하는 이름으로 변경 (예: `event_halloween_bg`)
5. 이벤트 관련 에셋마다 위 과정 반복

### 에디터 조작 순서 — 에셋 그룹 설정:
1. **`Window > Asset Management > Addressables > Groups`** 열기
2. `Create > Group > Packed Assets` 로 새 그룹 생성
3. 그룹 이름 설정:
   - `EventAssets_Halloween` — 할로윈 시즌 에셋
   - `EventAssets_Christmas` — 크리스마스 시즌 에셋
   - `EventAssets_Common` — 공통 이벤트 에셋
4. 각 에셋을 해당 그룹으로 드래그하여 이동
5. 그룹 선택 → Inspector에서 **Build & Load Paths** 설정:
   - Build Path: `RemoteBuildPath` 선택
   - Load Path: `RemoteLoadPath` 선택
6. **`Window > Asset Management > Addressables > Profiles`** 에서 Remote URL 설정:
   - `RemoteLoadPath`: `https://cdn.example.com/match3/[BuildTarget]` 형태

### 에디터 조작 순서 — Addressables 빌드:
1. Addressables Groups 창에서 `Build > New Build > Default Build Script` 클릭
2. 빌드 결과물 (`.bundle` 파일)을 CDN에 업로드

---

## 이벤트 종류

### 14-1. 주간 이벤트
- 제한 시간 챌린지 (72시간 내 특정 목표 달성)
- 특별 보상 (한정 타일 스킨, 대량 코인)
- 서버에서 이벤트 데이터(기간, 보상, 규칙) JSON으로 전달

### 14-2. 시즌 이벤트
- 할로윈, 크리스마스, 봄 등 테마별 이벤트
- 테마 타일 스킨 + 테마 배경 (Addressables로 다운로드)
- 시즌 전용 레벨 팩

### 14-3. 경쟁 이벤트
- 주간 토너먼트 (점수 경쟁, 리더보드)
- 팀 대항전
- 보상 티어 (상위 10%/30%/50%)

### 14-4. 보스 스테이지
- 10레벨마다 보스 레벨 (특수 규칙, 강화 장애물)
- 보스 클리어 시 대형 보상 + 스토리 진행

## 구현 코드

### EventManager:
```csharp
public class EventManager : MonoBehaviour
{
    [System.Serializable]
    public class EventData
    {
        public string eventId;
        public string eventType; // "weekly", "seasonal", "tournament"
        public string title;
        public long startTimestamp; // Unix timestamp (서버 시간)
        public long endTimestamp;
        public string addressableGroupKey; // Addressable 에셋 그룹 키
        public List<EventReward> rewards;
        public string ruleJson; // 이벤트별 커스텀 규칙
    }

    public IEnumerator FetchActiveEvents()
    {
        var request = UnityWebRequest.Get("https://api.example.com/match3/events/active");
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            var events = JsonUtility.FromJson<EventListWrapper>(request.downloadHandler.text);
            foreach (var evt in events.items)
            {
                if (IsEventActive(evt))
                    StartCoroutine(LoadEventAssets(evt));
            }
        }
    }

    private bool IsEventActive(EventData evt)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return now >= evt.startTimestamp && now <= evt.endTimestamp;
    }
}
```

### Addressable 에셋 로드:
```csharp
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class EventAssetLoader
{
    public IEnumerator LoadEventAssets(string addressableKey)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(addressableKey);
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            // 에셋 사용 (스킨 교체, 배경 교체 등)
            GameObject loadedAsset = handle.Result;
        }
        else
        {
            Debug.LogError($"Failed to load addressable: {addressableKey}");
        }
    }

    public void ReleaseAsset(AsyncOperationHandle handle)
    {
        Addressables.Release(handle); // 메모리 해제
    }
}
```

## 클라이언트 런타임 다운로드 로직 (NLM 피드백)

앱 실행 시 새로운 이벤트 에셋이 있는지 확인하고, 프로그레스 바와 함께 다운로드하는 흐름:

```csharp
public class AddressableUpdateChecker : MonoBehaviour
{
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private string nextSceneName = "Lobby";

    public IEnumerator CheckAndDownloadUpdates()
    {
        statusText.text = "업데이트 확인 중...";

        // 1. 카탈로그 업데이트 확인
        var checkHandle = Addressables.CheckForCatalogUpdates(false);
        yield return checkHandle;

        if (checkHandle.Status == AsyncOperationStatus.Succeeded && checkHandle.Result.Count > 0)
        {
            // 2. 카탈로그 업데이트 적용
            var updateHandle = Addressables.UpdateCatalogs(checkHandle.Result, false);
            yield return updateHandle;

            // 3. 다운로드할 에셋 크기 확인
            var sizeHandle = Addressables.GetDownloadSizeAsync("EventAssets");
            yield return sizeHandle;

            if (sizeHandle.Result > 0)
            {
                statusText.text = $"다운로드 중... ({sizeHandle.Result / 1024 / 1024}MB)";

                // 4. 에셋 다운로드 + 프로그레스 바
                var downloadHandle = Addressables.DownloadDependenciesAsync("EventAssets", false);
                while (!downloadHandle.IsDone)
                {
                    progressBar.value = downloadHandle.PercentComplete;
                    yield return null;
                }
                progressBar.value = 1f;
                Addressables.Release(downloadHandle);
            }

            Addressables.Release(updateHandle);
        }

        Addressables.Release(checkHandle);
        statusText.text = "로딩 완료!";

        // 5. 로비 씬으로 전환
        SceneManager.LoadScene(nextSceneName);
    }

    // NLM 최종 검증 피드백: 네트워크 에러 예외 처리
    void OnDownloadError(string errorMessage)
    {
        progressBar.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(true);
        statusText.text = "네트워크 에러: 다시 시도해주세요";
        retryButton.onClick.AddListener(() => StartCoroutine(CheckAndDownload()));
    }
}
```

- 로딩 씬에서 `CheckForCatalogUpdates()` → 다운로드 → 프로그레스 바 → 다음 씬 전환
- **(NLM 최종 피드백)** 다운로드 중 네트워크 끊김 시 → 프로그레스 바 숨기고 **"네트워크 에러: 재시도"** 버튼 표시
- 다운로드 실패 시 재시도 UI 표시 + 오프라인 모드 폴백

## 구현 방식
- 이벤트 데이터: 서버 JSON → 클라이언트 파싱
- 에셋: Addressables로 원격 다운로드 (CDN)
- 스케줄: 서버 시간 기준 (클라이언트 시간 변조 방지)
- 백오피스: Spring Boot 관리 페이지에서 이벤트 생성/관리

## 검증 항목
- [ ] Addressables 패키지 설치 확인 (Package Manager)
- [ ] 에셋 Inspector에서 Addressable 체크박스 활성화 확인
- [ ] Addressable Groups 창에서 그룹 생성 및 에셋 할당 확인
- [ ] Remote Build/Load Path 설정 확인
- [ ] Addressables 빌드 → `.bundle` 파일 생성 확인
- [ ] 서버에서 이벤트 데이터 JSON 수신 정상
- [ ] 이벤트 기간 내만 활성화 (서버 시간 기준)
- [ ] Addressables로 테마 에셋 다운로드 + 런타임 적용
- [ ] 에셋 메모리 해제 (Addressables.Release) 정상
- [ ] 런타임 카탈로그 업데이트 확인 (`CheckForCatalogUpdates`) 정상
- [ ] 다운로드 프로그레스 바 표시 + 완료 후 씬 전환
- [ ] 리더보드 정상 표시
- [ ] 보스 스테이지 특수 규칙 작동

## MCP 도구 호출 순서

```
Step 1: script-update-or-create → EventManager.cs, EventAssetLoader.cs (Addressables 다운로드)
Step 2: assets-refresh → 컴파일
Step 3: gameobject-component-add → 씬에 EventManager 부착
Step 4: scene-save → 씬 저장
Step 5: editor-application-set-state → Play 모드 테스트
```

**MCP로 처리:**
- Addressables Groups → script-execute로 AddressableAssetSettings API 호출하여 그룹 생성/에셋 할당
- Profiles Remote URL → script-execute로 AddressableAssetProfileSettings 설정
- Build → script-execute로 AddressableAssetSettings.BuildPlayerContent() 호출
- CDN 업로드 → Bash(ssh/scp) 또는 사용자 승인 후 수동
- 최종 확인만 사용자 검토

## 진행률: 0%
