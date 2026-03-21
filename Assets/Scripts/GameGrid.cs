using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Match3
{
    public class GameGrid : MonoBehaviour
    {
        [System.Serializable]
        public struct PiecePrefab
        {
            public PieceType type;
            public GameObject prefab;
        };

        [System.Serializable]
        public struct PiecePosition
        {
            public PieceType type;
            public int x;
            public int y;
        };

        public int xDim;
        public int yDim;
        public float fillTime;

        [Header("Animation Timing")]
        public float swapTime = 0.25f;
        public float swapBackTime = 0.2f;

        public Level level;

        public PiecePrefab[] piecePrefabs;
        public GameObject backgroundPrefab;

        public PiecePosition[] initialPieces;

        private Dictionary<PieceType, GameObject> _piecePrefabDict;

        private GamePiece[,] _pieces;

        private bool _inverse;

        private GamePiece _pressedPiece;
        private GamePiece _enteredPiece;

        private bool _gameOver;

        // FSM
        private GameState _currentState = GameState.PREGAME;
        public GameState CurrentState => _currentState;

        // Juice
        private int _comboCount;

        // 힌트 시스템
        [Header("Hint System")]
        public float hintDelay = 5f;
        private float _hintTimer;
        private List<GamePiece> _hintPieces;
        private Coroutine _hintCoroutine;

        public bool IsFilling { get; private set; }

        private void Awake()
        {
            _piecePrefabDict = new Dictionary<PieceType, GameObject>();
            for (int i = 0; i < piecePrefabs.Length; i++)
            {
                if (!_piecePrefabDict.ContainsKey(piecePrefabs[i].type))
                {
                    _piecePrefabDict.Add(piecePrefabs[i].type, piecePrefabs[i].prefab);
                }
            }

            for (int x = 0; x < xDim; x++)
            {
                for (int y = 0; y < yDim; y++)
                {
                    GameObject background = Instantiate(backgroundPrefab, GetWorldPosition(x, y), Quaternion.identity);
                    background.transform.parent = transform;
                }
            }

            _pieces = new GamePiece[xDim, yDim];

            for (int i = 0; i < initialPieces.Length; i++)
            {
                if (initialPieces[i].x >= 0 && initialPieces[i].y < xDim
                                            && initialPieces[i].y >=0 && initialPieces[i].y <yDim)
                {
                    SpawnNewPiece(initialPieces[i].x, initialPieces[i].y, initialPieces[i].type);
                }
            }

            for (int x = 0; x < xDim; x++)
            {
                for (int y = 0; y < yDim; y++)
                {
                    if (_pieces[x, y] == null)
                    {
                        SpawnNewPiece(x, y, PieceType.Empty);
                    }
                }
            }

            StartCoroutine(Fill());
        }

        private IEnumerator Fill()
        {
            bool needsRefill = true;
            IsFilling = true;
            _currentState = GameState.COLLAPSING;
            _comboCount = 0;

            while (needsRefill)
            {
                yield return new WaitForSeconds(fillTime);
                while (FillStep())
                {
                    _inverse = !_inverse;
                    yield return new WaitForSeconds(fillTime);
                }

                needsRefill = ClearAllValidMatches();
                if (needsRefill)
                {
                    _comboCount++;
                }
            }

            IsFilling = false;

            if (_gameOver)
            {
                _currentState = GameState.ENDGAME;
            }
            else
            {
                // 데드 보드 체크
                if (FindValidMove() == null)
                {
                    yield return StartCoroutine(ShuffleBoard());
                }

                _currentState = GameState.READY;
                ResetHintTimer();
            }
        }

        private bool FillStep()
        {
            bool movedPiece = false;
            for (int y = yDim - 2; y >= 0; y--)
            {
                for (int loopX = 0; loopX < xDim; loopX++)
                {
                    int x = loopX;
                    if (_inverse) { x = xDim - 1 - loopX; }
                    GamePiece piece = _pieces[x, y];

                    if (!piece.IsMovable()) continue;

                    GamePiece pieceBelow = _pieces[x, y + 1];

                    if (pieceBelow.Type == PieceType.Empty)
                    {
                        Destroy(pieceBelow.gameObject);
                        piece.MovableComponent.MoveDrop(x, y + 1, fillTime);
                        _pieces[x, y + 1] = piece;
                        SpawnNewPiece(x, y, PieceType.Empty);
                        movedPiece = true;
                    }
                    else
                    {
                        for (int diag = -1; diag <= 1; diag++)
                        {
                            if (diag == 0) continue;

                            int diagX = x + diag;

                            if (_inverse)
                            {
                                diagX = x - diag;
                            }

                            if (diagX < 0 || diagX >= xDim) continue;

                            GamePiece diagonalPiece = _pieces[diagX, y + 1];

                            if (diagonalPiece.Type != PieceType.Empty) continue;

                            bool hasPieceAbove = true;

                            for (int aboveY = y; aboveY >= 0; aboveY--)
                            {
                                GamePiece pieceAbove = _pieces[diagX, aboveY];

                                if (pieceAbove.IsMovable())
                                {
                                    break;
                                }
                                else if (pieceAbove.Type != PieceType.Empty)
                                {
                                    hasPieceAbove = false;
                                    break;
                                }
                            }

                            if (hasPieceAbove) continue;

                            Destroy(diagonalPiece.gameObject);
                            piece.MovableComponent.MoveDrop(diagX, y + 1, fillTime);
                            _pieces[diagX, y + 1] = piece;
                            SpawnNewPiece(x, y, PieceType.Empty);
                            movedPiece = true;
                            break;
                        }
                    }
                }
            }

            for (int x = 0; x < xDim; x++)
            {
                GamePiece pieceBelow = _pieces[x, 0];

                if (pieceBelow.Type != PieceType.Empty) continue;

                Destroy(pieceBelow.gameObject);
                GameObject newPiece = Instantiate(_piecePrefabDict[PieceType.Normal], GetWorldPosition(x, -1), Quaternion.identity, this.transform);

                _pieces[x, 0] = newPiece.GetComponent<GamePiece>();
                _pieces[x, 0].Init(x, -1, this, PieceType.Normal);
                _pieces[x, 0].MovableComponent.MoveDrop(x, 0, fillTime);
                _pieces[x, 0].ColorComponent.SetColor(GetWeightedRandomColor(_pieces[x, 0].ColorComponent.NumColors));
                movedPiece = true;
            }

            return movedPiece;
        }

        public Vector2 GetWorldPosition(int x, int y)
        {
            return new Vector2(
                transform.position.x - xDim / 2.0f + x,
                transform.position.y + yDim / 2.0f - y);
        }

        private GamePiece SpawnNewPiece(int x, int y, PieceType type)
        {
            GameObject newPiece = Instantiate(_piecePrefabDict[type], GetWorldPosition(x, y), Quaternion.identity, this.transform);
            _pieces[x, y] = newPiece.GetComponent<GamePiece>();
            _pieces[x, y].Init(x, y, this, type);

            return _pieces[x, y];
        }

        private static bool IsAdjacent(GamePiece piece1, GamePiece piece2) =>
            (piece1.X == piece2.X && Mathf.Abs(piece1.Y - piece2.Y) == 1) ||
            (piece1.Y == piece2.Y && Mathf.Abs(piece1.X - piece2.X) == 1);

        private void SwapPieces(GamePiece piece1, GamePiece piece2)
        {
            if (_gameOver || _currentState != GameState.READY) { return; }

            if (!piece1.IsMovable() || !piece2.IsMovable()) return;

            StartCoroutine(SwapPiecesCoroutine(piece1, piece2));
        }

        private IEnumerator SwapPiecesCoroutine(GamePiece piece1, GamePiece piece2)
        {
            _currentState = GameState.SWAPPING;

            int p1X = piece1.X, p1Y = piece1.Y;
            int p2X = piece2.X, p2Y = piece2.Y;

            // 배열 교환 (매치 확인용)
            _pieces[p1X, p1Y] = piece2;
            _pieces[p2X, p2Y] = piece1;

            // 시각 애니메이션 — Ease-Out + Z축 아크 (한쪽만 arc로 겹침 방지)
            piece1.MovableComponent.MoveVisual(p2X, p2Y, swapTime, useArc: true);
            piece2.MovableComponent.MoveVisual(p1X, p1Y, swapTime, useArc: false);

            yield return new WaitForSeconds(swapTime);

            // EVALUATING: 매치 확인
            _currentState = GameState.EVALUATING;

            bool hasMatch = GetMatch(piece1, p2X, p2Y) != null ||
                            GetMatch(piece2, p1X, p1Y) != null ||
                            piece1.Type == PieceType.Rainbow ||
                            piece2.Type == PieceType.Rainbow;

            if (hasMatch)
            {
                // 매치 성공 — 데이터(X,Y) 확정
                piece1.MovableComponent.SetPosition(p2X, p2Y);
                piece2.MovableComponent.SetPosition(p1X, p1Y);

                // Rainbow 처리
                if (piece1.Type == PieceType.Rainbow && piece1.IsClearable() && piece2.IsColored())
                {
                    ClearColorPiece clearColor = piece1.GetComponent<ClearColorPiece>();
                    if (clearColor)
                    {
                        clearColor.Color = piece2.ColorComponent.Color;
                    }
                    ClearPiece(piece1.X, piece1.Y);
                }

                if (piece2.Type == PieceType.Rainbow && piece2.IsClearable() && piece1.IsColored())
                {
                    ClearColorPiece clearColor = piece2.GetComponent<ClearColorPiece>();
                    if (clearColor)
                    {
                        clearColor.Color = piece1.ColorComponent.Color;
                    }
                    ClearPiece(piece2.X, piece2.Y);
                }

                // MATCHING
                _currentState = GameState.MATCHING;
                ClearAllValidMatches();

                // 특수 타일 클리어
                if (piece1.Type == PieceType.RowClear || piece1.Type == PieceType.ColumnClear)
                {
                    ClearPiece(piece1.X, piece1.Y);
                }
                if (piece2.Type == PieceType.RowClear || piece2.Type == PieceType.ColumnClear)
                {
                    ClearPiece(piece2.X, piece2.Y);
                }

                _pressedPiece = null;
                _enteredPiece = null;

                // COLLAPSING → Fill이 완료되면 READY로 전환
                StartCoroutine(Fill());

                level.OnMove();
            }
            else
            {
                // 매치 실패 — 배열 복원
                _pieces[p1X, p1Y] = piece1;
                _pieces[p2X, p2Y] = piece2;

                // 핑퐁: 원래 위치로 되돌리기 (빠르게)
                piece1.MovableComponent.MoveVisual(p1X, p1Y, swapBackTime);
                piece2.MovableComponent.MoveVisual(p2X, p2Y, swapBackTime);

                yield return new WaitForSeconds(swapBackTime);

                _currentState = GameState.READY;
            }
        }

        public void PressPiece(GamePiece piece) => _pressedPiece = piece;

        public void EnterPiece(GamePiece piece) => _enteredPiece = piece;

        public void ReleasePiece()
        {
            if (_pressedPiece != null && _enteredPiece != null && IsAdjacent(_pressedPiece, _enteredPiece))
            {
                SwapPieces(_pressedPiece, _enteredPiece);
            }
            _pressedPiece = null;
            _enteredPiece = null;
        }

        private Vector2 _touchStart;
        private bool _swiped;
        private float _swipeThreshold = 0.3f;

        private void Update()
        {
            // Android 뒤로가기 키
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("LevelSelect");
                return;
            }

            // FSM: READY 상태에서만 터치 입력 허용
            if (_currentState != GameState.READY) return;

            // 힌트 타이머
            _hintTimer -= Time.deltaTime;
            if (_hintTimer <= 0f && _hintCoroutine == null)
            {
                ShowHint();
            }

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
                StopHint();
                ResetHintTimer();
                _touchStart = worldPos;
                _swiped = false;
                RaycastHit2D hitBegin = Physics2D.Raycast(worldPos, Vector2.zero);
                GamePiece beginPiece = hitBegin.collider != null ? hitBegin.collider.GetComponent<GamePiece>() : null;
                if (beginPiece != null) PressPiece(beginPiece);
            }
            else if (moved)
            {
                if (_pressedPiece != null && !_swiped)
                {
                    Vector2 delta = worldPos - _touchStart;
                    if (delta.magnitude >= _swipeThreshold)
                    {
                        _swiped = true;
                        int dx = 0, dy = 0;
                        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                            dx = delta.x > 0 ? 1 : -1;
                        else
                            dy = delta.y > 0 ? 1 : -1;

                        int targetX = _pressedPiece.X + dx;
                        int targetY = _pressedPiece.Y - dy;
                        if (targetX >= 0 && targetX < xDim && targetY >= 0 && targetY < yDim)
                        {
                            EnterPiece(_pieces[targetX, targetY]);
                            SwapPieces(_pressedPiece, _enteredPiece);
                        }
                    }
                }
            }
            else if (ended)
            {
                if (!_swiped) ReleasePiece();
                _pressedPiece = null;
                _enteredPiece = null;
            }
        }

        private bool ClearAllValidMatches()
        {
            bool needsRefill = false;

            for (int y = 0; y < yDim; y++)
            {
                for (int x = 0; x < xDim; x++)
                {
                    if (!_pieces[x, y].IsClearable()) continue;

                    List<GamePiece> match = GetMatch(_pieces[x, y], x, y);

                    if (match == null) continue;

                    // 4매치 이상 시 카메라 쉐이크
                    if (match.Count >= 4)
                    {
                        StartCoroutine(CameraShake(0.05f, 0.2f));
                    }

                    PieceType specialPieceType = PieceType.Count;
                    GamePiece randomPiece = match[Random.Range(0, match.Count)];
                    int specialPieceX = randomPiece.X;
                    int specialPieceY = randomPiece.Y;

                    if (match.Count == 4)
                    {
                        if (_pressedPiece == null || _enteredPiece == null)
                        {
                            specialPieceType = (PieceType) Random.Range((int) PieceType.RowClear, (int) PieceType.ColumnClear);
                        }
                        else if (_pressedPiece.Y == _enteredPiece.Y)
                        {
                            specialPieceType = PieceType.RowClear;
                        }
                        else
                        {
                            specialPieceType = PieceType.ColumnClear;
                        }
                    }
                    else if (match.Count >= 5)
                    {
                        specialPieceType = PieceType.Rainbow;
                    }

                    foreach (var gamePiece in match)
                    {
                        if (!ClearPiece(gamePiece.X, gamePiece.Y)) continue;

                        needsRefill = true;

                        if (gamePiece != _pressedPiece && gamePiece != _enteredPiece) continue;

                        specialPieceX = gamePiece.X;
                        specialPieceY = gamePiece.Y;
                    }

                    if (specialPieceType == PieceType.Count) continue;

                    Destroy(_pieces[specialPieceX, specialPieceY]);
                    GamePiece newPiece = SpawnNewPiece(specialPieceX, specialPieceY, specialPieceType);

                    if ((specialPieceType == PieceType.RowClear || specialPieceType == PieceType.ColumnClear)
                        && newPiece.IsColored() && match[0].IsColored())
                    {
                        newPiece.ColorComponent.SetColor(match[0].ColorComponent.Color);
                    }
                    else if (specialPieceType == PieceType.Rainbow && newPiece.IsColored())
                    {
                        newPiece.ColorComponent.SetColor(ColorType.Any);
                    }
                }
            }

            return needsRefill;
        }

        private List<GamePiece> GetMatch(GamePiece piece, int newX, int newY)
        {
            if (!piece.IsColored()) return null;
            var color = piece.ColorComponent.Color;
            var horizontalPieces = new List<GamePiece>();
            var verticalPieces = new List<GamePiece>();
            var matchingPieces = new List<GamePiece>();

            horizontalPieces.Add(piece);

            for (int dir = 0; dir <= 1; dir++)
            {
                for (int xOffset = 1; xOffset < xDim; xOffset++)
                {
                    int x;

                    if (dir == 0)
                    {
                        x = newX - xOffset;
                    }
                    else
                    {
                        x = newX + xOffset;
                    }

                    if (x < 0 || x >= xDim) { break; }

                    if (_pieces[x, newY].IsColored() && _pieces[x, newY].ColorComponent.Color == color)
                    {
                        horizontalPieces.Add(_pieces[x, newY]);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (horizontalPieces.Count >= 3)
            {
                matchingPieces.AddRange(horizontalPieces);
            }

            if (horizontalPieces.Count >= 3)
            {
                for (int i = 0; i < horizontalPieces.Count; i++ )
                {
                    for (int dir = 0; dir <= 1; dir++)
                    {
                        for (int yOffset = 1; yOffset < yDim; yOffset++)
                        {
                            int y;

                            if (dir == 0)
                            {
                                y = newY - yOffset;
                            }
                            else
                            {
                                y = newY + yOffset;
                            }

                            if (y < 0 || y >= yDim)
                            {
                                break;
                            }

                            if (_pieces[horizontalPieces[i].X, y].IsColored() && _pieces[horizontalPieces[i].X, y].ColorComponent.Color == color)
                            {
                                verticalPieces.Add(_pieces[horizontalPieces[i].X, y]);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    if (verticalPieces.Count < 2)
                    {
                        verticalPieces.Clear();
                    }
                    else
                    {
                        matchingPieces.AddRange(verticalPieces);
                        break;
                    }
                }
            }

            if (matchingPieces.Count >= 3)
            {
                return matchingPieces;
            }

            horizontalPieces.Clear();
            verticalPieces.Clear();
            verticalPieces.Add(piece);

            for (int dir = 0; dir <= 1; dir++)
            {
                for (int yOffset = 1; yOffset < xDim; yOffset++)
                {
                    int y;

                    if (dir == 0)
                    {
                        y = newY - yOffset;
                    }
                    else
                    {
                        y = newY + yOffset;
                    }

                    if (y < 0 || y >= yDim) { break; }

                    if (_pieces[newX, y].IsColored() && _pieces[newX, y].ColorComponent.Color == color)
                    {
                        verticalPieces.Add(_pieces[newX, y]);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (verticalPieces.Count >= 3)
            {
                matchingPieces.AddRange(verticalPieces);
            }

            if (verticalPieces.Count >= 3)
            {
                for (int i = 0; i < verticalPieces.Count; i++)
                {
                    for (int dir = 0; dir <= 1; dir++)
                    {
                        for (int xOffset = 1; xOffset < yDim; xOffset++)
                        {
                            int x;

                            if (dir == 0)
                            {
                                x = newX - xOffset;
                            }
                            else
                            {
                                x = newX + xOffset;
                            }

                            if (x < 0 || x >= xDim)
                            {
                                break;
                            }

                            if (_pieces[x, verticalPieces[i].Y].IsColored() && _pieces[x, verticalPieces[i].Y].ColorComponent.Color == color)
                            {
                                horizontalPieces.Add(_pieces[x, verticalPieces[i].Y]);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    if (horizontalPieces.Count < 2)
                    {
                        horizontalPieces.Clear();
                    }
                    else
                    {
                        matchingPieces.AddRange(horizontalPieces);
                        break;
                    }
                }
            }

            if (matchingPieces.Count >= 3)
            {
                return matchingPieces;
            }

            return null;
        }

        private bool ClearPiece(int x, int y)
        {
            if (!_pieces[x, y].IsClearable() || _pieces[x, y].ClearableComponent.IsBeingCleared) return false;

            _pieces[x, y].ClearableComponent.Clear();
            SpawnNewPiece(x, y, PieceType.Empty);

            ClearObstacles(x, y);

            return true;
        }

        private void ClearObstacles(int x, int y)
        {
            for (int adjacentX = x - 1; adjacentX <= x + 1; adjacentX++)
            {
                if (adjacentX == x || adjacentX < 0 || adjacentX >= xDim) continue;

                if (_pieces[adjacentX, y].Type != PieceType.Bubble || !_pieces[adjacentX, y].IsClearable()) continue;

                _pieces[adjacentX, y].ClearableComponent.Clear();
                SpawnNewPiece(adjacentX, y, PieceType.Empty);
            }

            for (int adjacentY = y - 1; adjacentY <= y + 1; adjacentY++)
            {
                if (adjacentY == y || adjacentY < 0 || adjacentY >= yDim) continue;

                if (_pieces[x, adjacentY].Type != PieceType.Bubble || !_pieces[x, adjacentY].IsClearable()) continue;

                _pieces[x, adjacentY].ClearableComponent.Clear();
                SpawnNewPiece(x, adjacentY, PieceType.Empty);
            }
        }

        public void ClearRow(int row)
        {
            for (int x = 0; x < xDim; x++)
            {
                ClearPiece(x, row);
            }
        }

        public void ClearColumn(int column)
        {
            for (int y = 0; y < yDim; y++)
            {
                ClearPiece(column, y);
            }
        }

        public void ClearColor(ColorType color)
        {
            for (int x = 0; x < xDim; x++)
            {
                for (int y = 0; y < yDim; y++)
                {
                    if ((_pieces[x, y].IsColored() && _pieces[x, y].ColorComponent.Color == color)
                        || (color == ColorType.Any))
                    {
                        ClearPiece(x, y);
                    }
                }
            }
        }

        public void GameOver()
        {
            _gameOver = true;
            _currentState = GameState.ENDGAME;
        }

        public List<GamePiece> GetPiecesOfType(PieceType type)
        {
            var piecesOfType = new List<GamePiece>();

            for (int x = 0; x < xDim; x++)
            {
                for (int y = 0; y < yDim; y++)
                {
                    if (_pieces[x, y].Type == type)
                    {
                        piecesOfType.Add(_pieces[x, y]);
                    }
                }
            }

            return piecesOfType;
        }

        // === 스폰 가중치 시스템 ===

        private ColorType GetWeightedRandomColor(int numColors)
        {
            // 보드에 많은 색상일수록 스폰 확률 감소
            int[] colorCounts = new int[numColors];
            int totalPieces = 0;

            for (int x = 0; x < xDim; x++)
            {
                for (int y = 0; y < yDim; y++)
                {
                    if (_pieces[x, y].IsColored() && (int)_pieces[x, y].ColorComponent.Color < numColors)
                    {
                        colorCounts[(int)_pieces[x, y].ColorComponent.Color]++;
                        totalPieces++;
                    }
                }
            }

            if (totalPieces == 0)
                return (ColorType)Random.Range(0, numColors);

            // 역가중치: 보드에 적을수록 높은 확률
            float[] weights = new float[numColors];
            float totalWeight = 0f;

            for (int i = 0; i < numColors; i++)
            {
                weights[i] = 1f + (totalPieces / (float)numColors - colorCounts[i]) * 0.5f;
                if (weights[i] < 0.1f) weights[i] = 0.1f;
                totalWeight += weights[i];
            }

            float rand = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < numColors; i++)
            {
                cumulative += weights[i];
                if (rand <= cumulative)
                    return (ColorType)i;
            }

            return (ColorType)(numColors - 1);
        }

        // === 힌트 시스템 ===

        private List<GamePiece> FindValidMove()
        {
            for (int x = 0; x < xDim; x++)
            {
                for (int y = 0; y < yDim; y++)
                {
                    if (!_pieces[x, y].IsColored() || !_pieces[x, y].IsMovable()) continue;

                    // 상하좌우 4방향 가상 스왑
                    int[,] dirs = { {1,0}, {-1,0}, {0,1}, {0,-1} };
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = x + dirs[d, 0];
                        int ny = y + dirs[d, 1];

                        if (nx < 0 || nx >= xDim || ny < 0 || ny >= yDim) continue;
                        if (!_pieces[nx, ny].IsMovable()) continue;

                        // 가상 스왑
                        GamePiece temp = _pieces[x, y];
                        _pieces[x, y] = _pieces[nx, ny];
                        _pieces[nx, ny] = temp;

                        // 매치 확인
                        var match1 = GetMatch(_pieces[x, y], x, y);
                        var match2 = GetMatch(_pieces[nx, ny], nx, ny);

                        // 복원
                        temp = _pieces[x, y];
                        _pieces[x, y] = _pieces[nx, ny];
                        _pieces[nx, ny] = temp;

                        if (match1 != null)
                        {
                            return new List<GamePiece> { _pieces[x, y], _pieces[nx, ny] };
                        }
                        if (match2 != null)
                        {
                            return new List<GamePiece> { _pieces[x, y], _pieces[nx, ny] };
                        }
                    }
                }
            }

            return null; // 데드 보드
        }

        private void ResetHintTimer()
        {
            _hintTimer = hintDelay;
        }

        private void ShowHint()
        {
            _hintPieces = FindValidMove();
            if (_hintPieces == null) return;

            _hintCoroutine = StartCoroutine(HintPulseCoroutine());
        }

        private void StopHint()
        {
            if (_hintCoroutine != null)
            {
                StopCoroutine(_hintCoroutine);
                _hintCoroutine = null;
            }

            // 힌트 타일 스케일 복원
            if (_hintPieces != null)
            {
                foreach (var piece in _hintPieces)
                {
                    if (piece != null && piece.gameObject != null)
                    {
                        piece.transform.localScale = Vector3.one;
                    }
                }
                _hintPieces = null;
            }
        }

        private IEnumerator HintPulseCoroutine()
        {
            float pulseSpeed = 2f * Mathf.PI; // 주기 약 0.5초
            float elapsed = 0f;

            while (true)
            {
                elapsed += Time.deltaTime;
                float scale = 1f + 0.15f * Mathf.Sin(elapsed * pulseSpeed);

                if (_hintPieces != null)
                {
                    foreach (var piece in _hintPieces)
                    {
                        if (piece != null && piece.gameObject != null)
                        {
                            piece.transform.localScale = new Vector3(scale, scale, 1f);
                        }
                    }
                }

                yield return null;
            }
        }

        // === 카메라 쉐이크 ===

        private IEnumerator CameraShake(float magnitude, float duration)
        {
            var cam = Camera.main;
            if (cam == null) yield break;

            Vector3 originalPos = cam.transform.position;

            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                cam.transform.position = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
                yield return null;
            }

            cam.transform.position = originalPos;
        }

        // === 데드 보드 셔플 ===

        private IEnumerator ShuffleBoard()
        {
            _currentState = GameState.SHUFFLING;

            // Fisher-Yates 셔플 (색상만 섞기)
            var colorPieces = new List<GamePiece>();
            for (int x = 0; x < xDim; x++)
            {
                for (int y = 0; y < yDim; y++)
                {
                    if (_pieces[x, y].IsColored() && _pieces[x, y].Type == PieceType.Normal)
                    {
                        colorPieces.Add(_pieces[x, y]);
                    }
                }
            }

            int maxAttempts = 30;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Fisher-Yates
                for (int i = colorPieces.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    // 색상만 교환
                    ColorType tempColor = colorPieces[i].ColorComponent.Color;
                    colorPieces[i].ColorComponent.SetColor(colorPieces[j].ColorComponent.Color);
                    colorPieces[j].ColorComponent.SetColor(tempColor);
                }

                // 초기 매치 없는지 확인
                bool hasMatch = false;
                for (int x = 0; x < xDim && !hasMatch; x++)
                {
                    for (int y = 0; y < yDim && !hasMatch; y++)
                    {
                        if (GetMatch(_pieces[x, y], x, y) != null)
                            hasMatch = true;
                    }
                }

                // 유효 이동 존재 + 초기 매치 없음 → 성공
                if (!hasMatch && FindValidMove() != null)
                    break;
            }

            yield return new WaitForSeconds(0.3f);
        }

    }
}
