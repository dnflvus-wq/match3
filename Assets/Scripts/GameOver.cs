using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Match3
{
    public class GameOver : MonoBehaviour
    {
        public GameObject screenParent;
        public GameObject scoreParent;
        public Text loseText;
        public Text scoreText;
        public Image[] stars;

        private void Start ()
        {
            screenParent.SetActive(false);

            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].enabled = false;
            }

            EnsureCanvasScaler();
        }

        private void EnsureCanvasScaler()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 2160);
            scaler.matchWidthOrHeight = 0.5f;
        }

        public void ShowLose()
        {
            screenParent.SetActive(true);
            scoreParent.SetActive(false);

            Animator animator = GetComponent<Animator>();

            if (animator)
            {
                animator.Play("GameOverShow");
            }

            // 패배 효과음
            if (AudioManager.Instance != null) AudioManager.Instance.PlayLose();

            // 패배 텍스트 펄스 연출
            if (loseText != null)
            {
                StartCoroutine(TextPulse(loseText));
            }
        }

        public void ShowWin(int score, int starCount)
        {
            screenParent.SetActive(true);
            loseText.enabled = false;

            scoreText.text = score.ToString();
            scoreText.enabled = false;

            Animator animator = GetComponent<Animator>();

            if (animator)
            {
                animator.Play("GameOverShow");
            }

            // 승리 효과음
            if (AudioManager.Instance != null) AudioManager.Instance.PlayWin();

            StartCoroutine(ShowWinCoroutine(starCount));
        }

        private IEnumerator ShowWinCoroutine(int starCount)
        {
            yield return new WaitForSeconds(0.5f);

            if (starCount < stars.Length)
            {
                for (int i = 0; i <= starCount; i++)
                {
                    stars[i].enabled = true;

                    // 별 팝 애니메이션
                    StartCoroutine(StarPop(stars[i].transform));

                    if (i > 0)
                    {
                        stars[i - 1].enabled = false;
                    }

                    yield return new WaitForSeconds(0.5f);
                }
            }

            scoreText.enabled = true;

            // 점수 카운트업 연출
            StartCoroutine(ScoreCountUp(scoreText));
        }

        private IEnumerator StarPop(Transform star)
        {
            float duration = 0.3f;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float progress = t / duration;
                float scale = progress < 0.5f
                    ? Mathf.Lerp(0f, 1.3f, progress / 0.5f)
                    : Mathf.Lerp(1.3f, 1f, (progress - 0.5f) / 0.5f);
                star.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            star.localScale = Vector3.one;
        }

        private IEnumerator ScoreCountUp(Text text)
        {
            int targetScore;
            if (!int.TryParse(text.text, out targetScore)) yield break;

            float duration = 0.8f;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                int current = (int)Mathf.Lerp(0, targetScore, t / duration);
                text.text = current.ToString();
                yield return null;
            }
            text.text = targetScore.ToString();
        }

        private IEnumerator TextPulse(Text text)
        {
            float duration = 0.4f;
            RectTransform rt = text.GetComponent<RectTransform>();
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float progress = t / duration;
                float scale = progress < 0.5f
                    ? Mathf.Lerp(0.8f, 1.15f, progress / 0.5f)
                    : Mathf.Lerp(1.15f, 1f, (progress - 0.5f) / 0.5f);
                rt.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        public void OnReplayClicked()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        public void OnDoneClicked()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("LevelSelect");
        }

    }
}
