namespace Setup.Build.TemplateRegistry.Builders;

using Core.Domain.Effects.Components.Templates;
using Core.Domain.Effects.Templates;
using Core.Domain.Types;
using Setup.Config;
using Setup.Validation.Primitives;

internal static class EffectRepoBuilder
{
    public static Dictionary<EffectTemplateId, EffectTemplate> Build(
        IReadOnlyList<EffectTemplateConfig> configs,
        IReadOnlyDictionary<EffectComponentTemplateId, EffectComponentTemplate> builtComponents,
        ValidationCollector issues)
    {
        var result = new Dictionary<EffectTemplateId, EffectTemplate>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < configs.Count; i++)
        {
            var path = $"EffectTemplates[{i}]";
            var config = configs[i];

            if (!seenIds.Add(config.Id))
            {
                issues.Add(ContentIssueFactory.DuplicateId($"{path}.Id", config.Id));
                continue;
            }

            var componentIds = new List<EffectComponentTemplateId>();
            for (var j = 0; j < config.ComponentTemplateIds.Count; j++)
            {
                var rawId = config.ComponentTemplateIds[j];

                var componentId = new EffectComponentTemplateId(rawId);
                if (!builtComponents.ContainsKey(componentId))
                {
                    issues.Add(ContentIssueFactory.UnknownReference(
                        $"{path}.ComponentTemplateIds[{j}]",
                        "effect component template",
                        rawId));
                    continue;
                }

                componentIds.Add(componentId);
            }

            var id = new EffectTemplateId(config.Id);
            result[id] = new EffectTemplate(
                id,
                config.Name,
                config.IsHarmful,
                config.TotalTicks,
                config.MaxStacks,
                componentIds);
        }

        return result;
    }
}
