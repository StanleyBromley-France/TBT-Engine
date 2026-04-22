namespace GameRunner.Results;

public sealed record EvalMatchResult(
    string ScenarioId,
    int Seed,
    int AttackerTeamId,
    int DefenderTeamId,
    string Outcome,
    string TerminationReason,
    int AttackerTurnsTaken,
    int TurnsPlayed,
    int ActionCount,
    EvalMatchMapResult Map);
