namespace GameRunner.Results;

public sealed record EvalActionRecord(
    int TurnIndex,
    int ActingTeam,
    string ActionType,
    int UnitId,
    string? AbilityId,
    int? TargetUnitId,
    int? TargetHexQ,
    int? TargetHexR);
