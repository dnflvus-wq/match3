using UnityEngine;

namespace Match3
{
    public enum BoosterType
    {
        Hammer,     // 타일 1개 파괴
        Shuffle,    // 보드 셔플
        ExtraMoves  // +5 이동
    }

    public class BoosterSystem : MonoBehaviour
    {
        public static BoosterSystem Instance { get; private set; }

        public const int HAMMER_COST = 50;
        public const int SHUFFLE_COST = 30;
        public const int EXTRA_MOVES_COST = 80;

        private const string HAMMER_KEY = "Booster_Hammer";
        private const string SHUFFLE_KEY = "Booster_Shuffle";
        private const string EXTRA_KEY = "Booster_Extra";

        public int HammerCount { get; private set; }
        public int ShuffleCount { get; private set; }
        public int ExtraMovesCount { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            HammerCount = PlayerPrefs.GetInt(HAMMER_KEY, 3);
            ShuffleCount = PlayerPrefs.GetInt(SHUFFLE_KEY, 3);
            ExtraMovesCount = PlayerPrefs.GetInt(EXTRA_KEY, 2);
        }

        public int GetCount(BoosterType type)
        {
            switch (type)
            {
                case BoosterType.Hammer: return HammerCount;
                case BoosterType.Shuffle: return ShuffleCount;
                case BoosterType.ExtraMoves: return ExtraMovesCount;
                default: return 0;
            }
        }

        public int GetCost(BoosterType type)
        {
            switch (type)
            {
                case BoosterType.Hammer: return HAMMER_COST;
                case BoosterType.Shuffle: return SHUFFLE_COST;
                case BoosterType.ExtraMoves: return EXTRA_MOVES_COST;
                default: return 0;
            }
        }

        public bool UseBooster(BoosterType type)
        {
            switch (type)
            {
                case BoosterType.Hammer:
                    if (HammerCount <= 0) return false;
                    HammerCount--;
                    break;
                case BoosterType.Shuffle:
                    if (ShuffleCount <= 0) return false;
                    ShuffleCount--;
                    break;
                case BoosterType.ExtraMoves:
                    if (ExtraMovesCount <= 0) return false;
                    ExtraMovesCount--;
                    break;
            }
            Save();
            return true;
        }

        public bool BuyBooster(BoosterType type)
        {
            if (CoinSystem.Instance == null) return false;

            int cost = GetCost(type);
            if (!CoinSystem.Instance.SpendCoins(cost)) return false;

            switch (type)
            {
                case BoosterType.Hammer: HammerCount++; break;
                case BoosterType.Shuffle: ShuffleCount++; break;
                case BoosterType.ExtraMoves: ExtraMovesCount++; break;
            }
            Save();
            return true;
        }

        public void AddBooster(BoosterType type, int count = 1)
        {
            switch (type)
            {
                case BoosterType.Hammer: HammerCount += count; break;
                case BoosterType.Shuffle: ShuffleCount += count; break;
                case BoosterType.ExtraMoves: ExtraMovesCount += count; break;
            }
            Save();
        }

        private void Save()
        {
            PlayerPrefs.SetInt(HAMMER_KEY, HammerCount);
            PlayerPrefs.SetInt(SHUFFLE_KEY, ShuffleCount);
            PlayerPrefs.SetInt(EXTRA_KEY, ExtraMovesCount);
            PlayerPrefs.Save();
        }
    }
}
