namespace Core.Engine.Actions;

using Core.Domain.Abilities;
using Core.Domain.Types;

public sealed class UseAbilityAction : ActionChoice
{
    public AbilityId AbilityId { get; }

    public UnitInstanceId Target { get; }

    public UseAbilityAction(
        UnitInstanceId unitId,
        AbilityId abilityId,
        UnitInstanceId target)
        : base(unitId)
    {
        AbilityId = abilityId;
        Target = target;
    }
}