using Core.Domain.Abilities;
using Core.Domain.Effects.Components.Instances.Mutable;
using Core.Domain.Effects.Components.Templates;
using Core.Domain.Effects.Instances.Mutable;
using Core.Domain.Effects.Templates;
using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Game.Match;
using Core.Game.Session;
using Core.Game.State;
using Core.Map.Grid;
using Core.Map.Search;
using Core.Map.Terrain;
using Core.Tests.Engine.TestSupport;
using Core.Undo;

namespace Core.Tests.Game;

public class GameSessionSandboxTests
{
    // Builds a rich mid-game snapshot and verifies deep-clone parity across the full mutable runtime graph.
    [Fact]
    public void CreateSandbox_MidGameState_Copies_All_Mutable_Fields()
    {
        // Arrange: build a richer mid-game snapshot with 3 units per side and multiple active effects.
        var live = CreateMidGameSession();

        // Act: clone into sandbox.
        var sandbox = live.CreateSandbox();

        // Assert: full mutable runtime graph is copied by value and detached by reference.
        AssertRuntimeStateDeepCloneMatches(live, sandbox);

        // Assert: sandbox-only mutations do not change live runtime values.
        AssertSandboxMutationIsolationOnAllBranches(live, sandbox);
    }

    // Creates a realistic mid-match session with six units, progressed turn/phase/rng, and seeded effects.
    private static GameSession CreateMidGameSession()
    {
        var attacker1 = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0), hp: 9, mana: 6);
        var attacker2 = EngineTestFactory.CreateUnit(2, 1, new HexCoord(1, 0), hp: 7, mana: 4);
        var attacker3 = EngineTestFactory.CreateUnit(3, 1, new HexCoord(2, 0), hp: 1, mana: 0);

        var defender1 = EngineTestFactory.CreateUnit(4, 2, new HexCoord(0, 2), hp: 8, mana: 5);
        var defender2 = EngineTestFactory.CreateUnit(5, 2, new HexCoord(1, 2), hp: 6, mana: 3);
        var defender3 = EngineTestFactory.CreateUnit(6, 2, new HexCoord(2, 2), hp: 10, mana: 8);

        var allUnits = new[] { attacker1, attacker2, attacker3, defender1, defender2, defender3 };

        // Simulate ongoing combat resource/stat drift.
        attacker1.Resources.MovePoints = 1;
        attacker1.Resources.ActionPoints = 0;
        attacker1.DerivedStats.MaxHP = 11;

        attacker2.Resources.MovePoints = 2;
        attacker2.Resources.ActionPoints = 1;
        attacker2.DerivedStats.DamageDealt = 120;

        attacker3.Resources.HP = 0;
        attacker3.Resources.MovePoints = 0;
        attacker3.Resources.ActionPoints = 0;

        defender1.Resources.MovePoints = 1;
        defender1.Resources.ActionPoints = 1;
        defender1.DerivedStats.HealingReceived = 85;

        defender2.Resources.MovePoints = 0;
        defender2.Resources.ActionPoints = 0;
        defender2.DerivedStats.MagicDamageReceived = 130;

        defender3.Resources.MovePoints = 3;
        defender3.Resources.ActionPoints = 2;
        defender3.DerivedStats.PhysicalDamageReceived = 90;

        var state = new GameState(
            map: CreateMidGameMap(),
            unitInstances: allUnits.ToDictionary(u => u.Id, u => u),
            activeEffects: allUnits.ToDictionary(
                u => u.Id,
                _ => new Dictionary<EffectInstanceId, EffectInstance>()),
            turn: new Turn(attackerTurnsTaken: 4, teamToAct: new TeamId(2)),
            phase: new ActivationPhase(defender1.Id),
            rng: new RngState(seed: 777, position: 42));

        state.Phase.MarkCommitted(attacker1.Id);
        state.Phase.MarkCommitted(defender2.Id);
        state.Phase.SetCurrentlyCommiting(defender1.Id);

        AddMidGameEffects(state, attacker1.Id, attacker2.Id, defender1.Id, defender2.Id, defender3.Id);

        var session = EngineTestFactory.CreateSession(
            state,
            new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()));

        session.Runtime.SetGameOutcome(GameOutcome.Ongoing());
        session.Runtime.Undo.Commit(new UndoRecord());
        session.Runtime.Undo.Commit(new UndoRecord());

        return session;
    }

    // Creates a map with varied terrain so map tile cloning can be verified by both parity and isolation.
    private static Core.Map.Grid.Map CreateMidGameMap()
    {
        var tiles = new Tile[5, 5];

        for (var col = 0; col < 5; col++)
        {
            for (var row = 0; row < 5; row++)
            {
                tiles[col, row] = new Tile { Terrain = TerrainType.Plain };
            }
        }

        tiles[0, 2].Terrain = TerrainType.Mountain;
        tiles[2, 2].Terrain = TerrainType.Water;
        tiles[4, 1].Terrain = TerrainType.Mountain;

        return new Core.Map.Grid.Map(tiles);
    }

    // Seeds multiple effect/component combinations across targets to exercise nested clone paths.
    private static void AddMidGameEffects(
        GameState state,
        UnitInstanceId attacker1Id,
        UnitInstanceId attacker2Id,
        UnitInstanceId defender1Id,
        UnitInstanceId defender2Id,
        UnitInstanceId defender3Id)
    {
        var damageTemplate = new TestEffectTemplate(new EffectTemplateId("damage-template"), totalTicks: 3, maxStacks: 5);
        var sustainTemplate = new TestEffectTemplate(new EffectTemplateId("sustain-template"), totalTicks: 4, maxStacks: 3);
        var debuffTemplate = new TestEffectTemplate(new EffectTemplateId("debuff-template"), totalTicks: 2, maxStacks: 2);

        var dmgComp = new InstantDamageComponentInstance(
            new EffectComponentInstanceId(1001),
            new InstantDamageComponentTemplate(new EffectComponentTemplateId("cid-1001"), 5, DamageType.Physical, 0, 1f));
        ((IResolvableHpDeltaComponent)dmgComp).ResolvedHpDelta = 5;

        var hotComp = new HealOverTimeComponentInstance(
            new EffectComponentInstanceId(1002),
            new HealOverTimeComponentTemplate(new EffectComponentTemplateId("cid-1002"), 2));
        ((IResolvableHpDeltaComponent)hotComp).ResolvedHpDelta = 2;

        var dotComp = new DamageOverTimeComponentInstance(
            new EffectComponentInstanceId(1003),
            new DamageOverTimeComponentTemplate(new EffectComponentTemplateId("cid-1003"), 3, DamageType.Magical));
        ((IResolvableHpDeltaComponent)dotComp).ResolvedHpDelta = 3;

        var healComp = new InstantHealComponentInstance(
            new EffectComponentInstanceId(1004),
            new InstantHealComponentTemplate(new EffectComponentTemplateId("cid-1004"), 4));
        ((IResolvableHpDeltaComponent)healComp).ResolvedHpDelta = 4;

        var flatComp = new FlatAttributeModifierComponentInstance(
            new EffectComponentInstanceId(1005),
            new FlatAttributeModifierComponentTemplate(new EffectComponentTemplateId("cid-1005"), Core.Domain.Effects.Stats.StatType.MaxHP, 2));

        var percentComp = new PercentAttributeModifierComponentInstance(
            new EffectComponentInstanceId(1006),
            new PercentAttributeModifierComponentTemplate(new EffectComponentTemplateId("cid-1006"), Core.Domain.Effects.Stats.StatType.DamageDealt, 15));

        var effect1 = new EffectInstance(
            new EffectInstanceId(2001),
            damageTemplate,
            attacker1Id,
            defender1Id,
            new EffectComponentInstance[] { dmgComp, flatComp })
        {
            RemainingTicks = 2,
            CurrentStacks = 2
        };

        var effect2 = new EffectInstance(
            new EffectInstanceId(2002),
            sustainTemplate,
            defender3Id,
            defender1Id,
            new EffectComponentInstance[] { hotComp })
        {
            RemainingTicks = 3,
            CurrentStacks = 1
        };

        var effect3 = new EffectInstance(
            new EffectInstanceId(2003),
            debuffTemplate,
            attacker2Id,
            defender2Id,
            new EffectComponentInstance[] { dotComp, percentComp })
        {
            RemainingTicks = 1,
            CurrentStacks = 2
        };

        var effect4 = new EffectInstance(
            new EffectInstanceId(2004),
            sustainTemplate,
            defender2Id,
            defender3Id,
            new EffectComponentInstance[] { healComp })
        {
            RemainingTicks = 4,
            CurrentStacks = 1
        };

        state.ActiveEffects[defender1Id][effect1.Id] = effect1;
        state.ActiveEffects[defender1Id][effect2.Id] = effect2;
        state.ActiveEffects[defender2Id][effect3.Id] = effect3;
        state.ActiveEffects[defender3Id][effect4.Id] = effect4;
    }

    // Performs exhaustive field-by-field parity and reference-detachment checks on the cloned runtime state.
    private static void AssertRuntimeStateDeepCloneMatches(GameSession live, GameSession sandbox)
    {
        var liveRuntime = live.Runtime;
        var sandboxRuntime = sandbox.Runtime;
        var liveState = liveRuntime.State;
        var sandboxState = sandboxRuntime.State;

        // Assert session roots: shared immutable context and detached runtime/state references.
        Assert.Same(live.Context, sandbox.Context);
        Assert.NotSame(liveRuntime, sandboxRuntime);
        Assert.NotSame(liveState, sandboxState);

        // Assert runtime-level parity: outcome copied, undo history reset in sandbox.
        Assert.Equal(liveRuntime.Outcome.Type, sandboxRuntime.Outcome.Type);
        Assert.Equal(liveRuntime.Outcome.WinningTeam, sandboxRuntime.Outcome.WinningTeam);
        Assert.Equal(2, liveRuntime.Undo.Records.Count);
        Assert.Empty(sandboxRuntime.Undo.Records);

        // Assert map object is detached and dimensions are preserved.
        Assert.NotSame(liveState.Map, sandboxState.Map);
        Assert.Equal(liveState.Map.Width, sandboxState.Map.Width);
        Assert.Equal(liveState.Map.Height, sandboxState.Map.Height);

        // Assert every map tile exists in the same places and carries the same terrain values.
        for (var col = 0; col < liveState.Map.Width; col++)
        {
            for (var row = 0; row < liveState.Map.Height; row++)
            {
                var coord = HexCoordConverter.FromOffset(col, row);
                var liveHasTile = liveState.Map.TryGetTile(coord, out var liveTile);
                var sandboxHasTile = sandboxState.Map.TryGetTile(coord, out var sandboxTile);

                Assert.Equal(liveHasTile, sandboxHasTile);
                if (liveHasTile)
                {
                    Assert.NotSame(liveTile, sandboxTile);
                    Assert.Equal(liveTile.Terrain, sandboxTile.Terrain);
                }
            }
        }

        // Assert activation phase is detached but value-equivalent.
        Assert.NotSame(liveState.Phase, sandboxState.Phase);
        Assert.Equal(liveState.Phase.ActiveUnitId, sandboxState.Phase.ActiveUnitId);
        Assert.Equal(liveState.Phase.CurrentlyCommiting, sandboxState.Phase.CurrentlyCommiting);
        Assert.NotSame(liveState.Phase.CommittedThisPhase, sandboxState.Phase.CommittedThisPhase);
        Assert.True(liveState.Phase.CommittedThisPhase.SetEquals(sandboxState.Phase.CommittedThisPhase));

        // Assert RNG state is detached and copied exactly.
        Assert.NotSame(liveState.Rng, sandboxState.Rng);
        Assert.Equal(liveState.Rng.Seed, sandboxState.Rng.Seed);
        Assert.Equal(liveState.Rng.Position, sandboxState.Rng.Position);

        // Assert turn value is copied exactly.
        Assert.Equal(liveState.Turn.AttackerTurnsTaken, sandboxState.Turn.AttackerTurnsTaken);
        Assert.Equal(liveState.Turn.TeamToAct, sandboxState.Turn.TeamToAct);

        // Assert occupied-hex set is detached and value-equivalent.
        Assert.NotSame(liveState.OccupiedHexes, sandboxState.OccupiedHexes);
        Assert.True(liveState.OccupiedHexes.SetEquals(sandboxState.OccupiedHexes));

        // Assert unit dictionary is detached and has the same unit keys.
        Assert.NotSame(liveState.UnitInstances, sandboxState.UnitInstances);
        Assert.Equal(liveState.UnitInstances.Count, sandboxState.UnitInstances.Count);

        // Assert each unit snapshot is detached but matches identity, template ref, resources, and derived stats.
        foreach (var (unitId, liveUnit) in liveState.UnitInstances)
        {
            Assert.True(sandboxState.UnitInstances.TryGetValue(unitId, out var sandboxUnit));
            Assert.NotSame(liveUnit, sandboxUnit);

            Assert.Equal(liveUnit.Id, sandboxUnit.Id);
            Assert.Equal(liveUnit.Team, sandboxUnit.Team);
            Assert.Same(liveUnit.Template, sandboxUnit.Template);
            Assert.Equal(liveUnit.Position, sandboxUnit.Position);

            Assert.NotSame(liveUnit.Resources, sandboxUnit.Resources);
            Assert.Equal(liveUnit.Resources.HP, sandboxUnit.Resources.HP);
            Assert.Equal(liveUnit.Resources.MovePoints, sandboxUnit.Resources.MovePoints);
            Assert.Equal(liveUnit.Resources.ActionPoints, sandboxUnit.Resources.ActionPoints);
            Assert.Equal(liveUnit.Resources.Mana, sandboxUnit.Resources.Mana);

            Assert.NotSame(liveUnit.DerivedStats, sandboxUnit.DerivedStats);
            Assert.Equal(liveUnit.DerivedStats.MaxHP, sandboxUnit.DerivedStats.MaxHP);
            Assert.Equal(liveUnit.DerivedStats.MaxManaPoints, sandboxUnit.DerivedStats.MaxManaPoints);
            Assert.Equal(liveUnit.DerivedStats.MaxMovePoints, sandboxUnit.DerivedStats.MaxMovePoints);
            Assert.Equal(liveUnit.DerivedStats.MaxActionPoints, sandboxUnit.DerivedStats.MaxActionPoints);
            Assert.Equal(liveUnit.DerivedStats.DamageDealt, sandboxUnit.DerivedStats.DamageDealt);
            Assert.Equal(liveUnit.DerivedStats.HealingDealt, sandboxUnit.DerivedStats.HealingDealt);
            Assert.Equal(liveUnit.DerivedStats.HealingReceived, sandboxUnit.DerivedStats.HealingReceived);
            Assert.Equal(liveUnit.DerivedStats.PhysicalDamageReceived, sandboxUnit.DerivedStats.PhysicalDamageReceived);
            Assert.Equal(liveUnit.DerivedStats.MagicDamageReceived, sandboxUnit.DerivedStats.MagicDamageReceived);
        }

        // Assert active-effect root dictionary is detached and has the same target keys.
        Assert.NotSame(liveState.ActiveEffects, sandboxState.ActiveEffects);
        Assert.Equal(liveState.ActiveEffects.Count, sandboxState.ActiveEffects.Count);

        // Assert per-target effect dictionaries are detached and value-equivalent.
        foreach (var (targetId, liveEffectsById) in liveState.ActiveEffects)
        {
            Assert.True(sandboxState.ActiveEffects.TryGetValue(targetId, out var sandboxEffectsById));
            Assert.NotSame(liveEffectsById, sandboxEffectsById);
            Assert.Equal(liveEffectsById.Count, sandboxEffectsById.Count);

            // Assert each effect instance is detached with equivalent runtime values.
            foreach (var (effectId, liveEffect) in liveEffectsById)
            {
                Assert.True(sandboxEffectsById.TryGetValue(effectId, out var sandboxEffect));
                Assert.NotSame(liveEffect, sandboxEffect);

                Assert.Equal(liveEffect.Id, sandboxEffect.Id);
                Assert.Same(liveEffect.Template, sandboxEffect.Template);
                Assert.Equal(liveEffect.SourceUnitId, sandboxEffect.SourceUnitId);
                Assert.Equal(liveEffect.TargetUnitId, sandboxEffect.TargetUnitId);
                Assert.Equal(liveEffect.RemainingTicks, sandboxEffect.RemainingTicks);
                Assert.Equal(liveEffect.CurrentStacks, sandboxEffect.CurrentStacks);

                Assert.Equal(liveEffect.Components.Length, sandboxEffect.Components.Length);

                // Assert each component instance is detached and equivalent, including resolved HP deltas when present.
                for (var i = 0; i < liveEffect.Components.Length; i++)
                {
                    var liveComponent = liveEffect.Components[i];
                    var sandboxComponent = sandboxEffect.Components[i];

                    Assert.NotSame(liveComponent, sandboxComponent);
                    Assert.Equal(liveComponent.Id, sandboxComponent.Id);
                    Assert.Same(liveComponent.Template, sandboxComponent.Template);
                    Assert.Equal(liveComponent.GetType(), sandboxComponent.GetType());

                    if (liveComponent is IResolvableHpDeltaComponent liveResolvable &&
                        sandboxComponent is IResolvableHpDeltaComponent sandboxResolvable)
                    {
                        Assert.Equal(liveResolvable.ResolvedHpDelta, sandboxResolvable.ResolvedHpDelta);
                    }
                }
            }
        }
    }

    // Mutates every major sandbox branch and verifies the live session remains unchanged.
    private static void AssertSandboxMutationIsolationOnAllBranches(GameSession live, GameSession sandbox)
    {
        var liveState = live.Runtime.State;
        var sandboxState = sandbox.Runtime.State;

        // Mutate one sandbox map tile terrain.
        var liveMountainCoord = HexCoordConverter.FromOffset(0, 2);
        Assert.True(sandboxState.Map.TryGetTile(liveMountainCoord, out var sandboxTile));
        ((Tile)sandboxTile).Terrain = TerrainType.Water;

        // Assert live map terrain remains unchanged.
        Assert.True(liveState.Map.TryGetTile(liveMountainCoord, out var liveTile));
        Assert.Equal(TerrainType.Mountain, liveTile.Terrain);

        // Mutate sandbox turn/rng/phase/occupied-hex state.
        sandboxState.Turn = new Turn(99, new TeamId(1));
        sandboxState.Rng = new RngState(1, 2);
        sandboxState.Phase.ActiveUnitId = new UnitInstanceId(6);
        sandboxState.Phase.SetCurrentlyCommiting(new UnitInstanceId(124));
        sandboxState.Phase.MarkCommitted(new UnitInstanceId(123));
        sandboxState.OccupiedHexes.Add(new HexCoord(4, 4));

        // Assert live turn/rng/phase/occupied-hex state is unchanged.
        Assert.Equal(4, liveState.Turn.AttackerTurnsTaken);
        Assert.Equal(new TeamId(2), liveState.Turn.TeamToAct);
        Assert.Equal(777, liveState.Rng.Seed);
        Assert.Equal(42, liveState.Rng.Position);
        Assert.Equal(new UnitInstanceId(4), liveState.Phase.CurrentlyCommiting);
        Assert.False(liveState.Phase.HasCommitted(new UnitInstanceId(123)));
        Assert.DoesNotContain(new HexCoord(4, 4), liveState.OccupiedHexes);

        // Mutate sandbox unit position/resources/derived stats.
        var sandboxUnit = sandboxState.UnitInstances[new UnitInstanceId(2)];
        sandboxUnit.Position = new HexCoord(3, 3);
        sandboxUnit.Resources.HP = 1;
        sandboxUnit.Resources.MovePoints = 0;
        sandboxUnit.Resources.ActionPoints = 0;
        sandboxUnit.Resources.Mana = 0;
        sandboxUnit.DerivedStats.MaxHP = 999;
        sandboxUnit.DerivedStats.MaxManaPoints = 999;
        sandboxUnit.DerivedStats.MaxMovePoints = 999;
        sandboxUnit.DerivedStats.MaxActionPoints = 999;
        sandboxUnit.DerivedStats.DamageDealt = 999;
        sandboxUnit.DerivedStats.HealingDealt = 999;
        sandboxUnit.DerivedStats.HealingReceived = 999;
        sandboxUnit.DerivedStats.PhysicalDamageReceived = 999;
        sandboxUnit.DerivedStats.MagicDamageReceived = 999;

        // Assert live unit position/resources/derived stats are unchanged.
        var liveUnit = liveState.UnitInstances[new UnitInstanceId(2)];
        Assert.Equal(new HexCoord(1, 0), liveUnit.Position);
        Assert.Equal(7, liveUnit.Resources.HP);
        Assert.Equal(2, liveUnit.Resources.MovePoints);
        Assert.Equal(1, liveUnit.Resources.ActionPoints);
        Assert.Equal(4, liveUnit.Resources.Mana);
        Assert.Equal(10, liveUnit.DerivedStats.MaxHP);
        Assert.Equal(10, liveUnit.DerivedStats.MaxManaPoints);
        Assert.Equal(3, liveUnit.DerivedStats.MaxMovePoints);
        Assert.Equal(2, liveUnit.DerivedStats.MaxActionPoints);
        Assert.Equal(120, liveUnit.DerivedStats.DamageDealt);
        Assert.Equal(100, liveUnit.DerivedStats.HealingDealt);
        Assert.Equal(100, liveUnit.DerivedStats.HealingReceived);
        Assert.Equal(100, liveUnit.DerivedStats.PhysicalDamageReceived);
        Assert.Equal(100, liveUnit.DerivedStats.MagicDamageReceived);

        // Mutate sandbox effect runtime fields, resolvable component state, and effect collection membership.
        var sandboxEffect = sandboxState.ActiveEffects[new UnitInstanceId(4)][new EffectInstanceId(2001)];
        sandboxEffect.RemainingTicks = 0;
        sandboxEffect.CurrentStacks = 5;
        ((IResolvableHpDeltaComponent)sandboxEffect.Components[0]).ResolvedHpDelta = 42;
        sandboxState.ActiveEffects[new UnitInstanceId(4)].Remove(new EffectInstanceId(2002));

        // Assert live effect runtime fields/component state/collection membership are unchanged.
        var liveEffect = liveState.ActiveEffects[new UnitInstanceId(4)][new EffectInstanceId(2001)];
        Assert.Equal(2, liveEffect.RemainingTicks);
        Assert.Equal(2, liveEffect.CurrentStacks);
        Assert.Equal(5, ((IResolvableHpDeltaComponent)liveEffect.Components[0]).ResolvedHpDelta);
        Assert.True(liveState.ActiveEffects[new UnitInstanceId(4)].ContainsKey(new EffectInstanceId(2002)));

        // Mutate sandbox undo/outcome state.
        sandbox.Runtime.Undo.Commit(new UndoRecord());
        sandbox.Runtime.SetGameOutcome(GameOutcome.Draw());

        // Assert live undo/outcome state is unchanged.
        Assert.Equal(2, live.Runtime.Undo.Records.Count);
        Assert.Equal(GameOutcomeType.Ongoing, live.Runtime.Outcome.Type);
    }

    private sealed class TestEffectTemplate : EffectTemplate
    {
        public TestEffectTemplate(EffectTemplateId id, int totalTicks = 3, int maxStacks = 5)
            : base(id, "sandbox-test", isHarmful: true, totalTicks: totalTicks, maxStacks: maxStacks, components: Array.Empty<EffectComponentTemplateId>())
        {
        }
    }
}
