using UnityEngine;

namespace Match3
{
    public class GamePiece : MonoBehaviour
    {
        public int score;

        private int _x;
        private int _y;

        public int X
        {
            get => _x;
            set { if (IsMovable()) { _x = value; } }
        }

        public int Y
        {
            get => _y;
            set { if (IsMovable()) { _y = value; } }
        }
    
        private PieceType _type;

        public PieceType Type => _type;

        private GameGrid _gameGrid;

        private BoardModel _boardModel;
        public BoardModel BoardModelRef => _boardModel;

        private Vector2 _boardOrigin;
        private int _boardWidth, _boardHeight;

        public System.Action<GamePiece> OnCleared;

        private InputController _inputController;

        private MovablePiece _movableComponent;

        public MovablePiece MovableComponent => _movableComponent;

        private ColorPiece _colorComponent;

        public ColorPiece ColorComponent => _colorComponent;

        private ClearablePiece _clearableComponent;

        public ClearablePiece ClearableComponent => _clearableComponent;

        private void Awake()
        {
            _movableComponent = GetComponent<MovablePiece>();
            _colorComponent = GetComponent<ColorPiece>();
            _clearableComponent = GetComponent<ClearablePiece>();
        }

        public void Init(int x, int y, GameGrid gameGrid, PieceType type)
        {
            _x = x;
            _y = y;
            _gameGrid = gameGrid;
            _boardModel = gameGrid.GetComponent<BoardModel>();
            _inputController = gameGrid.GetComponent<InputController>();
            _type = type;
            _boardOrigin = gameGrid.transform.position;
            _boardWidth = gameGrid.xDim;
            _boardHeight = gameGrid.yDim;
            OnCleared = (p) => gameGrid.level.OnPieceCleared(p);
        }

        public Vector2 CalcWorldPosition(int px, int py)
        {
            return new Vector2(
                _boardOrigin.x - _boardWidth / 2.0f + px + 0.5f,
                _boardOrigin.y + _boardHeight / 2.0f - py - 0.5f);
        }

        private void OnMouseEnter() => _inputController?.EnterPiece(this);

        private void OnMouseDown() => _inputController?.PressPiece(this);

        private void OnMouseUp() => _inputController?.ReleasePiece();

        public bool IsMovable() => _movableComponent != null;

        public bool IsColored() => _colorComponent != null;

        public bool IsClearable() => _clearableComponent != null;
    }
}
