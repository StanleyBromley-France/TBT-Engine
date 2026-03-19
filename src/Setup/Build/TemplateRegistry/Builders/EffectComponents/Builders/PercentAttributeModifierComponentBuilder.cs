namespace Setup.Build.TemplateRegistry.Builders.EffectComponents.Builders;

using Core.Domain.Effects.Components.Templates;
using Core.Domain.Types;
using Setup.Build.TemplateRegistry.Builders.EffectComponents.Builders.Parsing;
using Setup.Config;
using Setup.Validation.Primitives;

internal sealed class PercentAttributeModifierComponentBuilder : IEffectComponentBuilder
{
    public bool TryBuild(
        EffectComponentTemplateConfig config,
        string path,
        EffectComponentTemplateId id,
        ValidationCollector issues,
        out EffectComponentTemplate built)
    {
        if (!EffectComponentContentParser.TryParseStatType(config.Stat, $"{path}.Stat", issues, out var stat) ||
            !EffectComponentContentParser.TryRequireInt(config.Percent, $"{path}.Percent", nameof(config.Percent), issues, out var percent))
        {
            built = null!;
            return false;
        }

        built = new PercentAttributeModifierComponentTemplate(id, stat, percent);
        return true;
    }
}
