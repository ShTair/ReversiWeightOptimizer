using ReversiWeightOptimizer.Reversi.AI;
using ReversiWeightOptimizer.Services;

namespace ReversiWeightOptimizer;

internal class Worker
{
    private readonly GeneticAlgorithm _ga;

    public Worker(GeneticAlgorithm ga)
    {
        _ga = ga;
    }

    public async Task ExecuteAsync()
    {
        Console.WriteLine("=== AI Weight Optimization Start ===");
        Console.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss.f}");

        var r = new Random();
        var enemy = Enumerable.Range(0, 5).Select(t => AIWeightsSet.RandomWeights(r)).ToList();

        var bestWeight = await _ga.OptimizeAsync(enemy);

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
}
