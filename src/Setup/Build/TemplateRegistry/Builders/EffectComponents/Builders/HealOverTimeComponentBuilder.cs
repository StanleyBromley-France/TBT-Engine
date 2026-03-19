namespace Setup.Build.TemplateRegistry.Builders.EffectComponents.Builders;

using Core.Domain.Effects.Components.Templates;
using Core.Domain.Types;
using Setup.Build.TemplateRegistry.Builders.EffectComponents.Builders.Parsing;
using Setup.Config;
using Setup.Validation.Primitives;

internal sealed class HealOverTimeComponentBuilder : IEffectComponentBuilder
{
    public bool TryBuild(
        EffectComponentTemplateConfig config,
        string path,
        EffectComponentTemplateId id,
        ValidationCollector issues,
        out EffectComponentTemplate built)
    {
        if (!EffectComponentContentParser.TryRequireInt(config.Heal, $"{path}.Heal", nameof(config.Heal), issues, out var heal))
        {
            built = null!;
            return false;
        }

        built = new HealOverTimeComponentTemplate(id, heal);
        return true;
    }
}
