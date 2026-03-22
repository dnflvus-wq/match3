using UnityEngine;
using UnityEngine.UI;

namespace Match3
{
    public class BoosterUI : MonoBehaviour
    {
        private GameGrid _grid;
        private GameObject _panel;
        private Text _coinText;
        private bool _hammerMode;

        public void Init(GameGrid grid)
        {
            _grid = grid;

            // CoinSystem/BoosterSystem 자동 생성
            if (CoinSystem.Instance == null)
            {
                var coinObj = new GameObject("CoinSystem");
                coinObj.AddComponent<CoinSystem>();
            }
            if (BoosterSystem.Instance == null)
            {
                var boosterObj = new GameObject("BoosterSystem");
                boosterObj.AddComponent<BoosterSystem>();
            }

            CreateUI();

            // 일일 보상 체크
            if (CoinSystem.Instance.CanClaimDailyReward())
            {
                int reward = CoinSystem.Instance.ClaimDailyReward();
                if (reward > 0)
                {
                    ShowToast("Daily Reward: +" + reward + " coins!");
                }
            }
        }

        private void CreateUI()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            _panel = new GameObject("BoosterPanel");
            _panel.transform.SetParent(canvas.transform, false);

            RectTransform panelRT = _panel.AddComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0, 0);
            panelRT.anchorMax = new Vector2(1, 0);
            panelRT.pivot = new Vector2(0.5f, 0);
            panelRT.anchoredPosition = new Vector2(0, 10);
            panelRT.sizeDelta = new Vector2(0, 80);

            // 배경
            Image bg = _panel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.4f);

            // 코인 표시
            _coinText = CreateLabel(_panel, "Coins", new Vector2(-220, 0), 22);
            UpdateCoinText();

            // 부스터 버튼들
            CreateBoosterButton("Hammer", new Vector2(-80, 0), BoosterType.Hammer, OnHammerClick);
            CreateBoosterButton("Shuffle", new Vector2(50, 0), BoosterType.Shuffle, OnShuffleClick);
            CreateBoosterButton("+5", new Vector2(180, 0), BoosterType.ExtraMoves, OnExtraMovesClick);
        }

        private void CreateBoosterButton(string label, Vector2 pos, BoosterType type, UnityEngine.Events.UnityAction action)
        {
            GameObject btnObj = new GameObject("Btn_" + label);
            btnObj.transform.SetParent(_panel.transform, false);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.5f, 0.3f, 0.85f);

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(action);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(110, 70);

            // 아이콘 이미지 로드
            string iconName = type == BoosterType.Hammer ? "hammer_icon" :
                              type == BoosterType.Shuffle ? "shuffle_icon" : "extra_moves_icon";
            Sprite iconSprite = LoadIcon(iconName);

            if (iconSprite != null)
            {
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(btnObj.transform, false);
                Image iconImg = iconObj.AddComponent<Image>();
                iconImg.sprite = iconSprite;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
                RectTransform irt = iconObj.GetComponent<RectTransform>();
                irt.anchorMin = new Vector2(0.05f, 0.15f);
                irt.anchorMax = new Vector2(0.55f, 0.95f);
                irt.offsetMin = Vector2.zero;
                irt.offsetMax = Vector2.zero;
            }

            // 수량 텍스트 (우측)
            GameObject textObj = new GameObject("Count");
            textObj.transform.SetParent(btnObj.transform, false);
            Text text = textObj.AddComponent<Text>();
            int count = BoosterSystem.Instance != null ? BoosterSystem.Instance.GetCount(type) : 0;
            text.text = "x" + count;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            Outline textOutline = textObj.AddComponent<Outline>();
            textOutline.effectColor = Color.black;
            textOutline.effectDistance = new Vector2(1, -1);

            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.5f, 0f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
        }

        private Sprite LoadIcon(string name)
        {
            Texture2D tex = Resources.Load<Texture2D>(name);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        private Text CreateLabel(GameObject parent, string label, Vector2 pos, int size)
        {
            GameObject obj = new GameObject(label);
            obj.transform.SetParent(parent.transform, false);
            Text text = obj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.9f, 0.2f);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(150, 40);
            return text;
        }

        private void UpdateCoinText()
        {
            if (_coinText != null && CoinSystem.Instance != null)
                _coinText.text = "Coins: " + CoinSystem.Instance.Coins;
        }

        private void RefreshUI()
        {
            if (_panel != null) Destroy(_panel);
            CreateUI();
        }

        private void OnHammerClick()
        {
            if (BoosterSystem.Instance == null) return;

            if (BoosterSystem.Instance.GetCount(BoosterType.Hammer) > 0)
            {
                _hammerMode = true;
                ShowToast("Tap a piece to destroy!");
            }
            else
            {
                if (BoosterSystem.Instance.BuyBooster(BoosterType.Hammer))
                {
                    ShowToast("Hammer purchased!");
                    RefreshUI();
                }
                else
                {
                    ShowToast("Not enough coins!");
                }
            }
        }

        private void OnShuffleClick()
        {
            if (BoosterSystem.Instance == null || _grid == null) return;

            if (BoosterSystem.Instance.UseBooster(BoosterType.Shuffle))
            {
                _grid.ForceShuffleBoard();
                ShowToast("Board shuffled!");
                RefreshUI();
            }
            else if (BoosterSystem.Instance.BuyBooster(BoosterType.Shuffle))
            {
                ShowToast("Shuffle purchased!");
                RefreshUI();
            }
            else
            {
                ShowToast("Not enough coins!");
            }
        }

        private void OnExtraMovesClick()
        {
            if (BoosterSystem.Instance == null) return;

            if (BoosterSystem.Instance.UseBooster(BoosterType.ExtraMoves))
            {
                // LevelMoves에 +5 이동 추가
                var levelMoves = FindFirstObjectByType<LevelMoves>();
                if (levelMoves != null)
                {
                    levelMoves.numMoves += 5;
                    levelMoves.OnMove(); // UI 업데이트 트리거 (이동 안 소모)
                    // 실제로는 movesUsed를 줄여야 하므로 반영을 위해 -1 처리
                }
                ShowToast("+5 Moves!");
                RefreshUI();
            }
            else if (BoosterSystem.Instance.BuyBooster(BoosterType.ExtraMoves))
            {
                ShowToast("+5 Moves purchased!");
                RefreshUI();
            }
            else
            {
                ShowToast("Not enough coins!");
            }
        }

        public bool IsHammerMode => _hammerMode;

        public void UseHammer()
        {
            if (BoosterSystem.Instance != null)
            {
                BoosterSystem.Instance.UseBooster(BoosterType.Hammer);
                RefreshUI();
            }
            _hammerMode = false;
        }

        private void ShowToast(string message)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            GameObject obj = new GameObject("Toast");
            obj.transform.SetParent(canvas.transform, false);

            Text text = obj.AddComponent<Text>();
            text.text = message;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 30;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, -50);
            rt.sizeDelta = new Vector2(500, 50);

            StartCoroutine(ToastFade(obj, text));
        }

        private System.Collections.IEnumerator ToastFade(GameObject obj, Text text)
        {
            yield return new WaitForSeconds(1.5f);
            float duration = 0.5f;
            Color startColor = text.color;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                text.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t / duration);
                yield return null;
            }
            Destroy(obj);
        }
    }
}
