namespace Core.Game.Bootstrap.Creation.Map;

using Core.Domain.Types;

public sealed class MapBuildOptions
{
    public static MapBuildOptions Default { get; } = new();

    public int Seed { get; set; }

    public IReadOnlyList<HexCoord> RequiredWalkableCoords { get; set; } = Array.Empty<HexCoord>();

    public bool RequireAllRequiredCoordsConnected { get; set; }

    public int MaxAttempts { get; set; } = 128;
}
