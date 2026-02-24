namespace Core.Engine.Actions;

using Core.Domain.Abilities;
using Core.Domain.Types;
using System.Collections.Generic;

public sealed class UseAbilityAction : ActionChoice
{
    public Ability Ability { get; }

    public IReadOnlyList<UnitInstanceId> Targets { get; }

    public UseAbilityAction(
        UnitInstanceId unitId,
        Ability ability,
        IReadOnlyList<UnitInstanceId> targets)
        : base(unitId)
    {
        Ability = ability;
        Targets = targets;
    }
}