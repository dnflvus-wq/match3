using System.Collections.Generic;
using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 보드 상태 관리 + 게임 로직 (매치, 클리어, 낙하, 셔플).
    /// 코루틴/타이밍 없음 — 순수 데이터 조작만.
    /// 시각 효과는 BoardView에 위임.
    /// </summary>
    public class BoardModel : MonoBehaviour, IBoardState
    {
        private const int MaxShuffleAttempts = 30;
        private const float ColorWeightMin = 0.1f;
        private const float ColorWeightBalance = 0.5f;

        /// <summary>보드 가로 크기 (IBoardState).</summary>
        public int Width => _controller.xDim;
        /// <summary>보드 세로 크기 (IBoardState).</summary>
        public int Height => _controller.yDim;

        private bool InBounds(int x, int y) => x >= 0 && x < _controller.xDim && y >= 0 && y < _controller.yDim;

        bool IBoardState.IsColored(int x, int y) => InBounds(x, y) && _pieces[x, y].IsColored();
        bool IBoardState.IsMovable(int x, int y) => InBounds(x, y) && _pieces[x, y].IsMovable();
        bool IBoardState.IsClearable(int x, int y) => InBounds(x, y) && _pieces[x, y].IsClearable();
        ColorType IBoardState.GetColor(int x, int y) => InBounds(x, y) && _pieces[x, y].IsColored() ? _pieces[x, y].ColorComponent.Color : ColorType.Any;
        PieceType IBoardState.GetType(int x, int y) => InBounds(x, y) ? _pieces[x, y].Type : PieceType.Empty;

        private GameGrid _controller;
        private Dictionary<PieceType, GameObject> _piecePrefabDict;
        private GamePiece[,] _pieces;
        private bool _inverse;

        /// <summary>낙하+매치 시퀀스 진행 중 여부.</summary>
        public bool IsFilling { get; set; }

        /// <summary>점수 팝업 요청 이벤트 (위치, 점수).</summary>
        public event System.Action<Vector3, int> OnScorePopup;
        /// <summary>피스 클리어 이벤트 (위치, 색상) — 파티클 재생용.</summary>
        public event System.Action<Vector3, Color> OnPieceCleared;
        /// <summary>매치 발생 이벤트 (콤보 수) — 매치 사운드 재생용.</summary>
        public event System.Action<int> OnMatchFound;
        /// <summary>빅매치(4+) 이벤트 (중심 위치, 색상) — 카메라 쉐이크 + 파티클 + 특수 사운드.</summary>
        public event System.Action<Vector3, Color> OnBigMatch;
        /// <summary>망치 타격 이벤트 — 카메라 쉐이크 + 특수 사운드.</summary>
        public event System.Action OnHammerHit;

        /// <summary>이 모델을 소유한 GameGrid 컨트롤러.</summary>
        public GameGrid Controller => _controller;

        /// <summary>컨트롤러 참조를 설정하고 보드를 초기화한다.</summary>
        public void Init(GameGrid controller)
        {
            _controller = controller;

            _piecePrefabDict = new Dictionary<PieceType, GameObject>();
            foreach (var pp in controller.piecePrefabs)
                if (!_piecePrefabDict.ContainsKey(pp.type))
                    _piecePrefabDict.Add(pp.type, pp.prefab);

            int xDim = controller.xDim, yDim = controller.yDim;

            for (int x = 0; x < xDim; x++)
                for (int y = 0; y < yDim; y++)
                    Instantiate(controller.backgroundPrefab, GetWorldPosition(x, y), Quaternion.identity, transform);

            _pieces = new GamePiece[xDim, yDim];

            foreach (var ip in controller.initialPieces)
                if (ip.x >= 0 && ip.x < xDim && ip.y >= 0 && ip.y < yDim)
                    SpawnNewPiece(ip.x, ip.y, ip.type);

            for (int x = 0; x < xDim; x++)
                for (int y = 0; y < yDim; y++)
                    if (_pieces[x, y] == null)
                        SpawnNewPiece(x, y, PieceType.Empty);
        }

        /// <summary>보드 좌표(x, y)를 월드 좌표로 변환한다.</summary>
        internal Vector2 GetWorldPosition(int x, int y)
        {
            return new Vector2(
                transform.position.x - _controller.xDim / 2.0f + x + 0.5f,
                transform.position.y + _controller.yDim / 2.0f - y - 0.5f);
        }

        /// <summary>지정 좌표의 피스를 반환한다. 범위 밖이면 null.</summary>
        public GamePiece GetPieceAt(int x, int y)
        {
            if (x < 0 || x >= _controller.xDim || y < 0 || y >= _controller.yDim) return null;
            return _pieces[x, y];
        }

        /// <summary>지정 좌표에 새 피스를 생성하고 배열에 등록한다.</summary>
        public GamePiece SpawnNewPiece(int x, int y, PieceType type)
        {
            var go = Instantiate(_piecePrefabDict[type], GetWorldPosition(x, y), Quaternion.identity, transform);
            _pieces[x, y] = go.GetComponent<GamePiece>();
            _pieces[x, y].Init(x, y, _controller, type);
            return _pieces[x, y];
        }

        /// <summary>지정 타입의 모든 피스를 반환한다.</summary>
        public List<GamePiece> GetPiecesOfType(PieceType type)
        {
            var result = new List<GamePiece>();
            for (int x = 0; x < _controller.xDim; x++)
                for (int y = 0; y < _controller.yDim; y++)
                    if (_pieces[x, y].Type == type) result.Add(_pieces[x, y]);
            return result;
        }

        /// <summary>낙하 1스텝을 실행한다. 이동이 있었으면 true.</summary>
        public bool FillStep()
        {
            int xDim = _controller.xDim, yDim = _controller.yDim;
            bool movedPiece = false;

            for (int y = yDim - 2; y >= 0; y--)
            {
                for (int loopX = 0; loopX < xDim; loopX++)
                {
                    int x = _inverse ? xDim - 1 - loopX : loopX;
                    GamePiece piece = _pieces[x, y];
                    if (!piece.IsMovable()) continue;

                    GamePiece pieceBelow = _pieces[x, y + 1];
                    if (pieceBelow.Type == PieceType.Empty)
                    {
                        Destroy(pieceBelow.gameObject);
                        piece.MovableComponent.MoveDrop(x, y + 1, _controller.fillTime);
                        _pieces[x, y + 1] = piece;
                        SpawnNewPiece(x, y, PieceType.Empty);
                        movedPiece = true;
                    }
                    else
                    {
                        for (int diag = -1; diag <= 1; diag++)
                        {
                            if (diag == 0) continue;
                            int diagX = _inverse ? x - diag : x + diag;
                            if (diagX < 0 || diagX >= xDim) continue;

                            GamePiece diagPiece = _pieces[diagX, y + 1];
                            if (diagPiece.Type != PieceType.Empty) continue;

                            bool hasPieceAbove = true;
                            for (int aboveY = y; aboveY >= 0; aboveY--)
                            {
                                GamePiece above = _pieces[diagX, aboveY];
                                if (above.IsMovable()) break;
                                if (above.Type != PieceType.Empty) { hasPieceAbove = false; break; }
                            }
                            if (hasPieceAbove) continue;

                            Destroy(diagPiece.gameObject);
                            piece.MovableComponent.MoveDrop(diagX, y + 1, _controller.fillTime);
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
                var go = Instantiate(_piecePrefabDict[PieceType.Normal], GetWorldPosition(x, -1), Quaternion.identity, transform);
                _pieces[x, 0] = go.GetComponent<GamePiece>();
                _pieces[x, 0].Init(x, -1, _controller, PieceType.Normal);
                _pieces[x, 0].MovableComponent.MoveDrop(x, 0, _controller.fillTime);
                _pieces[x, 0].ColorComponent.SetColor(GetWeightedRandomColor(_pieces[x, 0].ColorComponent.NumColors));
                movedPiece = true;
            }

            return movedPiece;
        }

        /// <summary>낙하 방향(좌→우 / 우→좌)을 토글한다.</summary>
        public void ToggleInverse() => _inverse = !_inverse;

        /// <summary>두 피스의 배열 위치를 교환한다 (애니메이션 전).</summary>
        public void SwapData(GamePiece piece1, GamePiece piece2)
        {
            int p1X = piece1.X, p1Y = piece1.Y;
            int p2X = piece2.X, p2Y = piece2.Y;
            _pieces[p1X, p1Y] = piece2;
            _pieces[p2X, p2Y] = piece1;
        }

        /// <summary>스왑을 되돌린다 (매치 실패 시).</summary>
        public void UndoSwapData(GamePiece piece1, GamePiece piece2)
        {
            int p1X = piece1.X, p1Y = piece1.Y;
            int p2X = piece2.X, p2Y = piece2.Y;
            _pieces[p1X, p1Y] = piece1;
            _pieces[p2X, p2Y] = piece2;
        }

        /// <summary>지정 좌표에 3+매치가 있는지 판정한다.</summary>
        public bool HasMatchAt(int x, int y) => MatchFinder.GetMatch(this, x, y) != null;

        /// <summary>스왑 결과를 평가한다. Rainbow, 특수 타일 처리 포함.</summary>
        public SwapResult EvaluateSwap(GamePiece piece1, GamePiece piece2)
        {
            int p1X = piece1.X, p1Y = piece1.Y;
            int p2X = piece2.X, p2Y = piece2.Y;

            bool hasMatch = HasMatchAt(p2X, p2Y) || HasMatchAt(p1X, p1Y) ||
                            piece1.Type == PieceType.Rainbow || piece2.Type == PieceType.Rainbow;

            if (!hasMatch) return SwapResult.NoMatch;

            piece1.MovableComponent.SetPosition(p2X, p2Y);
            piece2.MovableComponent.SetPosition(p1X, p1Y);

            // Rainbow + Rainbow → 전체 보드 클리어
            if (piece1.Type == PieceType.Rainbow && piece2.Type == PieceType.Rainbow)
            {
                ClearPiece(piece1.X, piece1.Y);
                ClearPiece(piece2.X, piece2.Y);
                for (int cx = 0; cx < _controller.xDim; cx++)
                    for (int cy = 0; cy < _controller.yDim; cy++)
                        ClearPiece(cx, cy);
                return SwapResult.RainbowDouble;
            }

            // Rainbow + Color
            if (piece1.Type == PieceType.Rainbow && piece1.IsClearable() && piece2.IsColored())
            {
                var cc = piece1.GetComponent<ClearColorPiece>();
                if (cc) cc.Color = piece2.ColorComponent.Color;
                ClearPiece(piece1.X, piece1.Y);
            }
            if (piece2.Type == PieceType.Rainbow && piece2.IsClearable() && piece1.IsColored())
            {
                var cc = piece2.GetComponent<ClearColorPiece>();
                if (cc) cc.Color = piece1.ColorComponent.Color;
                ClearPiece(piece2.X, piece2.Y);
            }

            ClearAllValidMatches();

            // RowClear / ColumnClear
            if (piece1.Type == PieceType.RowClear || piece1.Type == PieceType.ColumnClear)
                ClearPiece(piece1.X, piece1.Y);
            if (piece2.Type == PieceType.RowClear || piece2.Type == PieceType.ColumnClear)
                ClearPiece(piece2.X, piece2.Y);

            return SwapResult.Match;
        }

        /// <summary>보드 전체 매치를 찾아 클리어한다. 리필 필요하면 true.</summary>
        public bool ClearAllValidMatches()
        {
            var swap1 = _controller.SwapPiece1;
            var swap2 = _controller.SwapPiece2;
            (int x, int y)? swapFrom = swap1 != null ? (swap1.X, swap1.Y) : null;
            (int x, int y)? swapTo = swap2 != null ? (swap2.X, swap2.Y) : null;

            var matches = MatchFinder.FindAllMatches(this, swapFrom, swapTo);
            if (matches.Count == 0) return false;

            bool needsRefill = false;

            foreach (var mr in matches)
            {
                OnMatchFound?.Invoke(_controller.ComboCount);

                if (mr.Tiles.Count >= 4)
                {
                    Vector3 center = Vector3.zero;
                    foreach (var (tx, ty) in mr.Tiles)
                        center += _pieces[tx, ty].transform.position;
                    center /= mr.Tiles.Count;
                    OnBigMatch?.Invoke(center, BoardView.GetColorForType(mr.Color));
                }

                foreach (var (tx, ty) in mr.Tiles)
                    if (ClearPiece(tx, ty)) needsRefill = true;

                if (mr.SpecialType == PieceType.Count) continue;
                if (!InBounds(mr.SpecialX, mr.SpecialY)) continue;

                Destroy(_pieces[mr.SpecialX, mr.SpecialY]);
                GamePiece newPiece = SpawnNewPiece(mr.SpecialX, mr.SpecialY, mr.SpecialType);

                if ((mr.SpecialType == PieceType.RowClear || mr.SpecialType == PieceType.ColumnClear) && newPiece.IsColored())
                    newPiece.ColorComponent.SetColor(mr.Color);
                else if (mr.SpecialType == PieceType.Rainbow && newPiece.IsColored())
                    newPiece.ColorComponent.SetColor(ColorType.Any);
            }

            return needsRefill;
        }

        /// <summary>단일 피스를 클리어한다. 이벤트 발행 + 장애물 처리 포함.</summary>
        public bool ClearPiece(int x, int y)
        {
            if (!InBounds(x, y)) return false;
            if (!_pieces[x, y].IsClearable() || _pieces[x, y].ClearableComponent.IsBeingCleared) return false;

            if (_pieces[x, y].IsColored())
            {
                Vector3 pos = _pieces[x, y].transform.position;
                Color c = BoardView.GetColorForType(_pieces[x, y].ColorComponent.Color);
                OnPieceCleared?.Invoke(pos, c);
                int score = _pieces[x, y].score;
                if (score > 0) OnScorePopup?.Invoke(pos, score);
            }

            _pieces[x, y].ClearableComponent.Clear();
            SpawnNewPiece(x, y, PieceType.Empty);
            ClearObstacles(x, y);
            return true;
        }

        private void ClearObstacles(int x, int y)
        {
            int xDim = _controller.xDim, yDim = _controller.yDim;
            for (int ax = x - 1; ax <= x + 1; ax++)
            {
                if (ax == x || ax < 0 || ax >= xDim) continue;
                if (_pieces[ax, y].Type == PieceType.Bubble && _pieces[ax, y].IsClearable())
                { _pieces[ax, y].ClearableComponent.Clear(); SpawnNewPiece(ax, y, PieceType.Empty); }
            }
            for (int ay = y - 1; ay <= y + 1; ay++)
            {
                if (ay == y || ay < 0 || ay >= yDim) continue;
                if (_pieces[x, ay].Type == PieceType.Bubble && _pieces[x, ay].IsClearable())
                { _pieces[x, ay].ClearableComponent.Clear(); SpawnNewPiece(x, ay, PieceType.Empty); }
            }
        }

        /// <summary>지정 행의 모든 피스를 클리어한다 (RowClear용).</summary>
        public void ClearRow(int row)
        {
            for (int x = 0; x < _controller.xDim; x++) ClearPiece(x, row);
        }

        /// <summary>지정 열의 모든 피스를 클리어한다 (ColumnClear용).</summary>
        public void ClearColumn(int col)
        {
            for (int y = 0; y < _controller.yDim; y++) ClearPiece(col, y);
        }

        /// <summary>지정 색상의 모든 피스를 클리어한다 (Rainbow용).</summary>
        public void ClearColor(ColorType color)
        {
            for (int x = 0; x < _controller.xDim; x++)
                for (int y = 0; y < _controller.yDim; y++)
                    if ((_pieces[x, y].IsColored() && _pieces[x, y].ColorComponent.Color == color) || color == ColorType.Any)
                        ClearPiece(x, y);
        }

        /// <summary>망치 부스터 탭을 처리한다. 피스 클리어 성공 시 true.</summary>
        public bool HandleHammerTap(Vector2 worldPos, BoosterUI boosterUI)
        {
            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
            GamePiece piece = hit.collider != null ? hit.collider.GetComponent<GamePiece>() : null;
            if (piece != null && piece.IsClearable())
            {
                ClearPiece(piece.X, piece.Y);
                boosterUI.UseHammer();
                OnHammerHit?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>보드의 색상을 무작위로 섞는다. 매치 없음 + 유효 이동 있을 때까지 최대 30회 시도.</summary>
        public void ShuffleColors()
        {
            int xDim = _controller.xDim, yDim = _controller.yDim;

            var colorPieces = new List<GamePiece>();
            for (int x = 0; x < xDim; x++)
                for (int y = 0; y < yDim; y++)
                    if (_pieces[x, y].IsColored() && _pieces[x, y].Type == PieceType.Normal)
                        colorPieces.Add(_pieces[x, y]);

            for (int attempt = 0; attempt < MaxShuffleAttempts; attempt++)
            {
                for (int i = colorPieces.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    ColorType tmp = colorPieces[i].ColorComponent.Color;
                    colorPieces[i].ColorComponent.SetColor(colorPieces[j].ColorComponent.Color);
                    colorPieces[j].ColorComponent.SetColor(tmp);
                }

                bool hasMatch = false;
                for (int x = 0; x < xDim && !hasMatch; x++)
                    for (int y = 0; y < yDim && !hasMatch; y++)
                        if (MatchFinder.GetMatch(this, x, y) != null) hasMatch = true;

                if (!hasMatch && MatchFinder.FindValidMove(this) != null) break;
            }
        }

        // === 가중치 랜덤 색상 ===
        private ColorType GetWeightedRandomColor(int numColors)
        {
            int xDim = _controller.xDim, yDim = _controller.yDim;
            int[] counts = new int[numColors];
            int total = 0;

            for (int x = 0; x < xDim; x++)
                for (int y = 0; y < yDim; y++)
                    if (_pieces[x, y].IsColored() && (int)_pieces[x, y].ColorComponent.Color < numColors)
                    { counts[(int)_pieces[x, y].ColorComponent.Color]++; total++; }

            if (total == 0) return (ColorType)Random.Range(0, numColors);

            float[] weights = new float[numColors];
            float totalW = 0f;
            for (int i = 0; i < numColors; i++)
            {
                weights[i] = Mathf.Max(ColorWeightMin, 1f + (total / (float)numColors - counts[i]) * ColorWeightBalance);
                totalW += weights[i];
            }

            float rand = Random.Range(0f, totalW), cum = 0f;
            for (int i = 0; i < numColors; i++)
            {
                cum += weights[i];
                if (rand <= cum) return (ColorType)i;
            }
            return (ColorType)(numColors - 1);
        }
    }
}
