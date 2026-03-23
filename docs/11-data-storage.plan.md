# Phase 11: 데이터 저장 + DB 연동

## 목표
PlayerPrefs → JSON 로컬 저장 → REST API 서버 동기화

## ⚠️ Unity 에디터 작업 없음 (100% 코드)
이 Phase는 Unity 에디터 조작이 전혀 필요 없다. 모든 구현은 C# 스크립트와 백엔드 코드로만 진행한다.

## 반복 검증 프로세스
1. 구현 완료 → 계획서 대비 갭분석 (99% 일치까지 반복)
2. E2E 테스트 (Playwright MCP, UI 조작)
3. 최종 갭분석 (99% 확인)
4. 모든 단계 통과 시에만 완료 보고

---

## 데이터 구조

### UserData (POCO)
```csharp
[Serializable]
public class UserData
{
    public int coins;
    public int lives;
    public string lastLifeRegenTime; // ISO 8601 문자열 (JsonUtility DateTime 미지원)
    public List<LevelStarEntry> levelStars;    // JsonUtility는 Dictionary 미지원
    public List<BoosterEntry> boosterCounts;
    public string lastDailyReward;             // ISO 8601
    public int seasonPassTier;
    public int totalGamesPlayed;
}

[Serializable]
public class LevelStarEntry
{
    public string levelId;  // "Level01"
    public int stars;       // 0~3
}

[Serializable]
public class BoosterEntry
{
    public string boosterId; // "Hammer"
    public int count;
}
```

> **JsonUtility vs Newtonsoft.Json 선택:**
> - `JsonUtility`: Unity 내장, 빠름, GC 적음. 단 Dictionary/DateTime 미지원 → List<Entry>로 우회
> - `Newtonsoft.Json (Json.NET)`: Dictionary/DateTime 직접 지원, 복잡한 구조에 유리. Package Manager에서 `com.unity.nuget.newtonsoft-json` 설치 가능
> - ~~**1차 구현**: JsonUtility로 시작 (성능 우선)~~
> - **NLM 피드백 → Newtonsoft.Json 권장**: JsonUtility는 Dictionary, 다형성(상속), null 처리 등에 치명적 한계가 있어 데이터 구조가 복잡해지면 반드시 문제 발생. 처음부터 Newtonsoft.Json 사용 권장
> - Package Manager에서 `com.unity.nuget.newtonsoft-json` 설치

### DataManager 클래스
```csharp
public class DataManager : MonoBehaviour
{
    private static DataManager instance;
    public static DataManager Instance => instance;

    private UserData userData;
    private string SavePath => Path.Combine(Application.persistentDataPath, "userdata.json");

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        LoadLocal();
    }

    // --- 로컬 저장/로드 ---
    public void SaveLocal()
    {
        string json = JsonUtility.ToJson(userData, prettyPrint: true);
        File.WriteAllText(SavePath, json);
    }

    public void LoadLocal()
    {
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            userData = JsonUtility.FromJson<UserData>(json);
        }
        else
        {
            userData = CreateDefaultUserData();
            SaveLocal();
        }
    }

    // --- 서버 동기화 (향후) ---
    public IEnumerator SyncToServer()
    {
        string json = JsonUtility.ToJson(userData);
        var request = new UnityWebRequest("https://api.example.com/match3/user/save", "POST");
        request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        // 에러 처리...
    }

    public IEnumerator SyncFromServer()
    {
        var request = UnityWebRequest.Get("https://api.example.com/match3/user/load");
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            userData = JsonUtility.FromJson<UserData>(request.downloadHandler.text);
            SaveLocal(); // 로컬 캐시 갱신
        }
    }

    // --- 자동 저장 ---
    void OnApplicationPause(bool pause)
    {
        if (pause) SaveLocal();
    }

    void OnApplicationQuit()
    {
        SaveLocal();
    }

    private UserData CreateDefaultUserData() { /* 기본값 세팅 */ }
}
```

### 핵심 알고리즘
- **자동 저장 타이밍**: 레벨 클리어/실패, 결제 완료, 앱 Pause/Quit 시
- **데이터 무결성**: 저장 전 백업 파일 생성 → 저장 성공 시 백업 삭제 → 로드 실패 시 백업에서 복구
- **오프라인 캐시**: 서버 연결 불가 시 로컬에 변경 큐 저장 → 온라인 복귀 시 일괄 동기화

### 세이브 파일 보안 (NLM 피드백)
- **평문 JSON 저장 금지** — 사용자가 파일 편집으로 재화/레벨 치팅 가능
- **AES 암호화** 적용 (권장) 또는 최소한 Base64 인코딩

