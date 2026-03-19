namespace Setup.Build.TemplateRegistry.Builders.EffectComponents.Builders;

using Core.Domain.Effects.Components.Templates;
using Core.Domain.Types;
using Setup.Config;
using Setup.Validation.Primitives;

public interface IEffectComponentBuilder
{
    bool TryBuild(
        EffectComponentTemplateConfig config,
        string path,
        EffectComponentTemplateId id,
        ValidationCollector issues,
        out EffectComponentTemplate built);
}
