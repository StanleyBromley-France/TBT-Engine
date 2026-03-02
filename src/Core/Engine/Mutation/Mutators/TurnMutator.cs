namespace Core.Engine.Mutation.Mutators;

using Core.Domain.Types;
using Core.Engine.Mutation;

/// <summary>
/// Mutation-layer API for modifying turn-related state>.
/// </summary>
/// <remarks>
/// Responsible for updating the current <see cref="Turn"/> value and the
/// active <see cref="UnitInstanceId"/>. All turn state transitions must pass
/// through this mutator to ensure centralized control and future undo support.
/// </remarks>
public sealed class TurnMutator
{
    private readonly IGameMutationAccess _ctx;

    public TurnMutator(GameMutationContext ctx)
        => _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));

    public void SetTurn(Turn newTurn)
    {
        var state = _ctx.GetState();

        var before = state.Turn;
        state.Turn = newTurn;

        // TODO: Record undo step in UndoRecord
    }

    public void ChangeActiveUnit(UnitInstanceId newActiveUnitId)
    {
        var state = _ctx.GetState();

        var before = state.Phase.ActiveUnitId;
        state.Phase.ActiveUnitId = newActiveUnitId;

        // TODO: Record undo step in UndoRecord
    }

    public void CommitUnit(UnitInstanceId unitId)
    {
        var state = _ctx.GetState();

        if (state.Phase.CommittedThisPhase.Contains(unitId))
            return;

        state.Phase.CommittedThisPhase.Add(unitId);

        // TODO: Record undo step in UndoRecord
    }

    public void ResetActivationPhaseAndSetNew(UnitInstanceId newActiveUnitId)
    {
        var state = _ctx.GetState();

        var beforeCommitted = state.Phase.CommittedThisPhase.ToList();
        var beforeActive = state.Phase.ActiveUnitId;

        state.Phase.Reset(newActiveUnitId);

        // TODO: Record undo step in UndoRecord
    }
}
