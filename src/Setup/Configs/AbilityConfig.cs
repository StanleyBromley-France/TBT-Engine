namespace Setup.Configs;

public sealed class AbilityConfig
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";

    public string Category { get; set; } = "";

    public AbilityCostConfig Cost { get; set; } = new();
    public TargetingRulesConfig Targeting { get; set; } = new();

    public List<string> EffectTemplateIds { get; set; } = new();
}

public sealed class AbilityCostConfig
{
    public int Mana { get; set; }
}

public sealed class TargetingRulesConfig
{
    public int Range { get; set; }
    public bool RequiresLineOfSight { get; set; }
    public List<string> AllowedTargets { get; set; } = new();
    public AreaPatternConfig AreaPattern { get; set; } = new();
    public bool IncludeSelf { get; set; }
}

public sealed class AreaPatternConfig
{
    public string Shape { get; set; } = "";
    public int Radius { get; set; }
    public int Length { get; set; }
    public int Width { get; set; }
}
