namespace Core.Engine.Mutation.Mutators;

using Core.Domain.Types;
using Core.Engine.Mutation;
using Core.Engine.Undo.Steps.Turn;

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

        _ctx.GetUndo().AddStep(new TurnChangeUndo(before));
    }

    public void ChangeActiveUnit(UnitInstanceId newActiveUnitId)
    {
        var state = _ctx.GetState();

        var before = state.Phase.ActiveUnitId;
        state.Phase.ActiveUnitId = newActiveUnitId;

        _ctx.GetUndo().AddStep(new ActiveUnitChangeUndo(before));
    }

    public void CommitUnit(UnitInstanceId unitId)
    {
        var state = _ctx.GetState();

        if (state.Phase.CommittedThisPhase.Contains(unitId))
            return;

        state.Phase.CommittedThisPhase.Add(unitId);

        _ctx.GetUndo().AddStep(new PhaseCommitUnitUndo(unitId));
    }

    public void ResetActivationPhaseAndSetNew(UnitInstanceId newActiveUnitId)
    {
        var state = _ctx.GetState();

        var beforeCommitted = state.Phase.CommittedThisPhase.ToList();
        var beforeActive = state.Phase.ActiveUnitId;

        state.Phase.Reset(newActiveUnitId);

        _ctx.GetUndo().AddStep(new ActivationPhaseResetUndo(beforeActive, beforeCommitted));
    }
}
