using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 레벨별 설정 데이터 — Inspector에서 편집 가능한 ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Match3/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Board")]
        public int width = 6;
        public int height = 6;

        [Header("Rules")]
        public LevelType levelType = LevelType.Moves;
        public int moveLimit = 20;
        public float timeLimit = 90f;
        public int targetScore = 15000;

        [Header("Tiles")]
        [Range(4, 8)]
        public int numColors = 6;

        [Tooltip("각 색상별 스폰 가중치 (비워두면 균등)")]
        public float[] colorWeights;
    }
}
