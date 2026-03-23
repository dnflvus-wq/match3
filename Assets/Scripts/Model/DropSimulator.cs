using System.Collections.Generic;
using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 낙하 시뮬레이션 — 빈칸을 계산하고 어떤 타일이 어디로 이동해야 하는지 결정합니다.
    /// 현재는 GamePiece 배열에 직접 의존합니다 (향후 TileData 기반으로 전환 예정).
    /// FillStep()의 "계산" 부분만 추출한 것입니다.
    /// </summary>
    public struct DropMove
    {
        public int fromX, fromY;
        public int toX, toY;
        public bool isDiagonal;
    }

    public struct SpawnRequest
    {
        public int x, y;
        public ColorType color;
    }

    public static class DropSimulator
    {
        /// <summary>
        /// 한 스텝의 낙하 이동을 계산합니다.
        /// 실제 이동은 하지 않고 어디서 어디로 가야 하는지만 반환합니다.
        /// </summary>
        public static (List<DropMove> drops, List<SpawnRequest> spawns) CalculateDropStep(
            GamePiece[,] pieces, int width, int height, bool inverse)
        {
            var drops = new List<DropMove>();
            var spawns = new List<SpawnRequest>();

            // 아래에서 위로 스캔 (y가 큰 쪽이 아래)
            for (int y = height - 2; y >= 0; y--)
            {
                for (int loopX = 0; loopX < width; loopX++)
                {
                    int x = inverse ? width - 1 - loopX : loopX;
                    GamePiece piece = pieces[x, y];

                    if (!piece.IsMovable()) continue;

                    GamePiece pieceBelow = pieces[x, y + 1];

                    if (pieceBelow.Type == PieceType.Empty)
                    {
                        drops.Add(new DropMove { fromX = x, fromY = y, toX = x, toY = y + 1 });
                        continue;
                    }

                    // 대각선 이동 체크
                    for (int diag = -1; diag <= 1; diag++)
                    {
                        if (diag == 0) continue;

                        int diagX = inverse ? x - diag : x + diag;
                        if (diagX < 0 || diagX >= width) continue;

                        GamePiece diagonalPiece = pieces[diagX, y + 1];
                        if (diagonalPiece.Type != PieceType.Empty) continue;

                        bool hasPieceAbove = true;
                        for (int aboveY = y; aboveY >= 0; aboveY--)
                        {
                            GamePiece pieceAbove = pieces[diagX, aboveY];
                            if (pieceAbove.IsMovable()) break;
                            if (pieceAbove.Type != PieceType.Empty)
                            {
                                hasPieceAbove = false;
                                break;
                            }
                        }

                        if (hasPieceAbove) continue;

                        drops.Add(new DropMove { fromX = x, fromY = y, toX = diagX, toY = y + 1, isDiagonal = true });
                        break;
                    }
                }
            }

            // 맨 위 행에 빈칸이 있으면 새 타일 스폰 필요
            for (int x = 0; x < width; x++)
            {
                if (pieces[x, 0].Type == PieceType.Empty)
                {
                    spawns.Add(new SpawnRequest { x = x, y = 0 });
                }
            }

            return (drops, spawns);
        }
    }
}
