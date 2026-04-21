using Core.Domain.Abilities;
using Core.Domain.Effects.Components.Instances.Mutable;
using Core.Domain.Effects.Components.Templates;
using Core.Domain.Effects.Stats;
using Core.Domain.Effects.Templates;
using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Domain.Units.Instances.Mutable;
using Core.Engine.Effects;
using Core.Engine.Mutation;
using Core.Engine.Random;
using Core.Engine.Telemetry;
using Core.Undo;
using Core.Tests.Engine.TestSupport;
using Core.Game.State;

namespace Core.Tests.Domain.Effects;

public class EffectComponentCoverageTests
{
    [Fact]
    public void InstantHealComponent_OnApply_Heals_Target()
    {
        var (unit, context) = CreateSingleUnitContext(hp: 5);
        var component = new InstantHealComponentInstance(
            new EffectComponentInstanceId(1000),
            new InstantHealComponentTemplate(new EffectComponentTemplateId("instant-heal"), heal: 1));

        ((IResolvableHpDeltaComponent)component).ResolvedHpDelta = 4;
        var effect = CreateEffect(unit.Id, component);

        component.OnApply(context, effect);

        Assert.Equal(9, unit.Resources.HP);
    }

    [Fact]
    public void InstantHealComponent_OnApply_Records_Healing_Telemetry()
    {
        var (unit, context, telemetry) = CreateSingleUnitContextWithTelemetry(hp: 5);
        var component = new InstantHealComponentInstance(
            new EffectComponentInstanceId(1008),
            new InstantHealComponentTemplate(new EffectComponentTemplateId("instant-heal-telemetry"), heal: 1));

        ((IResolvableHpDeltaComponent)component).ResolvedHpDelta = 4;
        var effect = CreateEffect(sourceId: unit.Id, targetId: unit.Id, component);

        component.OnApply(context, effect);

        var healingEvent = Assert.Single(telemetry.HealingEvents);
        Assert.Equal(unit.Id, healingEvent.SourceUnitId);
        Assert.Equal(unit.Id, healingEvent.TargetUnitId);
        Assert.Equal(4, healingEvent.Amount);
    }

    [Fact]
    public void InstantDamageComponent_OnApply_Damages_Target()
    {
        var (unit, context) = CreateSingleUnitContext(hp: 9);
        var component = new InstantDamageComponentInstance(
            new EffectComponentInstanceId(1001),
            new InstantDamageComponentTemplate(new EffectComponentTemplateId("instant-dmg"), 1, DamageType.Physical, 0, 1f));

        ((IResolvableHpDeltaComponent)component).ResolvedHpDelta = 3;
        var effect = CreateEffect(unit.Id, component);

        component.OnApply(context, effect);

        Assert.Equal(6, unit.Resources.HP);
    }

    [Fact]
    public void InstantDamageComponent_OnApply_Records_Damage_Telemetry()
    {
        var (unit, context, telemetry) = CreateSingleUnitContextWithTelemetry(hp: 9);
        var component = new InstantDamageComponentInstance(
            new EffectComponentInstanceId(1009),
            new InstantDamageComponentTemplate(new EffectComponentTemplateId("instant-dmg-telemetry"), 1, DamageType.Physical, 0, 1f));

        ((IResolvableHpDeltaComponent)component).ResolvedHpDelta = 3;
        var effect = CreateEffect(sourceId: unit.Id, targetId: unit.Id, component);

        component.OnApply(context, effect);

        var damageEvent = Assert.Single(telemetry.DamageEvents);
        Assert.Equal(unit.Id, damageEvent.SourceUnitId);
        Assert.Equal(unit.Id, damageEvent.TargetUnitId);
        Assert.Equal(3, damageEvent.Amount);
    }

    [Fact]
    public void HealOverTimeComponent_OnTick_Heals_By_Resolved_Value_Times_Stacks()
    {
        var (unit, context) = CreateSingleUnitContext(hp: 5);
        var component = new HealOverTimeComponentInstance(
            new EffectComponentInstanceId(1002),
            new HealOverTimeComponentTemplate(new EffectComponentTemplateId("hot"), heal: 1));

        ((IResolvableHpDeltaComponent)component).ResolvedHpDelta = 2;
        var effect = CreateEffect(unit.Id, component);
        effect.CurrentStacks = 3;

        component.OnTick(context, effect);

        Assert.Equal(11, unit.Resources.HP);
    }

