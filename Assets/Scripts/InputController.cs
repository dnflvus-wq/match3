using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 터치/마우스 입력을 감지하여 그리드 좌표로 변환하고 GameGrid에 스왑을 요청합니다.
    /// GameGrid.Update()에서 추출된 코드입니다 (Strangler Pattern 1단계).
    /// </summary>
    public class InputController : MonoBehaviour
    {
        private GameGrid _gameGrid;
        private BoosterUI _boosterUI;

        private GamePiece _pressedPiece;
        private Vector2 _touchStart;
        private bool _swiped;
        private float _swipeThreshold = 0.3f;

        private void Awake()
        {
            _gameGrid = GetComponent<GameGrid>();
        }

        private void Start()
        {
            _boosterUI = FindFirstObjectByType<BoosterUI>();
        }

        private void Update()
        {
            // Android 뒤로가기 키
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("LevelSelect");
                return;
            }

            // FSM: READY 상태에서만 터치 입력 허용
            if (_gameGrid.CurrentState != GameState.READY) return;

            // 힌트 타이머 업데이트
            _gameGrid.UpdateHintTimer(Time.deltaTime);

            // 입력 감지
            bool began = false, moved = false, ended = false;
            Vector2 screenPos = Vector2.zero;

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
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

            Vector3 wp = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
            Vector2 worldPos = new Vector2(wp.x, wp.y);

            if (began)
            {
                HandleTouchBegan(worldPos);
            }
            else if (moved)
            {
                HandleTouchMoved(worldPos);
            }
            else if (ended)
            {
                HandleTouchEnded();
            }
        }

        private void HandleTouchBegan(Vector2 worldPos)
        {
            // 망치 모드
            if (_boosterUI != null && _boosterUI.IsHammerMode)
            {
                _gameGrid.HandleHammerTouch(worldPos);
                return;
            }

            _gameGrid.StopHint();
            _gameGrid.ResetHintTimer();

            _touchStart = worldPos;
            _swiped = false;

            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
            GamePiece piece = hit.collider != null ? hit.collider.GetComponent<GamePiece>() : null;
            if (piece != null)
            {
                _pressedPiece = piece;
            }
        }

        private void HandleTouchMoved(Vector2 worldPos)
        {
            if (_pressedPiece == null || _swiped) return;

            Vector2 delta = worldPos - _touchStart;
            if (delta.magnitude < _swipeThreshold) return;

            _swiped = true;

            int dx = 0, dy = 0;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                dx = delta.x > 0 ? 1 : -1;
            else
                dy = delta.y > 0 ? 1 : -1;

            int targetX = _pressedPiece.X + dx;
            int targetY = _pressedPiece.Y - dy;

            _gameGrid.TrySwap(_pressedPiece, targetX, targetY);
        }

        private void HandleTouchEnded()
        {
            _pressedPiece = null;
            _swiped = false;
        }
    }
}
