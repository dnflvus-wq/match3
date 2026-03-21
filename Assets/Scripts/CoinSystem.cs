using UnityEngine;

namespace Match3
{
    public class CoinSystem : MonoBehaviour
    {
        public static CoinSystem Instance { get; private set; }

        private const string COIN_KEY = "Coins";
        private const string DAILY_KEY = "LastDailyReward";
        public const int DAILY_REWARD = 100;

        public int Coins { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Coins = PlayerPrefs.GetInt(COIN_KEY, 200); // 시작 200코인
        }

        public void AddCoins(int amount)
        {
            Coins += amount;
            Save();
        }

        public bool SpendCoins(int amount)
        {
            if (Coins < amount) return false;
            Coins -= amount;
            Save();
            return true;
        }

        private void Save()
        {
            PlayerPrefs.SetInt(COIN_KEY, Coins);
            PlayerPrefs.Save();
        }

        public bool CanClaimDailyReward()
        {
            string lastClaim = PlayerPrefs.GetString(DAILY_KEY, "");
            if (string.IsNullOrEmpty(lastClaim)) return true;

            System.DateTime lastDate;
            if (System.DateTime.TryParse(lastClaim, out lastDate))
            {
                return (System.DateTime.Now - lastDate).TotalHours >= 24;
            }
            return true;
        }

        public int ClaimDailyReward()
        {
            if (!CanClaimDailyReward()) return 0;

            AddCoins(DAILY_REWARD);
            PlayerPrefs.SetString(DAILY_KEY, System.DateTime.Now.ToString("o"));
            PlayerPrefs.Save();
            return DAILY_REWARD;
        }
    }
}
