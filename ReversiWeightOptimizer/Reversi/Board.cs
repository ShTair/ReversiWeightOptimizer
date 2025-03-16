namespace ReversiWeightOptimizer.Reversi;

internal class Board
{
    public const byte EMPTY = 0;
    public const byte BLACK = 1;
    public const byte WHITE = 2;

    private const int SIZE = 8;
    private ulong blackStones;
    private ulong whiteStones;

    public Board()
    {
        blackStones = 0;
        whiteStones = 0;
        SetStone(3, 3, WHITE);
        SetStone(4, 4, WHITE);
        SetStone(3, 4, BLACK);
        SetStone(4, 3, BLACK);
    }

    private int GetIndex(int x, int y) => y * SIZE + x;

    public byte GetState(int x, int y)
    {
        ulong mask = 1UL << GetIndex(x, y);
        if ((blackStones & mask) != 0) return BLACK;
        if ((whiteStones & mask) != 0) return WHITE;
        return EMPTY;
    }

    public void SetStone(int x, int y, byte color)
    {
        ulong mask = 1UL << GetIndex(x, y);
        if (color == BLACK)
        {
            blackStones |= mask;
            whiteStones &= ~mask;
        }
        else if (color == WHITE)
        {
            whiteStones |= mask;
            blackStones &= ~mask;
        }
        else // EMPTY
        {
            blackStones &= ~mask;
            whiteStones &= ~mask;
        }
    }

    public Board Copy()
    {
        var newBoard = new Board
        {
            blackStones = this.blackStones,
            whiteStones = this.whiteStones
        };
        return newBoard;
    }

    public void PrintBoard()
    {
        var s = (Console.ForegroundColor, Console.BackgroundColor);
        for (int y = 0; y < SIZE; y++)
        {
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            for (int x = 0; x < SIZE; x++)
            {
                byte state = GetState(x, y);
                if (state == WHITE) Console.ForegroundColor = ConsoleColor.White;
                else if (state == EMPTY) Console.ForegroundColor = ConsoleColor.DarkGreen;
                else Console.ForegroundColor = ConsoleColor.Black;
                Console.Write(state switch { 1 => "● ", 2 => "● ", _ => "　", });
            }
            Console.BackgroundColor = s.BackgroundColor;
            Console.WriteLine();
        }
        (Console.ForegroundColor, Console.BackgroundColor) = s;
        Console.WriteLine();
    }
}
