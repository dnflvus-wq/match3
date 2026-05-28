using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 터치/마우스 입력 처리 + 힌트 타이머.
    /// GameGrid(추후 GameGrid)와 같은 GameObject에 부착.
    /// </summary>
    public class InputController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField, Range(0.1f, 1f)]
        private float swipeThreshold = 0.3f;

        [Header("Hint")]
        [SerializeField]
        private float hintDelay = 5f;

        private GameGrid _grid;
        private GamePiece _pressedPiece;
        private GamePiece _enteredPiece;
        private Vector2 _touchStart;
        private bool _swiped;
        private float _hintTimer;

        private void Awake()
        {
            _grid = GetComponent<GameGrid>();
        }

        /// <summary>힌트 타이머를 초기값으로 리셋한다.</summary>
        public void ResetHintTimer() => _hintTimer = hintDelay;

        /// <summary>피스를 누름 상태로 설정한다 (드래그 시작).</summary>
        public void PressPiece(GamePiece piece) => _pressedPiece = piece;

        /// <summary>드래그 중 진입한 피스를 설정한다.</summary>
        public void EnterPiece(GamePiece piece) => _enteredPiece = piece;

        /// <summary>피스 누름을 해제한다. 인접하면 스왑을 시도.</summary>
        public void ReleasePiece()
        {
            if (_pressedPiece != null && _enteredPiece != null
                && GameGrid.IsAdjacent(_pressedPiece, _enteredPiece))
            {
                _grid.SwapPieces(_pressedPiece, _enteredPiece);
            }
            _pressedPiece = null;
            _enteredPiece = null;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("LevelSelect");
                return;
            }

            if (_grid.CurrentState != GameState.READY) return;

            _hintTimer -= Time.deltaTime;
            if (_hintTimer <= 0f)
                _grid.TryShowHint();

            bool began = false, moved = false, ended = false;
            Vector2 screenPos = Vector2.zero;

            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                screenPos = touch.position;
                began = touch.phase == TouchPhase.Began;
                moved = touch.phase == TouchPhase.Moved;
                ended = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            }
            else
            {
                screenPos = Input.mousePosition;
                began = Input.GetMouseButtonDown(0);
                moved = Input.GetMouseButton(0) && !Input.GetMouseButtonDown(0);
                ended = Input.GetMouseButtonUp(0);
            }

            if (!began && !moved && !ended) return;

            var wp = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
            var worldPos = new Vector2(wp.x, wp.y);

            if (began) HandleBegan(worldPos);
            else if (moved) HandleMoved(worldPos);
            else if (ended) HandleEnded();
        }

        private void HandleBegan(Vector2 worldPos)
        {
            if (_grid.TryHandleHammerTap(worldPos)) return;

            _grid.StopHint();
            ResetHintTimer();
            _touchStart = worldPos;
            _swiped = false;

            var hit = Physics2D.Raycast(worldPos, Vector2.zero);
            _pressedPiece = hit.collider != null
                ? hit.collider.GetComponent<GamePiece>()
                : null;
        }

        private void HandleMoved(Vector2 worldPos)
        {
            if (_pressedPiece == null || _swiped) return;

            var delta = worldPos - _touchStart;
            if (delta.magnitude < swipeThreshold) return;

            _swiped = true;
            int dx = 0, dy = 0;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                dx = delta.x > 0 ? 1 : -1;
            else
                dy = delta.y > 0 ? 1 : -1;

            int targetX = _pressedPiece.X + dx;
            int targetY = _pressedPiece.Y - dy;

            var target = _grid.GetPieceAt(targetX, targetY);
            if (target != null)
                _grid.SwapPieces(_pressedPiece, target);
        }

        private void HandleEnded()
        {
            if (!_swiped) ReleasePiece();
            _pressedPiece = null;
            _enteredPiece = null;
        }
    }
}
