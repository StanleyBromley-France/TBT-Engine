namespace GameRunner.Results;

public sealed record EvalTeamFinalStateResult(
    int AliveCount,
    int TotalHp,
    int TotalMana);
