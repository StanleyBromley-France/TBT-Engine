namespace GameRunner.Results;

public sealed record EvalTeamPerformanceResult(
    int DamageDealt,
    int DamageTaken,
    int HealingDone,
    int Kills,
    int Deaths,
    int AbilityCasts);