    [Fact]
    public void DamageOverTimeComponent_OnTick_Damages_By_Resolved_Value_Times_Stacks()
    {
        var (unit, context) = CreateSingleUnitContext(hp: 12);
        var component = new DamageOverTimeComponentInstance(
            new EffectComponentInstanceId(1003),
            new DamageOverTimeComponentTemplate(new EffectComponentTemplateId("dot"), damage: 1, DamageType.Physical));

        ((IResolvableHpDeltaComponent)component).ResolvedHpDelta = 2;
        var effect = CreateEffect(unit.Id, component);
        effect.CurrentStacks = 4;

        component.OnTick(context, effect);

        Assert.Equal(4, unit.Resources.HP);
    }

    [Fact]
    public void InstantHealComponent_OnApply_Throws_When_Unresolved()
    {
        var (unit, context) = CreateSingleUnitContext();
        var component = new InstantHealComponentInstance(
            new EffectComponentInstanceId(1004),
            new InstantHealComponentTemplate(new EffectComponentTemplateId("instant-heal"), heal: 1));
        var effect = CreateEffect(unit.Id, component);

        Assert.Throws<InvalidOperationException>(() => component.OnApply(context, effect));
    }

    [Fact]
    public void InstantDamageComponent_OnApply_Throws_When_Unresolved()
    {
        var (unit, context) = CreateSingleUnitContext();
        var component = new InstantDamageComponentInstance(
            new EffectComponentInstanceId(1005),
            new InstantDamageComponentTemplate(new EffectComponentTemplateId("instant-dmg"), damage: 1, DamageType.Physical, 0, 1.5f));
        var effect = CreateEffect(unit.Id, component);

        Assert.Throws<InvalidOperationException>(() => component.OnApply(context, effect));
    }

    [Fact]
    public void HealOverTimeComponent_OnTick_Throws_When_Unresolved()
    {
        var (unit, context) = CreateSingleUnitContext();
        var component = new HealOverTimeComponentInstance(
            new EffectComponentInstanceId(1006),
            new HealOverTimeComponentTemplate(new EffectComponentTemplateId("hot"), heal: 1));
        var effect = CreateEffect(unit.Id, component);

        Assert.Throws<InvalidOperationException>(() => component.OnTick(context, effect));
    }

    [Fact]
    public void DamageOverTimeComponent_OnTick_Throws_When_Unresolved()
    {
        var (unit, context) = CreateSingleUnitContext();
        var component = new DamageOverTimeComponentInstance(
            new EffectComponentInstanceId(1007),
            new DamageOverTimeComponentTemplate(new EffectComponentTemplateId("dot"), damage: 1, DamageType.Physical));
        var effect = CreateEffect(unit.Id, component);

        Assert.Throws<InvalidOperationException>(() => component.OnTick(context, effect));
    }

    [Theory]
    [MemberData(nameof(AllStatTypes))]
    public void FlatModifierComponent_Applies_For_All_StatTypes(StatType stat)
    {
        var (unit, state) = CreateSingleUnitState();
        var component = new FlatAttributeModifierComponentInstance(
            new EffectComponentInstanceId(1010 + (int)stat),
            new FlatAttributeModifierComponentTemplate(new EffectComponentTemplateId($"flat-{stat}"), stat, amount: 3));

        var effect = CreateEffect(unit.Id, component);
        effect.CurrentStacks = 2;
        state.ActiveEffects[unit.Id][effect.Id] = effect;

        var derived = new DerivedStatsCalculator().Compute(state, unit.Id);
        var expected = GetBaseStat(unit.Template.BaseStats, stat) + 6;

        Assert.Equal(expected, GetDerivedStat(derived, stat));
    }

    [Theory]
    [MemberData(nameof(AllStatTypes))]
    public void PercentModifierComponent_Applies_For_All_StatTypes(StatType stat)
    {
        var (unit, state) = CreateSingleUnitState();
        var component = new PercentAttributeModifierComponentInstance(
            new EffectComponentInstanceId(1030 + (int)stat),
            new PercentAttributeModifierComponentTemplate(new EffectComponentTemplateId($"pct-{stat}"), stat, percent: 25));

        var effect = CreateEffect(unit.Id, component);
        effect.CurrentStacks = 2; // +50%
        state.ActiveEffects[unit.Id][effect.Id] = effect;

        var derived = new DerivedStatsCalculator().Compute(state, unit.Id);
        var baseValue = GetBaseStat(unit.Template.BaseStats, stat);
        var expected = baseValue + (int)MathF.Round(baseValue * 0.5f);

        Assert.Equal(expected, GetDerivedStat(derived, stat));
    }

    public static IEnumerable<object[]> AllStatTypes()
        => Enum.GetValues<StatType>().Select(s => new object[] { s });

