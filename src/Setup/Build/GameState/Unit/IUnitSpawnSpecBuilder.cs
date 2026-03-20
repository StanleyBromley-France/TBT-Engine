namespace Setup.Build.GameState.Unit;

using Core.Domain.Repositories;
using Core.Game.Bootstrap.Contracts;
using Setup.Config;
using Setup.Validation.Primitives;

public interface IUnitSpawnSpecBuilder
{
    IReadOnlyList<IUnitSpawnSpec> Build(
        GameStateConfig gameStateConfig,
        string configPath,
        IMapSpec mapSpec,
        TemplateRegistry templateRegistry,
        ValidationCollector issues);
}
