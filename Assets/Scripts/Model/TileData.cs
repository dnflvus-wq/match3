namespace Match3
{
    /// <summary>
    /// 타일의 순수 데이터 — Unity 의존 없음.
    /// GamePiece(MonoBehaviour)의 데이터 부분만 추출한 구조체.
    /// </summary>
    [System.Serializable]
    public struct TileData
    {
        public PieceType pieceType;
        public ColorType colorType;
        public bool isEmpty;
        public int score;

        public static TileData Empty => new TileData
        {
            pieceType = PieceType.Empty,
            colorType = ColorType.Any,
            isEmpty = true,
            score = 0
        };

        public static TileData Normal(ColorType color) => new TileData
        {
            pieceType = PieceType.Normal,
            colorType = color,
            isEmpty = false,
            score = 100
        };

        public bool IsNormal => pieceType == PieceType.Normal;
        public bool IsSpecial => pieceType == PieceType.RowClear ||
                                  pieceType == PieceType.ColumnClear ||
                                  pieceType == PieceType.Rainbow;
    }
}