> ⚠️ **(NLM 최종 피드백)** AES 암호화 키를 소스 코드에 `"1234567890123456"` 같이 **평문 하드코딩 절대 금지**.
> 바이트 연산(XOR)으로 난독화하여 런타임에 조립하거나, Android Keystore / iOS Keychain에서 불러올 것.
> ```csharp
> // ❌ 금지: private static readonly string key = "MySecretKey12345";
> // ✅ 권장: XOR 난독화로 조립
> private static byte[] GetKey()
> {
>     byte[] a = { 0x4D, 0x79, 0x53, 0x65 }; // 난독화된 파트 A
>     byte[] b = { 0x1A, 0x2B, 0x3C, 0x4D }; // 난독화된 파트 B
>     byte[] key = new byte[16];
>     for (int i = 0; i < 4; i++) key[i] = (byte)(a[i] ^ b[i]);
>     // ... 나머지 조립
>     return key;
> }
> ```

```csharp
// AES 암호화 저장 예시
public void SaveLocal()
{
    string json = JsonConvert.SerializeObject(userData, Formatting.Indented);
    byte[] encrypted = AesEncrypt(Encoding.UTF8.GetBytes(json), GetKey()); // 하드코딩 키 금지
    File.WriteAllBytes(SavePath, encrypted);
}

public void LoadLocal()
{
    if (File.Exists(SavePath))
    {
        byte[] encrypted = File.ReadAllBytes(SavePath);
        byte[] decrypted = AesDecrypt(encrypted, secretKey);
        string json = Encoding.UTF8.GetString(decrypted);
        userData = JsonConvert.DeserializeObject<UserData>(json);
    }
}
```

### 클라우드 세이브 확장 구조 (NLM 피드백)
- 향후 **Google Play Games Services (Saved Games API)** 또는 **Firebase Firestore** 연동
- 구조 설계:
  1. `ISaveProvider` 인터페이스 정의 (Save/Load/Sync)
  2. `LocalSaveProvider`: 로컬 JSON 파일 (현재)
  3. `CloudSaveProvider`: Google Play Games / Firebase Firestore (향후)
  4. `DataManager`에서 Provider 패턴으로 전환 가능하도록 설계
- 충돌 해결: 서버 타임스탬프 기준 최신 데이터 우선, 사용자에게 선택 UI 제공

## 단계별 구현
1. **1차**: PlayerPrefs → JSON 파일 전환 (JsonUtility)
2. **2차**: REST API 연동 (기존 Spring Boot 백엔드 활용)
3. **3차**: 오프라인 캐시 + 온라인 동기화
4. **4차**: 서버 측 검증 (치트 방지)

## 백엔드 연동 (Spring Boot)
- POST /api/match3/user/save — 유저 데이터 저장
- GET /api/match3/user/load — 유저 데이터 로드
- GET /api/match3/leaderboard — 리더보드
- POST /api/match3/iap/verify — 결제 검증

## 검증 항목
- [ ] JSON 저장 정상 (`Application.persistentDataPath`에 파일 생성 확인)
- [ ] JSON 로드 정상 (파일에서 UserData 역직렬화 확인)
- [ ] PlayerPrefs 완전 제거 (코드베이스에서 PlayerPrefs 검색 결과 0건)
- [ ] 앱 재시작 시 데이터 유지 (Play → Stop → Play 후 데이터 확인)
- [ ] 앱 Pause 시 자동 저장 (OnApplicationPause 동작 확인)
- [ ] 데이터 손상 시 백업 복구 정상
- [ ] Newtonsoft.Json 직렬화/역직렬화 정상 (Dictionary, DateTime 직접 지원 확인)
- [ ] 세이브 파일 AES 암호화 적용 확인 (평문 JSON 노출 없음)
- [ ] 암호화된 세이브 파일 로드 → 복호화 → 데이터 정상 복원
- [ ] (향후) REST API 연동 — UnityWebRequest POST/GET
- [ ] (향후) 오프라인→온라인 동기화 — 변경 큐 일괄 전송
- [ ] (향후) 클라우드 세이브 — ISaveProvider 인터페이스 기반 확장

## MCP 도구 호출 순서

```
Step 1: script-update-or-create → UserData.cs, DataManager.cs, AES 암호화 유틸리티
Step 2: assets-refresh → 컴파일
Step 3: script-execute → PlayerPrefs 기존 데이터 마이그레이션 테스트 (기존 값 읽기 → JSON 저장)
Step 4: editor-application-set-state → Play 모드 테스트
Step 5: screenshot-game-view → 저장/로드 후 데이터 유지 확인
```

> Unity 에디터 작업 없음 (100% 코드). NLM 확인 완료.

## 진행률: 0%
