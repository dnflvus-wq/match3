using System.Collections.Generic;
using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 게임 보드의 순수 데이터 모델 — Unity API 의존 없음.
    /// 타일 배열을 관리하고 좌표 유효성을 검증합니다.
    /// 현재는 GameGrid._pieces[,]와 병렬로 동작하는 중간 단계입니다.
    /// (향후 GameGrid._pieces를 완전히 대체할 예정)
    /// </summary>
    public class BoardModel
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        private TileData[,] _tiles;

        public BoardModel(int width, int height)
        {
            Width = width;
            Height = height;
            _tiles = new TileData[width, height];
        }

        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public TileData GetTileAt(int x, int y)
        {
            if (!IsValidPosition(x, y)) return TileData.Empty;
            return _tiles[x, y];
        }

        public void SetTileAt(int x, int y, TileData data)
        {
            if (!IsValidPosition(x, y)) return;
            _tiles[x, y] = data;
        }

        public void SwapData(int x1, int y1, int x2, int y2)
        {
            if (!IsValidPosition(x1, y1) || !IsValidPosition(x2, y2)) return;
            (_tiles[x1, y1], _tiles[x2, y2]) = (_tiles[x2, y2], _tiles[x1, y1]);
        }

        public void Clear(int x, int y)
        {
            if (!IsValidPosition(x, y)) return;
            _tiles[x, y] = TileData.Empty;
        }

        public bool IsEmpty(int x, int y)
        {
            if (!IsValidPosition(x, y)) return true;
            return _tiles[x, y].isEmpty || _tiles[x, y].pieceType == PieceType.Empty;
        }

        /// <summary>GamePiece 배열에서 데이터를 동기화 (중간 단계용)</summary>
        public void SyncFromGamePieces(GamePiece[,] pieces)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (pieces[x, y] == null)
                    {
                        _tiles[x, y] = TileData.Empty;
                        continue;
                    }

                    var gp = pieces[x, y];
                    _tiles[x, y] = new TileData
                    {
                        pieceType = gp.Type,
                        colorType = gp.IsColored() ? gp.ColorComponent.Color : ColorType.Any,
                        isEmpty = gp.Type == PieceType.Empty,
                        score = gp.score
                    };
                }
            }
        }

        /// <summary>디버그용: 보드 상태를 문자열로 출력</summary>
        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var t = _tiles[x, y];
                    sb.Append(t.isEmpty ? "." : ((int)t.colorType).ToString());
                    sb.Append(" ");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
