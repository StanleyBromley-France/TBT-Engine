using Core.Domain.Abilities;
using Core.Domain.Effects;
using Core.Domain.Effects.Components.Instances;
using Core.Domain.Effects.Components.Instances.Mutable;
using Core.Domain.Effects.Components.Instances.ReadOnly;
using Core.Domain.Effects.Components.Templates;
using Core.Domain.Effects.Instances.Mutable;
using Core.Domain.Effects.Instances.ReadOnly;
using Core.Domain.Effects.Templates;
using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Domain.Units.Instances.Mutable;
using Core.Domain.Units.Templates;
using Core.Engine.Effects;
using Core.Engine.Effects.Components.Calculators;
using Core.Engine.Mutation;
using Core.Engine.Random;
using Core.Undo;
using Core.Tests.Engine.TestSupport;
using Core.Game.Match;
using Core.Game.State;
using Core.Game.State.ReadOnly;
using Core.Game.Factories.EffectComponents;
using Core.Game.Factories.Effects;
using Core.Game.Factories.Units;
using Core.Game.Session;
using Core.Game.Requests;

namespace Core.Tests.Engine.Effects;

public class EffectManagerTests
{
    [Fact]
    public void ApplyOrStackEffect_Creates_New_Effect_Resolves_Hp_And_Recomputes_DerivedStats()
    {
        // Arrange: create source/target units and wire deterministic calculators so HP and derived stat outcomes are predictable
        var source = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0));
        var target = EngineTestFactory.CreateUnit(2, 2, new HexCoord(1, 0), hp: 10);
        var state = EngineTestFactory.CreateState(new[] { source, target }, teamToAct: 1);

        var templateId = new EffectTemplateId("test-effect");
        var template = new TestEffectTemplate(templateId, totalTicks: 3, maxStacks: 2);
        var healTemplate = new InstantHealComponentTemplate(new EffectComponentTemplateId("heal"), heal: 1);
        var damageTemplate = new InstantDamageComponentTemplate(new EffectComponentTemplateId("dmg"), damage: 1, DamageType.Physical, 0, 1.5f);
        var healComponent = new InstantHealComponentInstance(new EffectComponentInstanceId(11), healTemplate);
        var damageComponent = new InstantDamageComponentInstance(new EffectComponentInstanceId(12), damageTemplate);

        var factory = new FakeEffectFactory((src, tgt) =>
            new EffectInstance(
                new EffectInstanceId(100),
                template,
                src,
                tgt,
                new EffectComponentInstance[] { healComponent, damageComponent }));
        var session = CreateSession(state, factory, template);
        var undo = new UndoRecord();
        var context = new GameMutationContext(session, new DeterministicRng(), undo);

        var expectedDerived = new UnitDerivedStats(9, 100, 100, 20, 6, 2, 100, 100, 100);
        var derivedStats = new FakeDerivedStatsCalculator(expectedDerived);
        var healCalc = new FakeHealCalculator(3);
        var dmgCalc = new FakeDamageCalculator(5);
        var manager = new EffectManager(derivedStats, dmgCalc, healCalc);
        var request = new EffectApplicationRequest(templateId, source.Id, new[] { target.Id });

        // Act: apply the effect request to a target with no existing effect stack.
        manager.ApplyOrStackEffect(context, state, request);

        // Assert
        Assert.Equal(1, factory.CreateCallCount);
        Assert.True(state.ActiveEffects[target.Id].ContainsKey(new EffectInstanceId(100)));
        Assert.Equal(8, target.Resources.HP); // +3 heal then -5 damage
        Assert.Equal(expectedDerived, target.DerivedStats);
        Assert.Single(derivedStats.ComputeCalls);
        Assert.Equal(target.Id, derivedStats.ComputeCalls[0]);
    }

    [Fact]
    public void ApplyOrStackEffect_Stacks_Existing_Effect_Without_Creating_New_One()
    {
        // Arrange: seed an existing effect instance on the target so manager should stack/refresh instead of creating a new one
        var source = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0));
        var target = EngineTestFactory.CreateUnit(2, 2, new HexCoord(1, 0));
        var state = EngineTestFactory.CreateState(new[] { source, target }, teamToAct: 1);

        var templateId = new EffectTemplateId("test-effect");
        var template = new TestEffectTemplate(templateId, totalTicks: 4, maxStacks: 3);
        var healTemplate = new InstantHealComponentTemplate(new EffectComponentTemplateId("heal"), heal: 1);
        var healComponent = new InstantHealComponentInstance(new EffectComponentInstanceId(21), healTemplate);
        var existing = new EffectInstance(
            new EffectInstanceId(200),
            template,
            source.Id,
            target.Id,
            new EffectComponentInstance[] { healComponent });
        existing.CurrentStacks = 1;
        existing.RemainingTicks = 1;
        state.ActiveEffects[target.Id][existing.Id] = existing;

        var factory = new FakeEffectFactory((src, tgt) =>
            throw new InvalidOperationException("Factory should not be called for existing effect."));
        var session = CreateSession(state, factory, template);
        var undo = new UndoRecord();
        var context = new GameMutationContext(session, new DeterministicRng(), undo);
        var expectedDerived = new UnitDerivedStats(3, 100, 100, 10, 10, 2, 100, 100, 100);
        var manager = new EffectManager(
            new FakeDerivedStatsCalculator(expectedDerived),
            new FakeDamageCalculator(0),
            new FakeHealCalculator(2));
        var request = new EffectApplicationRequest(templateId, source.Id, new[] { target.Id });

        // Act: apply the same template again and hit the stack path.
        manager.ApplyOrStackEffect(context, state, request);

        // Assert
        Assert.Equal(0, factory.CreateCallCount);
        Assert.Equal(2, existing.CurrentStacks);
        Assert.Equal(4, existing.RemainingTicks);
        Assert.Equal(10, target.Resources.HP); // existing path does not call OnApply
    }

    [Fact]
    public void TickAll_Decrements_And_Removes_Expired_Effects_Then_Recomputes_DerivedStats()
    {
        // Arrange: place one effect at 1 remaining tick so a single TickAll call should expire and remove it
        var source = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0));
        var target = EngineTestFactory.CreateUnit(2, 2, new HexCoord(1, 0));
        var state = EngineTestFactory.CreateState(new[] { source, target }, teamToAct: 1);

        var template = new TestEffectTemplate(new EffectTemplateId("tick"), totalTicks: 1, maxStacks: 1);
        var effect = new EffectInstance(
            new EffectInstanceId(300),
            template,
            source.Id,
            target.Id,
            Array.Empty<EffectComponentInstance>());
        effect.RemainingTicks = 1;
        state.ActiveEffects[target.Id][effect.Id] = effect;
        var session = CreateSession(
            state,
            new FakeEffectFactory((src, tgt) => throw new NotImplementedException()),
            template);
        var context = new GameMutationContext(session, new DeterministicRng(), new UndoRecord());

        var derivedStats = new FakeDerivedStatsCalculator(new UnitDerivedStats(3, 100, 100, 10, 10, 2, 100, 100, 100));
        var manager = new EffectManager(
            derivedStats,
            new FakeDamageCalculator(0),
            new FakeHealCalculator(0));

        // Act: tick active effects for the full state
        manager.TickAll(context, state);

        // Assert
        Assert.False(state.ActiveEffects.ContainsKey(target.Id));
        Assert.Contains(target.Id, derivedStats.ComputeCalls);
    }

    [Fact]
    public void TickAll_Applies_Heal_And_Damage_Over_Time_Before_Decrementing_Ticks()
    {
        // Arrange: DoT + HoT are resolved on apply and then executed on each tick
        var source = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0));
        var target = EngineTestFactory.CreateUnit(2, 2, new HexCoord(1, 0), hp: 10);
        var state = EngineTestFactory.CreateState(new[] { source, target }, teamToAct: 1);

        var templateId = new EffectTemplateId("dot-hot");
        var effectTemplate = new TestEffectTemplate(templateId, totalTicks: 2, maxStacks: 1);
        var hotTemplate = new HealOverTimeComponentTemplate(new EffectComponentTemplateId("hot"), heal: 1);
        var dotTemplate = new DamageOverTimeComponentTemplate(new EffectComponentTemplateId("dot"), damage: 1, DamageType.Physical);
        var hot = new HealOverTimeComponentInstance(new EffectComponentInstanceId(500), hotTemplate);
        var dot = new DamageOverTimeComponentInstance(new EffectComponentInstanceId(501), dotTemplate);

        var effectId = new EffectInstanceId(502);
        var factory = new FakeEffectFactory((src, tgt) =>
            new EffectInstance(effectId, effectTemplate, src, tgt, new EffectComponentInstance[] { hot, dot }));
        var session = CreateSession(state, factory, effectTemplate);
        var context = new GameMutationContext(session, new DeterministicRng(), new UndoRecord());

        var manager = new EffectManager(
            new FakeDerivedStatsCalculator(new UnitDerivedStats(3, 100, 100, 10, 10, 2, 100, 100, 100)),
            new FakeDamageCalculator(5),
            new FakeHealCalculator(2));

        var request = new EffectApplicationRequest(templateId, source.Id, new[] { target.Id });

        // Act / Assert: apply resolves values but does not tick HoT/DoT yet
        manager.ApplyOrStackEffect(context, state, request);
        Assert.Equal(10, target.Resources.HP);

        // First tick applies +2 heal and -5 damage, then decrements to 1 remaining tick
        manager.TickAll(context, state);
        Assert.True(target.Resources.HP == 7);
        Assert.Equal(1, state.ActiveEffects[target.Id][effectId].RemainingTicks);

        // Second tick applies again and expires/removes effect
        manager.TickAll(context, state);
        Assert.Equal(4, target.Resources.HP);
        Assert.False(state.ActiveEffects.ContainsKey(target.Id));
    }

    [Fact]
    public void ApplyOrStackEffect_Throws_When_Resolvable_Component_HpType_Does_Not_Match_Template_Interface()
    {
        // Arrange: build a malformed component where resolved HP type disagrees with the template contract
        var source = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0));
        var target = EngineTestFactory.CreateUnit(2, 2, new HexCoord(1, 0));
        var state = EngineTestFactory.CreateState(new[] { source, target }, teamToAct: 1);

        var templateId = new EffectTemplateId("bad");
        var effectTemplate = new TestEffectTemplate(templateId, totalTicks: 2, maxStacks: 1);
        var damageTemplate = new InstantDamageComponentTemplate(new EffectComponentTemplateId("dmg"), 1, DamageType.Physical, 0, 1.5f);
        var mismatched = new MismatchedResolvableComponent(new EffectComponentInstanceId(400), damageTemplate, HpType.Heal);
        var factory = new FakeEffectFactory((src, tgt) =>
            new EffectInstance(new EffectInstanceId(401), effectTemplate, src, tgt, new EffectComponentInstance[] { mismatched }));
        var session = CreateSession(state, factory, effectTemplate);
        var context = new GameMutationContext(session, new DeterministicRng(), new UndoRecord());
        var manager = new EffectManager(
            new FakeDerivedStatsCalculator(new UnitDerivedStats(3, 100, 100, 10, 10, 2, 100, 100, 100)),
            new FakeDamageCalculator(1),
            new FakeHealCalculator(1));
        var request = new EffectApplicationRequest(templateId, source.Id, new[] { target.Id });

        // Act / Assert: applying should fail fast on invalid component wiring
        Assert.Throws<InvalidOperationException>(() => manager.ApplyOrStackEffect(context, state, request));
    }

    [Fact]
    public void ApplyOrStackEffect_Creating_Multiple_New_Effects_Assigns_Distinct_Effect_Ids()
    {
        var source = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0));
        var firstTarget = EngineTestFactory.CreateUnit(2, 2, new HexCoord(1, 0));
        var secondTarget = EngineTestFactory.CreateUnit(3, 2, new HexCoord(2, 0));
        var state = EngineTestFactory.CreateState(new[] { source, firstTarget, secondTarget }, teamToAct: 1);

        var templateId = new EffectTemplateId("unique-effect");
        var template = new TestEffectTemplate(templateId, totalTicks: 2, maxStacks: 1);
        var factory = new EffectInstanceFactory(
            effectIds: new EffectInstanceIdFactory(),
            componentFactory: new StubComponentFactory(),
            templates: CreateTemplates(template));
        var session = CreateSession(state, factory, template);
        var context = new GameMutationContext(session, new DeterministicRng(), new UndoRecord());

        var manager = new EffectManager(
            new FakeDerivedStatsCalculator(new UnitDerivedStats(3, 100, 100, 10, 10, 2, 100, 100, 100)),
            new FakeDamageCalculator(0),
            new FakeHealCalculator(0));

        var request = new EffectApplicationRequest(templateId, source.Id, new[] { firstTarget.Id, secondTarget.Id });

        manager.ApplyOrStackEffect(context, state, request);

        var firstEffectId = Assert.Single(state.ActiveEffects[firstTarget.Id]).Key;
        var secondEffectId = Assert.Single(state.ActiveEffects[secondTarget.Id]).Key;

        Assert.NotEqual(firstEffectId, secondEffectId);
    }

    private static GameSession CreateSession(GameState state, IEffectInstanceFactory effectFactory, params EffectTemplate[] effectTemplates)
    {
        var registry = CreateTemplates(effectTemplates);
        var context = new GameContext(
            content: registry,
            teams: new TeamPair(new TeamId(1), new TeamId(2)),
            sessionServices: new GameSessionServices(
                units: new UnitInstanceFactory(new UnitInstanceIdFactory(), registry.Units),
                effects: effectFactory));

        return new GameSession(
            context,
            new GameRuntime(state, new UndoHistory(), GameOutcome.Ongoing(), new InstanceAllocationState()));
    }

    private static TemplateRegistry CreateTemplates(params EffectTemplate[] effectTemplates)
    {
        return new TemplateRegistry(
            units: new UnitTemplateRepository(new Dictionary<UnitTemplateId, UnitTemplate>()),
            abilities: new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()),
            effects: new EffectTemplateRepository(effectTemplates.Select(template => new KeyValuePair<EffectTemplateId, EffectTemplate>(template.Id, template))),
            effectComponents: new EffectComponentTemplateRepository(new Dictionary<EffectComponentTemplateId, EffectComponentTemplate>()));
    }

    private sealed class FakeEffectFactory : IEffectInstanceFactory
    {
        private readonly Func<UnitInstanceId, UnitInstanceId, EffectInstance> _create;
        public int CreateCallCount { get; private set; }

        public FakeEffectFactory(Func<UnitInstanceId, UnitInstanceId, EffectInstance> create)
        {
            _create = create;
        }

        public EffectInstance Create(CreateEffectRequest effectRequest, InstanceAllocationState instanceAllocation)
        {
            CreateCallCount++;
            var effect = _create(effectRequest.SourceUnitId, effectRequest.TargetUnitId);
            return effect;
        }
    }

    private sealed class FakeDerivedStatsCalculator : IDerivedStatsCalculator
    {
        private readonly UnitDerivedStats _value;
        public List<UnitInstanceId> ComputeCalls { get; } = new();

        public FakeDerivedStatsCalculator(UnitDerivedStats value)
        {
            _value = value;
        }

        public UnitDerivedStats Compute(IReadOnlyGameState state, UnitInstanceId unitId)
        {
            ComputeCalls.Add(unitId);
            return _value;
        }
    }

    private sealed class FakeDamageCalculator : IDamageCalculator
    {
        private readonly int _value;

        public FakeDamageCalculator(int value)
        {
            _value = value;
        }

        public int Compute(GameMutationContext context, IReadOnlyGameState state, IReadOnlyEffectInstance effect, IDamageComponent componentTemplate) => _value;
    }

    private sealed class FakeHealCalculator : IHealCalculator
    {
        private readonly int _value;

        public FakeHealCalculator(int value)
        {
            _value = value;
        }

        public int Compute(GameMutationContext context, IReadOnlyGameState state, IReadOnlyEffectInstance effect, IHealComponent componentTemplate) => _value;
    }

    private sealed class StubComponentFactory : IEffectComponentInstanceFactory
    {
        public EffectComponentInstance Create(EffectComponentTemplate componentTemplate, InstanceAllocationState instanceAllocation)
            => throw new InvalidOperationException("This test uses effect templates without component templates.");
    }

    private sealed class TestEffectTemplate : EffectTemplate
    {
        public TestEffectTemplate(EffectTemplateId id, int totalTicks, int maxStacks)
            : base(id, name: "test", isHarmful: false, totalTicks: totalTicks, maxStacks: maxStacks, components: Array.Empty<EffectComponentTemplateId>())
        {
        }
    }

    private sealed class MismatchedResolvableComponent : EffectComponentInstance, IReadOnlyResolvableHpDeltaComponent
    {
        private int? _resolved;
        private readonly HpType _hpType;

        int? IResolvableHpDeltaComponent.ResolvedHpDelta
        {
            get => _resolved;
            set => _resolved = value;
        }

        HpType IReadOnlyResolvableHpDeltaComponent.HpType => _hpType;

        public MismatchedResolvableComponent(EffectComponentInstanceId id, EffectComponentTemplate template, HpType hpType)
            : base(id, template)
        {
            _hpType = hpType;
        }

        public override EffectComponentInstance DeepCloneForSimulation()
        {
            throw new NotImplementedException();
        }
    }
}
