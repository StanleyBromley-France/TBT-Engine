namespace Core.Engine.Rules;

using Domain.Abilities.Targeting;
using Domain.Repositories;
using Domain.Units.Instances.ReadOnly;
using Actions.Choice;
using Game;
using Map.Pathfinding;

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

        foreach (var change in GenerateSwitchUnitActions(state))
            yield return change;

        if (!state.UnitInstances.TryGetValue(state.Phase.ActiveUnitId, out var unit))
            yield break;

        // Only generate actions for the active unit
        if (unit.Team != state.Turn.TeamToAct || !unit.IsAlive)
            yield break;

        var end = new SkipActiveUnit(unit.Id);
        if (_validator.IsActionLegal(state, end)) yield return end;

        foreach (var move in GenerateMoveActions(state, unit))
            if (_validator.IsActionLegal(state, move)) yield return move;

        foreach (var use in GenerateAbilityActions(state, unit))
            yield return use;
    }

    private IEnumerable<ActionChoice> GenerateSwitchUnitActions(IReadOnlyGameState state)
    {
        foreach (var u in state.UnitInstances.Values)
        {
            if (!u.IsAlive) continue;
            if (u.Team != state.Turn.TeamToAct) continue;

            var action = new ChangeActiveUnitAction(state.Phase.ActiveUnitId, u.Id);

            if (_validator.IsActionLegal(state, action))
                yield return action;
        }
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
