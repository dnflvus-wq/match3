# Phase 5: 수익화 (IAP + 광고)

## 목표
하이브리드 캐주얼 수익 모델 구축 (IAP 중심 + 보상형 광고 보조)

## 반복 검증 프로세스
1. 구현 완료 → 계획서 대비 갭분석 (99% 일치까지 반복)
2. E2E 테스트 (Playwright MCP, UI 조작)
3. 최종 갭분석 (99% 확인)
4. 모든 단계 통과 시에만 완료 보고

---

## IAP 아이템 구성

| 아이템 | 가격대 | 용도 | Product ID | Product Type |
|--------|-------|------|-----------|-------------|
| 추가 이동수 +5 | ₩1,200 | 레벨 실패 직후 | `com.comes.match3.moves5` | Consumable |
| 부스터 팩 (3종×3개) | ₩3,500 | Hard 레벨 대비 | `com.comes.match3.boosterpack` | Consumable |
| 코인 팩 (소) | ₩1,200 | 범용 재화 | `com.comes.match3.coins_s` | Consumable |
| 코인 팩 (중) | ₩5,500 | 범용 재화 | `com.comes.match3.coins_m` | Consumable |
| 코인 팩 (대) | ₩12,000 | 범용 재화 | `com.comes.match3.coins_l` | Consumable |
| 광고 제거 | ₩5,500 (1회) | 영구 광고 제거 | `com.comes.match3.noads` | Non-Consumable |
| 시즌패스 | ₩5,500/월 | 일일 보상 + 독점 | `com.comes.match3.seasonpass` | Subscription |

## Unity IAP 설정

### 에디터 조작 순서 — 패키지 설치:
1. **`Window > Package Manager`** 열기
2. 좌측 상단 드롭다운 → `Unity Registry` 선택
3. 검색창에 `In-App Purchasing` 입력
4. **`Unity IAP (In-App Purchasing)`** 패키지 선택 → `Install` 클릭
5. 설치 완료 후 Unity 에디터 재시작

### 에디터 조작 순서 — IAP Catalog 등록:
1. **`Window > Unity IAP > IAP Catalog`** 메뉴 열기
2. `Add Product` 버튼 클릭
3. 각 상품에 대해:
   - **Product ID**: 위 테이블의 Product ID 입력 (예: `com.comes.match3.moves5`)
   - **Product Type**: Consumable / Non-Consumable / Subscription 선택
   - **Title**: 상품명 입력
   - **Description**: 설명 입력
   - **Price**: 가격 입력
4. 위 테이블의 7개 상품 모두 등록
5. `App Store Export` 버튼으로 Google Play Console용 CSV 내보내기

### 코드 구현:
```csharp
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class IAPManager : MonoBehaviour, IDetailedStoreListener
{
    private IStoreController storeController;
    private IExtensionProvider extensionProvider;

    void Start()
    {
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
        builder.AddProduct("com.comes.match3.moves5", ProductType.Consumable);
        builder.AddProduct("com.comes.match3.boosterpack", ProductType.Consumable);
        builder.AddProduct("com.comes.match3.coins_s", ProductType.Consumable);
        builder.AddProduct("com.comes.match3.coins_m", ProductType.Consumable);
        builder.AddProduct("com.comes.match3.coins_l", ProductType.Consumable);
        builder.AddProduct("com.comes.match3.noads", ProductType.NonConsumable);
        builder.AddProduct("com.comes.match3.seasonpass", ProductType.Subscription);
        UnityPurchasing.Initialize(this, builder);
    }

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        extensionProvider = extensions;
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        string productId = args.purchasedProduct.definition.id;
        // 서버 검증 후 보상 지급
        StartCoroutine(VerifyAndGrant(productId, args.purchasedProduct.receipt));
        return PurchaseProcessingResult.Pending; // 서버 검증 완료까지 Pending
    }

    public void BuyProduct(string productId)
    {
        storeController?.InitiatePurchase(productId);
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message) { /* 에러 처리 */ }
    public void OnPurchaseFailed(Product product, PurchaseFailureDescription desc) { /* 에러 처리 */ }
}
```

## 광고 SDK 설정

### 에디터 조작 순서 — SDK 설치 및 App Key 설정:
1. 광고 SDK (LevelPlay / AppLovin MAX) `.unitypackage` 다운로드
2. **`Assets > Import Package > Custom Package`** 에서 임포트
3. 임포트 완료 후 SDK 설정 창 자동 열림 (또는 메뉴에서 수동 열기)
   - LevelPlay: `Window > ironSource > Integration Manager`
   - AppLovin MAX: `AppLovin > Integration Manager`
4. **App Key 입력**: 대시보드에서 발급받은 App Key를 에디터 설정 창에 입력
5. 광고 네트워크 어댑터 설치 (AdMob, Meta Audience Network 등)
6. Android Manifest 자동 병합 확인

### 코드 구현:
```csharp
public class AdManager : MonoBehaviour
{
    private static AdManager instance;
    public static AdManager Instance => instance;

    private bool isRewardedAdReady = false;

    void Start()
    {
        // SDK 초기화 (예: LevelPlay)
        IronSource.Agent.init("APP_KEY_HERE");
        IronSource.Agent.shouldTrackNetworkState(true);

        // 보상형 광고 콜백
        IronSourceRewardedVideoEvents.onAdRewardedEvent += OnAdRewarded;
        IronSourceRewardedVideoEvents.onAdAvailableEvent += (info) => isRewardedAdReady = true;
    }

    public void ShowRewardedAd(System.Action<bool> onComplete)
    {
        if (isRewardedAdReady)
            IronSource.Agent.showRewardedVideo();
        else
            onComplete?.Invoke(false);
    }

    private void OnAdRewarded(IronSourcePlacement placement, IronSourceAdInfo info)
    {
        // 보상 지급: placement.getRewardName(), placement.getRewardAmount()
    }
}
```

