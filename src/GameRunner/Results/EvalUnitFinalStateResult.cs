namespace GameRunner.Results;

public sealed record EvalUnitFinalStateResult(
    bool Alive,
    int Hp,
    int Mana);
