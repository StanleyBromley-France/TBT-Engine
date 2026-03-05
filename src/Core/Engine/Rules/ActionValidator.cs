namespace Core.Engine.Rules;

using Domain.Abilities.Targeting;
using Domain.Repositories;
using Domain.Units.Instances.ReadOnly;
using Actions.Choice;
using Game;
using Map.Pathfinding;
using Map.Search;
using Core.Domain.Types;

internal sealed class ActionValidator : IActionValidator
{
    private readonly IPathfinder _pathfinder;
    private readonly IAbilityRepository _abilityRepository;

    public ActionValidator(IPathfinder pathfinder, IAbilityRepository abilityRepository)
    {
        _pathfinder = pathfinder ?? throw new ArgumentNullException(nameof(pathfinder));
        _abilityRepository = abilityRepository ?? throw new ArgumentNullException(nameof(abilityRepository));
    }

    public bool IsActionLegal(IReadOnlyGameState state, ActionChoice action)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));
        if (action == null) throw new ArgumentNullException(nameof(action));

        if (!IsValidIssuer(state, action.UnitId, out var issuer))
            return false;

        return action switch
        {
            ChangeActiveUnitAction change => IsChangeActiveUnitLegal(state, issuer, change),
            MoveAction move => IsMoveLegal(state, issuer, move),
            UseAbilityAction use => IsUseAbilityLegal(state, issuer, use),
            SkipActiveUnitAction skip => IsSkipActiveUnitLegal(issuer),
            _ => false
        };
    }

    private static bool IsChangeActiveUnitLegal(IReadOnlyGameState state, IReadOnlyUnitInstance issuer, ChangeActiveUnitAction action)
    {
        // If the issuer is committed, it can only be switched away once AP is 0
        if (state.Phase.CommittedThisPhase.Contains(issuer.Id) && issuer.Resources.ActionPoints != 0)
        {
            return false;
        }

        if (!IsValidAlly(state, issuer, action.NewActiveUnitId, out var next))
            return false;

        // Cannot switch to a committed unit (committed + not selected = no longer playable)
        if (state.Phase.CommittedThisPhase.Contains(next.Id))
            return false;

        return true;
    }

    private static bool IsSkipActiveUnitLegal(IReadOnlyUnitInstance unit)
    {
        // No point skipping if no AP remaining
        return unit.Resources.ActionPoints > 0;
    }

    private bool IsMoveLegal(IReadOnlyGameState state, IReadOnlyUnitInstance issuer, MoveAction action)
    {
        if (state.OccupiedHexes.Contains(action.TargetHex)) return false;

        return _pathfinder.IsMoveValid(state.Map, issuer.Position, action.TargetHex, issuer.DerivedStats.MaxMovePoints);
    }

    private bool IsUseAbilityLegal(IReadOnlyGameState state, IReadOnlyUnitInstance issuer, UseAbilityAction action)
    {
        // Issuer must have the ability
        if (!issuer.Template.AbilityIds.Contains(action.AbilityId))
            return false;

        var ability = _abilityRepository.Get(action.AbilityId);

        // Resource check
        if (issuer.Resources.Mana < ability.ManaCost)
            return false;

        // Target validation based on targeting rules
        if (!IsValidAbilityTarget(state, issuer, action.Target, ability.Targeting.AllowedTarget, out var target))
            return false;

        // Line of sight
        if (ability.Targeting.RequiresLineOfSight &&
            !_pathfinder.HasLineOfSight(state.Map, issuer.Position, target.Position))
            return false;

        if (MapSearch.GetDistance(issuer.Position, target.Position) > ability.Targeting.Range)
            return false;

        return true;
    }

    // Helpers

    private static bool IsValidIssuer(IReadOnlyGameState state, UnitInstanceId issuerId, out IReadOnlyUnitInstance issuer)
    {
        issuer = default!;

        if (!TryGetUnit(state, issuerId, out var found))
            return false;

        if (found.Team != state.Turn.TeamToAct)
            return false;

        if (found.Id != state.Phase.ActiveUnitId)
            return false;

        issuer = found;
        return true;
    }

    private static bool IsValidAlly(IReadOnlyGameState state, IReadOnlyUnitInstance issuer, UnitInstanceId targetId, out IReadOnlyUnitInstance ally)
    {
        if (!TryGetUnit(state, targetId, out ally))
            return false;

        if (ally.Team != issuer.Team)
            return false;

        if (ally.Id == issuer.Id)
            return false;

        return true;
    }

    private static bool IsValidEnemy(IReadOnlyGameState state, IReadOnlyUnitInstance issuer, UnitInstanceId targetId, out IReadOnlyUnitInstance enemy)
    {
        if (!TryGetUnit(state, targetId, out enemy))
            return false;

        return enemy.Team != issuer.Team;
    }

    private static bool IsValidSelf(IReadOnlyGameState state, IReadOnlyUnitInstance issuer, UnitInstanceId targetId, out IReadOnlyUnitInstance self)
    {
        if (!TryGetUnit(state, targetId, out self))
            return false;

        return self.Id == issuer.Id;
    }

    private static bool IsValidAbilityTarget(IReadOnlyGameState state, IReadOnlyUnitInstance issuer, UnitInstanceId targetId, TargetType type, out IReadOnlyUnitInstance target)
    {
        target = default!;

        return type switch
        {
            TargetType.Self => IsValidSelf(state, issuer, targetId, out target),
            TargetType.Ally => IsValidAlly(state, issuer, targetId, out target),
            TargetType.Enemy => IsValidEnemy(state, issuer, targetId, out target),
            _ => false
        };
    }

    private static bool TryGetUnit(IReadOnlyGameState state, UnitInstanceId unitId, out IReadOnlyUnitInstance unit)
    {
        unit = default!;

        if (!state.UnitInstances.TryGetValue(unitId, out var found))
            return false;

        if (!found.IsAlive)
            return false;

        unit = found;
        return true;
    }

}
