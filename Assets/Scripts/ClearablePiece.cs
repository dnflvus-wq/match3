using System.Collections;
using UnityEngine;

namespace Match3
{
    public class ClearablePiece : MonoBehaviour
    {
        public AnimationClip clearAnimation;

        public bool IsBeingCleared { get; private set; }

        protected GamePiece piece;

        private void Awake()
        {
            piece = GetComponent<GamePiece>();
        }

        public virtual void Clear()
        {
            piece.GameGridRef.level.OnPieceCleared(piece);
            IsBeingCleared = true;
            StartCoroutine(ClearCoroutine());
        }

        private IEnumerator ClearCoroutine()
        {
            var animator = GetComponent<Animator>();

            if (animator && clearAnimation != null)
            {
                animator.Play(clearAnimation.name);
                yield return new WaitForSeconds(clearAnimation.length);
            }
            else
            {
                // Juice 이펙트: 스케일 축소 + 페이드 아웃 (0.15초)
                float duration = 0.15f;
                Vector3 startScale = transform.localScale;
                SpriteRenderer sr = transform.Find("piece")?.GetComponent<SpriteRenderer>();
                Color startColor = sr != null ? sr.color : Color.white;

                for (float t = 0; t < duration; t += Time.deltaTime)
                {
                    float progress = t / duration;
                    transform.localScale = Vector3.Lerp(startScale, Vector3.zero, progress);
                    if (sr != null)
                    {
                        sr.color = new Color(startColor.r, startColor.g, startColor.b, 1f - progress);
                    }
                    yield return null;
                }
            }

            Destroy(gameObject);
        }
    }
}
