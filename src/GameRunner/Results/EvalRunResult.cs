namespace GameRunner.Results;

using Core.Game.Match;

public sealed record EvalRunResult(
    GameOutcomeType OutcomeType,
    int? WinningTeam,
    int AppliedActionCount,
    IReadOnlyList<EvalActionRecord> Actions,
    IReadOnlyList<EvalUnitResult> Units,
    IReadOnlyList<EvalTeamResult> Teams)
{
    public static EvalRunResult From(GameOutcome outcome, IReadOnlyList<EvalActionRecord> actions)
    {
        return new EvalRunResult(
            outcome.Type,
            outcome.WinningTeam?.Value,
            actions.Count,
            actions,
            Array.Empty<EvalUnitResult>(),
            Array.Empty<EvalTeamResult>());
    }
}
