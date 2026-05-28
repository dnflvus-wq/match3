using System.Collections;
using UnityEngine;

namespace Match3
{
    public class MovablePiece : MonoBehaviour
    {
        private const float SwapArcZ = -0.1f;
        private const float SquashDuration = 0.1f;
        private const float SquashScaleX = 1.15f;
        private const float SquashScaleY = 0.85f;

        private GamePiece _piece;
        private IEnumerator _moveCoroutine;

        private void Awake()
        {
            _piece = GetComponent<GamePiece>();
        }

        public void Move(int newX, int newY, float time)
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
            }

            _moveCoroutine = MoveCoroutine(newX, newY, time);
            StartCoroutine(_moveCoroutine);
        }

        private IEnumerator MoveCoroutine(int newX, int newY, float time)
        {
            _piece.X = newX;
            _piece.Y = newY;

            Vector3 startPos = transform.position;
            Vector3 endPos = _piece.CalcWorldPosition(newX, newY);

            for (float t = 0; t <= time; t += Time.deltaTime)
            {
                float normalizedT = t / time;
                float easedT = 1f - (1f - normalizedT) * (1f - normalizedT); // Quadratic Ease-Out
                _piece.transform.position = Vector3.Lerp(startPos, endPos, easedT);
                yield return null;
            }

            _piece.transform.position = endPos;
        }

        // 데이터(X,Y)만 설정 (시각 이동 없이)
        public void SetPosition(int newX, int newY)
        {
            _piece.X = newX;
            _piece.Y = newY;
        }

        // 시각 전용 이동 (X,Y 좌표 안 바꿈) — 스왑 애니메이션용
        // useArc=true: Z축 아크 모션으로 겹침 방지
        public void MoveVisual(int targetX, int targetY, float time, bool useArc = false)
        {
            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            _moveCoroutine = MoveVisualCoroutine(targetX, targetY, time, useArc);
            StartCoroutine(_moveCoroutine);
        }

        private IEnumerator MoveVisualCoroutine(int targetX, int targetY, float time, bool useArc)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = _piece.CalcWorldPosition(targetX, targetY);

            for (float t = 0; t <= time; t += Time.deltaTime)
            {
                float normalizedT = t / time;
                float easedT = 1f - (1f - normalizedT) * (1f - normalizedT); // Quadratic Ease-Out
                Vector3 pos = Vector3.Lerp(startPos, endPos, easedT);

                // Z축 아크: 교차 시 겹침 방지 (Sin 곡선으로 앞으로 나왔다 복귀)
                if (useArc)
                {
                    pos.z = Mathf.Sin(normalizedT * Mathf.PI) * SwapArcZ;
                }

                _piece.transform.position = pos;
                yield return null;
            }

            _piece.transform.position = endPos;
        }

        // 낙하 전용 이동 (데이터 변경 + Squash & Stretch)
        public void MoveDrop(int newX, int newY, float time)
        {
            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            _moveCoroutine = MoveDropCoroutine(newX, newY, time);
            StartCoroutine(_moveCoroutine);
        }

        private IEnumerator MoveDropCoroutine(int newX, int newY, float time)
        {
            _piece.X = newX;
            _piece.Y = newY;

            Vector3 startPos = transform.position;
            Vector3 endPos = _piece.CalcWorldPosition(newX, newY);

            // 낙하 (Ease-In: 가속)
            for (float t = 0; t <= time; t += Time.deltaTime)
            {
                float normalizedT = t / time;
                float easedT = normalizedT * normalizedT; // Quadratic Ease-In (가속)
                _piece.transform.position = Vector3.Lerp(startPos, endPos, easedT);
                yield return null;
            }

            _piece.transform.position = endPos;

            // Squash & Stretch: 착지 시 찌그러짐 → 복원
            for (float t = 0; t < SquashDuration; t += Time.deltaTime)
            {
                float progress = t / SquashDuration;
                float scaleX = Mathf.Lerp(SquashScaleX, 1f, progress);
                float scaleY = Mathf.Lerp(SquashScaleY, 1f, progress);
                transform.localScale = new Vector3(scaleX, scaleY, 1f);
                yield return null;
            }

            transform.localScale = Vector3.one;
        }
    }
}

