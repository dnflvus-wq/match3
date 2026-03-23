using System;
using System.Collections.Generic;
using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 중앙 이벤트 버스 — Model과 View 사이의 통신을 담당합니다.
    /// Model은 이벤트를 발행(Invoke)하고, View는 구독(Subscribe)합니다.
    /// </summary>
    public static class GameEvents
    {
        // === Board Events ===
        public static event Action<List<Vector2Int>> OnMatchFound;
        public static event Action<Vector2Int, Vector2Int> OnTilesSwapped;
        public static event Action<Vector2Int, Vector2Int> OnSwapFailed;
        public static event Action<int, int> OnPieceCleared; // x, y
        public static event Action<List<Vector2Int>, List<Vector2Int>> OnBoardCollapsed; // drops, spawns

        // === Score Events ===
        public static event Action<int> OnScoreChanged;
        public static event Action<int> OnComboTriggered;
        public static event Action<Vector3, int> OnScorePopup; // worldPos, score

        // === Game State Events ===
        public static event Action<GameState> OnStateChanged;
        public static event Action OnGameWin;
        public static event Action OnGameLose;

        // === Hint Events ===
        public static event Action<List<Vector2Int>> OnHintShow;
        public static event Action OnHintHide;

        // === Booster Events ===
        public static event Action<Vector2Int> OnHammerUsed;
        public static event Action OnShuffleUsed;

        // === 발행 메서드 ===
        public static void InvokeMatchFound(List<Vector2Int> positions) => OnMatchFound?.Invoke(positions);
        public static void InvokeTilesSwapped(Vector2Int from, Vector2Int to) => OnTilesSwapped?.Invoke(from, to);
        public static void InvokeSwapFailed(Vector2Int from, Vector2Int to) => OnSwapFailed?.Invoke(from, to);
        public static void InvokePieceCleared(int x, int y) => OnPieceCleared?.Invoke(x, y);
        public static void InvokeScoreChanged(int score) => OnScoreChanged?.Invoke(score);
        public static void InvokeComboTriggered(int combo) => OnComboTriggered?.Invoke(combo);
        public static void InvokeScorePopup(Vector3 pos, int score) => OnScorePopup?.Invoke(pos, score);
        public static void InvokeStateChanged(GameState state) => OnStateChanged?.Invoke(state);
        public static void InvokeGameWin() => OnGameWin?.Invoke();
        public static void InvokeGameLose() => OnGameLose?.Invoke();
        public static void InvokeHintShow(List<Vector2Int> positions) => OnHintShow?.Invoke(positions);
        public static void InvokeHintHide() => OnHintHide?.Invoke();

        /// <summary>씬 전환 시 모든 구독 해제</summary>
        public static void ClearAll()
        {
            OnMatchFound = null;
            OnTilesSwapped = null;
            OnSwapFailed = null;
            OnPieceCleared = null;
            OnBoardCollapsed = null;
            OnScoreChanged = null;
            OnComboTriggered = null;
            OnScorePopup = null;
            OnStateChanged = null;
            OnGameWin = null;
            OnGameLose = null;
            OnHintShow = null;
            OnHintHide = null;
            OnHammerUsed = null;
            OnShuffleUsed = null;
        }
    }
}
