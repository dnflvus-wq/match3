namespace Match3
{
    internal class ClearLinePiece : ClearablePiece
    {
        public bool isRow;

        public override void Clear()
        {
            base.Clear();

            if (isRow)
            {            
                piece.BoardModelRef.ClearRow(piece.Y);
            }
            else
            {            
                piece.BoardModelRef.ClearColumn(piece.X);
            }
        }
    }
}
