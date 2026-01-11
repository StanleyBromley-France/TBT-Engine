namespace Setup.Configs;

public sealed class EffectComponentTemplateConfig
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public int? Damage { get; set; }
    public int? DamagePerTick { get; set; }
    public string? DamageType { get; set; }
    public int? Heal { get; set; }
    public int? HealPerTick { get; set; }
    public string? AffectedStat { get; set; }
    public int? ModifierAmount { get; set; }
}