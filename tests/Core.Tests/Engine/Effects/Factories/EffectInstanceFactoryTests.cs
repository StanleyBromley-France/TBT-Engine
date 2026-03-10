using Core.Domain.Abilities;
using Core.Domain.Effects.Components.Instances.Mutable;
using Core.Domain.Effects.Components.Templates;
using Core.Domain.Effects.Templates;
using Core.Domain.Repositories;
using Core.Domain.Types;
using Core.Domain.Units.Instances.Mutable;
using Core.Domain.Units.Templates;
using Core.Engine.Effects.Components.Factories;
using Core.Engine.Effects.Factories;
using Core.Engine.Mutation;
using Core.Engine.Random;
using Core.Undo;
using Core.Tests.Engine.TestSupport;
using Core.Undo.Steps.Effects;

namespace Core.Tests.Engine.Effects.Factories;

public class EffectInstanceFactoryTests
{
    [Fact]
    public void Create_Adds_Effect_To_Target_And_Records_AddEffectUndo()
    {
        // Arrange: build a minimal match state and deterministic factory dependencies so the created effect ID is known
        var source = EngineTestFactory.CreateUnit(1, 1, new HexCoord(0, 0));
        var target = EngineTestFactory.CreateUnit(2, 2, new HexCoord(1, 0));
        var state = EngineTestFactory.CreateState(new[] { source, target }, teamToAct: 1, activeUnitId: source.Id);
        var session = EngineTestFactory.CreateSession(state, new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()));
        var undo = new UndoRecord();
        var context = new GameMutationContext(session, new DeterministicRng(), undo);

        var effectTemplateId = new EffectTemplateId("factory-test-effect");
        var componentTemplateId = new EffectComponentTemplateId("factory-test-component");

        var effectTemplate = new TestEffectTemplate(effectTemplateId, new[] { componentTemplateId });
        var componentTemplate = new InstantHealComponentTemplate(componentTemplateId, heal: 2);

        var templates = new TemplateRegistry(
            units: new UnitTemplateRepository(new Dictionary<UnitTemplateId, UnitTemplate>()),
            abilities: new AbilityRepository(Array.Empty<KeyValuePair<AbilityId, Ability>>()),
            effects: new EffectTemplateRepository(new[] { new KeyValuePair<EffectTemplateId, EffectTemplate>(effectTemplateId, effectTemplate) }),
            effectComponents: new EffectComponentTemplateRepository(new[] { new KeyValuePair<EffectComponentTemplateId, EffectComponentTemplate>(componentTemplateId, componentTemplate) }));

        var expectedEffectId = new EffectInstanceId(999);
        var factory = new EffectInstanceFactory(
            effectIds: new StubEffectIdFactory(expectedEffectId),
            componentFactory: new StubComponentFactory(componentTemplate, new EffectComponentInstanceId(111)),
            templates: templates);

        // Act: create an effect instance for target through the factory
        var created = factory.Create(context, effectTemplateId, source.Id, target.Id);

        // Assert
        Assert.Equal(expectedEffectId, created.Id);
        Assert.True(state.ActiveEffects[target.Id].ContainsKey(expectedEffectId));
        Assert.Single(undo.Steps);

        var undoStep = Assert.IsType<AddEffectUndo>(undo.Steps[0]);
        Assert.Equal(target.Id, undoStep.TargetUnitId);
        Assert.Equal(expectedEffectId, undoStep.EffectId);
    }

    private sealed class TestEffectTemplate : EffectTemplate
    {
        public TestEffectTemplate(EffectTemplateId id, IReadOnlyList<EffectComponentTemplateId> components)
            : base(id, name: "test-effect", isHarmful: false, totalTicks: 2, maxStacks: 1, components: components)
        {
        }
    }

    private sealed class StubEffectIdFactory : IEffectInstanceIdFactory
    {
        private readonly EffectInstanceId _id;

        public StubEffectIdFactory(EffectInstanceId id)
        {
            _id = id;
        }

        public EffectInstanceId Create() => _id;
    }

    private sealed class StubComponentFactory : IEffectComponentInstanceFactory
    {
        private readonly EffectComponentTemplate _expectedTemplate;
        private readonly EffectComponentInstance _component;

        public StubComponentFactory(EffectComponentTemplate expectedTemplate, EffectComponentInstanceId componentId)
        {
            _expectedTemplate = expectedTemplate;
            _component = new InstantHealComponentInstance(componentId, (InstantHealComponentTemplate)expectedTemplate);
        }

        public EffectComponentInstance Create(EffectComponentTemplate componentTemplate)
        {
            Assert.Same(_expectedTemplate, componentTemplate);
            return _component;
        }
    }
}
