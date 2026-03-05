namespace Core.Engine.Mutation.Mutators;

using Core.Domain.Types;
using Core.Engine.Mutation;
using Core.Domain.Units.Instances.Mutable;
using Core.Engine.Undo.Steps.Units;

public sealed class UnitsMutator : IUnitsMutator
{
    private readonly IGameMutationAccess _ctx;

    public UnitsMutator(GameMutationContext ctx) => _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));

    public void ChangeHp(UnitInstanceId unitId, int delta)
    {
        var state = _ctx.GetState();
        var unit = state.UnitInstances[unitId];

        var before = unit.Resources.HP;
        unit.Resources.HP += delta;

        _ctx.GetUndo().AddStep(new HpChangeUndo(unitId, before));
    }

    public void ChangeMana(UnitInstanceId unitId, int delta)
    {
        var state = _ctx.GetState();
        var unit = state.UnitInstances[unitId];

        var before = unit.Resources.Mana;
        unit.Resources.Mana += delta;

        _ctx.GetUndo().AddStep(new ManaChangeUndo(unitId, before));
    }

    public void ChangeActionPoints(UnitInstanceId unitId, int delta)
    {
        var state = _ctx.GetState();
        var unit = state.UnitInstances[unitId];

        var before = unit.Resources.ActionPoints;
        unit.Resources.ActionPoints += delta;

        _ctx.GetUndo().AddStep(new ActionPointsChangeUndo(unitId, before));
    }

    public void ChangeMovePoints(UnitInstanceId unitId, int delta)
    {
        var state = _ctx.GetState();
        var unit = state.UnitInstances[unitId];

        var before = unit.Resources.MovePoints;
        unit.Resources.MovePoints += delta;

        _ctx.GetUndo().AddStep(new MovePointsChangeUndo(unitId, before));
    }

    public void ResetActionPoints(UnitInstanceId unitId)
    {
        var state = _ctx.GetState();
        var unit = state.UnitInstances[unitId];

        var before = unit.Resources.ActionPoints;
        var max = unit.DerivedStats.MaxActionPoints;

        unit.Resources.ActionPoints = max;

        _ctx.GetUndo().AddStep(new ActionPointsChangeUndo(unitId, before));
    }

    public void ResetMovePoints(UnitInstanceId unitId)
    {
        var state = _ctx.GetState();
        var unit = state.UnitInstances[unitId];

        var before = unit.Resources.MovePoints;
        var max = unit.DerivedStats.MaxMovePoints;

        unit.Resources.MovePoints = max;

        _ctx.GetUndo().AddStep(new MovePointsChangeUndo(unitId, before));
    }

    public void SetDerivedStats(UnitInstanceId unitId, UnitDerivedStats newStats)
    {
        var state = _ctx.GetState();
        var unit = state.UnitInstances[unitId];

        var before = unit.DerivedStats;
        unit.DerivedStats = newStats;

        _ctx.GetUndo().AddStep(new DerivedStatsChangeUndo(unitId, before));
    }
}
