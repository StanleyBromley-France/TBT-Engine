namespace Setup.Configs;

public sealed class EffectTemplateConfig
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsHarmful { get; set; }
    public int TotalTicks { get; set; }
    public int MaxStacks { get; set; }
    public List<string> ComponentIds { get; set; } = new();
}
