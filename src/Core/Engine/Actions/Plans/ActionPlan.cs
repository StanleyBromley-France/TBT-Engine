namespace Core.Engine.Actions.Plans;

using Core.Domain.Types;

public abstract class ActionPlan
{
    public UnitInstanceId ActorUnitId { get; }

    protected ActionPlan(UnitInstanceId actorUnitId)
    {
        ActorUnitId = actorUnitId;
    }
}

public sealed class MovePlan : ActionPlan
{
    public HexCoord To { get; }
    public int MovementCost { get; }

    public MovePlan(
        UnitInstanceId actorUnitId,
        HexCoord to,
        int movementCost)
        : base(actorUnitId)
    {
        To = to;
        MovementCost = movementCost;
    }
}

public sealed class UseAbilityPlan : ActionPlan
{
    public UnitInstanceId BaseTarget { get; }
    public IReadOnlyList<UnitInstanceId> TargetUnitIds { get; }
    public EffectTemplateId EffectTemplateId { get; }
    public int ManaCost { get; }

    public UseAbilityPlan(
        UnitInstanceId actorUnitId,
        UnitInstanceId baseTarget,
        IReadOnlyList<UnitInstanceId> targetUnitIds,
        EffectTemplateId effectTemplateId,
        int manaCost)
        : base(actorUnitId)
    {
        BaseTarget = baseTarget;
        TargetUnitIds = new ReadOnlyCollection<UnitInstanceId>(new List<UnitInstanceId>(targetUnitIds));
        EffectTemplateId = effectTemplateId;
        ManaCost = manaCost;
    }
}

public sealed class EndTurnPlan : ActionPlan
{
    public EndTurnPlan(UnitInstanceId actorUnitId) : base(actorUnitId) { }
}

public sealed class ChangeActiveUnitPlan : ActionPlan
{
    public UnitInstanceId NewActiveUnitId { get; }

    public ChangeActiveUnitPlan(UnitInstanceId actor, UnitInstanceId newActive)
        : base(actor)
    {
        NewActiveUnitId = newActive;
    }
}
