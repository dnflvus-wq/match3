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
        }

        private void StretchBackground()
        {
            var cam = Camera.main;
            if (cam == null) return;

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.3f, 0.6f, 0.9f, 1f);

            var renderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            foreach (var sr in renderers)
            {
                string n = sr.gameObject.name.ToLower();
                if (!n.Contains("background") && !n.Contains("bg")) continue;
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

        public void OnButtonPress(string levelName)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(levelName);
        }
    }
}
