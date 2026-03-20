namespace Setup.Build.GameState.Map;

using Setup.Build.GameState.Results;
using Setup.Config;

public interface IMapSpecBuilder
{
    MapSpecBuildResult Build(
        MapGenConfig mapGenConfig,
        string configPath);
}
