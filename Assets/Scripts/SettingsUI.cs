using UnityEngine;
using UnityEngine.UI;

namespace Match3
{
    public class SettingsUI : MonoBehaviour
    {
        private GameObject _panel;
        private bool _isOpen;

        private const string BGM_KEY = "BGM_Enabled";
        private const string SFX_KEY = "SFX_Enabled";

        public static bool BgmEnabled
        {
            get => PlayerPrefs.GetInt(BGM_KEY, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(BGM_KEY, value ? 1 : 0);
                PlayerPrefs.Save();
                if (AudioManager.Instance != null)
                {
                    var bgmSource = AudioManager.Instance.GetComponent<AudioSource>();
                    // BGM은 두 번째 AudioSource
                    var sources = AudioManager.Instance.GetComponents<AudioSource>();
                    if (sources.Length >= 2)
                        sources[1].mute = !value;
                }
            }
        }

        public static bool SfxEnabled
        {
            get => PlayerPrefs.GetInt(SFX_KEY, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(SFX_KEY, value ? 1 : 0);
                PlayerPrefs.Save();
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.sfxVolume = value ? 0.7f : 0f;
                }
            }
        }

        private void Start()
        {
            // 설정 적용
            if (!SfxEnabled && AudioManager.Instance != null)
                AudioManager.Instance.sfxVolume = 0f;

            if (!BgmEnabled && AudioManager.Instance != null)
            {
                var sources = AudioManager.Instance.GetComponents<AudioSource>();
                if (sources.Length >= 2)
                    sources[1].mute = true;
            }
        }

        public void Toggle()
        {
            if (_isOpen)
                Close();
            else
                Open();
        }

        private void Open()
        {
            _isOpen = true;
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            _panel = new GameObject("SettingsPanel");
            _panel.transform.SetParent(canvas.transform, false);

            // 반투명 배경
            var bg = _panel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);
            var rt = _panel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // 타이틀
            CreateText("Settings", new Vector2(0, 150), 40);

            // BGM 토글
            CreateToggleButton("BGM: " + (BgmEnabled ? "ON" : "OFF"), new Vector2(0, 50), () =>
            {
                BgmEnabled = !BgmEnabled;
                RefreshPanel();
            });

            // SFX 토글
            CreateToggleButton("SFX: " + (SfxEnabled ? "ON" : "OFF"), new Vector2(0, -30), () =>
            {
                SfxEnabled = !SfxEnabled;
                RefreshPanel();
            });

            // 닫기 버튼
            CreateToggleButton("Close", new Vector2(0, -130), Close);
        }

        private void RefreshPanel()
        {
            Close();
            Open();
        }

        private void Close()
        {
            _isOpen = false;
            if (_panel != null)
                Destroy(_panel);
        }

        private void CreateText(string content, Vector2 pos, int size)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(_panel.transform, false);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(400, 60);
        }

        private void CreateToggleButton(string label, Vector2 pos, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject("Button");
            go.transform.SetParent(_panel.transform, false);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.3f, 0.9f);

            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(action);

            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(300, 60);

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            var trt = textGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
        }
    }
}
