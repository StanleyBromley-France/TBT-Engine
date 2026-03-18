namespace Setup.Config;

public sealed class AbilityConfig
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int ManaCost { get; set; }
    public TargetingRulesConfig Targeting { get; set; } = new();
    public string EffectTemplateId { get; set; } = string.Empty;
}
