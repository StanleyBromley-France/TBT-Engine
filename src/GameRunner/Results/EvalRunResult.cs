namespace GameRunner.Results;

public sealed record EvalRunResult(
    EvalMatchResult Match,
    IReadOnlyList<EvalTeamResult> Teams,
    IReadOnlyList<EvalUnitResult> Units,
    IReadOnlyList<EvalActionRecord> Actions)
{
    public static EvalRunResult Empty(IReadOnlyList<EvalActionRecord> actions)
    {
        return new EvalRunResult(
            new EvalMatchResult(
                ScenarioId: string.Empty,
                Seed: 0,
                AttackerTeamId: 0,
                DefenderTeamId: 0,
                Outcome: "invalid",
                TerminationReason: "invalid",
                AttackerTurnsTaken: 0,
                TurnsPlayed: 0,
                ActionCount: actions.Count,
                Map: new EvalMatchMapResult(
                    Width: 0,
                    Height: 0,
                    TileDistributionSpec: new Dictionary<string, double>(),
                    TileCountsActual: new Dictionary<string, int>())),
            Array.Empty<EvalTeamResult>(),
            Array.Empty<EvalUnitResult>(),
            actions);
    }
}
