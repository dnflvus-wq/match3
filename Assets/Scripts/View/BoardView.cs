using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Match3
{
    /// <summary>
    /// 시각 효과 + 애니메이션 시퀀스 전용. 비즈니스 로직 없음.
    /// 코루틴(Fill, Swap, Shuffle) 타이밍 제어 담당.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        private const float MatchShakeMagnitude = 0.05f;
        private const float MatchShakeDuration = 0.2f;
        private const float HammerShakeMagnitude = 0.03f;
        private const float HammerShakeDuration = 0.15f;
        private const float RainbowShakeMagnitude = 0.1f;
        private const float RainbowShakeDuration = 0.4f;
        private const float ShuffleDelay = 0.3f;
        private const int MaxShuffleRetries = 3;
        private const float HintPulseSpeed = 1.5f;
        private const float HintAlphaMin = 0.3f;
        private const float HintScaleMax = 1.1f;
        private const float CountdownDuration = 0.6f;
        private const float ScorePopupDuration = 0.8f;
        private const float ScorePopupDistance = 60f;
        private const float ComboFadeDuration = 1.2f;
        private const float ComboMoveDistance = 80f;
        private const float FillTime = 0.05f;

        private BoardModel _model;
        private GameGrid _controller;
        private List<GamePiece> _hintPieces;
        private Coroutine _hintCoroutine;

        /// <summary>컨트롤러·모델 참조를 설정하고 이벤트를 구독한다.</summary>
        public void Init(GameGrid controller, BoardModel model)
        {
            _controller = controller;
            _model = model;
            _model.OnScorePopup += ShowScorePopup;
            _model.OnPieceCleared += HandlePieceCleared;
            _model.OnMatchFound += HandleMatchFound;
            _model.OnBigMatch += HandleBigMatch;
            _model.OnHammerHit += HandleHammerHit;
        }

        private void HandlePieceCleared(Vector3 pos, Color color)
        {
            if (MatchParticles.Instance != null) MatchParticles.Instance.PlayAt(pos, color);
        }

        private void HandleMatchFound(int combo)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayMatch(combo);
        }

        private void HandleBigMatch(Vector3 center, Color color)
        {
            StartCoroutine(CameraShake(MatchShakeMagnitude, MatchShakeDuration));
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySpecial();
            if (MatchParticles.Instance != null) MatchParticles.Instance.PlayBigAt(center, color);
        }

        private void HandleHammerHit()
        {
            StartCoroutine(CameraShake(HammerShakeMagnitude, HammerShakeDuration));
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySpecial();
        }

        /// <summary>ColorType을 렌더링용 Color로 변환한다.</summary>
        public static Color GetColorForType(ColorType type)
        {
            switch (type)
            {
                case ColorType.Yellow: return new Color(1f, 0.85f, 0.1f);
                case ColorType.Purple: return new Color(0.7f, 0.2f, 0.9f);
                case ColorType.Red:    return new Color(1f, 0.2f, 0.2f);
                case ColorType.Blue:   return new Color(0.2f, 0.4f, 1f);
                case ColorType.Green:  return new Color(0.2f, 0.9f, 0.3f);
                case ColorType.Pink:   return new Color(1f, 0.4f, 0.7f);
                default: return Color.white;
            }
        }

        // ============================================================
        //  애니메이션 시퀀스 (코루틴) — BoardModel에서 이관
        // ============================================================

        /// <summary>낙하 → 매치 → 리필 반복 시퀀스. 콤보 텍스트·셔플 포함.</summary>
        /// <param name="setReadyOnComplete">완료 시 READY 상태로 전환할지 여부. 초기 시퀀스에서는 false.</param>
        public IEnumerator FillSequence(bool setReadyOnComplete = true)
        {
            bool needsRefill = true;
            _model.IsFilling = true;
            _controller.SetState(GameState.COLLAPSING);
            _controller.ComboCount = 0;

            while (needsRefill)
            {
                yield return new WaitForSeconds(FillTime);
                while (_model.FillStep())
                {
                    _model.ToggleInverse();
                    yield return new WaitForSeconds(FillTime);
                }

                needsRefill = _model.ClearAllValidMatches();
                if (needsRefill)
                {
                    _controller.ComboCount++;
                    if (_controller.ComboCount >= 2)
                    {
                        ShowComboText(_controller.ComboCount);
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayCombo();
                    }
                }
            }

            _model.IsFilling = false;

            if (_controller.GameIsOver)
            {
                _controller.SetState(GameState.ENDGAME);
            }
            else if (setReadyOnComplete)
            {
                int shuffleAttempts = 0;
                while (MatchFinder.FindValidMove(_model) == null && shuffleAttempts < MaxShuffleRetries)
                {
                    yield return StartCoroutine(ShuffleBoardSequence());
                    shuffleAttempts++;
                }

                _controller.SetState(GameState.READY);
                var ic = _controller.GetComponent<InputController>();
                if (ic != null) ic.ResetHintTimer();
            }
        }

        /// <summary>스왑 애니메이션 → 매치 평가 → 성공 시 Fill / 실패 시 되돌리기.</summary>
        public IEnumerator SwapPiecesSequence(GamePiece piece1, GamePiece piece2)
        {
            _controller.SetState(GameState.SWAPPING);

            int p1X = piece1.X, p1Y = piece1.Y;
            int p2X = piece2.X, p2Y = piece2.Y;

            _model.SwapData(piece1, piece2);

            if (AudioManager.Instance != null) AudioManager.Instance.PlaySwap();

            piece1.MovableComponent.MoveVisual(p2X, p2Y, _controller.swapTime, useArc: true);
            piece2.MovableComponent.MoveVisual(p1X, p1Y, _controller.swapTime, useArc: false);
            yield return new WaitForSeconds(_controller.swapTime);

            _controller.SetState(GameState.EVALUATING);

            var result = _model.EvaluateSwap(piece1, piece2);

            if (result == SwapResult.RainbowDouble)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySpecial();
                StartCoroutine(CameraShake(RainbowShakeMagnitude, RainbowShakeDuration));
                StartCoroutine(FillSequence());
                _controller.level.OnMove();
                yield break;
            }

            if (result == SwapResult.Match)
            {
                _controller.SetState(GameState.MATCHING);
                StartCoroutine(FillSequence());
                _controller.level.OnMove();
            }
            else
            {
                _model.UndoSwapData(piece1, piece2);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayFail();
                piece1.MovableComponent.MoveVisual(p1X, p1Y, _controller.swapBackTime);
                piece2.MovableComponent.MoveVisual(p2X, p2Y, _controller.swapBackTime);
                yield return new WaitForSeconds(_controller.swapBackTime);
                _controller.SetState(GameState.READY);
            }
        }

        /// <summary>보드 셔플 애니메이션 시퀀스.</summary>
        public IEnumerator ShuffleBoardSequence()
        {
            _controller.SetState(GameState.SHUFFLING);
            _model.ShuffleColors();
            yield return new WaitForSeconds(ShuffleDelay);

            if (_controller.CurrentState == GameState.SHUFFLING)
            {
                _controller.SetState(GameState.READY);
                var ic = _controller.GetComponent<InputController>();
                if (ic != null) ic.ResetHintTimer();
            }
        }

        // ============================================================
        //  힌트
        // ============================================================

        /// <summary>힌트가 아직 표시되지 않았으면 힌트를 시작한다.</summary>
        public void TryShowHint()
        {
            if (_hintCoroutine == null) ShowHint();
        }

        private void ShowHint()
        {
            var move = MatchFinder.FindValidMove(_model);
            if (move == null) { _hintPieces = null; return; }
            var (x1, y1, x2, y2) = move.Value;
            _hintPieces = new List<GamePiece> { _model.GetPieceAt(x1, y1), _model.GetPieceAt(x2, y2) };
            _hintCoroutine = StartCoroutine(HintPulseCoroutine());
        }

        /// <summary>힌트 펄스 애니메이션을 중지하고 피스를 원래 상태로 복원한다.</summary>
        public void StopHint()
        {
            if (_hintCoroutine != null) { StopCoroutine(_hintCoroutine); _hintCoroutine = null; }
            if (_hintPieces != null)
            {
                foreach (var piece in _hintPieces)
                {
                    if (piece == null || piece.gameObject == null) continue;
                    piece.transform.localScale = Vector3.one;
                    var sr = piece.transform.Find("piece")?.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f);
                }
                _hintPieces = null;
            }
        }

        private IEnumerator HintPulseCoroutine()
        {
            float elapsed = 0f;
            while (true)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Sin(elapsed * HintPulseSpeed * Mathf.PI * 2f) * 0.5f + 0.5f;
                float alpha = Mathf.Lerp(HintAlphaMin, 1f, t);
                float scale = Mathf.Lerp(1f, HintScaleMax, t);

                if (_hintPieces != null)
                {
                    foreach (var piece in _hintPieces)
                    {
                        if (piece == null || piece.gameObject == null) continue;
                        piece.transform.localScale = new Vector3(scale, scale, 1f);
                        var sr = piece.transform.Find("piece")?.GetComponent<SpriteRenderer>();
                        if (sr != null) sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
                    }
                }
                yield return null;
            }
        }

        // ============================================================
        //  카메라 쉐이크
        // ============================================================

        /// <summary>카메라 흔들림 효과를 재생한다.</summary>
        private IEnumerator CameraShake(float magnitude, float duration)
        {
            var cam = Camera.main;
            if (cam == null) yield break;
            Vector3 orig = cam.transform.position;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                cam.transform.position = new Vector3(
                    orig.x + Random.Range(-1f, 1f) * magnitude,
                    orig.y + Random.Range(-1f, 1f) * magnitude, orig.z);
                yield return null;
            }
            cam.transform.position = orig;
        }

        // ============================================================
        //  카운트다운
        // ============================================================

        /// <summary>카운트다운 숫자를 스케일+페이드 애니메이션으로 표시한다.</summary>
        public IEnumerator ShowCountdownNumber(Canvas canvas, string text)
        {
            var obj = new GameObject("Countdown");
            obj.transform.SetParent(canvas.transform, false);

            var uiText = obj.AddComponent<Text>();
            uiText.text = text;
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiText.fontSize = 100;
            uiText.fontStyle = FontStyle.Bold;
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.color = Color.white;

            var outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.8f);
            outline.effectDistance = new Vector2(3, -3);

            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(300, 200);

            float duration = CountdownDuration;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float p = t / duration;
                float scale, alpha;
                if (p < 0.2f) { scale = Mathf.Lerp(2f, 1f, p / 0.2f); alpha = Mathf.Lerp(0f, 1f, p / 0.2f); }
                else if (p < 0.7f) { scale = 1f; alpha = 1f; }
                else { scale = 1f; alpha = Mathf.Lerp(1f, 0f, (p - 0.7f) / 0.3f); }
                rt.localScale = new Vector3(scale, scale, 1f);
                uiText.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }
            Destroy(obj);
        }

        // ============================================================
        //  점수 팝업
        // ============================================================

        /// <summary>월드 좌표에 점수 팝업(+N)을 표시한다.</summary>
        private void ShowScorePopup(Vector3 worldPos, int score)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            var obj = new GameObject("ScorePopup");
            obj.transform.SetParent(canvas.transform, false);

            var text = obj.AddComponent<Text>();
            text.text = "+" + score;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 32;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            obj.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);

            var rt = obj.GetComponent<RectTransform>();
            Vector2 sp = Camera.main.WorldToScreenPoint(worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(), sp, canvas.worldCamera, out Vector2 lp);
            rt.anchoredPosition = lp;
            rt.sizeDelta = new Vector2(200, 50);

            StartCoroutine(FadeUp(obj, text, ScorePopupDuration, ScorePopupDistance));
        }

        // ============================================================
        //  콤보 텍스트
        // ============================================================

        /// <summary>콤보 텍스트(xN COMBO!)를 화면 중앙에 표시한다.</summary>
        private void ShowComboText(int combo)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            var obj = new GameObject("ComboText");
            obj.transform.SetParent(canvas.transform, false);

            var text = obj.AddComponent<Text>();
            text.text = "x" + combo + " COMBO!";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 60;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.9f, 0.1f);

            var outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color(0.8f, 0.2f, 0f);
            outline.effectDistance = new Vector2(3, -3);

            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 100);
            rt.sizeDelta = new Vector2(500, 100);

            StartCoroutine(ComboFade(obj, text));
        }

        private IEnumerator FadeUp(GameObject obj, Text text, float duration, float dist)
        {
            var rt = obj.GetComponent<RectTransform>();
            Vector2 start = rt.anchoredPosition;
            Color sc = text.color;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float p = t / duration;
                rt.anchoredPosition = start + new Vector2(0, p * dist);
                text.color = new Color(sc.r, sc.g, sc.b, 1f - p);
                yield return null;
            }
            Destroy(obj);
        }

        private IEnumerator ComboFade(GameObject obj, Text text)
        {
            var rt = obj.GetComponent<RectTransform>();
            Vector2 start = rt.anchoredPosition;
            Color sc = text.color;
            for (float t = 0; t < ComboFadeDuration; t += Time.deltaTime)
            {
                float p = t / ComboFadeDuration;
                rt.anchoredPosition = start + new Vector2(0, p * ComboMoveDistance);
                text.color = new Color(sc.r, sc.g, sc.b, 1f - p);
                float scale = p < 0.15f ? Mathf.Lerp(0.5f, 1.2f, p / 0.15f) :
                              p < 0.3f ? Mathf.Lerp(1.2f, 1f, (p - 0.15f) / 0.15f) : 1f;
                rt.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            Destroy(obj);
        }
    }
}
