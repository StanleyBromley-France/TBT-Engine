namespace GameRunner.Results;

public sealed record EvalMatchMapResult(
    int Width,
    int Height,
    IReadOnlyDictionary<string, double> TileDistributionSpec,
    IReadOnlyDictionary<string, int> TileCountsActual);
