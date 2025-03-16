namespace ReversiWeightOptimizer.Reversi.AI;

internal class AIWeights
{
    public float StoneDifference { get; }

    public float PositionWeight { get; }

    public float StableStone { get; }

    public float Mobility { get; }

    public float CornerRisk { get; }

    public float EdgeControl { get; }

    public float FrontierDiscs { get; }

    public float Parity { get; }

    public AIWeights(double stoneDifference, double positionWeight, double stableStone, double mobility,
                     double cornerRisk, double edgeControl, double frontierDiscs, double parity) : this((float)stoneDifference, (float)positionWeight, (float)stableStone, (float)mobility,
                     (float)cornerRisk, (float)edgeControl, (float)frontierDiscs, (float)parity)
    { }

    public AIWeights(float stoneDifference, float positionWeight, float stableStone, float mobility,
                     float cornerRisk, float edgeControl, float frontierDiscs, float parity)
    {
        var length = MathF.Sqrt(stoneDifference * stoneDifference + positionWeight * positionWeight + stableStone * stableStone + mobility * mobility + cornerRisk * cornerRisk + edgeControl * edgeControl + frontierDiscs * frontierDiscs + parity * parity);

        StoneDifference = stoneDifference / length;
        PositionWeight = positionWeight / length;
        StableStone = stableStone / length;
        Mobility = mobility / length;
        CornerRisk = cornerRisk / length;
        EdgeControl = edgeControl / length;
        FrontierDiscs = frontierDiscs / length;
        Parity = parity / length;
    }

    public float Length => MathF.Sqrt(StoneDifference * StoneDifference + PositionWeight * PositionWeight + StableStone * StableStone + Mobility * Mobility + CornerRisk * CornerRisk + EdgeControl * EdgeControl + FrontierDiscs * FrontierDiscs + Parity * Parity);

    public static float RandomF(Random random)
    {
        return (float)random.NextDouble() - 0.5f;
    }

    // ランダムな重みを作成
    public static AIWeights RandomWeights(Random random)
    {
        return new AIWeights(
            RandomF(random), RandomF(random), RandomF(random), RandomF(random),
            RandomF(random), RandomF(random), RandomF(random), RandomF(random));
    }

    public AIWeights Clone()
    {
        return new AIWeights(StoneDifference, PositionWeight, StableStone, Mobility,
                             CornerRisk, EdgeControl, FrontierDiscs, Parity);
    }

    public override string ToString()
    {
        return $"{StoneDifference},{PositionWeight},{StableStone},{Mobility},{CornerRisk},{EdgeControl},{FrontierDiscs},{Parity}";
    }
}
