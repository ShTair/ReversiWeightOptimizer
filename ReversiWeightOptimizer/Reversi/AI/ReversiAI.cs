namespace ReversiWeightOptimizer.Reversi.AI;

internal class ReversiAI
{
    private readonly ReversiGame game;
    private readonly Dictionary<string, AIWeights> weightConfig;
    private readonly Dictionary<string, float> transpositionTable;

    public ReversiAI(ReversiGame game, Dictionary<string, AIWeights> weightConfig)
    {
        this.game = game;
        this.weightConfig = weightConfig;
        this.transpositionTable = new Dictionary<string, float>();
    }

    private AIWeights GetWeightFactors()
    {
        int remainingMoves = game.GetEmptyCount();
        if (remainingMoves > 40) return weightConfig["opening"];
        if (remainingMoves > 20) return weightConfig["midgame"];
        return weightConfig["endgame"];
    }

    public Move? GetBestMove(byte color)
    {
        List<Move> moves = game.GetValidMoves(color);
        if (moves.Count == 0) return null;

        int maxDepth = GetAdaptiveDepth();
        Move bestMove = moves[0];

        // 初期ムーブオーダリング（QuickHeuristic によるソート）
        moves.Sort((a, b) => QuickHeuristic(b, color).CompareTo(QuickHeuristic(a, color)));

        // 反復深化ループ
        for (int depth = 1; depth <= maxDepth; depth++)
        {
            List<(Move move, float score)> scoredMoves = new List<(Move, float)>();

            foreach (var move in moves)
            {
                ReversiGame tempGame = game.Copy();
                tempGame.PlaceStone(move.X, move.Y, color);
                // 深さが 1 のときは、直後の評価を使う（depth-1=0）
                float score = Minimax(tempGame, depth - 1, float.MinValue, float.MaxValue, false, color);
                scoredMoves.Add((move, score));
            }

            // 評価の高い順にソート
            scoredMoves.Sort((a, b) => b.score.CompareTo(a.score));
            bestMove = scoredMoves[0].move;

            // 次の深さ探索のためにムーブ順を更新（これによりよりよいムーブオーダリングが得られる）
            moves = scoredMoves.Select(s => s.move).ToList();
        }

        return bestMove;
    }

    private int GetAdaptiveDepth()
    {
        int remainingMoves = game.GetEmptyCount();
        return (50 / remainingMoves) + 5;
    }

