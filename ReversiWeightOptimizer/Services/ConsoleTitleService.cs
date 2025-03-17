namespace ReversiWeightOptimizer.Services;

internal class ConsoleTitleService
{
    private const string Title = "Reversi Weight Optimizer";

    private int _generations;

    private int _generation;

    private int _bestScore = int.MinValue;

    private void UpdateTitle() => Console.Title = $"{_generation}/{_generations} : {_bestScore} {Title}";

    public void SetGenerations(int generations)
    {
        _generations = generations;
        UpdateTitle();
    }

    public void SetGeneration(int generation)
    {
        _generation = generation;
        UpdateTitle();
    }

    public void SetBestScore(int score)
    {
        if (score > _bestScore)
        {
            _bestScore = score;
            UpdateTitle();
        }
    }
}
