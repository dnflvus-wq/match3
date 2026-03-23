# Phase 6: 메타게임

## 목표
퍼즐 외 장기 목표를 제공하여 유저 리텐션 극대화

## 반복 검증 프로세스
1. 구현 완료 → 계획서 대비 갭분석 (99% 일치까지 반복)
2. E2E 테스트 (Playwright MCP, UI 조작)
3. 최종 갭분석 (99% 확인)
4. 모든 단계 통과 시에만 완료 보고

---

## 메타게임 후보 (택 1~2)

### A. 스토리 + 꾸미기 (Homescapes 스타일) — ⭐ NLM 추천
- 레벨 클리어 → 별 획득 → 방/정원 개조
- 스토리 대화 이벤트 (캐릭터 간 대사)
- 꾸미기 선택지 (가구/벽지/바닥 등)
- **NLM 피드백**: 수익성+리텐션 면에서 가장 검증된 모델. Homescapes/Gardenscapes 등 상위권 매치3 대부분이 채택. 꾸미기(Decor)가 IAP 구매 동기를 자연스럽게 유발

### B. 캐릭터 수집 (가챠 스타일)
- 레벨 클리어 → 조각 획득 → 캐릭터 합성
- 캐릭터별 패시브 스킬 (추가 이동수, 특정 색상 보너스 등)

### C. 월드맵 탐험
- 레벨 진행 → 맵에서 새로운 지역 해금
- 지역별 테마 변경 (숲→사막→바다→우주)
- 보스 스테이지 (특수 규칙)

---

## 월드맵 UI 구현 (Unity 에디터)

### 에디터 조작 순서 — ScrollRect 월드맵 캔버스:

> **NLM 피드백**: 레벨이 많아지면(100+) 기본 ScrollRect는 모든 노드를 한 번에 활성화하여 성능 저하. **Recycling ScrollRect**(오브젝트 풀링 기반)로 화면에 보이는 노드만 활성화하는 것을 권장. Asset Store에서 "Recycling Scroll Rect" 또는 직접 구현하여 `OnBecameVisible/Invisible`로 풀링 처리

1. Hierarchy에서 Canvas 우클릭 → `UI > Scroll View` 생성
2. ScrollRect 컴포넌트 설정:
   - `Horizontal` 체크, `Vertical` 체크 (2D 맵 스크롤)
   - `Movement Type → Elastic`
   - `Elasticity → 0.1`
   - `Inertia` 체크, `Deceleration Rate → 0.135`
3. Scroll View > Viewport > **Content** 오브젝트 선택:
   - `Content Size Fitter` 컴포넌트 추가 (Horizontal/Vertical Fit → Preferred Size)
   - RectTransform: 월드맵 전체 크기 설정 (예: 2000×5000)
4. Content 안에 월드맵 배경 이미지 배치 (Image 컴포넌트, 큰 맵 스프라이트)

### 에디터 조작 순서 — 레벨 노드 버튼 배치:
1. Content 오브젝트 아래에 빈 오브젝트 `LevelNodes` 생성
2. 각 레벨 노드를 **Button** 프리팹으로 생성:
   - `UI > Button - TextMeshPro` 생성 → 노드 스프라이트로 교체
   - 레벨 번호 텍스트 표시
   - 별 표시 (0~3개)
   - 잠금/해금 상태 아이콘
3. 각 노드를 맵 위 원하는 위치에 **드래그로 배치** (곡선 경로 형태)
4. 노드 간 연결선: `UI > Image` (Sliced)로 경로 라인 배치 또는 LineRenderer

### 코드 구현:
```csharp
public class WorldMapManager : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform levelNodesParent;
    [SerializeField] private LevelNodeButton nodePrefab;

    private List<LevelNodeButton> nodes = new List<LevelNodeButton>();

    public void Initialize(UserData userData)
    {
        foreach (var node in nodes)
        {
            int levelIndex = node.LevelIndex;
            bool unlocked = levelIndex <= userData.GetMaxClearedLevel() + 1;
            int stars = userData.GetStars(levelIndex);
            node.SetState(unlocked, stars);
        }

        // 현재 레벨 위치로 스크롤
        ScrollToCurrentLevel(userData.GetMaxClearedLevel());
    }

    private void ScrollToCurrentLevel(int levelIndex)
    {
        // 해당 노드의 anchoredPosition 기반으로 scrollRect.normalizedPosition 계산
    }
}

public class LevelNodeButton : MonoBehaviour
{
    public int LevelIndex;
    [SerializeField] private Image[] starImages;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private Button button;

    public void SetState(bool unlocked, int stars)
    {
        lockIcon.SetActive(!unlocked);
        button.interactable = unlocked;
        for (int i = 0; i < starImages.Length; i++)
            starImages[i].enabled = i < stars;
    }

    public void OnClick()
    {
        // 레벨 진입
        GameManager.Instance.StartLevel(LevelIndex);
    }
}
```

---

## 스토리 시스템 (ScriptableObject 관리)

### 에디터 조작 순서 — SO 생성:
1. Project 창에서 `Assets/Data/Stories` 폴더 생성
2. 우클릭 → `Create > Match3 > Story Data` (커스텀 메뉴, 코드에서 정의)
3. 각 스토리 SO에 대사/캐릭터 이미지/배경 설정

