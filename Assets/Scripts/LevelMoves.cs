using UnityEngine;
using UnityEngine.UI;

namespace Match3
{
    public class LevelMoves : Level
    {

        public int numMoves;
        public int targetScore;

        private int _movesUsed = 0;

        private void Start()
        {
            type = LevelType.Moves;

            hud.SetLevelType(type);
            hud.SetScore(currentScore);
            hud.SetTarget(targetScore);
            hud.SetRemaining(numMoves);
        }

        public void RefreshRemainingUI()
        {
            int remaining = numMoves - _movesUsed;
            hud.SetRemaining(remaining);
        }

        public override void OnMove()
        {
            _movesUsed++;

            int remaining = numMoves - _movesUsed;
            hud.SetRemaining(remaining);

            // 이동수 부족 경고 (5이하)
            if (remaining <= 5 && remaining > 0)
            {
                if (hud.remainingText != null)
                {
                    hud.remainingText.color = remaining <= 3 ? Color.red : new Color(1f, 0.5f, 0f);
                    StartCoroutine(PulseText(hud.remainingText));
                }
            }

            if (remaining != 0) return;

            if (currentScore >= targetScore)
            {
                GameWin();
            }
            else
            {
                GameLose();
            }
        }

        private System.Collections.IEnumerator PulseText(Text text)
        {
            RectTransform rt = text.GetComponent<RectTransform>();
            if (rt == null) yield break;

            Vector3 originalScale = rt.localScale;
            float duration = 0.2f;

            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float progress = t / duration;
                float scale = progress < 0.5f
                    ? Mathf.Lerp(1f, 1.4f, progress / 0.5f)
                    : Mathf.Lerp(1.4f, 1f, (progress - 0.5f) / 0.5f);
                rt.localScale = originalScale * scale;
                yield return null;
            }

            rt.localScale = originalScale;
        }
    }
}
