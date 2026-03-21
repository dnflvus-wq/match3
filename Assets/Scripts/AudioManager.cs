using UnityEngine;

namespace Match3
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Sound Effects")]
        public AudioClip matchSound;
        public AudioClip swapSound;
        public AudioClip failSound;
        public AudioClip comboSound;
        public AudioClip specialSound;
        public AudioClip winSound;
        public AudioClip loseSound;

        [Header("BGM")]
        public AudioClip bgm;

        [Header("Settings")]
        [Range(0f, 1f)] public float sfxVolume = 0.7f;
        [Range(0f, 1f)] public float bgmVolume = 0.4f;

        private AudioSource _sfxSource;
        private AudioSource _bgmSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
        }

        private void Start()
        {
            PlayBGM();
        }

        public void PlayBGM()
        {
            if (bgm == null) return;
            _bgmSource.clip = bgm;
            _bgmSource.volume = bgmVolume;
            _bgmSource.Play();
        }

        public void PlayMatch(int comboCount = 0)
        {
            if (matchSound == null) return;
            float pitch = 1f + comboCount * 0.1f;
            pitch = Mathf.Clamp(pitch, 1f, 2f);
            PlaySFX(matchSound, pitch);
        }

        public void PlaySwap()
        {
            PlaySFX(swapSound);
        }

        public void PlayFail()
        {
            PlaySFX(failSound);
        }

        public void PlayCombo()
        {
            PlaySFX(comboSound, 1.2f);
        }

        public void PlaySpecial()
        {
            PlaySFX(specialSound, 1.1f);
        }

        public void PlayWin()
        {
            PlaySFX(winSound);
        }

        public void PlayLose()
        {
            PlaySFX(loseSound);
        }

        private void PlaySFX(AudioClip clip, float pitch = 1f)
        {
            if (clip == null || _sfxSource == null) return;
            _sfxSource.pitch = pitch;
            _sfxSource.volume = sfxVolume;
            _sfxSource.PlayOneShot(clip);
        }
    }
}
