namespace Core.Domain.Effects.Components.Templates;

using Core.Domain.Types;

/// <summary>
/// Serves as the base definition for an effect component, providing
/// identifiers and lifecycle behavior hooks for effect instances
/// </summary>
public abstract class EffectComponentTemplate
{
    public EffectComponentTemplateId Id { get; }

    public EffectComponentTemplate(EffectComponentTemplateId id)
    {
        Id = id;
    }
}

