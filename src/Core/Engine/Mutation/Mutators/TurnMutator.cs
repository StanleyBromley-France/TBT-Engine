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

    public void SetActiveUnit(UnitInstanceId unitId)
    {
        var state = _ctx.GetState();

        var before = state.ActiveUnitId;
        state.ActiveUnitId = unitId;

        // TODO: Record undo step in UndoRecord
    }
}
