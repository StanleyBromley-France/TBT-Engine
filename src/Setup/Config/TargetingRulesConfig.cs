namespace Setup.Config;

public sealed class TargetingRulesConfig
{
    public int Range { get; set; }
    public bool RequiresLineOfSight { get; set; }
    public string AllowedTarget { get; set; } = string.Empty;
    public int Radius { get; set; }
}
