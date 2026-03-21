using UnityEngine;

namespace Match3
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "Match3/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Grid")]
        public int gridWidth = 8;
        public int gridHeight = 8;

        [Header("Goals")]
        public LevelType levelType = LevelType.Moves;
        public int numMoves = 20;
        public int targetScore = 1000;
        public float timeLimit = 60f;

        [Header("Scoring")]
        public int score1Star = 500;
        public int score2Star = 1000;
        public int score3Star = 2000;

        [Header("Timing")]
        public float fillTime = 0.1f;
        public float swapTime = 0.25f;
        public float swapBackTime = 0.2f;

        [Header("Spawn Weights")]
        [Range(0f, 1f)]
        public float weightBalance = 0.5f; // 0=순수랜덤, 1=강한 균형
    }
}
