using UnityEngine;
using UnityEngine.UI;

namespace Match3
{
    public class MobileHUDLayout : MonoBehaviour
    {
        [Tooltip("상단 HUD 높이 (px, 1080x1920 기준)")]
        public float hudHeight = 320f;

        private void Start()
        {
            SetupHUD();
            CreateBackButton();
        }

        private Transform FindDirectChild(string name)
        {
            foreach (Transform child in transform)
                if (child.name == name) return child;
            return null;
        }

        private void SetupHUD()
        {
            // HUD 패널 배경 이미지 로드
            Sprite hudPanelSprite = null;
            var tex = Resources.Load<Texture2D>("hud_panel");
            if (tex != null)
                hudPanelSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

            var scorePanel     = transform.Find("Score");
            var targetPanel    = transform.Find("Target");
            var remainingPanel = FindDirectChild("RemainingMoves/Time");
            var gameOverPanel  = transform.Find("GameOver");

            if (targetPanel    != null) SetupTopPanel(targetPanel,    0f,      1f / 3f, hudPanelSprite);
            if (remainingPanel != null) SetupTopPanel(remainingPanel, 1f / 3f, 2f / 3f, hudPanelSprite);
            if (scorePanel     != null) SetupTopPanel(scorePanel,     2f / 3f, 1f,      hudPanelSprite);

            // 별(stars) 이미지들: GameUICanvas 직접 자식 → Score 패널 내부로 이동
            if (scorePanel != null)
            {
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    var child = transform.GetChild(i);
                    if (child.name.ToLower().Contains("star") && child != scorePanel && child != targetPanel && child != remainingPanel && child != gameOverPanel)
                    {
                        child.SetParent(scorePanel, false);
                        var rt = child.GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            // 나무판 안쪽 평평한 부분, subtext 자리에 작게 배치
                            rt.anchorMin = new Vector2(0.25f, 0.30f);
                            rt.anchorMax = new Vector2(0.75f, 0.46f);
                            rt.pivot = new Vector2(0.5f, 0.5f);
                            rt.sizeDelta = Vector2.zero;
                            rt.anchoredPosition = Vector2.zero;
                            rt.localScale = Vector3.one;
                            var starImg = child.GetComponent<Image>();
                            if (starImg != null) starImg.preserveAspect = true;
                        }
                    }
                }
            }

