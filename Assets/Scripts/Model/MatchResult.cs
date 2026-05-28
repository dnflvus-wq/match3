using System.Collections.Generic;

namespace Match3
{
    public struct MatchResult
    {
        public List<(int x, int y)> Tiles;
        public PieceType SpecialType;  // PieceType.Count = 특수 타일 없음
        public int SpecialX;
        public int SpecialY;
        public ColorType Color;
    }

    public enum SwapResult
    {
        NoMatch,
        Match,
        RainbowDouble
    }
}
