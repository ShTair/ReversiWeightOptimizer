using Microsoft.Extensions.Options;
using ReversiWeightOptimizer.Reversi.AI;
using ReversiWeightOptimizer.Services;

namespace ReversiWeightOptimizer;

internal class Worker
{
    private readonly IOptions<Options> _options;
    private readonly GeneticAlgorithm _ga;

    public Worker(IOptions<Options> options, GeneticAlgorithm ga)
    {
        _options = options;
        _ga = ga;
    }

    public async Task ExecuteAsync()
    {
        Console.WriteLine("=== AI Weight Optimization Start ===");
        Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss.f}");

        var enemies = (_options.Value.Enemies ?? []).Select((t, i) =>
        {
            var w = new AIWeightsSet(CreateAIWeights(t.Opening), CreateAIWeights(t.Midgame), CreateAIWeights(t.Endgame));
            Console.WriteLine();
            Console.WriteLine($"Enemy loaded {i + 1}");
            Console.WriteLine(w);
            return w;
        }).ToList();

        var r = new Random();
        for (int i = enemies.Count; i < _options.Value.EnemyCount; i++)
        {
            enemies.Add(AIWeightsSet.RandomWeights(r));
        }

        Console.WriteLine();
        var bestWeight = await _ga.OptimizeAsync(enemies);

        Console.WriteLine("=== Optimization Complete! ===");
        Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss.f}");

        Console.WriteLine();
        Console.WriteLine($"phase,Stone Difference,Position Weight,Stable Stone,Mobility,Corner Risk,Edge Control,Frontier Discs,Parity");
        Console.WriteLine(bestWeight);

        while (true)
        {
            Console.WriteLine("終了するにはeを入力");
            if (Console.ReadLine() == "e") break;
        }
    }

    private static AIWeights CreateAIWeights(float[] values)
    {
        return new AIWeights(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]);
    }

    public class Options
    {
        public int EnemyCount { get; set; }

        public EnemyOptions[]? Enemies { get; set; }
    }

    public class EnemyOptions
    {
        public float[] Opening { get; set; } = null!;

        public float[] Midgame { get; set; } = null!;

        public float[] Endgame { get; set; } = null!;
    }
}