## IAP 영수증 검증 (NLM 피드백)

> **필수**: Fake IAP(위조 결제) 방지를 위해 서버 측 Receipt Validation 로직 반드시 구현

```csharp
private IEnumerator VerifyAndGrant(string productId, string receipt)
{
    // 1. 영수증을 서버로 전송
    var requestBody = new { productId = productId, receipt = receipt };
    string json = JsonUtility.ToJson(requestBody);

    var request = new UnityWebRequest("https://api.example.com/match3/iap/verify", "POST");
    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
    request.downloadHandler = new DownloadHandlerBuffer();
    request.SetRequestHeader("Content-Type", "application/json");
    yield return request.SendWebRequest();

    if (request.result == UnityWebRequest.Result.Success)
    {
        var result = JsonUtility.FromJson<VerifyResult>(request.downloadHandler.text);
        if (result.valid)
        {
            GrantReward(productId); // 검증 성공 시에만 보상 지급
            storeController.ConfirmPendingPurchase(
                storeController.products.WithID(productId));
        }
    }
}
```

- 서버에서 Google Play Developer API / Apple App Store Server API로 영수증 진위 확인
- `ProcessPurchase`에서 `PurchaseProcessingResult.Pending` 반환 → 서버 검증 완료 후 `ConfirmPendingPurchase` 호출

## 광고 배치 전략 (NLM 피드백)

> **보상형 광고(Rewarded Video) 위주**로 배치하고, 강제 전면 광고(Interstitial)는 지양
> - 강제 전면 광고는 유저 이탈률을 크게 높이는 주요 원인
> - 보상형 광고는 유저가 자발적으로 시청 → 긍정적 경험 + 높은 eCPM

## 보상형 광고 배치

| 위치 | 보상 | 타이밍 |
|-----|------|--------|
| 레벨 실패 직후 | +5 이동수 | 게임오버 팝업 |
| 레벨 클리어 직후 | 보상 2배 | 결과 화면 |
| 일일 보상 | 보너스 코인 | 로비 |
| 부스터 충전 | 부스터 1개 | 인게임 상점 |

## 시즌패스 구조
- 무료 트랙: 코인, 부스터 소량
- 유료 트랙: 독점 타일 스킨, 대량 보상, 광고 제거
- 레벨 클리어/일일 미션으로 경험치 획득 → 티어 해금

```csharp
public class SeasonPassManager
{
    public int CurrentTier { get; private set; }
    public bool IsPremium { get; private set; } // IAP 구매 여부

    public List<SeasonPassReward> GetRewardsForTier(int tier)
    {
        // SO에서 보상 데이터 로드
    }

    public void AddXP(int amount)
    {
        // 경험치 누적 → 티어 업 체크
    }

    public void ClaimReward(int tier, bool premiumTrack)
    {
        if (premiumTrack && !IsPremium) return;
        // 보상 지급
    }
}
```

## 구현 SDK
- **Unity IAP**: Google Play Billing + Apple StoreKit
- **Unity LevelPlay 또는 AppLovin MAX**: 광고 미디에이션
- **Firebase Analytics**: 결제 패턴/이탈 구간 추적

## 검증 항목
- [ ] Unity IAP 패키지 설치 확인 (Package Manager에서 In-App Purchasing 표시)
- [ ] IAP Catalog에 7개 상품 등록 완료 (Product ID, Type 정확)
- [ ] IAP 결제 흐름 정상 (테스트 모드 — Google Play Console 테스트 계정)
- [ ] ProcessPurchase 콜백에서 서버 검증 호출 (Receipt Validation)
- [ ] Fake IAP 방지 — 서버 측 Google Play / Apple 영수증 검증 API 연동
- [ ] 강제 전면 광고 미사용 확인 (보상형 광고 위주 배치)
- [ ] 광고 SDK 임포트 및 App Key 설정 완료
- [ ] 보상형 광고 재생 + 보상 지급 (각 4개 배치 위치별 확인)
- [ ] 시즌패스 티어 진행 (XP 획득 → 티어 업 → 보상 수령)
- [ ] 결제 후 아이템 정상 지급 (코인/부스터/광고제거 각각)
- [ ] Non-Consumable(광고 제거) 복원 기능 동작
- [ ] Subscription(시즌패스) 갱신/만료 처리

## MCP 도구 호출 순서

```
Step 1: script-update-or-create → IAPManager.cs, AdManager.cs 작성
Step 2: assets-refresh → 컴파일
Step 3: gameobject-component-add → 씬에 IAPManager, AdManager 부착
Step 4: gameobject-component-modify → Inspector 설정
Step 5: editor-application-set-state → Play 모드 테스트
Step 6: screenshot-game-view → 확인
```

**MCP로 처리:**
- Unity IAP Catalog 상품 등록 → script-execute로 IAPCatalog API 호출 또는 win32-inspector로 UI 클릭
- 광고 SDK App Key → script-execute로 PlayerSettings에 설정
- 최종 확인만 사용자 검토

## 진행률: 0%
