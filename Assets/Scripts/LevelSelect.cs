using UnityEngine;
using UnityEngine.UI;

namespace Match3
{
    public class LevelSelect : MonoBehaviour
    {
        [System.Serializable]
        public struct ButtonPlayerPrefs
        {
            public GameObject gameObject;
            public string playerPrefKey;
        };

        public ButtonPlayerPrefs[] buttons;

        private void Start()
        {
            // 기존 씬 버튼(Button01~03) 숨기기
            HideSceneButtons();

            // 배경 교체
            StretchBackground();

            // 1~5 전부 동일한 방식으로 생성
            CreateAllLevelButtons();
        }

        private void HideSceneButtons()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].gameObject != null)
                    buttons[i].gameObject.SetActive(false);
            }
        }

        private void StretchBackground()
        {
            var cam = Camera.main;
            if (cam == null) return;

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.15f, 0.1f, 0.25f, 1f);
        }

        private void CreateAllLevelButtons()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 2160);
                scaler.matchWidthOrHeight = 0.5f;
            }

            Sprite btnSprite = Resources.Load<Sprite>("level_btn");
            Sprite btnLockedSprite = Resources.Load<Sprite>("level_btn_locked");

            // 레이아웃: 3열 그리드 (1행: 1,2,3 / 2행: 4,5)
            float btnSize = 220f;
            float spacingX = 240f;
            float spacingY = 280f;
            float startY = -100f; // 1행 아래(큰 y), 2행 위(작은 y)

            for (int level = 1; level <= 5; level++)
            {
                string sceneName = $"Level{level:D2}";
                int stars = PlayerPrefs.GetInt(sceneName, 0);
                bool unlocked = level <= 3 || PlayerPrefs.GetInt($"Level{(level-1):D2}", 0) > 0;

                var btnObj = new GameObject("LevelBtn_" + level);
                btnObj.transform.SetParent(canvas.transform, false);

                // 이미지
                var img = btnObj.AddComponent<Image>();
                Sprite sp = unlocked ? btnSprite : btnLockedSprite;
                if (sp != null)
                {
                    img.sprite = sp;
                    img.preserveAspect = true;
                }
                if (!unlocked)
                    img.color = new Color(0.5f, 0.5f, 0.5f, 0.85f);

                // 클릭
                var btn = btnObj.AddComponent<Button>();
                if (unlocked)
                {
                    string sn = sceneName;
                    btn.onClick.AddListener(() => OnButtonPress(sn));
                }

                // 위치: 1행(1,2,3) 2행(4,5)
                int row, col, colsInRow;
                if (level <= 3)
                {
                    row = 0;
                    col = level - 1;
                    colsInRow = 3;
                }
                else
                {
                    row = 1;
                    col = level - 4;
                    colsInRow = 2;
                }

                float rowWidth = (colsInRow - 1) * spacingX;
                float x = -rowWidth / 2f + col * spacingX;
                float y = -startY - row * spacingY;

                var rt = btnObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(x, y);
                rt.sizeDelta = new Vector2(btnSize, btnSize);

                // 레벨 번호
                var textObj = new GameObject("Text");
                textObj.transform.SetParent(btnObj.transform, false);
                var text = textObj.AddComponent<Text>();
                text.text = level.ToString();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 52;
                text.fontStyle = FontStyle.Bold;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = unlocked ? Color.white : new Color(0.6f, 0.6f, 0.6f);

                var outline = textObj.AddComponent<Outline>();
                outline.effectColor = new Color(0.1f, 0.05f, 0f);
                outline.effectDistance = new Vector2(2, -2);

                var trt = textObj.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0, 0.2f);
                trt.anchorMax = new Vector2(1, 1f);
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = Vector2.zero;

                // 별점
                var starObj = new GameObject("Stars");
                starObj.transform.SetParent(btnObj.transform, false);
                var starText = starObj.AddComponent<Text>();
                starText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                starText.fontSize = 20;
                starText.alignment = TextAnchor.MiddleCenter;

                if (!unlocked)
                {
                    starText.text = "\U0001F512";
                    starText.color = new Color(0.5f, 0.5f, 0.5f);
                }
                else if (stars > 0)
                {
                    starText.text = new string('\u2605', stars) + new string('\u2606', 3 - stars);
                    starText.color = new Color(1f, 0.85f, 0f);
                }
                else
                {
                    starText.text = "\u2606\u2606\u2606";
                    starText.color = new Color(0.7f, 0.6f, 0.4f);
                }

                var srt = starObj.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(0, 0);
                srt.anchorMax = new Vector2(1, 0.25f);
                srt.offsetMin = Vector2.zero;
                srt.offsetMax = Vector2.zero;
            }
        }

        public void OnButtonPress(string levelName)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(levelName);
        }
    }
}
