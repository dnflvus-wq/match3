namespace Match3
{
    /// <summary>
    /// 보드 상태 읽기 전용 인터페이스. 순수 C# — UnityEngine 의존 없음.
    /// GameGrid(현재)와 BoardModel(미래) 모두 이 인터페이스를 구현.
    /// </summary>
    public interface IBoardState
    {
        int Width { get; }
        int Height { get; }
        bool IsColored(int x, int y);
        bool IsMovable(int x, int y);
        bool IsClearable(int x, int y);
        ColorType GetColor(int x, int y);
        PieceType GetType(int x, int y);
    }
}
