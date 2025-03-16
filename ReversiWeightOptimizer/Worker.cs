using ReversiWeightOptimizer.Reversi.AI;

namespace ReversiWeightOptimizer;

public class Worker
{
    public async Task ExecuteAsync()
    {
        Console.WriteLine("=== AI Weight Optimization Start ===");

        var r = new Random();
        var enemy = Enumerable.Range(0, 5).Select(t => AIWeightsSet.RandomWeights(r)).ToList();

        var ga = new GeneticAlgorithm();
        var bestWeight = await ga.OptimizeAsync(enemy);

        Console.WriteLine("=== Optimization Complete! ===");

        Console.WriteLine();
        Console.WriteLine($"phase,Stone Difference,Position Weight,Stable Stone,Mobility,Corner Risk,Edge Control,Frontier Discs,Parity");
        Console.WriteLine(bestWeight);

        Console.ReadLine();
    }
}