    private float Minimax(ReversiGame game, int depth, float alpha, float beta, bool maximizingPlayer, byte color)
    {
        // 盤面全体の状態を連結してハッシュ文字列を作成
        var sb = new System.Text.StringBuilder();
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                sb.Append(game.board.GetState(x, y));
            }
        }
        // 現在の探索深さと、ターン情報（maximizingPlayer=trueなら評価対象の色、falseなら相手の色）を付加
        byte currentTurnColor = maximizingPlayer ? color : (color == Board.BLACK ? Board.WHITE : Board.BLACK);
        sb.Append($"_{depth}_{currentTurnColor}");
        string boardHash = sb.ToString();

        // トランスポジションテーブルの利用
        if (transpositionTable.TryGetValue(boardHash, out float cachedValue))
        {
            return cachedValue;
        }

        byte opponent = (color == Board.BLACK) ? Board.WHITE : Board.BLACK;
        if (depth == 0 || game.IsGameOver())
        {
            float evalScore = EvaluateBoard(game, color);
            transpositionTable[boardHash] = evalScore;
            return evalScore;
        }

        byte currentColor = maximizingPlayer ? color : opponent;
        List<Move> moves = game.GetValidMoves(currentColor);
        moves.Sort((a, b) => QuickHeuristic(b, currentColor).CompareTo(QuickHeuristic(a, currentColor)));

        if (moves.Count == 0)
        {
            float evalScore = Minimax(game, depth - 1, alpha, beta, !maximizingPlayer, color);
            transpositionTable[boardHash] = evalScore;
            return evalScore;
        }

        float value;
        if (maximizingPlayer)
        {
            value = float.MinValue;
            foreach (var move in moves)
            {
                ReversiGame tempGame = game.Copy();
                tempGame.PlaceStone(move.X, move.Y, color);
                float evalScore = Minimax(tempGame, depth - 1, alpha, beta, false, color);
                value = Math.Max(value, evalScore);
                alpha = Math.Max(alpha, evalScore);
                if (beta <= alpha) break; // βカット
            }
        }
        else
        {
            value = float.MaxValue;
            foreach (var move in moves)
            {
                ReversiGame tempGame = game.Copy();
                tempGame.PlaceStone(move.X, move.Y, opponent);
                float evalScore = Minimax(tempGame, depth - 1, alpha, beta, true, color);
                value = Math.Min(value, evalScore);
                beta = Math.Min(beta, evalScore);
                if (beta <= alpha) break; // αカット
            }
        }

        transpositionTable[boardHash] = value;
        return value;
    }

    private float EvaluateBoard(ReversiGame game, byte color)
    {
        AIWeights weights = GetWeightFactors();
        byte opponent = color == Board.BLACK ? Board.WHITE : Board.BLACK;
        (int black, int white) scores = game.GetScore();

        float myScore = scores.black;
        float opponentScore = scores.white;

        float score = (myScore - opponentScore) * weights.StoneDifference;
        score += EvaluatePosition(game, color) * weights.PositionWeight;
        score += EvaluateStableStones(game, color) * weights.StableStone;
        score += (game.GetValidMoves(color).Count - game.GetValidMoves(opponent).Count) * weights.Mobility;
        score += EvaluateCornerRisk(game, color) * weights.CornerRisk;
        score += EvaluateEdgeControl(game, color) * weights.EdgeControl;
        score += EvaluateFrontierDiscs(game, color) * weights.FrontierDiscs;
        score += EvaluateParity(game, color) * weights.Parity;

        return score;
    }
    private int QuickHeuristic(Move move, byte color)
    {
        if ((move.X == 0 && move.Y == 0) ||
            (move.X == 0 && move.Y == 7) ||
            (move.X == 7 && move.Y == 0) ||
            (move.X == 7 && move.Y == 7))
        {
            return 100;
        }
        if (move.X == 0 || move.X == 7 || move.Y == 0 || move.Y == 7)
        {
            return 10;
        }
        return 0;
    }

    public float EvaluatePosition(ReversiGame game, byte color)
    {
        int[,] weight = {
            {100, -20, 10, 5, 5, 10, -20, 100},
            {-20, -50, -2, -2, -2, -2, -50, -20},
            {10, -2, 0, 0, 0, 0, -2, 10},
            {5, -2, 0, 0, 0, 0, -2, 5},
            {5, -2, 0, 0, 0, 0, -2, 5},
            {10, -2, 0, 0, 0, 0, -2, 10},
            {-20, -50, -2, -2, -2, -2, -50, -20},
            {100, -20, 10, 5, 5, 10, -20, 100}
        };

        float score = 0;
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                byte state = game.GetBoardState(x, y);
                if (state == color)
                    score += weight[y, x];
                else if (state != 0)
                    score -= weight[y, x];
            }
        }
        return score;
    }

    public float EvaluateStableStones(ReversiGame game, byte color)
    {
        int stableStoneCount = 0;
        bool[,] stableMap = new bool[8, 8];
        int[][] corners = new int[][] { new int[] { 0, 0 }, new int[] { 0, 7 }, new int[] { 7, 0 }, new int[] { 7, 7 } };

        foreach (var corner in corners)
        {
            if (game.GetBoardState(corner[0], corner[1]) == color)
            {
                stableMap[corner[1], corner[0]] = true;
                stableStoneCount++;
            }
        }
        return stableStoneCount;
    }

    public float EvaluateCornerRisk(ReversiGame game, byte color)
    {
        float riskScore = 0;
        int[][] corners = new int[][] { new int[] { 0, 0 }, new int[] { 0, 7 }, new int[] { 7, 0 }, new int[] { 7, 7 } };
        int[][] xSquares = new int[][] { new int[] { 1, 1 }, new int[] { 1, 6 }, new int[] { 6, 1 }, new int[] { 6, 6 } };
        int[][] cSquares = new int[][] {
            new int[] { 0, 1 }, new int[] { 1, 0 }, new int[] { 0, 6 }, new int[] { 1, 7 },
            new int[] { 6, 0 }, new int[] { 7, 1 }, new int[] { 6, 7 }, new int[] { 7, 6 }
        };

        for (int i = 0; i < 4; i++)
        {
            int cx = corners[i][0], cy = corners[i][1];
            if (game.GetBoardState(cx, cy) == 0)
            {
                int x = xSquares[i][0], y = xSquares[i][1];
                if (game.GetBoardState(x, y) == color) riskScore -= 30;
                int[] c1 = cSquares[i * 2], c2 = cSquares[i * 2 + 1];
                if (game.GetBoardState(c1[0], c1[1]) == color) riskScore -= 15;
                if (game.GetBoardState(c2[0], c2[1]) == color) riskScore -= 15;
            }
        }
        return riskScore;
    }

    public float EvaluateEdgeControl(ReversiGame game, byte color)
    {
        float edgeScore = 0;
        List<List<int[]>> edges = new List<List<int[]>> {
            new List<int[]> { new int[] { 0, 1 }, new int[] { 0, 2 }, new int[] { 0, 3 }, new int[] { 0, 4 }, new int[] { 0, 5 }, new int[] { 0, 6 } },
            new List<int[]> { new int[] { 1, 0 }, new int[] { 2, 0 }, new int[] { 3, 0 }, new int[] { 4, 0 }, new int[] { 5, 0 }, new int[] { 6, 0 } },
            new List<int[]> { new int[] { 7, 1 }, new int[] { 7, 2 }, new int[] { 7, 3 }, new int[] { 7, 4 }, new int[] { 7, 5 }, new int[] { 7, 6 } },
            new List<int[]> { new int[] { 1, 7 }, new int[] { 2, 7 }, new int[] { 3, 7 }, new int[] { 4, 7 }, new int[] { 5, 7 }, new int[] { 6, 7 } }
        };

        foreach (var edge in edges)
        {
            bool stable = true;
            int count = 0;
            foreach (var pos in edge)
            {
                int x = pos[0], y = pos[1];
                if (game.GetBoardState(x, y) == color)
                {
                    count++;
                }
                else if (game.GetBoardState(x, y) != 0)
                {
                    stable = false;
                }
            }
            edgeScore += stable ? count * 2 : count;
        }
        return edgeScore;
    }

    public float EvaluateFrontierDiscs(ReversiGame game, byte color)
    {
        int frontierCount = 0;
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                if (game.GetBoardState(x, y) == color)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx >= 0 && nx < 8 && ny >= 0 && ny < 8)
                            {
                                if (game.GetBoardState(nx, ny) == 0)
                                {
                                    frontierCount++;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        return -frontierCount;
    }

    public float EvaluateParity(ReversiGame game, byte color)
    {
        int emptyCount = 0;
        for (int i = 0; i < 64; i++)
        {
            if (game.GetBoardState(i % 8, i / 8) == 0)
                emptyCount++;
        }
        return (emptyCount % 2 == 0) ? 10 : -10;
    }
}
