namespace Core.Engine.Mutation.Mutators;

using Core.Domain.Types;
using Core.Engine.Mutation;
using Core.Undo.Steps.Turn;

public sealed class TurnMutator : ITurnMutator
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

    public void SetCurrentlyCommiting(UnitInstanceId unitId)
    {
        var state = _ctx.GetState();

        if (state.Phase.CurrentlyCommiting == unitId)
            return;

        var before = state.Phase.CurrentlyCommiting;
        state.Phase.SetCurrentlyCommiting(unitId);

        _ctx.GetUndo().AddStep(new CurrentCommitingChangeUndo(before));
    }

    public void ClearCurrentlyCommiting()
    {
        var state = _ctx.GetState();

        if (!state.Phase.CurrentlyCommiting.HasValue)
            return;

        var before = state.Phase.CurrentlyCommiting;
        state.Phase.ClearCurrentlyCommiting();

        _ctx.GetUndo().AddStep(new CurrentCommitingChangeUndo(before));
    }

    public void ResetActivationPhaseAndSetNew(UnitInstanceId newActiveUnitId)
    {
        var state = _ctx.GetState();

        var beforeCommitted = state.Phase.CommittedThisPhase.ToList();
        var beforeActive = state.Phase.ActiveUnitId;
        var beforeCurrentlyCommiting = state.Phase.CurrentlyCommiting;

        state.Phase.Reset(newActiveUnitId);

        _ctx.GetUndo().AddStep(new ActivationPhaseResetUndo(beforeActive, beforeCurrentlyCommiting, beforeCommitted));
    }
}
