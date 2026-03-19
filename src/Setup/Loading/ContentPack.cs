namespace Setup.Loading;

using Setup.Config;
using Setup.Validation.Primitives;

public sealed class ContentPack
{
    public List<UnitTemplateConfig> UnitTemplates { get; set; } = new();
    public List<AbilityConfig> Abilities { get; set; } = new();
    public List<EffectTemplateConfig> EffectTemplates { get; set; } = new();
    public List<EffectComponentTemplateConfig> EffectComponentTemplates { get; set; } = new();
    public List<GameStateConfig> GameStates { get; set; } = new();
    public List<ContentIssue> Issues { get; set; } = new();

    public bool HasErrors =>
        Issues.Any(i => i.Severity == ContentIssueSeverity.Error);

}
