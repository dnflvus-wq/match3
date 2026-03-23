using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Match3
{
    [RequireComponent(typeof(InputController))]
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

        [Header("Config (Optional)")]
        public GameConfig gameConfig;

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

        // 부스터
        private BoosterUI _boosterUI;

        // 힌트 시스템
        [Header("Hint System")]
        public float hintDelay = 5f;
        private float _hintTimer;
        private List<GamePiece> _hintPieces;
        private Coroutine _hintCoroutine;

        public bool IsFilling { get; private set; }

        private void Awake()
        {
            // GameConfig가 있으면 값 적용
            if (gameConfig != null)
            {
                fillTime = gameConfig.fillTime;
                swapTime = gameConfig.swapTime;
                swapBackTime = gameConfig.swapBackTime;
                hintDelay = gameConfig.hintDelay;
            }

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

            // MobileCameraSetup 자동 적용 (카메라에 없으면)
            if (Camera.main != null && Camera.main.GetComponent<MobileCameraSetup>() == null)
            {
                Camera.main.gameObject.AddComponent<MobileCameraSetup>();
            }

            // AudioManager 자동 생성 (씬에 없으면)
            if (AudioManager.Instance == null)
            {
                var audioObj = new GameObject("AudioManager");
                audioObj.AddComponent<AudioManager>();
            }

            // MatchParticles 자동 생성
            if (MatchParticles.Instance == null)
            {
                var particleObj = new GameObject("MatchParticles");
                particleObj.AddComponent<MatchParticles>();
            }

            StartCoroutine(StartSequence());
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
                    if (_comboCount >= 2)
                    {
                        ShowComboText(_comboCount);
                        if (AudioManager.Instance != null) AudioManager.Instance.PlayCombo();
                    }
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

            // 스왑 효과음
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySwap();

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

                // 특수 + 특수 조합: 레인보우 + 레인보우 = 전체 클리어
                if (piece1.Type == PieceType.Rainbow && piece2.Type == PieceType.Rainbow)
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySpecial();
                    StartCoroutine(CameraShake(0.1f, 0.4f));
                    ClearPiece(piece1.X, piece1.Y);
                    ClearPiece(piece2.X, piece2.Y);
                    // 전체 보드 클리어
                    for (int cx = 0; cx < xDim; cx++)
                    {
                        for (int cy = 0; cy < yDim; cy++)
                        {
                            ClearPiece(cx, cy);
                        }
                    }
                    _pressedPiece = null;
                    _enteredPiece = null;
                    StartCoroutine(Fill());
                    level.OnMove();
                    yield break;
                }

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

                // 실패 효과음
                if (AudioManager.Instance != null) AudioManager.Instance.PlayFail();

                // 핑퐁: 원래 위치로 되돌리기 (빠르게)
                piece1.MovableComponent.MoveVisual(p1X, p1Y, swapBackTime);
                piece2.MovableComponent.MoveVisual(p2X, p2Y, swapBackTime);

                yield return new WaitForSeconds(swapBackTime);

                _currentState = GameState.READY;
            }
        }

        // === InputController에서 호출하는 public API ===

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

        /// <summary>InputController에서 스왑을 요청할 때 호출</summary>
        public void TrySwap(GamePiece piece, int targetX, int targetY)
        {
            if (targetX < 0 || targetX >= xDim || targetY < 0 || targetY >= yDim) return;
            EnterPiece(_pieces[targetX, targetY]);
            SwapPieces(piece, _enteredPiece);
        }

        /// <summary>InputController에서 망치 터치를 처리할 때 호출</summary>
        public void HandleHammerTouch(Vector2 worldPos)
        {
            RaycastHit2D hammerHit = Physics2D.Raycast(worldPos, Vector2.zero);
            GamePiece hammerPiece = hammerHit.collider != null ? hammerHit.collider.GetComponent<GamePiece>() : null;
            if (hammerPiece != null && hammerPiece.IsClearable())
            {
                ClearPiece(hammerPiece.X, hammerPiece.Y);
                if (_boosterUI != null) _boosterUI.UseHammer();
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySpecial();
                StartCoroutine(CameraShake(0.03f, 0.15f));
                StartCoroutine(Fill());
            }
        }

        /// <summary>InputController에서 힌트 타이머를 업데이트할 때 호출</summary>
        public void UpdateHintTimer(float deltaTime)
        {
            _hintTimer -= deltaTime;
            if (_hintTimer <= 0f && _hintCoroutine == null)
            {
                ShowHint();
            }
        }

        // Update()는 InputController.cs로 이동됨 (Strangler Pattern 1단계)

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

                    // 매치 효과음 (콤보 피치 상승)
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayMatch(_comboCount);
                    }

                    // 4매치 이상 시 카메라 쉐이크 + 특수 효과음 + 강화 파티클
                    if (match.Count >= 4)
                    {
                        StartCoroutine(CameraShake(0.05f, 0.2f));
                        if (AudioManager.Instance != null) AudioManager.Instance.PlaySpecial();

                        // 4매치 중심에 강화 파티클
                        if (MatchParticles.Instance != null && match.Count > 0)
                        {
                            Vector3 center = Vector3.zero;
                            foreach (var p in match) center += p.transform.position;
                            center /= match.Count;
                            Color c = match[0].IsColored() ? GetColorForType(match[0].ColorComponent.Color) : Color.white;
                            MatchParticles.Instance.PlayBigAt(center, c);
                        }
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

            // 파티클 이펙트 + 점수 팝업
            if (_pieces[x, y].IsColored())
            {
                Vector3 piecePos = _pieces[x, y].transform.position;
                Color particleColor = GetColorForType(_pieces[x, y].ColorComponent.Color);

                if (MatchParticles.Instance != null)
                    MatchParticles.Instance.PlayAt(piecePos, particleColor);

                // 점수 팝업
                int score = _pieces[x, y].score;
                if (score > 0)
                    ShowScorePopup(piecePos, score);
            }

            _pieces[x, y].ClearableComponent.Clear();
            SpawnNewPiece(x, y, PieceType.Empty);

            ClearObstacles(x, y);

            return true;
        }

        private Color GetColorForType(ColorType type)
        {
            switch (type)
            {
                case ColorType.Yellow: return new Color(1f, 0.85f, 0.1f);
                case ColorType.Purple: return new Color(0.7f, 0.2f, 0.9f);
                case ColorType.Red: return new Color(1f, 0.2f, 0.2f);
                case ColorType.Blue: return new Color(0.2f, 0.4f, 1f);
                case ColorType.Green: return new Color(0.2f, 0.9f, 0.3f);
                case ColorType.Pink: return new Color(1f, 0.4f, 0.7f);
                default: return Color.white;
            }
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

        // === 시작 카운트다운 ===

        private IEnumerator StartSequence()
        {
            // 먼저 보드 채우기
            yield return StartCoroutine(Fill());

            // 카운트다운 동안 입력 차단 (PREGAME 상태 유지)
            _currentState = GameState.PREGAME;

            // 보드 채운 후 카메라 재조정 (그리드 크기 확정 후)
            var camSetup = Camera.main != null ? Camera.main.GetComponent<MobileCameraSetup>() : null;
            if (camSetup != null) camSetup.Adjust();

            // BoosterUI 생성
            var boosterObj = new GameObject("BoosterUI");
            _boosterUI = boosterObj.AddComponent<BoosterUI>();
            _boosterUI.Init(this);

            // 카운트다운 3-2-1
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                for (int i = 3; i >= 1; i--)
                {
                    yield return StartCoroutine(ShowCountdownNumber(canvas, i.ToString()));
                }
                yield return StartCoroutine(ShowCountdownNumber(canvas, "GO!"));
            }

            // 카운트다운 완료 후 입력 허용
            _currentState = GameState.READY;
            ResetHintTimer();
        }

        private IEnumerator ShowCountdownNumber(Canvas canvas, string text)
        {
            GameObject obj = new GameObject("Countdown");
            obj.transform.SetParent(canvas.transform, false);

            Text uiText = obj.AddComponent<Text>();
            uiText.text = text;
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiText.fontSize = 100;
            uiText.fontStyle = FontStyle.Bold;
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.color = Color.white;

            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            outline.effectDistance = new Vector2(4, -4);

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(400, 150);

            // 팝 애니메이션
            float duration = 0.6f;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float progress = t / duration;
                float scale;
                float alpha;

                if (progress < 0.2f)
                {
                    scale = Mathf.Lerp(2f, 1f, progress / 0.2f);
                    alpha = Mathf.Lerp(0f, 1f, progress / 0.2f);
                }
                else if (progress < 0.7f)
                {
                    scale = 1f;
                    alpha = 1f;
                }
                else
                {
                    scale = 1f;
                    alpha = Mathf.Lerp(1f, 0f, (progress - 0.7f) / 0.3f);
                }

                rt.localScale = new Vector3(scale, scale, 1f);
                uiText.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }

            Destroy(obj);
        }

        // === 점수 팝업 ===

        private void ShowScorePopup(Vector3 worldPos, int score)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            GameObject obj = new GameObject("ScorePopup");
            obj.transform.SetParent(canvas.transform, false);

            Text text = obj.AddComponent<Text>();
            text.text = "+" + score;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 32;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.8f);
            outline.effectDistance = new Vector2(2, -2);

            // 월드 좌표 → 스크린 → 캔버스 로컬
            RectTransform rt = obj.GetComponent<RectTransform>();
            Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(), screenPos, canvas.worldCamera, out Vector2 localPos);
            rt.anchoredPosition = localPos;
            rt.sizeDelta = new Vector2(200, 50);

            StartCoroutine(ScorePopupAnimation(obj, text));
        }

        private IEnumerator ScorePopupAnimation(GameObject obj, Text text)
        {
            float duration = 0.8f;
            RectTransform rt = obj.GetComponent<RectTransform>();
            Vector2 startPos = rt.anchoredPosition;
            Color startColor = text.color;

            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float progress = t / duration;
                rt.anchoredPosition = startPos + new Vector2(0, progress * 60f);
                text.color = new Color(startColor.r, startColor.g, startColor.b, 1f - progress);
                yield return null;
            }

            Destroy(obj);
        }

        // === 콤보 텍스트 UI ===

        private void ShowComboText(int combo)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            GameObject comboObj = new GameObject("ComboText");
            comboObj.transform.SetParent(canvas.transform, false);

            Text text = comboObj.AddComponent<Text>();
            text.text = "x" + combo + " COMBO!";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 60;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.9f, 0.1f); // 노란색

            // 외곽선
            Outline outline = comboObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.8f, 0.2f, 0f);
            outline.effectDistance = new Vector2(3, -3);

            RectTransform rt = comboObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 100);
            rt.sizeDelta = new Vector2(500, 100);

            StartCoroutine(ComboTextAnimation(comboObj, text));
        }

        private IEnumerator ComboTextAnimation(GameObject obj, Text text)
        {
            float duration = 1.2f;
            RectTransform rt = obj.GetComponent<RectTransform>();
            Vector2 startPos = rt.anchoredPosition;
            Color startColor = text.color;

            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float progress = t / duration;
                // 위로 올라가면서 페이드 아웃
                rt.anchoredPosition = startPos + new Vector2(0, progress * 80f);
                text.color = new Color(startColor.r, startColor.g, startColor.b, 1f - progress);

                // 초반 스케일 팝
                float scale = progress < 0.15f ? Mathf.Lerp(0.5f, 1.2f, progress / 0.15f) :
                              progress < 0.3f ? Mathf.Lerp(1.2f, 1f, (progress - 0.15f) / 0.15f) : 1f;
                rt.localScale = new Vector3(scale, scale, 1f);

                yield return null;
            }

            Destroy(obj);
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

        public void ResetHintTimer()
        {
            _hintTimer = hintDelay;
        }

        private void ShowHint()
        {
            _hintPieces = FindValidMove();
            if (_hintPieces == null) return;

            _hintCoroutine = StartCoroutine(HintPulseCoroutine());
        }

        public void StopHint()
        {
            if (_hintCoroutine != null)
            {
                StopCoroutine(_hintCoroutine);
                _hintCoroutine = null;
            }

            // 힌트 타일 투명도 복원
            if (_hintPieces != null)
            {
                foreach (var piece in _hintPieces)
                {
                    if (piece != null && piece.gameObject != null)
                    {
                        piece.transform.localScale = Vector3.one;
                        var sr = piece.transform.Find("piece")?.GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 1f);
                        }
                    }
                }
                _hintPieces = null;
            }
        }

        private IEnumerator HintPulseCoroutine()
        {
            float pulseSpeed = 1.5f; // 부드러운 펄스 속도
            float elapsed = 0f;

            while (true)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Sin(elapsed * pulseSpeed * Mathf.PI * 2f) * 0.5f + 0.5f; // 0~1

                // 확실한 깜빡임 (alpha 0.3~1.0) + 부드러운 스케일 펄스 (1.0~1.1)
                float alpha = Mathf.Lerp(0.3f, 1f, t);
                float scale = Mathf.Lerp(1f, 1.1f, t);

                if (_hintPieces != null)
                {
                    foreach (var piece in _hintPieces)
                    {
                        if (piece != null && piece.gameObject != null)
                        {
                            // 부드러운 스케일 펄스
                            piece.transform.localScale = new Vector3(scale, scale, 1f);

                            // 투명도 깜빡임
                            var sr = piece.transform.Find("piece")?.GetComponent<SpriteRenderer>();
                            if (sr != null)
                            {
                                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
                            }
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

        public void ForceShuffleBoard()
        {
            StartCoroutine(ShuffleBoard());
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

            // 셔플 완료 후 입력 허용 (ForceShuffleBoard에서 호출 시 필수)
            if (_currentState == GameState.SHUFFLING)
            {
                _currentState = GameState.READY;
                ResetHintTimer();
            }
        }

    }
}
