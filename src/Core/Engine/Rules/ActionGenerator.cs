namespace Core.Engine.Rules;

using Domain.Abilities.Targeting;
using Domain.Repositories;
using Domain.Units.Instances.ReadOnly;
using Actions.Choice;
using Game;
using Map.Pathfinding;
using Core.Game.State.ReadOnly;

internal class ActionGenerator : IActionGenerator
{
    private readonly IPathfinder _pathfinder;
    private readonly IAbilityRepository _abilityRepository;
    private readonly IActionValidator _validator;

    public ActionGenerator(
        IPathfinder pathfinder,
        IAbilityRepository abilityRepository,
        IActionValidator validator)
    {
        _pathfinder = pathfinder ?? throw new ArgumentNullException(nameof(pathfinder));
        _abilityRepository = abilityRepository ?? throw new ArgumentNullException(nameof(abilityRepository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public IEnumerable<ActionChoice> GetLegalActions(IReadOnlyGameState state)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));

        // Actions will only be generated for CurrentlyCommiting if it has value
        if (state.Phase.CurrentlyCommiting.HasValue)
        {
            if (!state.UnitInstances.TryGetValue(state.Phase.CurrentlyCommiting.Value, out var unit))
                yield break;

            if (!unit.IsAlive || unit.Team != state.Turn.TeamToAct || state.Phase.HasCommitted(unit.Id))
                yield break;

            foreach (var action in GenerateActionsForUnit(state, unit))
                yield return action;

            yield break;
        }

        // Otherwise actions are generated for every unit instance thats alive, on team, and not commited 
        foreach (var unit in state.UnitInstances.Values)
        {
            if (!unit.IsAlive) continue;
            if (unit.Team != state.Turn.TeamToAct) continue;
            if (state.Phase.HasCommitted(unit.Id)) continue;

            foreach (var action in GenerateActionsForUnit(state, unit))
                yield return action;
        }
    }

    private IEnumerable<ActionChoice> GenerateActionsForUnit(IReadOnlyGameState state, IReadOnlyUnitInstance unit)
    {
        var end = new SkipActiveUnitAction(unit.Id);
        if (_validator.IsActionLegal(state, end)) yield return end;

        foreach (var move in GenerateMoveActions(state, unit))
            if (_validator.IsActionLegal(state, move)) yield return move;

        foreach (var use in GenerateAbilityActions(state, unit))
            yield return use;
    }

    private IEnumerable<ActionChoice> GenerateMoveActions(IReadOnlyGameState state, IReadOnlyUnitInstance unit)
    {
        var reachable = _pathfinder.GetReachable(state.Map, unit.Position, unit.DerivedStats.MaxMovePoints);

        foreach (var kvp in reachable)
        {
            var hex = kvp.Key;

            if (hex.Equals(unit.Position))
                continue;

            var action = new MoveAction(unit.Id, hex);

            yield return action;
        }
    }

    private IEnumerable<ActionChoice> GenerateAbilityActions(IReadOnlyGameState state, IReadOnlyUnitInstance unit)
    {
        foreach (var abilityId in unit.Template.AbilityIds)
        {
            var ability = _abilityRepository.Get(abilityId);

            // Makes sure only abilities unit can use is concidered 
            if (unit.Resources.Mana < ability.ManaCost)
                continue;

            foreach (var target in GetCandidateTargets(state, unit, ability.Targeting.AllowedTarget))
            {
                var action = new UseAbilityAction(unit.Id, ability.Id, target.Id);

                if (_validator.IsActionLegal(state, action))
                    yield return action;
            }
        }
    }

    private static IEnumerable<IReadOnlyUnitInstance> GetCandidateTargets(
        IReadOnlyGameState state,
        IReadOnlyUnitInstance unit,
        TargetType type)
    {
        return type switch
        {
            TargetType.Self =>
                unit.IsAlive ? new[] { unit } : Enumerable.Empty<IReadOnlyUnitInstance>(),

            TargetType.Ally =>
                state.UnitInstances.Values.Where(u => u.IsAlive && u.Team == unit.Team && u.Id != unit.Id),

            TargetType.Enemy =>
                state.UnitInstances.Values.Where(u => u.IsAlive && u.Team != unit.Team),

            _ => Enumerable.Empty<IReadOnlyUnitInstance>()
        };
    }
}
