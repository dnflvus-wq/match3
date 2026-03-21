using System;
using UnityEngine;

namespace Match3
{
    public class LifeSystem : MonoBehaviour
    {
        public static LifeSystem Instance { get; private set; }

        public int maxLives = 5;
        public int rechargeMinutes = 30;

        public int CurrentLives { get; private set; }
        public float TimeToNextLife { get; private set; }

        private const string LIVES_KEY = "Lives";
        private const string LAST_TIME_KEY = "LastLifeTime";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadLives();
        }

        private void Update()
        {
            if (CurrentLives < maxLives)
            {
                TimeToNextLife -= Time.deltaTime;
                if (TimeToNextLife <= 0f)
                {
                    CurrentLives++;
                    TimeToNextLife = rechargeMinutes * 60f;
                    SaveLives();
                }
            }
        }

        private void LoadLives()
        {
            CurrentLives = PlayerPrefs.GetInt(LIVES_KEY, maxLives);
            string lastTimeStr = PlayerPrefs.GetString(LAST_TIME_KEY, "");

            if (!string.IsNullOrEmpty(lastTimeStr) && CurrentLives < maxLives)
            {
                DateTime lastTime = DateTime.Parse(lastTimeStr);
                double elapsedMinutes = (DateTime.Now - lastTime).TotalMinutes;
                int livesRecovered = (int)(elapsedMinutes / rechargeMinutes);

                CurrentLives = Mathf.Min(CurrentLives + livesRecovered, maxLives);
                double remainingMinutes = elapsedMinutes % rechargeMinutes;
                TimeToNextLife = (float)((rechargeMinutes - remainingMinutes) * 60);
            }
            else
            {
                TimeToNextLife = rechargeMinutes * 60f;
            }

            SaveLives();
        }

        private void SaveLives()
        {
            PlayerPrefs.SetInt(LIVES_KEY, CurrentLives);
            PlayerPrefs.SetString(LAST_TIME_KEY, DateTime.Now.ToString("o"));
            PlayerPrefs.Save();
        }

        public bool UseLive()
        {
            if (CurrentLives <= 0) return false;
            CurrentLives--;
            SaveLives();
            return true;
        }

        public void AddLife(int count = 1)
        {
            CurrentLives = Mathf.Min(CurrentLives + count, maxLives);
            SaveLives();
        }

        public string GetTimeString()
        {
            if (CurrentLives >= maxLives) return "FULL";
            int minutes = (int)(TimeToNextLife / 60);
            int seconds = (int)(TimeToNextLife % 60);
            return $"{minutes}:{seconds:D2}";
        }
    }
}
