using Core.Domain.Effects.Components.Instances.Mutable;
using Core.Domain.Effects.Components.Templates;
using Core.Domain.Types;
using Core.Game.Factories.EffectComponents;
using Core.Game.Factories.EffectComponents.Creators;
using Core.Game.Factories.EffectComponents.Registry;

namespace Core.Tests.Engine.Effects.Factories;

public class EffectComponentFactoriesTests
{
    [Fact]
    public void EffectComponentInstanceFactory_Create_Uses_Resolved_Creator_Result()
    {
        // Arrange: wire a registry that always resolves to our stub creator and predefine the instance it should return
        var template = new InstantHealComponentTemplate(new EffectComponentTemplateId("heal-template"), heal: 3);
        var expected = new InstantHealComponentInstance(new EffectComponentInstanceId(700), template);
        var creator = new StubCreator(template, expected);
        var registry = new StubRegistry(creator);
        var factory = new EffectComponentInstanceFactory(registry);

        // Act: ask the factory to create from template (it should resolve creator then delegate creation)
        var created = factory.Create(template);

        // Assert
        Assert.Same(expected, created);
        Assert.Equal(1, registry.ResolveCallCount);
        Assert.Equal(1, creator.CreateCallCount);
    }

    [Fact]
    public void ComponentInstanceCreatorRegistry_Resolve_Returns_First_Matching_Creator()
    {
        // Arrange: register two creators that can both handle the template to verify first-match selection order
        var template = new InstantHealComponentTemplate(new EffectComponentTemplateId("heal-template"), heal: 2);
        var createdByFirst = new InstantHealComponentInstance(new EffectComponentInstanceId(701), template);
        var createdBySecond = new InstantHealComponentInstance(new EffectComponentInstanceId(702), template);
        var first = new StubCreator(template, createdByFirst);
        var second = new StubCreator(template, createdBySecond);

        var registry = new ComponentInstanceCreatorRegistry(new IComponentInstanceCreator[] { first, second });

        // Act: resolve creator from registry, then create through that resolved creator
        var resolved = registry.Resolve(template);
        var created = resolved.Create(template);

        // Assert
        Assert.Same(first, resolved);
        Assert.Same(createdByFirst, created);
        Assert.Equal(1, first.CreateCallCount);
        Assert.Equal(0, second.CreateCallCount);
    }

    private sealed class StubRegistry : IComponentInstanceCreatorRegistry
    {
        private readonly IComponentInstanceCreator _creator;
        public int ResolveCallCount { get; private set; }

        public StubRegistry(IComponentInstanceCreator creator)
        {
            _creator = creator;
        }

        public IComponentInstanceCreator Resolve(EffectComponentTemplate template)
        {
            ResolveCallCount++;
            return _creator;
        }
    }

    private sealed class StubCreator : IComponentInstanceCreator
    {
        private readonly EffectComponentTemplate _supported;
        private readonly EffectComponentInstance _instance;
        public int CreateCallCount { get; private set; }

        public StubCreator(EffectComponentTemplate supported, EffectComponentInstance instance)
        {
            _supported = supported;
            _instance = instance;
        }

        public bool CanCreate(EffectComponentTemplate template)
            => ReferenceEquals(_supported, template);

        public EffectComponentInstance Create(EffectComponentTemplate template)
        {
            CreateCallCount++;
            return _instance;
        }
    }
}
