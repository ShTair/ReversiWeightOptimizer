using ReversiWeightOptimizer.Reversi;
using ReversiWeightOptimizer.Reversi.AI;
using System.Diagnostics;

namespace ReversiWeightOptimizer;

internal class GeneticAlgorithm
{
    private const int PopulationSize = 8;
    private const int TopPerformersSize = 3;
    private const int Generations = 20;
    private const float MutationRate = 0.3f;
    private readonly Random random = new();

    private readonly Dictionary<(AIWeightsSet, AIWeightsSet), ReversiGame[]> _scores = [];

    public async Task<AIWeightsSet> OptimizeAsync(List<AIWeightsSet> enemy)
    {
        List<AIWeightsSet> population = InitializePopulation(enemy);

        for (int generation = 0; generation < Generations; generation++)
        {
            var sw = new Stopwatch();
            sw.Start();

            Console.WriteLine($"########################################################################");
            Console.WriteLine($"Generation {generation + 1} / {Generations}");
            Console.Title = $"ReversiAI {generation + 1} / {Generations}";

            var scores = await EvaluatePopulationAsync(generation, population, enemy);

            List<AIWeightsSet> selected = SelectTopPerformers(scores);

            population = GenerateNextGeneration(selected);

            var bestAI = scores.OrderByDescending(kv => kv.Value).First();

            Console.WriteLine();
            Console.WriteLine(sw.Elapsed);
            Console.WriteLine($"Best AI Score: {bestAI.Value}");
            Console.WriteLine(bestAI.Key);
            Console.WriteLine();
        }

        return population[0];
    }

    private List<AIWeightsSet> InitializePopulation(List<AIWeightsSet> enemy)
    {
        var population = new List<AIWeightsSet>();
        int i = population.Count;
        for (; i < PopulationSize; i++)
        {
            population.Add(AIWeightsSet.RandomWeights(random));
        }
        return population;
    }

    private async Task<Dictionary<AIWeightsSet, float>> EvaluatePopulationAsync(int generation, List<AIWeightsSet> population, List<AIWeightsSet> enemy)
    {
        var scores = new Dictionary<AIWeightsSet, float>();

        foreach (var (ai, i) in population.Select((ai, i) => (ai, i)))
        {
            var sw = new Stopwatch();
            sw.Start();
            Console.WriteLine($"==================== {generation + 1} : {i + 1} ========================");
            float score = 0;
            foreach (var opponent in enemy)
            {
                if (ai == opponent) continue;

                if (!_scores.TryGetValue((ai, opponent), out var games))
                {
                    games = await Task.WhenAll(Task.Run(() => PlayMatch(ai, opponent)), Task.Run(() => PlayMatch(opponent, ai)));
                    _scores.Add((ai, opponent), games);
                }

                for (int j = 0; j < 2; j++)
                {
                    var (b, w) = games[j].GetScore();
                    var s = b - w;
                    if (j == 1) s *= -1;

                    if (s < 0) s -= games[j].GetEmptyCount();

                    var z = (Console.ForegroundColor, Console.BackgroundColor);
                    Console.BackgroundColor = ConsoleColor.DarkGreen;

                    if (j == 1) Console.ForegroundColor = ConsoleColor.White;
                    else Console.ForegroundColor = ConsoleColor.Black;

                    Console.Write("● ");

                    (Console.ForegroundColor, Console.BackgroundColor) = z;

                    Console.WriteLine($" : {s}");


                    //games[j].board.PrintBoard();
                    score += s;
                }
            }
            scores[ai] = score;
            Console.WriteLine(sw.Elapsed);
            Console.WriteLine($"score: {score}");
        }

        return scores;
    }

