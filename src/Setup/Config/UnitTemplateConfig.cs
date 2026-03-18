namespace Setup.Config;

public sealed class UnitTemplateConfig
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int MaxHP { get; set; }
    public int MaxManaPoints { get; set; }
    public int MovePoints { get; set; }
    public int PhysicalDamageReceived { get; set; } = 100;
    public int MagicDamageReceived { get; set; } = 100;
    public List<string> AbilityIds { get; set; } = new();
}
