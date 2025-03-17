using Microsoft.Extensions.Options;
using ReversiWeightOptimizer.Reversi;
using ReversiWeightOptimizer.Reversi.AI;
using System.Diagnostics;

namespace ReversiWeightOptimizer.Services;

internal class GeneticAlgorithm
{
    private readonly Random _random = new();

    private readonly int _populationSize;
    private readonly int _topPerformersSize;
    private readonly int _generations;
    private readonly float _mutationRate;

    private readonly ConsoleTitleService _consoleTitleService;

    private readonly Dictionary<(AIWeightsSet, AIWeightsSet), ReversiGame[]> _scores = [];

    public GeneticAlgorithm(IOptions<Options> options, ConsoleTitleService consoleTitleService)
    {
        _populationSize = options.Value.PopulationSize;
        _topPerformersSize = options.Value.TopPerformersSize;
        _generations = options.Value.Generations;
        _mutationRate = options.Value.MutationRate;

        _consoleTitleService = consoleTitleService;
    }

    public async Task<AIWeightsSet> OptimizeAsync(List<AIWeightsSet> enemies)
    {
        Console.WriteLine($"EnemySize: {enemies.Count}");
        Console.WriteLine($"PopulationSize: {_populationSize}");
        Console.WriteLine($"TopPerformersSize: {_topPerformersSize}");
        Console.WriteLine($"MutationRate: {_mutationRate}");
        Console.WriteLine();
        _consoleTitleService.SetGenerations(_generations);

        var population = InitializePopulation(enemies);

        for (var generation = 0; generation < _generations; generation++)
        {
            var sw = new Stopwatch();
            sw.Start();

            Console.WriteLine($"########################################################################");
            Console.WriteLine($"Generation: {generation + 1} / {_generations}");
            _consoleTitleService.SetGeneration(generation + 1);

            var scores = await EvaluatePopulationAsync(generation, population, enemies);

            var selected = SelectTopPerformers(scores);

            population = GenerateNextGeneration(selected);

            var bestAI = scores.OrderByDescending(kv => kv.Value).First();

            Console.WriteLine();
            Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss.f} {sw.Elapsed}");
            Console.WriteLine($"Best AI Score: {bestAI.Value}");
            Console.WriteLine(bestAI.Key);
            Console.WriteLine();
        }

        return population[0];
    }

    private List<AIWeightsSet> InitializePopulation(List<AIWeightsSet> enemies)
    {
        var population = new List<AIWeightsSet>();
        var i = population.Count;
        for (; i < _populationSize; i++)
        {
            population.Add(AIWeightsSet.RandomWeights(_random));
        }
        return population;
    }

    private async Task<Dictionary<AIWeightsSet, float>> EvaluatePopulationAsync(int generation, List<AIWeightsSet> population, List<AIWeightsSet> enemies)
    {
        var scores = new Dictionary<AIWeightsSet, float>();

        foreach (var (ai, i) in population.Select((ai, i) => (ai, i)))
        {
            var sw = new Stopwatch();
            sw.Start();
            Console.WriteLine($"==================== {generation + 1} : {i + 1} ========================");
            float score = 0;
            foreach (var opponent in enemies)
            {
                if (ai == opponent) continue;

                if (!_scores.TryGetValue((ai, opponent), out var games))
                {
                    games = await Task.WhenAll(Task.Run(() => PlayMatch(ai, opponent)), Task.Run(() => PlayMatch(opponent, ai)));
                    _scores.Add((ai, opponent), games);
                }

                for (var j = 0; j < 2; j++)
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
            Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss.f} {sw.Elapsed}");
            Console.WriteLine($"score: {score}");
            _consoleTitleService.SetBestScore((int)score);
        }

        return scores;
    }

    private ReversiGame PlayMatch(AIWeightsSet ai1, AIWeightsSet ai2)
    {
        var game = new ReversiGame();
        var player1 = new ReversiAI(game, new Dictionary<string, AIWeights> {
            { "opening", ai1.Opening },
            { "midgame", ai1.Midgame },
            { "endgame", ai1.Endgame },
        });
        var player2 = new ReversiAI(game, new Dictionary<string, AIWeights> {
            { "opening", ai2.Opening },
            { "midgame", ai2.Midgame },
            { "endgame", ai2.Endgame },
        });

        var currentPlayer = Board.BLACK;
        while (!game.IsGameOver())
        {
            var move = currentPlayer == Board.BLACK ? player1.GetBestMove(currentPlayer) : player2.GetBestMove(currentPlayer);
            if (move != null)
            {
                game.PlaceStone(move.Value.X, move.Value.Y, currentPlayer);
            }
            currentPlayer = currentPlayer == Board.BLACK ? Board.WHITE : Board.BLACK;
        }


        return game;
    }

    private List<AIWeightsSet> SelectTopPerformers(Dictionary<AIWeightsSet, float> scores)
    {
        return scores.OrderByDescending(kv => kv.Value)
                     .Take(_topPerformersSize)
                     .Select(kv => kv.Key)
                     .ToList();
    }

    private List<AIWeightsSet> GenerateNextGeneration(List<AIWeightsSet> selected)
    {
        var newGeneration = new List<AIWeightsSet>();
        newGeneration.AddRange(selected);

        while (newGeneration.Count < _populationSize)
        {
            var parent1 = selected[_random.Next(selected.Count)];
            var parent2 = selected[_random.Next(selected.Count)];
            var child = Crossover(parent1, parent2);
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
            _random.NextDouble() < 0.5 ? parent1.StoneDifference : parent2.StoneDifference,
            _random.NextDouble() < 0.5 ? parent1.PositionWeight : parent2.PositionWeight,
            _random.NextDouble() < 0.5 ? parent1.StableStone : parent2.StableStone,
            _random.NextDouble() < 0.5 ? parent1.Mobility : parent2.Mobility,
            _random.NextDouble() < 0.5 ? parent1.CornerRisk : parent2.CornerRisk,
            _random.NextDouble() < 0.5 ? parent1.EdgeControl : parent2.EdgeControl,
            _random.NextDouble() < 0.5 ? parent1.FrontierDiscs : parent2.FrontierDiscs,
            _random.NextDouble() < 0.5 ? parent1.Parity : parent2.Parity
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
        if (_random.NextDouble() < _mutationRate)
        {
            var mutationAmount = (float)(_random.NextDouble() * 0.05);
            return value + mutationAmount;
        }
        return value;
    }

    public class Options
    {
        public int PopulationSize { get; set; }

        public int TopPerformersSize { get; set; }

        public int Generations { get; set; }

        public float MutationRate { get; set; }
    }
}
