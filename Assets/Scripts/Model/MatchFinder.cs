using System;
using System.Collections.Generic;

namespace Match3
{
    /// <summary>
    /// 매치 탐지, 데드보드 감지, 힌트. 순수 C# — UnityEngine 의존 없음.
    /// </summary>
    public static class MatchFinder
    {
        private static readonly Random _rng = new Random();
        /// <summary>
        /// 특정 위치에서 3+매치를 찾는다. L/T자 교차도 감지.
        /// </summary>
        public static List<(int x, int y)> GetMatch(IBoardState board, int newX, int newY)
        {
            if (!board.IsColored(newX, newY)) return null;
            var color = board.GetColor(newX, newY);
            var horizontal = new List<(int, int)>();
            var vertical = new List<(int, int)>();
            var result = new List<(int, int)>();

            horizontal.Add((newX, newY));

            // 가로 스캔
            for (int dir = -1; dir <= 1; dir += 2)
            {
                for (int offset = 1; offset < board.Width; offset++)
                {
                    int x = newX + dir * offset;
                    if (x < 0 || x >= board.Width) break;
                    if (board.IsColored(x, newY) && board.GetColor(x, newY) == color)
                        horizontal.Add((x, newY));
                    else
                        break;
                }
            }

            if (horizontal.Count >= 3)
            {
                result.AddRange(horizontal);

                // L/T자 감지: 가로 매치의 각 타일에서 세로 확장
                foreach (var (hx, hy) in horizontal)
                {
                    var cross = new List<(int, int)>();
                    for (int dir = -1; dir <= 1; dir += 2)
                    {
                        for (int offset = 1; offset < board.Height; offset++)
                        {
                            int y = newY + dir * offset;
                            if (y < 0 || y >= board.Height) break;
                            if (board.IsColored(hx, y) && board.GetColor(hx, y) == color)
                                cross.Add((hx, y));
                            else
                                break;
                        }
                    }
                    if (cross.Count >= 2)
                    {
                        result.AddRange(cross);
                        break;
                    }
                }

                if (result.Count >= 3)
                    return result;
            }

            // 세로 스캔
            horizontal.Clear();
            vertical.Clear();
            result.Clear();
            vertical.Add((newX, newY));

            for (int dir = -1; dir <= 1; dir += 2)
            {
                for (int offset = 1; offset < board.Height; offset++)
                {
                    int y = newY + dir * offset;
                    if (y < 0 || y >= board.Height) break;
                    if (board.IsColored(newX, y) && board.GetColor(newX, y) == color)
                        vertical.Add((newX, y));
                    else
                        break;
                }
            }

            if (vertical.Count >= 3)
            {
                result.AddRange(vertical);

                // L/T자 감지: 세로 매치의 각 타일에서 가로 확장
                foreach (var (vx, vy) in vertical)
                {
                    var cross = new List<(int, int)>();
                    for (int dir = -1; dir <= 1; dir += 2)
                    {
                        for (int offset = 1; offset < board.Width; offset++)
                        {
                            int x = newX + dir * offset;
                            if (x < 0 || x >= board.Width) break;
                            if (board.IsColored(x, vy) && board.GetColor(x, vy) == color)
                                cross.Add((x, vy));
                            else
                                break;
                        }
                    }
                    if (cross.Count >= 2)
                    {
                        result.AddRange(cross);
                        break;
                    }
                }

                if (result.Count >= 3)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// 보드 전체에서 매치를 찾고, 특수 타일 생성 정보를 결정한다.
        /// swapFrom/swapTo: 플레이어가 스왑한 두 위치 (null이면 캐스케이드 매치).
        /// </summary>
        public static List<MatchResult> FindAllMatches(
            IBoardState board,
            (int x, int y)? swapFrom = null,
            (int x, int y)? swapTo = null)
        {
            var results = new List<MatchResult>();
            var alreadyMatched = new HashSet<(int, int)>();

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    if (alreadyMatched.Contains((x, y))) continue;

                    var tiles = GetMatch(board, x, y);
                    if (tiles == null) continue;

                    // 중복 제거
                    var unique = new List<(int x, int y)>();
                    foreach (var t in tiles)
                    {
                        if (alreadyMatched.Add(t))
                            unique.Add(t);
                    }

                    if (unique.Count < 3) continue;

                    var mr = new MatchResult
                    {
                        Tiles = unique,
                        Color = board.GetColor(x, y),
                        SpecialType = PieceType.Count, // 없음
                        SpecialX = unique[0].x,
                        SpecialY = unique[0].y
                    };

                    // 특수 타일 결정
                    if (unique.Count == 4)
                    {
                        if (swapFrom == null || swapTo == null)
                        {
                            // 캐스케이드: 랜덤 방향
                            mr.SpecialType = _rng.Next(2) == 0
                                ? PieceType.RowClear : PieceType.ColumnClear;
                        }
                        else if (swapFrom.Value.y == swapTo.Value.y)
                            mr.SpecialType = PieceType.RowClear;
                        else
                            mr.SpecialType = PieceType.ColumnClear;
                    }
                    else if (unique.Count >= 5)
                    {
                        mr.SpecialType = PieceType.Rainbow;
                    }

                    // 스왑 위치에 특수 타일 배치 (가능하면)
                    if (mr.SpecialType != PieceType.Count && swapFrom != null)
                    {
                        foreach (var t in unique)
                        {
                            if (t == swapFrom.Value || t == swapTo.Value)
                            {
                                mr.SpecialX = t.x;
                                mr.SpecialY = t.y;
                                break;
                            }
                        }
                    }

                    results.Add(mr);
                }
            }

            return results;
        }

