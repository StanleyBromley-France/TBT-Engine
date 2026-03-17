namespace Agents.Mcts.Hashing;

using Core.Domain.Effects.Components.Instances.ReadOnly;
using Core.Game.State.ReadOnly;
using Core.Map.Search;

public sealed partial class GameStateHasher : IGameStateHasher
{
    public GameStateKey Compute(IReadOnlyGameState state)
    {
        if (state is null)
            throw new ArgumentNullException(nameof(state));

        var hash = new HashBuilder();

        // Hash every piece of mutable simulation state that can influence future
        // legal actions, deterministic RNG progression, or rollout outcomes.
        HashMap(state, ref hash);
        HashTurnState(state, ref hash);
        HashUnits(state, ref hash);
        HashEffects(state, ref hash);

        return hash.ToKey();
    }

    private static void HashMap(IReadOnlyGameState state, ref HashBuilder hash)
    {
        hash.Add(state.Map.Width);
        hash.Add(state.Map.Height);

        // Walk the full grid in a canonical order so equivalent maps hash the same
        // regardless of how higher-level collections were created.
        for (var col = 0; col < state.Map.Width; col++)
        {
            for (var row = 0; row < state.Map.Height; row++)
            {
                var coord = HexCoordConverter.FromOffset(col, row);
                var tile = state.Map.GetTile(coord);

                hash.Add(tile is not null);
                if (tile is not null)
                    hash.Add((int)tile.Terrain);
            }
        }
    }

    private static void HashTurnState(IReadOnlyGameState state, ref HashBuilder hash)
    {
        hash.Add(state.Turn.AttackerTurnsTaken);
        hash.Add(state.Turn.TeamToAct.Value);
        hash.Add(state.Phase.CurrentlyCommiting.HasValue);
        if (state.Phase.CurrentlyCommiting.HasValue)
            hash.Add(state.Phase.CurrentlyCommiting.Value.Value);

        foreach (var committedUnitId in state.Phase.CommittedThisPhase.OrderBy(id => id.Value))
            hash.Add(committedUnitId.Value);

        hash.Add(state.Rng.Seed);
        hash.Add(state.Rng.Position);
    }

    private static void HashUnits(IReadOnlyGameState state, ref HashBuilder hash)
    {
        // Dictionaries are ordered by stable ids before hashing so insertion order does not affect the final state key
        foreach (var unitEntry in state.UnitInstances.OrderBy(entry => entry.Key.Value))
        {
            var unit = unitEntry.Value;

            hash.Add(unit.Id.Value);
            hash.Add(unit.Team.Value);
            hash.Add(unit.Template.Id.Value);

            hash.Add(unit.Position.Q);
            hash.Add(unit.Position.R);
            hash.Add(unit.IsAlive);

            hash.Add(unit.Resources.HP);
            hash.Add(unit.Resources.MovePoints);
            hash.Add(unit.Resources.ActionPoints);
            hash.Add(unit.Resources.Mana);

            hash.Add(unit.DerivedStats.MaxHP);
            hash.Add(unit.DerivedStats.MaxManaPoints);
            hash.Add(unit.DerivedStats.MaxMovePoints);
            hash.Add(unit.DerivedStats.MaxActionPoints);
            hash.Add(unit.DerivedStats.DamageDealt);
            hash.Add(unit.DerivedStats.HealingDealt);
            hash.Add(unit.DerivedStats.HealingReceived);
            hash.Add(unit.DerivedStats.PhysicalDamageReceived);
            hash.Add(unit.DerivedStats.MagicDamageReceived);

            hash.Add(unit.Template.AbilityIds.Length);
            foreach (var abilityId in unit.Template.AbilityIds)
                hash.Add(abilityId.Value);
        }
    }

    private static void HashEffects(IReadOnlyGameState state, ref HashBuilder hash)
    {
        foreach (var unitEffectsEntry in state.ActiveEffects.OrderBy(entry => entry.Key.Value))
        {
            hash.Add(unitEffectsEntry.Key.Value);

            foreach (var effectEntry in unitEffectsEntry.Value.OrderBy(entry => entry.Key.Value))
            {
                var effect = effectEntry.Value;

                hash.Add(effect.Id.Value);
                hash.Add(effect.Template.Id.Value);
                hash.Add(effect.SourceUnitId.Value);
                hash.Add(effect.TargetUnitId.Value);
                hash.Add(effect.RemainingTicks);
                hash.Add(effect.CurrentStacks);

                foreach (var component in effect.Components.OrderBy(component => component.Id.Value))
                {
                    hash.Add(component.Id.Value);
                    hash.Add(component.Template.Id.Value);

                    if (component is IReadOnlyResolvableHpDeltaComponent hpDeltaComponent)
                    {
                        hash.Add(true);
                        hash.Add((int)hpDeltaComponent.HpType);
                        hash.Add(hpDeltaComponent.ResolvedHpDelta.HasValue);

                        if (hpDeltaComponent.ResolvedHpDelta.HasValue)
                            hash.Add(hpDeltaComponent.ResolvedHpDelta.Value);
                    }
                    else
                    {
                        hash.Add(false);
                    }
                }
            }
        }
    }
}
