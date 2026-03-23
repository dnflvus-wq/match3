using System.Collections.Generic;
using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 매치 검사 알고리즘 — GameGrid.GetMatch() + FindValidMove()에서 추출.
    /// GamePiece 배열을 파라미터로 받아서 매치를 검사합니다.
    /// (향후 BoardModel의 TileData로 완전 전환 예정)
    /// </summary>
    public static class MatchFinder
    {
        /// <summary>특정 위치에서 3매치 이상을 찾습니다.</summary>
        public static List<GamePiece> GetMatch(GamePiece[,] pieces, GamePiece piece, int newX, int newY, int width, int height)
        {
            if (!piece.IsColored()) return null;
            var color = piece.ColorComponent.Color;
            var horizontalPieces = new List<GamePiece>();
            var verticalPieces = new List<GamePiece>();
            var matchingPieces = new List<GamePiece>();

            // === 가로 매치 검사 ===
            horizontalPieces.Add(piece);

            for (int dir = 0; dir <= 1; dir++)
            {
                for (int xOffset = 1; xOffset < width; xOffset++)
                {
                    int x = dir == 0 ? newX - xOffset : newX + xOffset;
                    if (x < 0 || x >= width) break;

                    if (pieces[x, newY].IsColored() && pieces[x, newY].ColorComponent.Color == color)
                        horizontalPieces.Add(pieces[x, newY]);
                    else
                        break;
                }
            }

            if (horizontalPieces.Count >= 3)
            {
                matchingPieces.AddRange(horizontalPieces);

                // 가로 매치된 타일들에서 세로 확장 (L/T자 검사)
                for (int i = 0; i < horizontalPieces.Count; i++)
                {
                    for (int dir = 0; dir <= 1; dir++)
                    {
                        for (int yOffset = 1; yOffset < height; yOffset++)
                        {
                            int y = dir == 0 ? newY - yOffset : newY + yOffset;
                            if (y < 0 || y >= height) break;

                            if (pieces[horizontalPieces[i].X, y].IsColored() &&
                                pieces[horizontalPieces[i].X, y].ColorComponent.Color == color)
                                verticalPieces.Add(pieces[horizontalPieces[i].X, y]);
                            else
                                break;
                        }
                    }

                    if (verticalPieces.Count < 2)
                        verticalPieces.Clear();
                    else
                    {
                        matchingPieces.AddRange(verticalPieces);
                        break;
                    }
                }
            }

            if (matchingPieces.Count >= 3) return matchingPieces;

            // === 세로 매치 검사 ===
            horizontalPieces.Clear();
            verticalPieces.Clear();
            verticalPieces.Add(piece);

            for (int dir = 0; dir <= 1; dir++)
            {
                for (int yOffset = 1; yOffset < height; yOffset++)
                {
                    int y = dir == 0 ? newY - yOffset : newY + yOffset;
                    if (y < 0 || y >= height) break;

                    if (pieces[newX, y].IsColored() && pieces[newX, y].ColorComponent.Color == color)
                        verticalPieces.Add(pieces[newX, y]);
                    else
                        break;
                }
            }

            if (verticalPieces.Count >= 3)
            {
                matchingPieces.AddRange(verticalPieces);

                // 세로 매치된 타일들에서 가로 확장 (L/T자 검사)
                for (int i = 0; i < verticalPieces.Count; i++)
                {
                    for (int dir = 0; dir <= 1; dir++)
                    {
                        for (int xOffset = 1; xOffset < width; xOffset++)
                        {
                            int x = dir == 0 ? newX - xOffset : newX + xOffset;
                            if (x < 0 || x >= width) break;

                            if (pieces[x, verticalPieces[i].Y].IsColored() &&
                                pieces[x, verticalPieces[i].Y].ColorComponent.Color == color)
                                horizontalPieces.Add(pieces[x, verticalPieces[i].Y]);
                            else
                                break;
                        }
                    }

                    if (horizontalPieces.Count < 2)
                        horizontalPieces.Clear();
                    else
                    {
                        matchingPieces.AddRange(horizontalPieces);
                        break;
                    }
                }
            }

            return matchingPieces.Count >= 3 ? matchingPieces : null;
        }

        /// <summary>보드에서 유효한 이동(힌트)을 찾습니다. 없으면 null (데드 보드).</summary>
        public static List<GamePiece> FindValidMove(GamePiece[,] pieces, int width, int height)
        {
            int[,] dirs = { {1,0}, {-1,0}, {0,1}, {0,-1} };

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (!pieces[x, y].IsColored() || !pieces[x, y].IsMovable()) continue;

                    for (int d = 0; d < 4; d++)
                    {
                        int nx = x + dirs[d, 0];
                        int ny = y + dirs[d, 1];

                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                        if (!pieces[nx, ny].IsMovable()) continue;

                        // 가상 스왑
                        (pieces[x, y], pieces[nx, ny]) = (pieces[nx, ny], pieces[x, y]);

                        var match1 = GetMatch(pieces, pieces[x, y], x, y, width, height);
                        var match2 = GetMatch(pieces, pieces[nx, ny], nx, ny, width, height);

                        // 복원
                        (pieces[x, y], pieces[nx, ny]) = (pieces[nx, ny], pieces[x, y]);

                        if (match1 != null || match2 != null)
                            return new List<GamePiece> { pieces[x, y], pieces[nx, ny] };
                    }
                }
            }

            return null;
        }
    }
}
