using Core.Domain.Abilities;
using Core.Domain.Abilities.Targeting;
using Core.Domain.Effects.Components.Templates;
using Core.Domain.Effects.Templates;
using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Domain.Units.Templates;
using Core.Engine;
using Core.Engine.Actions.Choice;
using Core.Game.Match;
using Core.Game.Session;
using Core.Tests.Engine.TestSupport;
using Core.Undo;

namespace Core.Tests.Engine;

public class EngineSandboxIsolationTests
{
    [Fact]
    public void CreateSandbox_Applying_Effect_In_Sandbox_Does_Not_Advance_Live_Effect_Ids()
    {
        // Arrange: create a live engine and a sandbox engine from it.
        var abilityId = new AbilityId("strike");
        var effectId = new EffectTemplateId("strike-effect");
        var componentId = new EffectComponentTemplateId("strike-damage");
        var ability = new Ability(
            abilityId,
            "Strike",
            AbilityCategory.OffensiveSpell,
            cost: 0,
            targeting: new TargetingRules(range: 1, requiresLineOfSight: false, allowedTarget: TargetType.Enemy, radius: 0),
            effect: effectId);
        var effect = new TestEffectTemplate(effectId, componentId);
        var component = new InstantDamageComponentTemplate(componentId, damage: 1, DamageType.Physical, critChance: 0, critMultiplier: 1.5f);

        var caster = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0), abilityIds: abilityId);
        var target = EngineTestFactory.CreateUnit(2, 2, new HexCoord(1, 0), hp: 10);
        var state = EngineTestFactory.CreateState(new[] { caster, target }, teamToAct: 1);
        var session = CreateSession(state, caster.Template, target.Template, ability, effect, component);

        var live = EngineCompositionRoot.Create(session, turnCount: 12);
        var sandbox = live.CreateSandbox();
        var action = new UseAbilityAction(caster.Id, ability.Id, target.Id);

        // Act: create one effect in the sandbox, then create the first effect in the live engine.
        sandbox.ApplyAction(action);
        live.ApplyAction(action);

        // Assert: live should still allocate its first effect instance id independently.
        var liveEffect = Assert.Single(session.Runtime.State.ActiveEffects[target.Id]);
        Assert.Equal(new EffectInstanceId(1), liveEffect.Key);
    }

    private static GameSession CreateSession(
        Core.Game.State.GameState state,
        UnitTemplate casterTemplate,
        UnitTemplate targetTemplate,
        IEnumerable<Ability> abilities,
        IEnumerable<EffectTemplate> effects,
        IEnumerable<EffectComponentTemplate> components)
    {
        var registry = new TemplateRegistry(
            units: new UnitTemplateRepository(new[]
            {
                new KeyValuePair<UnitTemplateId, UnitTemplate>(casterTemplate.Id, casterTemplate),
                new KeyValuePair<UnitTemplateId, UnitTemplate>(targetTemplate.Id, targetTemplate),
            }),
            abilities: new AbilityRepository(abilities.Select(a => new KeyValuePair<AbilityId, Ability>(a.Id, a))),
            effects: new EffectTemplateRepository(effects.Select(e => new KeyValuePair<EffectTemplateId, EffectTemplate>(e.Id, e))),
            effectComponents: new EffectComponentTemplateRepository(components.Select(c => new KeyValuePair<EffectComponentTemplateId, EffectComponentTemplate>(c.Id, c))));

        var context = new GameContext(
            content: registry,
            teams: new TeamPair(new TeamId(1), new TeamId(2)),
            sessionServices: EngineTestFactory.CreateSessionServices(registry));

        var runtime = new GameRuntime(
            state: state,
            undo: new UndoHistory(),
            outcome: GameOutcome.Ongoing(),
            instanceAllocation: new InstanceAllocationState()
            );

        return new GameSession(context, runtime);
    }

    private static GameSession CreateSession(
        Core.Game.State.GameState state,
        UnitTemplate casterTemplate,
        UnitTemplate targetTemplate,
        Ability ability,
        EffectTemplate effect,
        EffectComponentTemplate component)
        => CreateSession(
            state,
            casterTemplate,
            targetTemplate,
            abilities: new[] { ability },
            effects: new[] { effect },
            components: new[] { component });

    private sealed class TestEffectTemplate : EffectTemplate
    {
        public TestEffectTemplate(EffectTemplateId id, EffectComponentTemplateId componentId)
            : base(
                id,
                name: "test-effect",
                isHarmful: true,
                totalTicks: 3,
                maxStacks: 1,
                components: new[] { componentId })
        {
        }
    }
}
