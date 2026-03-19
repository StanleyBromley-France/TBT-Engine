namespace Setup.Build.TemplateRegistry.Builders;

using Core.Domain.Effects.Components.Templates;
using Core.Domain.Types;
using Setup.Build.TemplateRegistry.Builders.EffectComponents;
using Setup.Config;
using Setup.Validation.Primitives;

internal static class EffectComponentRepoBuilder
{
    public static Dictionary<EffectComponentTemplateId, EffectComponentTemplate> Build(
        IReadOnlyList<EffectComponentTemplateConfig> configs,
        ValidationCollector issues,
        IEffectComponentBuilderResolver strategyRegistry)
    {
        var result = new Dictionary<EffectComponentTemplateId, EffectComponentTemplate>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < configs.Count; i++)
        {
            var path = $"EffectComponentTemplates[{i}]";
            var config = configs[i];

            if (!seenIds.Add(config.Id))
            {
                issues.Add(ContentIssueFactory.DuplicateId($"{path}.Id", config.Id));
                continue;
            }

            if (!strategyRegistry.TryResolve(config.Type, out var strategy))
            {
                issues.Add(ContentIssueFactory.UnsupportedComponentType($"{path}.Type", config.Type));
                continue;
            }

            var id = new EffectComponentTemplateId(config.Id);
            if (!strategy.TryBuild(config, path, id, issues, out var built))
                continue;

            result[new EffectComponentTemplateId(config.Id)] = built;
        }

        return result;
    }
}
