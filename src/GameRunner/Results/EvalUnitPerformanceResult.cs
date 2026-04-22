namespace GameRunner.Results;

public sealed record EvalUnitPerformanceResult(
    int DamageDealt,
    int DamageTaken,
    int HealingDone,
    int Kills,
    int Deaths,
    int BuffEffectsApplied,
    int DebuffEffectsApplied,
    int BuffUptimeTicksGranted,
    int DebuffUptimeTicksGranted);
