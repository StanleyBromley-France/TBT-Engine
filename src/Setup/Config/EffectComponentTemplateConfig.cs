namespace Setup.Config;

public sealed class EffectComponentTemplateConfig
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? Damage { get; set; }
    public string? DamageType { get; set; }
    public int? Heal { get; set; }
    public string? Stat { get; set; }
    public int? Amount { get; set; }
    public int? Percent { get; set; }
    public int? CritChance { get; set; }
    public float? CritMultiplier { get; set; }
}