            if (gameOverPanel != null)
            {
                var rt = gameOverPanel.GetComponent<RectTransform>();
                rt.anchorMin       = Vector2.zero;
                rt.anchorMax       = Vector2.one;
                rt.pivot           = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta       = Vector2.zero;
                StretchAllChildren(rt, 1);
            }
        }

        private void SetupTopPanel(Transform panel, float anchorMinX, float anchorMaxX, Sprite bgSprite = null)
        {
            var rt = panel.GetComponent<RectTransform>();
            float pad = 0.005f;
            rt.anchorMin       = new Vector2(anchorMinX + pad, 1f);
            rt.anchorMax       = new Vector2(anchorMaxX - pad, 1f);
            rt.pivot           = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta       = new Vector2(0f, hudHeight);
            rt.localScale      = Vector3.one;

            // 패널 배경 이미지 적용
            if (bgSprite != null)
            {
                var img = panel.GetComponent<Image>();
                if (img == null) img = panel.gameObject.AddComponent<Image>();
                img.sprite = bgSprite;
                img.type = Image.Type.Sliced;
                img.preserveAspect = false;
            }

            // 패널 경계 클리핑
            if (panel.GetComponent<RectMask2D>() == null)
                panel.gameObject.AddComponent<RectMask2D>();

            StretchAllChildren(rt, 0);
        }

        private void StretchAllChildren(RectTransform parent, int depth)
        {
            foreach (Transform t in parent)
            {
                var child = t as RectTransform;
                if (child == null) continue;

                var text = child.GetComponent<Text>();
                var img  = child.GetComponent<UnityEngine.UI.Image>();
                string childName = child.name.ToLower();

                if (text != null)
                {
                    // 나무판 테두리 안쪽 영역만 사용 (상15%, 하20%, 좌우10%)
                    bool isSub = childName.Contains("sub");
                    if (isSub)
                    {
                        // subtext: 나무판 안쪽 하단 (테두리 안쪽)
                        child.anchorMin = new Vector2(0.12f, 0.28f);
                        child.anchorMax = new Vector2(0.88f, 0.48f);
                        text.resizeTextMaxSize = 28;
                        text.color = new Color(1f, 1f, 0.85f, 1f);
                    }
                    else
                    {
                        // 메인 숫자: 나무판 안쪽 상단 (테두리 아래)
                        child.anchorMin = new Vector2(0.12f, 0.45f);
                        child.anchorMax = new Vector2(0.88f, 0.82f);
                        text.resizeTextMaxSize = 64;
                        text.color = Color.white;
                    }
                    child.pivot            = new Vector2(0.5f, 0.5f);
                    child.sizeDelta        = Vector2.zero;
                    child.anchoredPosition = Vector2.zero;

                    text.resizeTextForBestFit  = true;
                    text.resizeTextMinSize     = 4;
                    text.alignment             = TextAnchor.MiddleCenter;
                    text.horizontalOverflow    = HorizontalWrapMode.Wrap;
                    text.verticalOverflow      = VerticalWrapMode.Truncate;

                    // 텍스트에 그림자 추가 (가독성)
                    var shadow = child.GetComponent<Shadow>();
                    if (shadow == null) shadow = child.gameObject.AddComponent<Shadow>();
                    shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
                    shadow.effectDistance = new Vector2(1f, -1f);
                }
                else if (img != null)
                {
                    // 별(stars) 이미지 → 완전히 패널 안에 고정
                    if (childName.Contains("star"))
                    {
                        // 별을 패널 하단 중앙에 작게 배치
                        child.anchorMin = new Vector2(0.15f, 0.10f);
                        child.anchorMax = new Vector2(0.85f, 0.38f);
                        child.pivot     = new Vector2(0.5f, 0.5f);
                        child.sizeDelta = Vector2.zero;
                        child.anchoredPosition = Vector2.zero;
                        img.preserveAspect = true;
                    }
                    else
                    {
                        // 일반 이미지(배경): 전체 stretch
                        child.anchorMin        = Vector2.zero;
                        child.anchorMax        = Vector2.one;
                        child.pivot            = new Vector2(0.5f, 0.5f);
                        child.sizeDelta        = Vector2.zero;
                        child.anchoredPosition = Vector2.zero;
                    }
                }

                if (depth > 0) StretchAllChildren(child, depth - 1);
            }
        }

        private void CreateBackButton()
        {
            // 뒤로가기 버튼 (왼쪽 상단, 점수판 아래)
            var btnObj = new GameObject("BackButton");
            btnObj.transform.SetParent(transform, false);

            var rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(20f, -(hudHeight + 10f));
            rt.sizeDelta = new Vector2(120f, 120f);

            // 뒤로가기 아이콘 이미지
            var img = btnObj.AddComponent<Image>();
            Sprite backSprite = Resources.Load<Sprite>("back_icon");
            if (backSprite == null)
            {
                Texture2D backTex = Resources.Load<Texture2D>("back_icon");
                if (backTex != null && backTex.isReadable)
                    backSprite = Sprite.Create(backTex, new Rect(0, 0, backTex.width, backTex.height), new Vector2(0.5f, 0.5f));
            }
            if (backSprite != null)
            {
                img.sprite = backSprite;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else
            {
                img.color = new Color(0.15f, 0.1f, 0.05f, 0.7f);
                // fallback 텍스트
                var textObj = new GameObject("Text");
                textObj.transform.SetParent(btnObj.transform, false);
                var textRt = textObj.AddComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.sizeDelta = Vector2.zero;
                var text = textObj.AddComponent<Text>();
                text.text = "←";
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 50;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
            }

            // 버튼 클릭 이벤트
            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() =>
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("LevelSelect");
            });
        }
    }
}
