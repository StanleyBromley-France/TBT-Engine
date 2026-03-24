namespace Core.Game.Bootstrap.Builders.Map;

using Core.Game.Bootstrap.Builders.Map.Results;
using Core.Game.Bootstrap.Contracts;

public interface IMapBuilder
{
    MapBuildResult Build(IMapSpec mapSpec, MapBuildOptions options);
}