    private ReversiGame PlayMatch(AIWeightsSet ai1, AIWeightsSet ai2)
    {
        ReversiGame game = new ReversiGame();
        ReversiAI player1 = new ReversiAI(game, new Dictionary<string, AIWeights> {
            { "opening", ai1.Opening },
            { "midgame", ai1.Midgame },
            { "endgame", ai1.Endgame },
        });
        ReversiAI player2 = new ReversiAI(game, new Dictionary<string, AIWeights> {
            { "opening", ai2.Opening },
            { "midgame", ai2.Midgame },
            { "endgame", ai2.Endgame },
        });

        byte currentPlayer = Board.BLACK;
        while (!game.IsGameOver())
        {
            Move? move = (currentPlayer == Board.BLACK) ? player1.GetBestMove(currentPlayer) : player2.GetBestMove(currentPlayer);
            if (move != null)
            {
                game.PlaceStone(move.Value.X, move.Value.Y, currentPlayer);
            }
            currentPlayer = (currentPlayer == Board.BLACK) ? Board.WHITE : Board.BLACK;
        }


        return game;
    }

    private List<AIWeightsSet> SelectTopPerformers(Dictionary<AIWeightsSet, float> scores)
    {
        return scores.OrderByDescending(kv => kv.Value)
                     .Take(TopPerformersSize)
                     .Select(kv => kv.Key)
                     .ToList();
    }

    private List<AIWeightsSet> GenerateNextGeneration(List<AIWeightsSet> selected)
    {
        var newGeneration = new List<AIWeightsSet>();
        newGeneration.AddRange(selected);

        while (newGeneration.Count < PopulationSize)
        {
            AIWeightsSet parent1 = selected[random.Next(selected.Count)];
            AIWeightsSet parent2 = selected[random.Next(selected.Count)];
            AIWeightsSet child = Crossover(parent1, parent2);
            child = Mutate(child);
            newGeneration.Add(child);
        }

        return newGeneration;
    }

    private AIWeightsSet Crossover(AIWeightsSet parent1, AIWeightsSet parent2)
    {
        return new AIWeightsSet(
            CrossoverWeights(parent1.Opening, parent2.Opening),
            CrossoverWeights(parent1.Midgame, parent2.Midgame),
            CrossoverWeights(parent1.Endgame, parent2.Endgame)
        );
    }

    private AIWeightsSet Mutate(AIWeightsSet ai)
    {
        return new AIWeightsSet(MutateWeights(ai.Opening), MutateWeights(ai.Midgame), MutateWeights(ai.Endgame));
    }

    private AIWeights CrossoverWeights(AIWeights parent1, AIWeights parent2)
    {
        return new AIWeights(
            random.NextDouble() < 0.5 ? parent1.StoneDifference : parent2.StoneDifference,
            random.NextDouble() < 0.5 ? parent1.PositionWeight : parent2.PositionWeight,
            random.NextDouble() < 0.5 ? parent1.StableStone : parent2.StableStone,
            random.NextDouble() < 0.5 ? parent1.Mobility : parent2.Mobility,
            random.NextDouble() < 0.5 ? parent1.CornerRisk : parent2.CornerRisk,
            random.NextDouble() < 0.5 ? parent1.EdgeControl : parent2.EdgeControl,
            random.NextDouble() < 0.5 ? parent1.FrontierDiscs : parent2.FrontierDiscs,
            random.NextDouble() < 0.5 ? parent1.Parity : parent2.Parity
        );
    }

    private AIWeights MutateWeights(AIWeights weights)
    {
        return new AIWeights(
            MutateValue(weights.StoneDifference),
            MutateValue(weights.PositionWeight),
            MutateValue(weights.StableStone),
            MutateValue(weights.Mobility),
            MutateValue(weights.CornerRisk),
            MutateValue(weights.EdgeControl),
            MutateValue(weights.FrontierDiscs),
            MutateValue(weights.Parity)
        );
    }

    private float MutateValue(float value)
    {
        if (random.NextDouble() < MutationRate)
        {
            var mutationAmount = (float)(random.NextDouble() * 0.05);
            return value + mutationAmount;
        }
        return value;
    }
}
