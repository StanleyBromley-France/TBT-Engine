namespace Core.Game.Factories.Effects;

using Domain.Effects.Components.Instances.Mutable;
using Domain.Repositories;
using Domain.Effects.Instances.Mutable;
using Domain.Types;
using Core.Game.Factories.EffectComponents;
using Core.Game.Session;
using Core.Game.Requests;

internal sealed class EffectInstanceFactory : IEffectInstanceFactory
{
    private readonly IEffectInstanceIdFactory _effectIds;
    private readonly IEffectComponentInstanceFactory _componentFactory;
    private readonly TemplateRegistry _templates;

    public EffectInstanceFactory(
        IEffectInstanceIdFactory effectIds,
        IEffectComponentInstanceFactory componentFactory,
        TemplateRegistry templates)
    {
        _effectIds = effectIds ?? throw new ArgumentNullException(nameof(effectIds));
        _componentFactory = componentFactory ?? throw new ArgumentNullException(nameof(componentFactory));
        _templates = templates ?? throw new ArgumentNullException(nameof(templates));
    }

    public EffectInstance Create(
        CreateEffectRequest effectRequest,
        InstanceAllocationState instanceAllocation)
    {
        var template = _templates.Effects.Get(effectRequest.TemplateId);
        var effectId = _effectIds.Create(instanceAllocation);

        var components = new List<EffectComponentInstance>();

        foreach (var componentTemplateId in template.ComponentTemplateIds)
        {
            var componentTemplate = _templates.EffectComponents.Get(componentTemplateId);
            var component = _componentFactory.Create(componentTemplate, instanceAllocation);
            components.Add(component);
        }

        var effect = new EffectInstance(
            effectId,
            template,
            effectRequest.SourceUnitId,
            effectRequest.TargetUnitId,
            [.. components]
            );

        return effect;
    }
}
