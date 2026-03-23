using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 게임 상태를 관리하는 유한 상태 머신 (FSM).
    /// 상태 전환 시 GameEvents.OnStateChanged를 발행합니다.
    ///
    /// 상태 흐름:
    /// PREGAME → READY → SWAPPING → EVALUATING → MATCHING → COLLAPSING → (재귀) → READY
    ///                                                                           → ENDGAME
    /// READY → SHUFFLING → READY
    /// </summary>
    public class GameStateMachine
    {
        private GameState _currentState = GameState.PREGAME;

        public GameState CurrentState => _currentState;

        /// <summary>READY 상태에서만 플레이어 입력을 허용</summary>
        public bool CanAcceptInput => _currentState == GameState.READY;

        /// <summary>상태를 전환합니다. 이벤트를 발행합니다.</summary>
        public void TransitionTo(GameState newState)
        {
            if (_currentState == newState) return;

            var oldState = _currentState;
            _currentState = newState;

            GameEvents.InvokeStateChanged(newState);

#if UNITY_EDITOR
            Debug.Log($"[FSM] {oldState} → {newState}");
#endif
        }

        /// <summary>현재 상태를 초기화합니다.</summary>
        public void Reset()
        {
            _currentState = GameState.PREGAME;
        }
    }
}