    private static (UnitInstance unit, GameMutationContext context) CreateSingleUnitContext(int hp = 10)
    {
        var (unit, state) = CreateSingleUnitState(hp);
        var session = EngineTestFactory.CreateSession(
            state,
            new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()));
        var context = new GameMutationContext(session, new DeterministicRng(), new UndoRecord());
        return (unit, context);
    }

    private static (UnitInstance unit, GameMutationContext context, RecordingCombatTelemetrySink telemetry) CreateSingleUnitContextWithTelemetry(int hp = 10)
    {
        var (unit, state) = CreateSingleUnitState(hp);
        var session = EngineTestFactory.CreateSession(
            state,
            new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()));
        var telemetry = new RecordingCombatTelemetrySink();
        var context = new GameMutationContext(session, new DeterministicRng(), new UndoRecord(), telemetry);
        return (unit, context, telemetry);
    }

    private static (UnitInstance unit, GameState state) CreateSingleUnitState(int hp = 10)
    {
        var unit = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0), hp: hp);
        var state = EngineTestFactory.CreateState(new[] { unit }, teamToAct: 1);
        return (unit, state);
    }

    private static Core.Domain.Effects.Instances.Mutable.EffectInstance CreateEffect(
        UnitInstanceId sourceId,
        UnitInstanceId targetId,
        params EffectComponentInstance[] components)
    {
        return new Core.Domain.Effects.Instances.Mutable.EffectInstance(
            new EffectInstanceId(2000 + components.Length),
            new TestEffectTemplate(new EffectTemplateId("component-test")),
            sourceUnitId: sourceId,
            targetUnitId: targetId,
            components);
    }

    private static Core.Domain.Effects.Instances.Mutable.EffectInstance CreateEffect(UnitInstanceId targetId, params EffectComponentInstance[] components)
        => CreateEffect(targetId, targetId, components);

    private static int GetBaseStat(Core.Domain.Units.UnitBaseStats baseStats, StatType stat)
    {
        return stat switch
        {
            StatType.MaxHP => baseStats.MaxHP,
            StatType.MaxManaPoints => baseStats.MaxManaPoints,
            StatType.MovePoints => baseStats.MovePoints,
            StatType.ActionPoints => baseStats.ActionPoints,
            StatType.DamageDealt => baseStats.DamageDealt,
            StatType.HealingDealt => baseStats.HealingDealt,
            StatType.HealingReceived => baseStats.HealingReceived,
            StatType.PhysicalDamageReceived => baseStats.PhysicalDamageReceived,
            StatType.MagicDamageReceived => baseStats.MagicDamageReceived,
            _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null)
        };
    }

    private static int GetDerivedStat(UnitDerivedStats derived, StatType stat)
    {
        return stat switch
        {
            StatType.MaxHP => derived.MaxHP,
            StatType.MaxManaPoints => derived.MaxManaPoints,
            StatType.MovePoints => derived.MaxMovePoints,
            StatType.ActionPoints => derived.MaxActionPoints,
            StatType.DamageDealt => derived.DamageDealt,
            StatType.HealingDealt => derived.HealingDealt,
            StatType.HealingReceived => derived.HealingReceived,
            StatType.PhysicalDamageReceived => derived.PhysicalDamageReceived,
            StatType.MagicDamageReceived => derived.MagicDamageReceived,
            _ => throw new ArgumentOutOfRangeException(nameof(stat), stat, null)
        };
    }

    private sealed class TestEffectTemplate : EffectTemplate
    {
        public TestEffectTemplate(EffectTemplateId id)
            : base(id, "component-test", isHarmful: false, totalTicks: 3, maxStacks: 5, components: Array.Empty<EffectComponentTemplateId>())
        {
        }
    }

    private sealed class RecordingCombatTelemetrySink : ICombatTelemetrySink
    {
        public List<(UnitInstanceId SourceUnitId, UnitInstanceId TargetUnitId, int Amount)> DamageEvents { get; } = new();

        public List<(UnitInstanceId SourceUnitId, UnitInstanceId TargetUnitId, int Amount)> HealingEvents { get; } = new();

        public void RecordDamage(UnitInstanceId sourceUnitId, UnitInstanceId targetUnitId, int amount)
            => DamageEvents.Add((sourceUnitId, targetUnitId, amount));

        public void RecordHealing(UnitInstanceId sourceUnitId, UnitInstanceId targetUnitId, int amount)
            => HealingEvents.Add((sourceUnitId, targetUnitId, amount));
    }
}
