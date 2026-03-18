namespace Setup.Config;

public sealed class EffectTemplateConfig
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsHarmful { get; set; }
    public int TotalTicks { get; set; }
    public int MaxStacks { get; set; }
    public List<string> ComponentTemplateIds { get; set; } = new();
}
