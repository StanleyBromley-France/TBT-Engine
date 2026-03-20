namespace Core.Game.Bootstrap.Creation.Map.Results;

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