        /// <summary>
        /// 유효한 이동을 하나 찾는다 (힌트/데드보드 감지). null이면 데드보드.
        /// </summary>
        public static (int x1, int y1, int x2, int y2)? FindValidMove(IBoardState board)
        {
            int[,] dirs = { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } };

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    if (!board.IsColored(x, y) || !board.IsMovable(x, y)) continue;

                    for (int d = 0; d < 4; d++)
                    {
                        int nx = x + dirs[d, 0];
                        int ny = y + dirs[d, 1];
                        if (nx < 0 || nx >= board.Width || ny < 0 || ny >= board.Height) continue;
                        if (!board.IsMovable(nx, ny)) continue;

                        // IBoardState는 읽기 전용이라 가상 스왑 불가 → SwappedBoardView 사용
                        var swapped = new SwappedBoard(board, x, y, nx, ny);
                        if (GetMatch(swapped, x, y) != null || GetMatch(swapped, nx, ny) != null)
                            return (x, y, nx, ny);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 가상 스왑된 보드. 원본을 변경하지 않고 두 위치가 교환된 것처럼 보이게 한다.
        /// </summary>
        private class SwappedBoard : IBoardState
        {
            private readonly IBoardState _inner;
            private readonly int _ax, _ay, _bx, _by;

            public SwappedBoard(IBoardState inner, int ax, int ay, int bx, int by)
            {
                _inner = inner;
                _ax = ax; _ay = ay; _bx = bx; _by = by;
            }

            public int Width => _inner.Width;
            public int Height => _inner.Height;

            private (int x, int y) Remap(int x, int y)
            {
                if (x == _ax && y == _ay) return (_bx, _by);
                if (x == _bx && y == _by) return (_ax, _ay);
                return (x, y);
            }

            public bool IsColored(int x, int y) { var (rx, ry) = Remap(x, y); return _inner.IsColored(rx, ry); }
            public bool IsMovable(int x, int y) { var (rx, ry) = Remap(x, y); return _inner.IsMovable(rx, ry); }
            public bool IsClearable(int x, int y) { var (rx, ry) = Remap(x, y); return _inner.IsClearable(rx, ry); }
            public ColorType GetColor(int x, int y) { var (rx, ry) = Remap(x, y); return _inner.GetColor(rx, ry); }
            public PieceType GetType(int x, int y) { var (rx, ry) = Remap(x, y); return _inner.GetType(rx, ry); }
        }
    }
}
