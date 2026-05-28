using System.Collections;
using UnityEngine;

namespace Match3
{
    /// <summary>게임 오케스트레이터 (Controller). 비즈니스 로직 → BoardModel, 시각 → BoardView.</summary>
    public class GameGrid : MonoBehaviour
    {
        /// <summary>피스 타입과 프리팹 매핑.</summary>
        [System.Serializable]
        public struct PiecePrefab { public PieceType type; public GameObject prefab; }
        /// <summary>초기 배치용 피스 위치 정보.</summary>
        [System.Serializable]
        public struct PiecePosition { public PieceType type; public int x; public int y; }

        [Header("Level Config (SO 우선)")]
        [SerializeField] private LevelConfig _levelConfig;

        /// <summary>보드 가로 크기.</summary>
        public int xDim;
        /// <summary>보드 세로 크기.</summary>
        public int yDim;
        /// <summary>피스 낙하 1스텝 소요 시간(초).</summary>
        public float fillTime = 0.05f;
        [Header("Animation Timing")]
        /// <summary>스왑 애니메이션 소요 시간(초).</summary>
        public float swapTime = 0.25f;
        /// <summary>스왑 실패 시 되돌리기 애니메이션 시간(초).</summary>
        public float swapBackTime = 0.2f;
        /// <summary>현재 레벨 스크립트 (LevelMoves/LevelTimer/LevelObstacles).</summary>
        public Level level;
        /// <summary>피스 타입별 프리팹 배열 (Inspector 설정).</summary>
        public PiecePrefab[] piecePrefabs;
        /// <summary>배경 타일 프리팹.</summary>
        public GameObject backgroundPrefab;
        /// <summary>레벨 시작 시 고정 배치할 피스 목록.</summary>
        public PiecePosition[] initialPieces;

        private BoardModel _model;
        private BoardView _view;
        private InputController _inputController;
        private BoosterUI _boosterUI;

        private readonly GameStateMachine _fsm = new GameStateMachine();
        /// <summary>현재 게임 상태 (FSM).</summary>
        public GameState CurrentState => _fsm.Current;
        /// <summary>게임 상태를 전환한다.</summary>
        public void SetState(GameState state) => _fsm.SetState(state);
        private bool _gameOver;
        /// <summary>게임 오버 여부.</summary>
        public bool GameIsOver => _gameOver;

        private GamePiece _swapPiece1, _swapPiece2;
        /// <summary>직전 스왑의 첫 번째 피스.</summary>
        public GamePiece SwapPiece1 => _swapPiece1;
        /// <summary>직전 스왑의 두 번째 피스.</summary>
        public GamePiece SwapPiece2 => _swapPiece2;
        /// <summary>현재 연쇄(콤보) 횟수.</summary>
        public int ComboCount { get; set; }
        /// <summary>낙하+매치 시퀀스 진행 중 여부.</summary>
        public bool IsFilling => _model != null && _model.IsFilling;

        private void Awake()
        {
            if (_levelConfig != null)
            {
                xDim = _levelConfig.width;
                yDim = _levelConfig.height;
                fillTime = _levelConfig.fillTime;
            }

            _model = GetComponent<BoardModel>();
            _view = GetComponent<BoardView>();
            _inputController = GetComponent<InputController>();
            if (_model == null) _model = gameObject.AddComponent<BoardModel>();
            if (_view == null) _view = gameObject.AddComponent<BoardView>();
            if (_inputController == null) _inputController = gameObject.AddComponent<InputController>();

            _view.Init(this, _model);
            _model.Init(this);

            if (Camera.main != null && Camera.main.GetComponent<MobileCameraSetup>() == null)
                Camera.main.gameObject.AddComponent<MobileCameraSetup>();
            if (AudioManager.Instance == null)
                new GameObject("AudioManager").AddComponent<AudioManager>();
            if (MatchParticles.Instance == null)
                new GameObject("MatchParticles").AddComponent<MatchParticles>();

            StartCoroutine(StartSequence());
        }

        /// <summary>두 피스의 스왑을 시도한다. READY 상태에서만 동작.</summary>
        public void SwapPieces(GamePiece piece1, GamePiece piece2)
        {
            if (_gameOver || CurrentState != GameState.READY) return;
            if (!piece1.IsMovable() || !piece2.IsMovable()) return;
            _swapPiece1 = piece1;
            _swapPiece2 = piece2;
            StartCoroutine(_view.SwapPiecesSequence(piece1, piece2));
        }

        /// <summary>지정 좌표의 피스를 반환한다.</summary>
        public GamePiece GetPieceAt(int x, int y) => _model.GetPieceAt(x, y);

        /// <summary>두 피스가 상하좌우 인접한지 판정한다.</summary>
        public static bool IsAdjacent(GamePiece p1, GamePiece p2) =>
            (p1.X == p2.X && Mathf.Abs(p1.Y - p2.Y) == 1) ||
            (p1.Y == p2.Y && Mathf.Abs(p1.X - p2.X) == 1);

        /// <summary>망치 부스터 탭 처리. 망치 모드가 아니면 false.</summary>
        public bool TryHandleHammerTap(Vector2 worldPos)
        {
            if (_boosterUI == null || !_boosterUI.IsHammerMode) return false;
            if (_model.HandleHammerTap(worldPos, _boosterUI))
                StartCoroutine(_view.FillSequence());
            return true;
        }

        /// <summary>힌트 표시를 시도한다.</summary>
        public void TryShowHint() => _view.TryShowHint();
        /// <summary>힌트 애니메이션을 중지한다.</summary>
        public void StopHint() => _view.StopHint();
        /// <summary>게임 오버 처리.</summary>
        public void GameOver() { _gameOver = true; _fsm.SetState(GameState.ENDGAME); }
        /// <summary>보드 셔플을 강제 실행한다.</summary>
        public void ForceShuffleBoard() => StartCoroutine(_view.ShuffleBoardSequence());

        /// <summary>지정 타입의 모든 피스를 반환한다 (LevelObstacles용).</summary>
        public System.Collections.Generic.List<GamePiece> GetPiecesOfType(PieceType type) => _model.GetPiecesOfType(type);

        private IEnumerator StartSequence()
        {
            yield return StartCoroutine(_view.FillSequence(setReadyOnComplete: false));
            _fsm.SetState(GameState.PREGAME);
            var camSetup = Camera.main != null ? Camera.main.GetComponent<MobileCameraSetup>() : null;
            if (camSetup != null) camSetup.Adjust();

            var boosterObj = new GameObject("BoosterUI");
            _boosterUI = boosterObj.AddComponent<BoosterUI>();
            _boosterUI.Init(this);

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                for (int i = 3; i >= 1; i--)
                    yield return StartCoroutine(_view.ShowCountdownNumber(canvas, i.ToString()));
                yield return StartCoroutine(_view.ShowCountdownNumber(canvas, "GO!"));
            }
            _fsm.SetState(GameState.READY);
            _inputController.ResetHintTimer();
        }
    }
}
