namespace Core.Engine.Rules;

using Core.Domain.Abilities;
using Core.Domain.Abilities.Targeting;
using Core.Domain.Types;
using Core.Domain.Units.Instances.ReadOnly;
using Core.Engine.Actions;
using Core.Engine.Mutation;
using Core.Game;
using Core.Map.Pathfinding;

public sealed class CombatRules : IGameRules
{
    private readonly IPathfinder _pathfinder;

    public CombatRules(IPathfinder pathfinder)
    {
        _pathfinder = pathfinder;
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
        return _pathfinder.IsMoveValid(state.Map, unit.Position, action.Target, unit.DerivedStats.MovePoints);
    }

    private bool IsUseAbilityLegal(IReadOnlyGameState state, IReadOnlyUnitInstance unit, UseAbilityAction action)
    {
        if (unit.Template.AbilityIds.Contains(action.Ability.Id)) return false;
        if (unit.Resources.Mana < action.Ability.ManaCost) return false;
        if (action.Targets.Count < action.Ability.Targeting.MinTargets) return false;
        if (action.Targets.Count > action.Ability.Targeting.MaxTargets) return false;

        foreach (var targetId in action.Targets) 
        {
            if (!state.UnitInstances.TryGetValue(targetId, out var target))
                return false;

            if (!target.IsAlive)
                return false;

            if (!MatchesTargetType(unit, target, action.Ability.Targeting.AllowedTarget))
                return false;

            if (action.Ability.Targeting.RequiresLineOfSight == true && !_pathfinder.HasLineOfSight(state.Map, unit.Position, target.Position))
                return false;
        }

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

    public IReadOnlyList<ActionChoice> GetLegalActions(IReadOnlyGameState state)
        => throw new NotImplementedException();

    public TeamId? GetWinner(IReadOnlyGameState state)
        => throw new NotImplementedException();
}