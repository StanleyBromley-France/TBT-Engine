namespace Setup.Build.GameState;

using Core.Domain.Repositories;
using Setup.Build.GameState.Results;
using Setup.Loading;
using Setup.Validation.Primitives;

public interface IGameStateSpecBuilder
{
    GameStateSpecBuildResult Build(
        ContentPack pack,
        TemplateRegistry templateRegistry,
        string gameStateId,
        ContentValidationMode mode);
}
