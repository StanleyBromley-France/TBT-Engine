using System;
namespace Core.Engine.Effects.Factories;

using Domain.Effects.Components.Instances.Mutable;
using Domain.Repositories;
using Engine.Effects.Components.Factories;
using Domain.Effects.Instances.Mutable;
using Domain.Types;
using Core.Engine.Mutation;
using Core.Domain.Effects.Instances.ReadOnly;

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

    public IReadOnlyEffectInstance Create(
        GameMutationContext context,
        EffectTemplateId templateId,
        UnitInstanceId sourceUnitId,
        UnitInstanceId targetUnitId)
    {
        var template = _templates.Effects.Get(templateId);
        var effectId = _effectIds.Create();

        var components = new List<EffectComponentInstance>();

        foreach (var componentTemplateId in template.ComponentTemplateIds)
        {
            var componentTemplate = _templates.EffectComponents.Get(componentTemplateId);
            var component = _componentFactory.Create(componentTemplate);
            components.Add(component);
        }

        var effect = new EffectInstance(
            effectId,
            template,
            sourceUnitId,
            targetUnitId,
            [.. components]
            );

        context.Effects.AddEffect(targetUnitId, effect);

        return effect;
    }
}
