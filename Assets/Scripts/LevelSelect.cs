using UnityEngine;

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
            for (int i = 0; i < buttons.Length; i++)
            {
                int score = PlayerPrefs.GetInt(buttons[i].playerPrefKey, 0);

                for (int starIndex = 1; starIndex <= 3; starIndex++)
                {
                    Transform star = buttons[i].gameObject.transform.Find($"star{starIndex}");
                    if (star != null)
                        star.gameObject.SetActive(starIndex <= score);
                }
            }

            // 배경을 화면 전체에 맞추기
            StretchBackground();

            // 레벨 4~10 버튼 동적 생성
            CreateExtraLevelButtons();
        }

        private void StretchBackground()
        {
            var cam = Camera.main;
            if (cam == null) return;

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.3f, 0.7f, 0.3f, 1f);

            // 새 배경 이미지 로드 시도
            Texture2D newBgTex = Resources.Load<Texture2D>("level_select_bg");

            var renderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            foreach (var sr in renderers)
            {
                string n = sr.gameObject.name.ToLower();
                if (!n.Contains("background") && !n.Contains("bg")) continue;

                // 새 배경으로 교체
                if (newBgTex != null)
                {
                    sr.sprite = Sprite.Create(newBgTex,
                        new Rect(0, 0, newBgTex.width, newBgTex.height),
                        new Vector2(0.5f, 0.5f));
                }

                if (sr.sprite == null) continue;

                float camH = cam.orthographicSize * 2f;
                float camW = camH * cam.aspect;
                float sprW = sr.sprite.bounds.size.x;
                float sprH = sr.sprite.bounds.size.y;

                float scale = Mathf.Max(camW / sprW, camH / sprH);
                scale = Mathf.Max(scale, 1f) * 1.1f;

                sr.transform.localScale = new Vector3(scale, scale, 1f);
                sr.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 5f);
                sr.sortingOrder = -100;
            }
        }

        private void CreateExtraLevelButtons()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            // 기존 버튼 3개 아래에 레벨 4~10 버튼 추가
            for (int level = 4; level <= 10; level++)
            {
                string sceneName = $"Level{level:D2}";

                var btnObj = new GameObject("LevelBtn_" + level);
                btnObj.transform.SetParent(canvas.transform, false);

                var img = btnObj.AddComponent<Image>();
                img.color = new Color(0.8f, 0.5f, 0.2f, 0.9f);

                var btn = btnObj.AddComponent<Button>();
                string sn = sceneName;
                btn.onClick.AddListener(() => OnButtonPress(sn));

                var rt = btnObj.GetComponent<RectTransform>();
                // 2열 배치: 왼쪽/오른쪽
                int idx = level - 4;
                int col = idx % 2;
                int row = idx / 2;
                float x = col == 0 ? -120f : 120f;
                float y = -50f - row * 130f;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(x, y);
                rt.sizeDelta = new Vector2(200, 100);

                // 레벨 번호 텍스트
                var textObj = new GameObject("Text");
                textObj.transform.SetParent(btnObj.transform, false);
                var text = textObj.AddComponent<Text>();
                text.text = level.ToString();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 40;
                text.fontStyle = FontStyle.Bold;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;

                var outline = textObj.AddComponent<Outline>();
                outline.effectColor = new Color(0.4f, 0.2f, 0f);
                outline.effectDistance = new Vector2(2, -2);

                var trt = textObj.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = Vector2.zero;

                // 별점 표시
                int stars = PlayerPrefs.GetInt(sceneName, 0);
                if (stars > 0)
                {
                    var starObj = new GameObject("Stars");
                    starObj.transform.SetParent(btnObj.transform, false);
                    var starText = starObj.AddComponent<Text>();
                    starText.text = new string('★', stars);
                    starText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    starText.fontSize = 20;
                    starText.alignment = TextAnchor.MiddleCenter;
                    starText.color = new Color(1f, 0.85f, 0f);
                    var srt = starObj.GetComponent<RectTransform>();
                    srt.anchorMin = new Vector2(0, 0);
                    srt.anchorMax = new Vector2(1, 0.3f);
                    srt.offsetMin = Vector2.zero;
                    srt.offsetMax = Vector2.zero;
                }
            }
        }

        public void OnButtonPress(string levelName)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(levelName);
        }
    }
}
