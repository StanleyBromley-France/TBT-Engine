namespace Core.Domain.Abilities.Targeting;

/// <summary>
/// Defines the parameters of an ability's area of effect,
/// including shape-specific dimensions such as radius, length, and width.
/// </summary>
public sealed class AreaPattern
{
    public AreaShape Shape { get; }
    public int Radius { get; }
    public int Length { get; }
    public int Width { get; }

    /// <summary>
    /// Creates an area pattern using the specified shape and optional size values
    /// relevant to that shape.
    /// </summary>
    public AreaPattern(AreaShape shape, int radius = 0, int length = 0, int width = 0)
    {
        Shape = shape;
        Radius = radius;
        Length = length;
        Width = width;
    }
}
