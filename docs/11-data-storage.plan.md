# Phase 11: 데이터 저장 + DB 연동

## 목표
PlayerPrefs → JSON 로컬 저장 → REST API 서버 동기화

## 데이터 구조

### UserData (POCO)
```csharp
[Serializable]
class UserData {
    int coins;
    int lives;
    DateTime lastLifeRegenTime;
    Dictionary<string, int> levelStars;    // "Level01" → 3
    Dictionary<string, int> boosterCounts;  // "Hammer" → 2
    DateTime lastDailyReward;
    int seasonPassTier;
    int totalGamesPlayed;
}
```

### DataManager
- SaveLocal(): JSON → Application.persistentDataPath
- LoadLocal(): 파일 → UserData
- SyncToServer(): REST API POST (향후)
- SyncFromServer(): REST API GET (향후)
- 서버 측 검증: 비정상 데이터 필터링

## 단계별 구현
1. **1차**: PlayerPrefs → JSON 파일 전환
2. **2차**: REST API 연동 (기존 Spring Boot 백엔드 활용)
3. **3차**: 오프라인 캐시 + 온라인 동기화
4. **4차**: 서버 측 검증 (치트 방지)

## 백엔드 연동 (Spring Boot)
- POST /api/match3/user/save — 유저 데이터 저장
- GET /api/match3/user/load — 유저 데이터 로드
- GET /api/match3/leaderboard — 리더보드
- POST /api/match3/iap/verify — 결제 검증

## 검증 항목
- [ ] JSON 저장/로드 정상
- [ ] PlayerPrefs 완전 제거
- [ ] 앱 재시작 시 데이터 유지
- [ ] (향후) REST API 연동
- [ ] (향후) 오프라인→온라인 동기화

## 진행률: 0%
