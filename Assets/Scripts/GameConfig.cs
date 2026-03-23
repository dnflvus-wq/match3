using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 게임 전체 설정값 — Inspector에서 편집 가능한 ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Match3/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Board")]
        public float fillTime = 0.1f;

        [Header("Animation")]
        public float swapTime = 0.25f;
        public float swapBackTime = 0.2f;

        [Header("Hint")]
        public float hintDelay = 5f;

        [Header("Match")]
        public int matchLength = 3;

        [Header("Input")]
        public float swipeThreshold = 0.3f;
    }
}
