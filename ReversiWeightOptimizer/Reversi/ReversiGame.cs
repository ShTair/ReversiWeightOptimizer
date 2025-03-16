namespace ReversiWeightOptimizer.Reversi;

internal class ReversiGame
{
    public readonly Board board;
    private readonly (int dx, int dy)[] directions =
    {
        (-1, -1), (-1, 0), (-1, 1),
        (0, -1),         (0, 1),
        (1, -1), (1, 0), (1, 1)
    };

    public ReversiGame(Board? existingBoard = null)
    {
        board = existingBoard != null ? existingBoard.Copy() : new Board();
    }

    public ReversiGame Copy()
    {
        return new ReversiGame(board.Copy());
    }

    public bool CanPlaceStone(int x, int y, byte color)
    {
        if (board.GetState(x, y) != Board.EMPTY) return false;

        foreach (var (dx, dy) in directions)
        {
            if (CheckDirection(x, y, dx, dy, color))
            {
                return true;
            }
        }
        return false;
    }

    public List<Move> GetValidMoves(byte color)
    {
        var moves = new List<Move>();
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                if (CanPlaceStone(x, y, color))
                {
                    moves.Add(new Move(x, y));  // ← Move 構造体を使う
                }
            }
        }
        return moves;
    }

    public bool PlaceStone(int x, int y, byte color)
    {
        if (!CanPlaceStone(x, y, color)) return false;

        board.SetStone(x, y, color);

        foreach (var (dx, dy) in directions)
        {
            FlipDirection(x, y, dx, dy, color);
        }
        return true;
    }

    private bool CheckDirection(int x, int y, int dx, int dy, byte color)
    {
        int cx = x + dx, cy = y + dy;
        bool foundOpponent = false;

        while (cx >= 0 && cx < 8 && cy >= 0 && cy < 8)
        {
            byte state = board.GetState(cx, cy);
            if (state == Board.EMPTY) return false;
            if (state == color) return foundOpponent;
            foundOpponent = true;
            cx += dx;
            cy += dy;
        }
        return false;
    }

    private void FlipDirection(int x, int y, int dx, int dy, byte color)
    {
        int cx = x + dx, cy = y + dy;
        var toFlip = new List<(int, int)>();

        while (cx >= 0 && cx < 8 && cy >= 0 && cy < 8)
        {
            byte state = board.GetState(cx, cy);
            if (state == Board.EMPTY) return;
            if (state == color)
            {
                foreach (var (fx, fy) in toFlip)
                {
                    board.SetStone(fx, fy, color);
                }
                return;
            }
            toFlip.Add((cx, cy));
            cx += dx;
            cy += dy;
        }
    }

    public (int black, int white) GetScore()
    {
        int blackCount = 0, whiteCount = 0;
        for (int i = 0; i < 64; i++)
        {
            if (board.GetState(i % 8, i / 8) == Board.BLACK) blackCount++;
            if (board.GetState(i % 8, i / 8) == Board.WHITE) whiteCount++;
        }
        return (blackCount, whiteCount);
    }

    public int GetEmptyCount()
    {
        int emptyCount = 0;
        for (int i = 0; i < 64; i++)
        {
            if (board.GetState(i % 8, i / 8) == Board.EMPTY) emptyCount++;
        }
        return emptyCount;
    }

    public byte GetBoardState(int x, int y) => board.GetState(x, y);

    public bool IsGameOver()
    {
        return GetValidMoves(Board.BLACK).Count == 0 && GetValidMoves(Board.WHITE).Count == 0;
    }
}
