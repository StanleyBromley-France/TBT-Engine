namespace Core.Game.Bootstrap.Builders.Map;

using Core.Game.Bootstrap.Contracts;
using Core.Game.Bootstrap.Creation.Map.Results;

public interface IMapBuilder
{
    MapBuildResult Build(IMapSpec mapSpec, MapBuildOptions options);
}