### 코드 구현:
```csharp
[CreateAssetMenu(fileName = "StoryData", menuName = "Match3/Story Data")]
public class StoryData : ScriptableObject
{
    public string storyId;
    public int triggerAfterLevel; // 이 레벨 클리어 후 발동

    [System.Serializable]
    public class DialogueLine
    {
        public string characterName;
        public Sprite characterImage;
        public string dialogue;           // 대사 텍스트
        public Sprite backgroundImage;    // 배경 (null이면 변경 없음)
        public AudioClip voiceClip;       // 음성 (선택)
    }

    public List<DialogueLine> dialogueLines;
}

public class StoryManager : MonoBehaviour
{
    [SerializeField] private StoryData[] allStories;
    [SerializeField] private StoryDialogueUI dialogueUI;

    public bool HasStoryAfterLevel(int levelIndex)
    {
        return allStories.Any(s => s.triggerAfterLevel == levelIndex);
    }

    public void PlayStory(int levelIndex)
    {
        var story = allStories.FirstOrDefault(s => s.triggerAfterLevel == levelIndex);
        if (story != null)
            dialogueUI.Show(story);
    }
}
```

---

## 퍼즐과의 연동
- 코어 루프: 퍼즐 클리어 → 재화 획득 → 메타게임 진행 → 새 콘텐츠 해금 → 동기 부여 → 퍼즐 계속
- 재화 싱크: 별(메타 진행용) + 코인(부스터 구매용) 분리

## 소셜 기능
- 글로벌/친구 리더보드
- 팀 시스템 (하트 주고받기)
- 팀 대항전 이벤트
- Firebase + PlayFab 활용

## 일일 시스템

### 코드 구현:
```csharp
public class DailyRewardManager : MonoBehaviour
{
    [SerializeField] private DailyRewardData[] rewards; // 7일 사이클 SO

    // NLM 피드백: 로컬 시간(DateTime.UtcNow) 대신 서버 시간(NTP/Firebase) 기반으로 검증
    // 로컬 시간 조작으로 일일 보상 중복 수령 방지
    public bool CanClaimToday()
    {
        var lastClaim = DataManager.Instance.UserData.lastDailyReward;
        var serverNow = TimeManager.Instance.GetServerUtcNow(); // NTP 또는 Firebase 서버 시간
        return (serverNow - DateTime.Parse(lastClaim)).TotalHours >= 24;
    }

    public void ClaimReward()
    {
        int day = DataManager.Instance.UserData.consecutiveLoginDays % 7;
        var reward = rewards[day];
        reward.Grant(); // 코인/부스터/스킨 지급
        DataManager.Instance.UserData.lastDailyReward = DateTime.UtcNow.ToString("O");
        DataManager.Instance.SaveLocal();
    }
}

public class DailyMissionManager : MonoBehaviour
{
    public List<DailyMission> todayMissions; // 매일 3개 랜덤 생성

    public void OnMatchMade(int count) { /* 미션 진행 체크 */ }
    public void OnBoosterUsed(string type) { /* 미션 진행 체크 */ }
    public void OnLevelCleared(int level) { /* 미션 진행 체크 */ }
}
```

- 일일 보상 (연속 출석 보너스, 7일 사이클)
- 일일 미션 3개 (매치 100회, 부스터 사용 등)
- 주간 이벤트 (한정 보상)
- **서버 시간 검증 (NLM 피드백)**: NTP 서버 또는 Firebase Realtime Database의 `ServerValue.TIMESTAMP`로 현재 시간을 받아 로컬 시간 조작 방지

## 검증 항목
- [ ] 메타게임 종류 결정 (사용자와 협의)
- [ ] ScrollRect 월드맵 스크롤 정상 (터치 드래그, 관성)
- [ ] 레벨 노드 버튼 터치 → 해당 레벨 진입
- [ ] 잠긴 레벨 노드 터치 불가
- [ ] 별 표시 (0~3개) 정상 반영
- [ ] 현재 레벨 위치로 자동 스크롤
- [ ] StoryData SO에서 대사/이미지 로드 정상
- [ ] 레벨 클리어 후 스토리 자동 재생
- [ ] 코어 루프 ↔ 메타 연동 (별/코인 흐름)
- [ ] 리더보드 표시
- [ ] 일일 보상 수령 + 연속 출석 카운트
- [ ] 일일 보상 시간 검증이 서버 시간 기반인지 확인 (로컬 시간 조작으로 중복 수령 불가)
- [ ] 일일 미션 진행/완료/보상 수령
- [ ] ScrollRect 레벨 노드 100개 이상에서 성능 저하 없음 (Recycling 적용 시)

## MCP 도구 호출 순서

```
Step 1: script-update-or-create → WorldMapManager.cs, StoryData.cs(SO), DailyRewardManager.cs
Step 2: assets-refresh → 컴파일
Step 3: script-execute → StoryData SO 에셋 생성 (대사/이미지 데이터)
Step 4: script-execute → Canvas에 ScrollRect 월드맵 구조 생성, 레벨 노드 버튼 배치
Step 5: gameobject-component-add → 씬에 WorldMapManager, DailyRewardManager 부착
Step 6: gameobject-component-modify → Inspector에 SO/Prefab 할당
Step 7: scene-save → 씬 저장
Step 8: editor-application-set-state → Play 모드 테스트
```

**MCP로 처리:**
- ScrollRect 앵커/피벗 → script-execute로 RectTransform 코드 설정
- 레벨 노드 위치 → script-execute로 좌표 계산하여 배치
- 최종 확인만 사용자가 screenshot-game-view로 검토

## 진행률: 0%
