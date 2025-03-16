namespace ReversiWeightOptimizer.Reversi.AI;

internal class AIWeightsSet
{
    public AIWeights Opening { get; set; }
    public AIWeights Midgame { get; set; }
    public AIWeights Endgame { get; set; }

    public AIWeightsSet(AIWeights opening, AIWeights midgame, AIWeights endgame)
    {
        Opening = opening;
        Midgame = midgame;
        Endgame = endgame;
    }

    // ランダムな遺伝子を作成
    public static AIWeightsSet RandomWeights(Random random)
    {
        return new AIWeightsSet(
            AIWeights.RandomWeights(random),
            AIWeights.RandomWeights(random),
            AIWeights.RandomWeights(random)
        );
    }

    // クローン（遺伝子のコピー）
    public AIWeightsSet Clone()
    {
        return new AIWeightsSet(Opening.Clone(), Midgame.Clone(), Endgame.Clone());
    }

    public override string ToString()
    {
        return $"opening,{Opening}{Environment.NewLine}midgame,{Midgame}{Environment.NewLine}endgame,{Endgame}";
    }
}
