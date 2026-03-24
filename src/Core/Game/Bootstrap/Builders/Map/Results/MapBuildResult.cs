namespace Core.Game.Bootstrap.Builders.Map.Results;

using Core.Map.Grid;

public sealed class MapBuildResult
{
    public Map Map { get; }
    public int AttemptUsed { get; }

    public MapBuildResult(Map map, int attemptUsed)
    {
        Map = map ?? throw new ArgumentNullException(nameof(map));
        AttemptUsed = attemptUsed;
    }
}
