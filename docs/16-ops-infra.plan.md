# Phase 16: 운영 인프라

## 목표
출시 후 안정적 운영을 위한 모니터링/분석/알림 인프라

## 반복 검증 프로세스
1. 구현 완료 → 계획서 대비 갭분석 (99% 일치까지 반복)
2. E2E 테스트 (Playwright MCP, UI 조작)
3. 최종 갭분석 (99% 확인)
4. 모든 단계 통과 시에만 완료 보고

---

## Firebase SDK 설치

### 에디터 조작 순서 — SDK 임포트:
1. [Firebase Unity SDK](https://firebase.google.com/download/unity) 다운로드 (`.zip`)
2. 압축 해제 → 필요한 `.unitypackage` 파일 확인:
   - `FirebaseAnalytics.unitypackage`
   - `FirebaseCrashlytics.unitypackage`
   - `FirebaseMessaging.unitypackage` (FCM)
   - `FirebaseRemoteConfig.unitypackage`
3. **`Assets > Import Package > Custom Package`** 에서 각 패키지 임포트
4. 임포트 시 **External Dependency Manager (EDM)** 가 자동 포함됨
   - EDM이 Android/iOS 네이티브 의존성 자동 해결
5. 임포트 완료 후 **`Assets > External Dependency Manager > Android Resolver > Resolve`** 실행
   - Gradle 의존성 자동 다운로드

### 에디터 조작 순서 — google-services.json 설정:
1. [Firebase Console](https://console.firebase.google.com/) 접속
2. 프로젝트 생성 또는 기존 프로젝트 선택
3. Android 앱 추가:
   - Package Name: `com.comes.match3` 입력
   - SHA-1 인증서 등록 (Google Play 서명 키)
4. **`google-services.json`** 다운로드
5. 다운로드한 파일을 **Unity 프로젝트의 `Assets/` 폴더 루트**에 배치
6. Inspector에서 파일 선택 → 플랫폼이 `Android`로 설정되었는지 확인
7. **(iOS 출시 시 필수 — NLM 피드백)** `GoogleService-Info.plist`도 Firebase Console에서 다운로드 → **`Assets/` 폴더에 배치**
   - iOS 앱을 추가하지 않으면 Firebase 초기화 실패
   - Bundle ID가 Xcode 프로젝트와 일치하는지 반드시 확인

### 에디터 확인:
1. `Assets > External Dependency Manager > Android Resolver > Settings` 에서:
   - `Enable Auto-Resolution` 체크 확인
   - `Patch mainTemplate.gradle` 체크 확인
2. `File > Build Settings > Player Settings > Publishing Settings`:
   - `Custom Main Gradle Template` 체크 (mainTemplate.gradle 생성)

---

## Firebase 초기화 (NLM 피드백)

> **필수**: 게임 첫 실행 시 `FirebaseApp.CheckAndFixDependenciesAsync()` 호출이 반드시 먼저 수행되어야 함. Analytics/Crashlytics/Messaging 등 모든 Firebase 서비스보다 선행 필수

```csharp
// GameManager.cs 또는 별도 FirebaseInitializer.cs — 게임 최초 진입점
public class FirebaseInitializer : MonoBehaviour
{
    public static bool IsReady { get; private set; } = false;

    void Awake()
    {
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == Firebase.DependencyStatus.Available)
            {
                IsReady = true;
                Debug.Log("Firebase 초기화 성공");
                // 이후 Analytics, Crashlytics 등 초기화
                AnalyticsManager.Initialize();
                CrashReporter.Initialize();
            }
            else
            {
                Debug.LogError($"Firebase 초기화 실패: {task.Result}");
            }
        });
    }
}
```

- `CheckAndFixDependenciesAsync()`는 Google Play Services 의존성을 확인하고 필요 시 자동 수정
- 이 호출 없이 Firebase API를 사용하면 크래시 발생

---

## 16-1. Analytics (사용자 행동 분석)

### 코드 구현:
```csharp
using Firebase.Analytics;

public class AnalyticsManager
{
    public static void Initialize()
    {
        // FirebaseInitializer에서 CheckAndFixDependenciesAsync 완료 후 호출됨
        Debug.Log("Analytics initialized");
    }

    // 추적 이벤트들
    public static void LogLevelStart(int level)
    {
        FirebaseAnalytics.LogEvent("level_start",
            new Parameter("level_id", level));
    }

    public static void LogLevelComplete(int level, int score, int stars, int movesLeft)
    {
        FirebaseAnalytics.LogEvent("level_complete",
            new Parameter("level_id", level),
            new Parameter("score", score),
            new Parameter("stars", stars),
            new Parameter("moves_left", movesLeft));
    }

    public static void LogLevelFail(int level, int movesUsed)
    {
        FirebaseAnalytics.LogEvent("level_fail",
            new Parameter("level_id", level),
            new Parameter("moves_used", movesUsed));
    }

    public static void LogBoosterUse(string boosterType)
    {
        FirebaseAnalytics.LogEvent("booster_use",
            new Parameter("booster_type", boosterType));
    }

    public static void LogIAPPurchase(string productId, double price, string currency)
    {
        FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventPurchase,
            new Parameter(FirebaseAnalytics.ParameterItemId, productId),
            new Parameter(FirebaseAnalytics.ParameterValue, price),
            new Parameter(FirebaseAnalytics.ParameterCurrency, currency));
    }

    public static void LogAdWatched(string placement)
    {
        FirebaseAnalytics.LogEvent("ad_watched",
            new Parameter("placement", placement));
    }
}
```

- 추적 이벤트: 레벨 시작/클리어/실패, 부스터 사용, IAP 결제, 세션 길이, DAU/MAU, 이탈 구간, 광고 시청률

## 16-2. 크래시 리포팅

### 코드 구현:
```csharp
using Firebase.Crashlytics;

public class CrashReporter
{
    public static void Initialize()
    {
        Crashlytics.ReportUncaughtExceptionsAsFatal = true;
        // 사용자 식별 (선택)
        Crashlytics.SetUserId("user_unique_id");
    }

    public static void LogException(System.Exception ex)
    {
        Crashlytics.LogException(ex);
    }

    public static void Log(string message)
    {
        Crashlytics.Log(message);
    }

    public static void SetCustomKey(string key, string value)
    {
        Crashlytics.SetCustomKey(key, value);
    }
}
```

- Firebase Crashlytics
- 자동 크래시 로그 수집
- 사용자 환경(기기, OS) 자동 태깅
- 심각도별 알림

## 16-3. 푸시 알림

### 코드 구현:
```csharp
using Firebase.Messaging;

public class PushManager : MonoBehaviour
{
    void Start()
    {
        FirebaseMessaging.TokenReceived += OnTokenReceived;
        FirebaseMessaging.MessageReceived += OnMessageReceived;
    }

    private void OnTokenReceived(object sender, TokenReceivedEventArgs e)
    {
        // FCM 토큰을 서버에 전송하여 등록
        StartCoroutine(RegisterTokenToServer(e.Token));
    }

    private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        // 앱 포그라운드 상태에서 수신한 메시지 처리
        string title = e.Message.Notification?.Title;
        string body = e.Message.Notification?.Body;
        ShowInAppNotification(title, body);
    }

    // 로컬 알림 (생명 회복 등 시간 기반)
    public void ScheduleLocalNotification(string title, string body, int delaySeconds)
    {
        // Unity Mobile Notifications 패키지 사용
    }
}
```

- Firebase Cloud Messaging (FCM)
- 알림 종류: 생명 회복 완료, 일일 보상 미수령, 이벤트 시작/종료 임박, 친구가 하트 보냄
- 사용자 세그먼트별 타겟 푸시

## 16-4. A/B 테스트 (향후)

### 코드 구현:
```csharp
using Firebase.RemoteConfig;

public class RemoteConfigManager
{
    public static async Task FetchAndActivate()
    {
        var defaults = new Dictionary<string, object>
        {
            { "level_difficulty_multiplier", 1.0 },
            { "iap_discount_percent", 0 },
            { "ad_frequency_minutes", 5 },
            { "tutorial_version", "A" }
        };
        await FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults);
        await FirebaseRemoteConfig.DefaultInstance.FetchAndActivateAsync();
    }

    public static double GetDouble(string key)
    {
        return FirebaseRemoteConfig.DefaultInstance.GetValue(key).DoubleValue;
    }

    public static string GetString(string key)
    {
        return FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue;
    }
}
```

- Firebase Remote Config
- 테스트 대상: 난이도, IAP 가격, 광고 빈도, UI 레이아웃

### Remote Config → BoardModel 데이터 바인딩 (NLM 최종 검증 피드백)

> **누락됐던 핵심**: Remote Config로 값을 가져오기만 하고, 게임에 적용하는 과정이 빠져 있었음

```csharp
// GameManager 또는 LevelLoader 초기화 시
async void ApplyRemoteConfig(LevelConfig levelConfig)
{
    await RemoteConfigManager.FetchAndActivate();

    // Remote Config 값으로 LevelConfig SO 값 덮어쓰기
    int remoteMoves = (int)RemoteConfigManager.GetDouble("level_" + levelConfig.name + "_moves");
    if (remoteMoves > 0)
        levelConfig.moveLimit = remoteMoves;

    float remoteSpawnBias = (float)RemoteConfigManager.GetDouble("spawn_bias_override");
    if (remoteSpawnBias >= 0)
        levelConfig.spawnBias = remoteSpawnBias;
}
```

- Remote Config 키: `level_Level01_moves`, `spawn_bias_override` 등
- Firebase Console에서 값 변경 → 앱 재시작 시 반영 (FetchAndActivate)
- 유저 그룹별 다른 설정 적용 → 지표 비교

## 16-5. 서버 모니터링
- Spring Boot Actuator + Prometheus + Grafana (기존 인프라 활용)
- API 응답 시간, 에러율 모니터링

## 검증 항목
- [ ] Firebase SDK `.unitypackage` 임포트 완료 (Analytics, Crashlytics, Messaging, RemoteConfig)
- [ ] External Dependency Manager 설치 확인 및 Android Resolve 실행 완료
- [ ] `google-services.json` 파일이 `Assets/` 루트에 존재
- [ ] `google-services.json` Inspector에서 Android 플랫폼 설정 확인
- [ ] (iOS) `GoogleService-Info.plist` 파일이 `Assets/` 루트에 존재
- [ ] `FirebaseApp.CheckAndFixDependenciesAsync()` 가 게임 첫 실행 시 호출되는지 확인
- [ ] `FirebaseApp.CheckAndFixDependenciesAsync()` 성공 (DependencyStatus.Available)
- [ ] Analytics 이벤트 발송 → Firebase Console 실시간 대시보드에서 수신 확인
- [ ] Crashlytics 테스트 크래시 → Firebase Console 크래시 리포트 확인
- [ ] FCM 토큰 수신 + 서버 등록
- [ ] 푸시 알림 발송 → 기기 수신 확인
- [ ] Remote Config 값 변경 → 앱에서 반영 확인
- [ ] Custom Main Gradle Template 활성화 확인

## MCP 도구 호출 순서

```
Step 1: script-update-or-create → FirebaseInitializer.cs, AnalyticsManager.cs, RemoteConfigManager.cs
Step 2: assets-refresh → 컴파일
Step 3: gameobject-component-add → 씬에 FirebaseInitializer, AnalyticsManager 부착
Step 4: gameobject-component-modify → Inspector 설정
Step 5: editor-application-set-state → Play 모드 테스트
Step 6: console-get-logs → Firebase 초기화 로그 확인
```

**MCP로 처리:**
- Firebase SDK 임포트 → script-execute로 AssetDatabase.ImportPackage() 호출
- google-services.json 배치 → Bash로 파일 복사 (cp)
- Android Resolver → script-execute로 PlayServicesResolver.Resolve() 호출
- Gradle Template → script-execute로 PlayerSettings 설정
- Firebase Console 앱 등록 → 사용자가 웹에서 (유일하게 Unity 외부 작업)
- 최종 확인만 사용자 검토

## 진행률: 0%
