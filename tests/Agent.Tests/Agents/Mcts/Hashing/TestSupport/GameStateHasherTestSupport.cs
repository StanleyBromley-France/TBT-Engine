using Agents.Mcts.Hashing;
using Agent.Tests.Engine.TestSupport;
using Core.Domain.Effects.Components.Instances.Mutable;
using Core.Domain.Effects.Components.Templates;
using Core.Domain.Effects.Instances.Mutable;
using Core.Domain.Effects.Templates;
using Core.Domain.Effects.Stats;
using Core.Domain.Types;
using Core.Game.State;

namespace Agent.Tests.Agents.Mcts.Hashing.TestSupport;

internal static class GameStateHasherTestSupport
{
    public static IGameStateHasher CreateHasher() => new GameStateHasher();

    public static GameState CreateBaselineState()
    {
        var attacker = EngineTestFactory.CreateUnit(1, team: 1, position: new HexCoord(0, 0));
        var defender = EngineTestFactory.CreateUnit(2, team: 2, position: new HexCoord(1, 0));

        return EngineTestFactory.CreateState(
            new[] { attacker, defender },
            teamToAct: 1,
            attackerTurnsTaken: 2);
    }

    public static GameState CreateStateWithTwoAllies()
    {
        var attackerA = EngineTestFactory.CreateUnit(1, team: 1, position: new HexCoord(0, 0));
        var attackerB = EngineTestFactory.CreateUnit(2, team: 1, position: new HexCoord(1, 0));
        var defender = EngineTestFactory.CreateUnit(3, team: 2, position: new HexCoord(2, 0));

        return EngineTestFactory.CreateState(
            new[] { attackerA, attackerB, defender },
            teamToAct: 1,
            attackerTurnsTaken: 2);
    }

    public static GameState CreateStateWithEffect(
        int effectId = 10,
        string effectTemplateId = "burn",
        string componentTemplateId = "burn-damage",
        int remainingTicks = 3,
        int currentStacks = 1,
        int resolvedHpDelta = 6)
    {
        var state = CreateBaselineState();

        var effect = CreateInstantDamageEffect(
            effectId: effectId,
            effectTemplateId: effectTemplateId,
            componentTemplateId: componentTemplateId,
            sourceUnitId: new UnitInstanceId(1),
            targetUnitId: new UnitInstanceId(2),
            remainingTicks: remainingTicks,
            currentStacks: currentStacks,
            resolvedHpDelta: resolvedHpDelta);

        state.ActiveEffects[new UnitInstanceId(2)].Add(effect.Id, effect);
        return state;
    }

    public static EffectInstance CreateInstantDamageEffect(
        int effectId,
        string effectTemplateId,
        string componentTemplateId,
        UnitInstanceId sourceUnitId,
        UnitInstanceId targetUnitId,
        int remainingTicks,
        int currentStacks,
        int resolvedHpDelta)
    {
        var componentTemplate = new InstantDamageComponentTemplate(
            new EffectComponentTemplateId(componentTemplateId),
            damage: 4,
            damageType: DamageType.Magical,
            critChance: 0,
            critMultiplier: 1f);

        var component = new InstantDamageComponentInstance(
            new EffectComponentInstanceId(effectId * 10),
            componentTemplate);

        ((IResolvableHpDeltaComponent)component).ResolvedHpDelta = resolvedHpDelta;

        var effect = new EffectInstance(
            new EffectInstanceId(effectId),
            new TestEffectTemplate(
                new EffectTemplateId(effectTemplateId),
                new EffectComponentTemplateId(componentTemplateId)),
            sourceUnitId,
            targetUnitId,
            new EffectComponentInstance[] { component });

        effect.RemainingTicks = remainingTicks;
        effect.CurrentStacks = currentStacks;
        return effect;
    }

    public static EffectInstance CreateFlatModifierEffect(
        int effectId,
        string effectTemplateId,
        string componentTemplateId,
        UnitInstanceId sourceUnitId,
        UnitInstanceId targetUnitId,
        int remainingTicks,
        int currentStacks)
    {
        var component = new FlatAttributeModifierComponentInstance(
            new EffectComponentInstanceId(effectId * 10),
            new FlatAttributeModifierComponentTemplate(
                new EffectComponentTemplateId(componentTemplateId),
                StatType.MaxHP,
                amount: 2));

        var effect = new EffectInstance(
            new EffectInstanceId(effectId),
            new TestEffectTemplate(
                new EffectTemplateId(effectTemplateId),
                new EffectComponentTemplateId(componentTemplateId)),
            sourceUnitId,
            targetUnitId,
            new EffectComponentInstance[] { component });

        effect.RemainingTicks = remainingTicks;
        effect.CurrentStacks = currentStacks;
        return effect;
    }

    private sealed class TestEffectTemplate : EffectTemplate
    {
        public TestEffectTemplate(
            EffectTemplateId id,
            params EffectComponentTemplateId[] componentIds)
            : base(
                id,
                name: id.Value,
                isHarmful: true,
                totalTicks: 3,
                maxStacks: 2,
                components: componentIds)
        {
        }
    }
}

