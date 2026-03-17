namespace Core.Engine.Mutation.Mutators;

using Core.Domain.Types;

/// <summary>
/// Mutation-layer API for modifying turn-related state>.
/// </summary>
/// <remarks>
/// Responsible for updating the current <see cref="Turn"/> value and the
/// activation-phase progress. All turn state transitions must pass
/// through this mutator to ensure centralized control and future undo support.
/// </remarks>
public interface ITurnMutator
{
    void SetTurn(Turn newTurn);

    void CommitUnit(UnitInstanceId unitId);

    void SetCurrentlyCommiting(UnitInstanceId unitId);

    void ClearCurrentlyCommiting();

    void ResetActivationPhase();
}
