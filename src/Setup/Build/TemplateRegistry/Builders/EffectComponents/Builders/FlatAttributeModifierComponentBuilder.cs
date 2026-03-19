namespace Setup.Build.TemplateRegistry.Builders.EffectComponents.Builders;

using Core.Domain.Effects.Components.Templates;
using Core.Domain.Types;
using Setup.Build.TemplateRegistry.Builders.EffectComponents.Builders.Parsing;
using Setup.Config;
using Setup.Validation.Primitives;

internal sealed class FlatAttributeModifierComponentBuilder : IEffectComponentBuilder
{
    public bool TryBuild(
        EffectComponentTemplateConfig config,
        string path,
        EffectComponentTemplateId id,
        ValidationCollector issues,
        out EffectComponentTemplate built)
    {
        if (!EffectComponentContentParser.TryParseStatType(config.Stat, $"{path}.Stat", issues, out var stat) ||
            !EffectComponentContentParser.TryRequireInt(config.Amount, $"{path}.Amount", nameof(config.Amount), issues, out var amount))
        {
            built = null!;
            return false;
        }

        built = new FlatAttributeModifierComponentTemplate(id, stat, amount);
        return true;
    }
}
