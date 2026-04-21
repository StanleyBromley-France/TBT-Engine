namespace GameRunner.Results;

public sealed record EvalUnitResult(
    int UnitInstanceId,
    string UnitTemplateId,
    int TeamId,
    string Side,
    EvalUnitRolesResult Roles,
    EvalUnitFinalStateResult FinalState,
    EvalUnitPerformanceResult Performance);
