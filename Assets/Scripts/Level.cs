using System.Collections;
using UnityEngine;

namespace Match3
{
    public class Level : MonoBehaviour
    {
        public GameGrid gameGrid;
        public Hud hud;

        public int score1Star;
        public int score2Star;
        public int score3Star;    

        protected LevelType type;

        protected int currentScore;

        private bool _didWin;

        private void Start()
        {
            hud.SetScore(currentScore);
        }

        public LevelType Type => type;

        protected virtual void GameWin()
        {
            gameGrid.GameOver();
            _didWin = true;

            // 승리 시 코인 보상
            if (CoinSystem.Instance != null)
            {
                int coinReward = 10 + currentScore / 100;
                CoinSystem.Instance.AddCoins(coinReward);
            }

            // 승리 시 부스터 보상 (랜덤)
            if (BoosterSystem.Instance != null && Random.value > 0.5f)
            {
                BoosterSystem.Instance.AddBooster((BoosterType)Random.Range(0, 3));
            }

            StartCoroutine(WaitForGridFill());
        }

        protected virtual void GameLose()
        {        
            gameGrid.GameOver();
            _didWin = false;
            StartCoroutine(WaitForGridFill());
        }
    
        public virtual void OnMove()
        {
        }

        public virtual void OnPieceCleared(GamePiece piece)
        {
            currentScore += piece.score;
            hud.SetScore(currentScore);
        }

        protected virtual IEnumerator WaitForGridFill()
        {
            while (gameGrid.IsFilling)
            {
                yield return null;
            }

            if (_didWin)
            {
                hud.OnGameWin(currentScore);
            }
            else
            {
                hud.OnGameLose();
            }
        }
    }
}
