namespace Core.Engine.Mutation.Mutators;

using Core.Domain.Types;

/// <summary>
/// Mutation-layer API for modifying turn-related state>.
/// </summary>
/// <remarks>
/// Responsible for updating the current <see cref="Turn"/> value and the
/// active <see cref="UnitInstanceId"/>. All turn state transitions must pass
/// through this mutator to ensure centralized control and future undo support.
/// </remarks>
public interface ITurnMutator
{
    void SetTurn(Turn newTurn);

    void ChangeActiveUnit(UnitInstanceId newActiveUnitId);

    void CommitUnit(UnitInstanceId unitId);

    void SetCurrentlyCommiting(UnitInstanceId unitId);

    void ClearCurrentlyCommiting();

    void ResetActivationPhaseAndSetNew(UnitInstanceId newActiveUnitId);
}