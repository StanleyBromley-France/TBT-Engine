namespace Setup.Build.Scenarios;

using Setup.Build.TemplateRegistry;
using Setup.Build.TemplateRegistry.Builders.EffectComponents;
using Setup.Build.TemplateRegistry.Builders.EffectComponents.Builders;
using Setup.Loading.Scenarios;
using Setup.Validation.Primitives;

public sealed class ScenarioSourceBuilder : IScenarioSourceBuilder
{
    public IScenarioSource Build(
        LoadedScenarioContent content,
        ContentValidationMode validationMode)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (content.HasErrors && validationMode == ContentValidationMode.Strict)
            return new ScenarioSource(content.ContentPack, null, content.IssueView);

        var templateBuilder = new TemplateRegistryBuilder(CreateEffectComponentBuilderResolver());
        var templateResult = templateBuilder.Build(content.ContentPack.Templates, validationMode);

        var issues = new ValidationCollector();
        CopyIssues(content.IssueView, issues);
        CopyIssues(templateResult.IssueView, issues);

        return new ScenarioSource(content.ContentPack, templateResult.TemplateRegistry, issues);
    }

    private static EffectComponentBuilderResolver CreateEffectComponentBuilderResolver()
    {
        return new EffectComponentBuilderResolver(
            new Dictionary<string, IEffectComponentBuilder>(StringComparer.Ordinal)
            {
                ["DamageOverTime"] = new DamageOverTimeComponentBuilder(),
                ["FlatAttributeModifier"] = new FlatAttributeModifierComponentBuilder(),
                ["HealOverTime"] = new HealOverTimeComponentBuilder(),
                ["InstantDamage"] = new InstantDamageComponentBuilder(),
                ["InstantHeal"] = new InstantHealComponentBuilder(),
                ["PercentAttributeModifier"] = new PercentAttributeModifierComponentBuilder(),
            });
    }

    private static void CopyIssues(IContentIssueView source, ValidationCollector destination)
    {
        foreach (var issue in source.Issues)
            destination.Add(issue);
    }
}
