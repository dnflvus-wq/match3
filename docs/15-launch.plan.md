# Phase 15: 출시 준비

## 목표
Google Play (향후 App Store) 출시에 필요한 모든 요소 준비

## 반복 검증 프로세스
1. 구현 완료 → 계획서 대비 갭분석 (99% 일치까지 반복)
2. E2E 테스트 (Playwright MCP, UI 조작)
3. 최종 갭분석 (99% 확인)
4. 모든 단계 통과 시에만 완료 보고

---

## 15-1. 다국어 지원 (Localization)

### 에디터 조작 순서 — Localization 패키지 설치:
1. **`Window > Package Manager`** 열기
2. 좌측 상단 드롭다운 → `Unity Registry` 선택
3. 검색창에 `Localization` 입력
4. **`Localization`** 패키지 선택 → `Install` 클릭

### 에디터 조작 순서 — Localization Settings 초기 설정:
1. **`Edit > Project Settings > Localization`** 열기
2. `Create` 버튼 클릭 → Localization Settings 에셋 생성
3. **Locale 추가:**
   - `Locale Generator` 버튼 클릭
   - `Korean (ko)` 체크 → `Generate` 클릭
   - `English (en)` 체크 → `Generate` 클릭
4. `Specific Locale Selector`에서 기본 로케일 → `Korean` 설정

### 에디터 조작 순서 — String Table 생성:
1. **`Window > Asset Management > Localization Tables`** 열기
2. `New Table Collection` 탭 선택
3. Table Collection Name: `UIStrings` 입력
4. Type: `String Table Collection` 선택
5. 포함할 Locale 체크: `Korean`, `English`
6. `Create` 클릭
7. 생성된 테이블에 키-값 추가:
   - Key: `btn_play` → ko: `시작`, en: `Play`
   - Key: `btn_settings` → ko: `설정`, en: `Settings`
   - Key: `level_clear` → ko: `레벨 클리어!`, en: `Level Clear!`
   - Key: `game_over` → ko: `게임 오버`, en: `Game Over`
   - (모든 UI 텍스트에 대해 반복)

### 에디터 조작 순서 — UI에 Localization 적용:
1. TextMeshPro 오브젝트 선택
2. Inspector에서 `Add Component > Localize String Event` 추가
3. String Reference에서 테이블과 키 선택 (예: `UIStrings > btn_play`)
4. Update String 이벤트에 TextMeshPro.text 연결

### 에디터 조작 순서 — 폰트 설정 (다국어 지원):

> **NLM 피드백**: `.ttf` 파일을 그대로 사용하면 런타임에 래스터라이징되어 품질 저하+성능 낭비. 반드시 **TMP Font Asset Creator**로 SDF 폰트 에셋을 미리 Bake할 것

1. Noto Sans KR / Noto Sans 폰트 파일 (`.ttf` 또는 `.otf`) 임포트
2. **`Window > TextMeshPro > Font Asset Creator`** 열기 (`.ttf` 그대로 사용 금지!)
3. Source Font File에 Noto Sans KR 드래그
4. **Atlas Resolution: 4096×4096** (한국어는 글리프 수가 많으므로 4096 필수)
5. Character Set: `Unicode Range` 선택
   - 한국어: `0xAC00-0xD7AF` (한글 완성형)
   - 기본 라틴: `0x0020-0x007E`
6. Sampling Point Size: `Auto Sizing` 또는 `42`~`64`
7. Packing Method: `Optimum`
8. `Generate Font Atlas` 클릭 → `Save` 클릭
9. 영어용 폰트도 동일하게 생성 (영어는 Atlas 2048×2048로 충분)
10. **Dynamic Fallback 설정**: TMP Settings (`Edit > Project Settings > TextMeshPro`)에서 Fallback Font Assets 리스트에 한국어 폰트 에셋 추가 → 아틀라스에 없는 글리프 자동 렌더링
11. **Locale별 폰트 전환** (선택):
    - `Localize Font Event` 컴포넌트로 로케일별 다른 폰트 에셋 적용

### 코드 구현:
```csharp
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LocalizationManager
{
    public static void SetLocale(string localeCode)
    {
        var locale = LocalizationSettings.AvailableLocales.Locales
            .Find(l => l.Identifier.Code == localeCode);
        if (locale != null)
            LocalizationSettings.SelectedLocale = locale;
    }

    public static async Task<string> GetLocalizedString(string tableName, string key)
    {
        var op = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(tableName, key);
        await op.Task;
        return op.Result;
    }
}
```

## 15-1b. Safe Area 대응 (NLM 피드백)

> 노치/펀치홀/Dynamic Island 등으로 UI가 가려지는 것을 방지하는 앵커 스크립트 필수

```csharp
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaAdapter : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea = Rect.zero;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        Rect safeArea = Screen.safeArea;
        if (safeArea != lastSafeArea)
        {
            ApplySafeArea(safeArea);
            lastSafeArea = safeArea;
        }
    }

    private void ApplySafeArea(Rect safeArea)
    {
        // Safe Area를 앵커 좌표로 변환
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
    }
}
```

