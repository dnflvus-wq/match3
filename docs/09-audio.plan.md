# Phase 9: 사운드/오디오

> **Unity 에디터 작업이 핵심**: Audio Mixer 생성, Group 구성, Exposed Parameter, Import Settings.
> NotebookLM 리서치 기반.

## Unity 에디터에서 반드시 해야 하는 작업

### 1. Audio Mixer 생성 (에디터 조작 순서)

```
1. Window > Audio > Audio Mixer 창 열기
2. "GameAudioMixer" 새 믹서 생성
3. Master 그룹 아래 자식 그룹 생성:
   - "BGM" (배경음)
   - "SFX" (효과음)
4. BGM 그룹 Inspector:
   - Volume 기본값: -10dB
   - Volume 글자 우클릭 → "Expose Volume to script" 선택
   - Exposed Parameters에서 이름: "BGM_Volume"
5. SFX 그룹 Inspector:
   - Volume 기본값: 0dB
   - 동일하게 Expose → 이름: "SFX_Volume"
6. Snapshot 추가:
   - 기본 "Default" 스냅샷 외에 "Ducked" 스냅샷 생성
   - "Ducked" 선택 후 BGM Volume: -30dB (게임오버/팝업 시 BGM 줄이기용)
7. SFX 그룹에 Compressor 이펙트 추가:
   - 연쇄 반응 시 동시 다수 SFX 클리핑 방지
   - Threshold: -20dB, Ratio: 4:1
```

### 2. Audio Source 설정 (씬에서)

```
AudioManager 오브젝트 Inspector:
1. BGM Audio Source:
   - Output: GameAudioMixer > BGM
   - Play On Awake: true
   - Loop: true
   - Volume: 0.8
2. SFX Audio Source (2개, 동시 재생용):
   - Output: GameAudioMixer > SFX
   - Play On Awake: false
   - Loop: false
```

### 3. Audio Clip Import Settings (에디터에서 각 파일마다)

```
BGM 파일 (.mp3):
  - Load Type: "Streaming" (메모리 절약, NLM 권장)
  - Compression Format: Vorbis
  - Quality: 70%

SFX 파일 (.wav):
  - Load Type: "Decompress On Load" (짧은 파일, 즉시 재생)
  - Compression Format: ADPCM (모바일 최적)
  - Force Mono: true (스테레오 불필요, 메모리 절약)
```

## 연쇄 피치 상승 (코드 — NLM 확인)

```csharp
// 연쇄 반응 시 피치 점진 상승
int comboCount = 0;

void PlayMatchSFX()
{
    float pitch = 1.0f + comboCount * 0.05f;
    pitch = Mathf.Clamp(pitch, 1.0f, 1.5f);  // 최대 1.5배
    sfxSource.pitch = pitch;
    sfxSource.PlayOneShot(matchClip);
    comboCount++;
}

void OnBoardStable()  // FSM READY 상태 진입 시
{
    comboCount = 0;
    sfxSource.pitch = 1.0f;
}
```

## SFX 목록 (NLM 확인: 짧고 멜로디컬한 톤, 공격적 소리 금지)

| 클립 | 타이밍 | 길이 | 톤 |
|------|--------|------|----|
| match.wav | 3매치 파괴 시 | 0.3s | 밝은 팝 |
| match_special.wav | 4+매치 (특수 생성) | 0.4s | 높은 차임 |
| swap.wav | 타일 스왑 | 0.2s | 슈웅 |
| swap_fail.wav | 스왑 실패 핑퐁 | 0.3s | 퉁 |
| special_stripe.wav | 스트라이프 기폭 | 0.4s | 레이저 |
| special_bomb.wav | 폭탄 기폭 | 0.5s | 폭발 |
| special_colorbomb.wav | 컬러밤 기폭 | 0.7s | 매직 |
| booster.wav | 부스터 사용 | 0.3s | 뿅 |
| clear.wav | 레벨 클리어 | 1.5s | 팡파레 |
| fail.wav | 레벨 실패 | 1.0s | 하강음 |
| ui_click.wav | UI 버튼 | 0.1s | 틱 |
| star.wav | 별 획득 | 0.3s | 짤랑 |
| shuffle.wav | 셔플 | 0.8s | 와르르 |

## BGM 목록 (NLM: 가사 없는 기악곡, Seamless loop)

| 클립 | 상황 | BPM | 분위기 |
|------|------|-----|--------|
| bgm_lobby.mp3 | 레벨 선택/로비 | 90~100 | 차분, 이완 |
| bgm_ingame.mp3 | 일반 인게임 | 110~120 | 경쾌, 집중 |
| bgm_boss.mp3 | 하드/보스 레벨 | 130~140 | 긴박, 아드레날린 |

## 코드 작업

- `AudioManager.cs` 리팩토링:
  - `AudioMixer mixer` 참조 (Inspector 할당)
  - `SetBGMVolume(float 0~1)`: `mixer.SetFloat("BGM_Volume", Mathf.Log10(value) * 20)`
  - `SetSFXVolume(float 0~1)`: 동일
  - `PlayCascadeSFX(int level)`: 피치 상승 적용
  - `TransitionToDucked()`: 게임오버 시 BGM 줄이기
- `ProceduralAudio.cs` → **삭제** (163줄)
- 동시 SFX 재생 제한: 같은 클립 최대 3개까지만

## 검증 항목
- [ ] Audio Mixer: BGM/SFX 독립 볼륨 조절 (Exposed Parameter)
- [ ] 연쇄 피치 상승 (1연쇄=1.0, 2연쇄=1.05, ... 최대 1.5)
- [ ] BGM Streaming 로드 (메모리 측정)
- [ ] SFX Compressor로 클리핑 방지 (연쇄 폭발 시)
- [ ] Ducked 스냅샷 전환 (게임오버 팝업 시)
- [ ] ProceduralAudio.cs 완전 제거
- [ ] 설정 UI 슬라이더 → 볼륨 반영 + PlayerPrefs 저장

## MCP 도구 호출 순서 (NLM 검증 완료)

```
Step 1: script-update-or-create → AudioManager.cs (피치 상승, Mixer 연동) 작성
Step 2: assets-refresh → 컴파일
Step 3: script-execute → AudioMixerController API로 GameAudioMixer 생성
  → Master/BGM/SFX 그룹 생성, Exposed Parameter 설정, 파일 저장
  (기본 뼈대 생성 후, 세밀 설정도 script-execute로 추가 조정 가능)
Step 4: gameobject-component-add → 씬에 AudioManager + AudioSource 2~3개 추가
Step 5: gameobject-component-modify → AudioSource의 outputAudioMixerGroup 매핑, BGM Loop=true
Step 6: script-delete → ProceduralAudio.cs 삭제
Step 7: scene-save → 씬 저장
Step 8: editor-application-set-state → Play 모드 테스트
```

**MCP로 처리:**
- Snapshot/Compressor → script-execute로 AudioMixerController API 호출
- 볼륨 미세 조정 → script-execute로 SetFloat 설정
- 최종 확인만 사용자가 Play 모드에서 청취 검토

## 진행률: 0%
