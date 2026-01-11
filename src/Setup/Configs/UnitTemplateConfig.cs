namespace Setup.Configs;

public sealed class UnitTemplateConfig
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int MaxHP { get; set; }
    public int MaxManaPoints { get; set; }
    public int MovePoints { get; set; }
    public int DefaultActionPoints { get; set; } = 2;
    public int ArmourPoints { get; set; }
    public List<string> AbilityIds { get; set; } = new();
}