using Core.Domain.Abilities;
using Core.Domain.Abilities.Targeting;
using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Domain.Units.Instances.ReadOnly;
using Core.Engine.Actions.Choice;
using Core.Engine.Effects;
using Core.Engine.Mutation;
using Core.Game;
using Core.Map.Pathfinding;
using Core.Map.Search;

namespace Core.Engine.Actions.Execution;

public sealed class UseAbilityActionHandler : IActionHandler<UseAbilityAction>
{
    private readonly IAbilityRepository _abilityRepository;
    private readonly IPathfinder _pathfinder;
    private readonly EffectManager _effectManager;

    internal UseAbilityActionHandler(
        IAbilityRepository abilityRepository,
        IPathfinder pathfinder,
        EffectManager effectManager)
    {
        _abilityRepository = abilityRepository;
        _pathfinder = pathfinder;
        _effectManager = effectManager;
    }

    public void Execute(
        IReadOnlyGameState state,
        GameMutationContext ctx,
        UseAbilityAction action)
    {
        var unit = state.UnitInstances[action.UnitId];
        var ability = _abilityRepository.Get(action.AbilityId);

        var targets = ResolveTargets(state, action, ability);

        ctx.Units.ChangeMana(action.UnitId, -ability.ManaCost);

        var request = new EffectApplicationRequest(
            templateId: ability.Effect,
            sourceUnitId: action.UnitId,
            targetUnitIds: targets);

        _effectManager.ApplyOrStackEffect(ctx, state, request);

        ctx.Turn.CommitUnit(action.UnitId);
    }

    private UnitInstanceId[] ResolveTargets(IReadOnlyGameState state, UseAbilityAction abilityAction, Ability ability)
    {
        // if radius is 0 no searching required
        if (ability.Targeting.Radius == 0)
            return [abilityAction.Target];

        var caster = state.UnitInstances[abilityAction.UnitId];
        var baseTarget = state.UnitInstances[abilityAction.Target];

        // AoE origin: around the chosen base target unit
        var coords = MapSearch.GetCoordsInRadius(
            state.Map,
            baseTarget.Position,
            ability.Targeting.Radius);

        var coordSet = new HashSet<HexCoord>(coords);

        var result = new List<UnitInstanceId>();

        foreach (var candidate in state.UnitInstances.Values)
        {
            if (!candidate.IsAlive)
                continue;

            if (!coordSet.Contains(candidate.Position))
                continue;

            // Respect allowed target type (Self/Ally/Enemy)
            if (!MatchesTargetType(caster, candidate, ability.Targeting.AllowedTarget))
                continue;

            if (ability.Targeting.RequiresLineOfSight &&
                !_pathfinder.HasLineOfSight(state.Map, caster.Position, candidate.Position))
                continue;

            result.Add(candidate.Id);
        }

        return result.ToArray();
    }

    private static bool MatchesTargetType(IReadOnlyUnitInstance unit, IReadOnlyUnitInstance target, TargetType type)
    {
        return type switch
        {
            TargetType.Self => unit.Id == target.Id,
            TargetType.Ally => unit.Team == target.Team && unit.Id != target.Id,
            TargetType.Enemy => unit.Team != target.Team,
            _ => false
        };
    }
}