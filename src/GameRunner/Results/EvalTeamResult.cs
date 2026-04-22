namespace GameRunner.Results;

public sealed record EvalTeamResult(
    int TeamId,
    string Side,
    IReadOnlyList<string> UnitTemplateIds,
    IReadOnlyList<string> RolesPresent,
    EvalTeamFinalStateResult FinalState,
    EvalTeamPerformanceResult Performance);
