namespace Core.Engine.Rules;

using Core.Domain.Abilities;
using Core.Domain.Abilities.Targeting;
using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Domain.Units.Instances.ReadOnly;
using Core.Engine.Actions.Choice;
using Core.Engine.Actions.Plans;
using Core.Engine.Mutation;
using Core.Game;
using Core.Map.Pathfinding;
using Core.Map.Search;

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
            ChangeActiveUnitAction change => IsChangeActiveUnitLegal(state, unit, change),
            MoveAction move => IsMoveLegal(state, unit, move),
            UseAbilityAction use => IsUseAbilityLegal(state, unit, use),
            EndTurnAction end => IsEndTurnLegal(state, unit, end),
            _ => false
        };
    }

    private bool IsChangeActiveUnitLegal(IReadOnlyGameState state, IReadOnlyUnitInstance unit, ChangeActiveUnitAction action)
    {
        // Must be issued by the currently active unit (keeps ActionChoice.UnitId meaningful)
        if (unit.Id != state.Phase.ActiveUnitId)
            return false;

        // Cant switch away once player has started using this active unit
        if (state.Phase.CommittedThisPhase.Contains(unit.Id))
            return false;

        // New active must exist
        if (!state.UnitInstances.TryGetValue(action.NewActiveUnitId, out var next))
            return false;

        // Must be same team and alive
        if (!next.IsAlive) return false;
        if (next.Team != state.Turn.TeamToAct) return false;

        // Cant switch to already-committed units
        if (state.Phase.CommittedThisPhase.Contains(next.Id))
            return false;

        return true;
    }

    private bool IsEndTurnLegal(IReadOnlyGameState state, IReadOnlyUnitInstance unit, EndTurnAction action)
    {
        // At the moment, end turn is always legal
        return true;
    }

    private bool IsMoveLegal(IReadOnlyGameState state, IReadOnlyUnitInstance unit, MoveAction action)
    {
        if (state.OccupiedHexes.Contains(action.Target)) return false;

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
    public ActionPlan BuildPlan(IReadOnlyGameState state, ActionChoice action)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));
        if (action is null) throw new ArgumentNullException(nameof(action));

        if (!IsActionLegal(state, action))
            throw new InvalidOperationException("Action is illegal for the current game state.");

        switch (action)
        {
            case MoveAction move:
                {
                    // Resolve movement cost 
                    int cost = _pathfinder.GetMoveCost(state.Map, state.UnitInstances[move.UnitId].Position, move.Target)!.Value;
                    return new MovePlan(move.UnitId, move.Target, cost);
                }

            case UseAbilityAction abilityAction:
                {
                    var ability = _abilityRepository.Get(abilityAction.AbilityId);

                    var resolvedTargets = ResolveAbilityTargets(state, abilityAction, ability);

                    int manaCost = ability.ManaCost;

                    return new UseAbilityPlan(
                        actorUnitId: abilityAction.UnitId,
                        baseTarget: abilityAction.Target,
                        targetUnitIds: resolvedTargets,
                        effectTemplateId: ability.Effect,
                        manaCost: manaCost);
                }
            case ChangeActiveUnitAction change:
                return new ChangeActiveUnitPlan(change.UnitId, change.NewActiveUnitId);

            case EndTurnAction end:
                return new EndTurnPlan(end.UnitId);

            default:
                throw new InvalidOperationException($"Unknown action type: {action.GetType().Name}");
        }
    }

    private IReadOnlyList<UnitInstanceId> ResolveAbilityTargets(IReadOnlyGameState state, UseAbilityAction abilityAction, Ability ability)
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

        return result;
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

        var end = new EndTurnAction(unit.Id);
        if (IsActionLegal(state, end)) yield return end;

        foreach (var move in GenerateMoveActions(state, unit))
            if (IsActionLegal(state, move)) yield return move;

        foreach (var use in GenerateAbilityActions(state, unit))
            yield return use;
    }

    private IEnumerable<ActionChoice> GenerateSwitchUnitActions(IReadOnlyGameState state)
    {
        var activeId = state.Phase.ActiveUnitId;

        // Active unit must exist (if state can ever be inconsistent)
        if (!state.UnitInstances.TryGetValue(activeId, out var active))
            yield break;

        // Only switch before the active unit commits
        if (state.Phase.CommittedThisPhase.Contains(activeId))
            yield break;

        foreach (var u in state.UnitInstances.Values)
        {
            if (!u.IsAlive) continue;
            if (u.Team != state.Turn.TeamToAct) continue;
            if (u.Id == activeId) continue;

            var action = new ChangeActiveUnitAction(activeId, u.Id);

            if (IsActionLegal(state, action))
                yield return action;
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