- Canvas 바로 아래에 `SafeAreaPanel` 오브젝트를 두고 `SafeAreaAdapter` 컴포넌트 부착
- 모든 UI 요소를 `SafeAreaPanel` 하위에 배치
- 배경 이미지 등 전체 화면이 필요한 요소만 SafeArea 밖에 배치

## 15-2. 접근성 (Accessibility)
- 색맹 모드 (타일에 형태/패턴 추가)
- 터치 영역 확대 옵션
- 텍스트 크기 조절
- 진동 피드백 on/off

```csharp
public class AccessibilitySettings
{
    public bool colorBlindMode;    // 타일에 심볼 오버레이
    public float touchAreaScale;   // 1.0 ~ 1.5
    public float textScale;        // 1.0 ~ 1.5
    public bool vibrationEnabled;

    public void ApplyColorBlindMode(bool enabled)
    {
        // 모든 타일 프리팹에 심볼 스프라이트 표시/숨김
        colorBlindMode = enabled;
        EventBus.Publish(new ColorBlindModeChangedEvent(enabled));
    }
}
```

## 15-3. 앱스토어 등록 (ASO)
- 앱 아이콘 (512×512, 1024×1024)
- 스토어 스크린샷 5~8장 (각 화면 하이라이트)
- 피처 그래픽 (1024×500)
- 프로모션 영상 (30초)
- 앱 설명 (한국어/영어)
- 키워드 최적화
- 연령 등급 심사

## 15-4. 약관/법률
- 개인정보처리방침
- 이용약관
- 결제 관련 고지 (미성년자 결제 제한)
- GDPR/CCPA 대응 (글로벌 출시 시)

## 15-5. 앱 업데이트 체계

```csharp
public class VersionChecker : MonoBehaviour
{
    [System.Serializable]
    public class VersionInfo
    {
        public string minVersion;      // 강제 업데이트 기준
        public string latestVersion;   // 권장 업데이트 기준
        public string storeUrl;
    }

    public IEnumerator CheckVersion()
    {
        var request = UnityWebRequest.Get("https://api.example.com/match3/version");
        yield return request.SendWebRequest();
        var info = JsonUtility.FromJson<VersionInfo>(request.downloadHandler.text);

        string currentVersion = Application.version;
        if (CompareVersions(currentVersion, info.minVersion) < 0)
            ShowForceUpdatePopup(info.storeUrl);
        else if (CompareVersions(currentVersion, info.latestVersion) < 0)
            ShowOptionalUpdatePopup(info.storeUrl);
    }

    private int CompareVersions(string a, string b) { /* 버전 비교 */ }
}
```

- 강제 업데이트 (최소 버전 체크)
- 선택 업데이트 (권장 알림)
- 서버에서 최소 버전 관리

## 검증 항목
- [ ] Localization 패키지 설치 확인 (Package Manager)
- [ ] Locale 생성 확인 (Korean, English)
- [ ] String Table 생성 및 키-값 등록 확인
- [ ] UI TextMeshPro에 Localize String Event 적용 확인
- [ ] 한국어 ↔ 영어 런타임 전환 정상 (모든 UI 텍스트 변경됨)
- [ ] 다국어 폰트 (Noto Sans KR) **SDF Font Asset** Bake 완료 (4096x4096 아틀라스)
- [ ] Dynamic Fallback Font 설정 확인
- [ ] Safe Area 대응 — 노치/펀치홀 기기에서 UI 가림 없음
- [ ] 색맹 모드 타일 구분 가능 (심볼 오버레이)
- [ ] 스토어 에셋 전부 준비 (아이콘, 스크린샷, 피처 그래픽)
- [ ] 개인정보처리방침 URL 등록
- [ ] 강제 업데이트 팝업 표시 + 스토어 이동
- [ ] 선택 업데이트 팝업 닫기 가능

## MCP 도구 호출 순서

```
Step 1: script-update-or-create → LocalizationHelper.cs, SafeAreaHandler.cs
Step 2: assets-refresh → 컴파일
Step 3: gameobject-component-add → Canvas에 SafeAreaHandler 부착
Step 4: gameobject-component-modify → SafeAreaHandler Inspector 설정
Step 5: scene-save → 씬 저장
```

**MCP로 처리:**
- Localization 패키지 설치 → MCP package-add 또는 script-execute로 PackageManager API
- Locale 생성 → script-execute로 LocaleGeneratorWindow API 호출
- String Table → script-execute로 StringTableCollection 생성 + 키-값 등록
- SDF 폰트 Bake → script-execute로 TMP_FontAssetCreator API (또는 win32-inspector로 Font Asset Creator 창 조작)
- Localize String Event → gameobject-component-add로 UI Text에 부착
- 최종 확인만 사용자 검토

## 진행률: 0%
