using System;

namespace Match3
{
    /// <summary>
    /// FSM 상태 관리. 순수 C# — UnityEngine 의존 없음.
    /// 상태: PREGAME → READY → SWAPPING → EVALUATING → MATCHING → COLLAPSING → READY / ENDGAME
    /// 입력은 READY 상태에서만 허용.
    /// </summary>
    public class GameStateMachine
    {
        public GameState Current { get; private set; } = GameState.PREGAME;

        public event Action<GameState, GameState> OnStateChanged;

        public bool IsReady => Current == GameState.READY;

        public void SetState(GameState newState)
        {
            if (Current == newState) return;
            var prev = Current;
            Current = newState;
            OnStateChanged?.Invoke(prev, newState);
        }
    }
}
