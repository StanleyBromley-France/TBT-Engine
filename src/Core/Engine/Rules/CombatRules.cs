namespace Core.Engine.Rules;

using Core.Domain.Abilities;
using Core.Domain.Abilities.Targeting;
using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Domain.Units.Instances.ReadOnly;
using Core.Engine.Actions;
using Core.Engine.Mutation;
using Core.Game;
using Core.Map.Pathfinding;

public sealed class CombatRules : IGameRules
{
    private readonly IPathfinder _pathfinder;
    private readonly IAbilityRepository _abilityRepository;

    public CombatRules(IPathfinder pathfinder, IAbilityRepository abilityRepository)
    {
        _pathfinder = pathfinder;
        _abilityRepository = abilityRepository;
    }
    public bool IsActionLegal(IReadOnlyGameState state, ActionChoice action)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));
        if (action == null) throw new ArgumentNullException(nameof(action));

        if (!state.UnitInstances.TryGetValue(action.UnitId, out var unit))
            return false;

        if (unit.Team != state.Turn.TeamToAct)
            return false;

        if (!unit.IsAlive)
            return false;

        return action switch
        {
            MoveAction move => IsMoveLegal(state, unit, move),
            UseAbilityAction use => IsUseAbilityLegal(state, unit, use),
            EndTurnAction end => IsEndTurnLegal(state, unit, end),
            _ => false
        };
    }

    private bool IsEndTurnLegal(IReadOnlyGameState state, IReadOnlyUnitInstance unit, EndTurnAction action)
    {
        // At the moment, end turn is always legal
        return true;
    }

    private bool IsMoveLegal(IReadOnlyGameState state, IReadOnlyUnitInstance unit, MoveAction action)
    {
        var occupied = new HashSet<HexCoord>(
        state.UnitInstances.Values
        .Where(u => u.IsAlive)
        .Select(u => u.Position));

        if (occupied.Contains(action.Target)) return false;

        return _pathfinder.IsMoveValid(state.Map, unit.Position, action.Target, unit.DerivedStats.MovePoints);
    }

    private bool IsUseAbilityLegal(IReadOnlyGameState state, IReadOnlyUnitInstance unit, UseAbilityAction action)
    {
        // Unit must have the ability

        if (!unit.Template.AbilityIds.Contains(action.AbilityId)) return false;

        var ability = _abilityRepository.Get(action.AbilityId);

        // Resources
        if (unit.Resources.Mana < ability.ManaCost) return false;


        if (!state.UnitInstances.TryGetValue(action.Target, out var target)) return false;

        if (!target.IsAlive) return false;

        if (!MatchesTargetType(unit, target, ability.Targeting.AllowedTarget)) return false;

        if (ability.Targeting.RequiresLineOfSight &&
            !_pathfinder.HasLineOfSight(state.Map, unit.Position, target.Position))
            return false;

        // if (!_rangeService.IsInRange(unit.Position, target.Position, action.Ability.Targeting.Range)) return false;
        

        return true;
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

    // Not implemented yet
    public void ApplyAction(GameMutationContext context, ActionChoice action)
        => throw new NotImplementedException();

    public IEnumerable<ActionChoice> GetLegalActions(IReadOnlyGameState state)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));

        foreach (var unit in state.UnitInstances.Values)
        {
            if (unit.Team != state.Turn.TeamToAct)
                continue;

            if (!unit.IsAlive)
                continue;

            var end = new EndTurnAction(unit.Id);
            if (IsActionLegal(state, end))
                yield return end;

            foreach (var move in GenerateMoveActions(state, unit))
            {
                if (IsActionLegal(state, move))
                    yield return move;
            }

            foreach (var use in GenerateAbilityActions(state, unit))
                yield return use;
        }
    }

    private IEnumerable<ActionChoice> GenerateMoveActions(IReadOnlyGameState state, IReadOnlyUnitInstance unit)
    {
        var reachable = _pathfinder.GetReachable(state.Map, unit.Position, unit.DerivedStats.MovePoints);

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

                if (IsActionLegal(state, action))
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

    public TeamId? GetWinner(IReadOnlyGameState state)
        => throw new NotImplementedException();
}