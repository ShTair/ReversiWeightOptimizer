namespace ReversiWeightOptimizer.Reversi;

internal readonly struct Move(int x, int y)
{
    public int X { get; } = x;

    public int Y { get; } = y;
}
